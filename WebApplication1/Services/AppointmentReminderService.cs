using Microsoft.EntityFrameworkCore;
using WebApplication1.Data; // Replace with your actual namespace

namespace WebApplication1.Services // Replace with your actual namespace
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly ILogger<AppointmentReminderService> _logger;
        private readonly IServiceProvider _services;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public AppointmentReminderService(
            ILogger<AppointmentReminderService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment Reminder Service is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Use your actual DbContext
                        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                        // Calculate the target date (tomorrow)
                        var targetDate = DateTime.Now.AddDays(1).Date;

                        // Find appointments for tomorrow that haven't had confirmations sent
                        var appointments = await dbContext.MED_APPOINMENT_DETAILS
                            .Where(a => a.MAD_APPOINMENT_DATE.Date == targetDate &&
                                       !a.MAD_CONFIRMATION_SENT)
                            .ToListAsync(stoppingToken);

                        _logger.LogInformation("Found {Count} appointments needing confirmation.", appointments.Count);

                        foreach (var appointment in appointments)
                        {
                            try
                            {
                                var formattedTime = appointment.MAD_ALLOCATED_TIME?.ToString(@"hh\:mm");
                                var formattedDate = appointment.MAD_APPOINMENT_DATE.ToString("yyyy-MM-dd");

                                string messageBody =
                                    $"Dear {appointment.MAD_FULL_NAME},\n\n" +
                                    $"This is a reminder for your appointment with Dr. {appointment.MAD_DOCTOR}\n" +
                                    $"on {formattedDate} at {formattedTime}.\n" +
                                    $"Please arrive 15 minutes before your scheduled time.\n\n" +
                                    $"If you need to reschedule or cancel, please contact us at 0776970808.\n\n" +
                                    $"Thank you,\n Medicare Hospital";

                                string smsApiUrl = $"https://esystems.cdl.lk/Backend/SMSGateway/api/SMS/DTSSendMessage" +
                                                $"?mobileNo={appointment.MAD_CONTACT}" +
                                                $"&message={Uri.EscapeDataString(messageBody)}";

                                var httpClient = httpClientFactory.CreateClient();
                                var response = await httpClient.GetAsync(smsApiUrl, stoppingToken);

                                if (response.IsSuccessStatusCode)
                                {
                                    appointment.MAD_CONFIRMATION_SENT = true;
                                    appointment.MAD_CONFIRMATION_SENT_DATE = DateTime.Now;
                                    await dbContext.SaveChangesAsync(stoppingToken);
                                    _logger.LogInformation("Sent confirmation to {Name}", appointment.MAD_FULL_NAME);
                                }
                                else
                                {
                                    var errorContent = await response.Content.ReadAsStringAsync();
                                    _logger.LogWarning("Failed to send SMS to {Name}. Status: {StatusCode}, Error: {Error}",
                                        appointment.MAD_FULL_NAME,
                                        response.StatusCode,
                                        errorContent);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sending confirmation to {Name}", appointment.MAD_FULL_NAME);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Appointment Reminder Service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}