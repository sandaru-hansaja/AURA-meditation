using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;


namespace webapplication3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(ApplicationDbContext context, ILogger<AppointmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        //[HttpPost]
        //public async Task<ActionResult<MED_APPOINMENT_DETAILS>> PostAppointment(MED_APPOINMENT_DETAILS appointment)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    _context.MED_APPOINMENT_DETAILS.Add(appointment);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.MAD_APPOINMENT_ID }, appointment);
        //}

        //[HttpPost]
        //public async Task<ActionResult<MED_APPOINMENT_DETAILS>> PostAppointment(MED_APPOINMENT_DETAILS appointment)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    // Step 1: Check if the same user already booked this timeslot
        //    bool alreadyBooked = await _context.MED_APPOINMENT_DETAILS.AnyAsync(a =>
        //        a.MAD_PATIENT_CODE == appointment.MAD_PATIENT_CODE &&
        //        a.MAD_APPOINMENT_DATE.Date == appointment.MAD_APPOINMENT_DATE.Date &&
        //        a.MAD_SLOT_ID == appointment.MAD_SLOT_ID &&
        //        a.MAD_DOCTOR == appointment.MAD_DOCTOR);

        //    if (alreadyBooked)
        //    {
        //        // Step 2: Rollback the earlier timeslot increment
        //        var timeslot = await _context.MED_TIMESLOT.FirstOrDefaultAsync(t => t.MT_SLOT_ID == appointment.MAD_SLOT_ID);
        //        if (timeslot != null && timeslot.MT_PATIENT_NO > 0)
        //        {
        //            timeslot.MT_PATIENT_NO -= 1;
        //            _context.MED_TIMESLOT.Update(timeslot);
        //            await _context.SaveChangesAsync();
        //        }

        //        return Conflict("You already have an appointment for this timeslot.");
        //    }

        //    // Step 3: Save appointment
        //    _context.MED_APPOINMENT_DETAILS.Add(appointment);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.MAD_APPOINMENT_ID }, appointment);
        //}

        //[HttpGet("pending-appointments")]
        //public async Task<IActionResult> GetPendingAppointments()
        //{
        //    var appointments = await _context.MED_APPOINMENT_DETAILS
        //        .Where(a => a.MAD_STATUS == "P" || a.MAD_STATUS == null)
        //        .Select(a => new
        //        {
        //            a.MAD_FULL_NAME,
        //            a.MAD_PATIENT_NO,
        //            a.MAD_DOCTOR,
        //            a.MAD_STATUS
        //        })
        //        .ToListAsync();

        //    if (appointments == null || !appointments.Any())
        //        return NotFound(new { message = "No pending appointments found." });

        //    return Ok(appointments);
        //}

        [HttpGet("pending-appointments/{date}")]
        public async Task<IActionResult> GetPendingAppointmentsByDate(DateTime date)
        {
            var appointments = await _context.MED_APPOINMENT_DETAILS
                .Where(a =>
                    (a.MAD_STATUS == "P" || a.MAD_STATUS == null) &&
                    a.MAD_APPOINMENT_DATE.Date == date.Date)
                .Select(a => new
                {
                    a.MAD_FULL_NAME,
                    a.MAD_PATIENT_NO,
                    a.MAD_DOCTOR,
                    a.MAD_STATUS,
                    a.MAD_APPOINMENT_DATE
                })
                .ToListAsync();

            if (appointments == null || !appointments.Any())
                return NotFound(new { message = "No pending appointments found for this date." });

            return Ok(appointments);
        }



        //[HttpPost]
        //public async Task<ActionResult<MED_APPOINMENT_DETAILS>> PostAppointment(MED_APPOINMENT_DETAILS appointment)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    //  Get related time slot
        //    var timeslot = await _context.MED_TIMESLOT
        //        .FirstOrDefaultAsync(t => t.MT_SLOT_ID == appointment.MAD_SLOT_ID);

        //    if (timeslot == null)
        //    {
        //        return BadRequest("Invalid time slot selected.");
        //    }

        //    //  Prevent double booking by same patient for same slot/date/doctor
        //    bool alreadyBooked = await _context.MED_APPOINMENT_DETAILS.AnyAsync(a =>
        //        a.MAD_PATIENT_CODE == appointment.MAD_PATIENT_CODE &&
        //        a.MAD_APPOINMENT_DATE.Date == appointment.MAD_APPOINMENT_DATE.Date &&
        //        a.MAD_SLOT_ID == appointment.MAD_SLOT_ID &&
        //        a.MAD_DOCTOR == appointment.MAD_DOCTOR);

        //    if (alreadyBooked)
        //    {
        //        return Conflict("You already have an appointment for this timeslot.");
        //    }

        //    //  Assign real allocated time from time slot
        //    appointment.MAD_ALLOCATED_TIME = timeslot.MT_ALLOCATED_TIME;

        //    //  If you want to keep appointment start and end within slot window
        //    appointment.MAD_START_TIME = timeslot.MT_START_TIME;
        //    appointment.MAD_END_TIME = timeslot.MT_END_TIME;

        //    // 🔹 Save appointment
        //    _context.MED_APPOINMENT_DETAILS.Add(appointment);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetAppointmentById),
        //        new { id = appointment.MAD_APPOINMENT_ID }, appointment);
        //}


        [HttpPost]
        public async Task<ActionResult<MED_APPOINMENT_DETAILS>> PostAppointment(MED_APPOINMENT_DETAILS appointment)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using (var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
            {
                var timeslot = await _context.MED_TIMESLOT
                    .FirstOrDefaultAsync(t => t.MT_SLOT_ID == appointment.MAD_SLOT_ID);

                if (timeslot == null)
                    return BadRequest("Invalid time slot selected.");

                // Prevent double booking
                bool alreadyBooked = await _context.MED_APPOINMENT_DETAILS.AnyAsync(a =>
                    a.MAD_PATIENT_CODE == appointment.MAD_PATIENT_CODE &&
                    a.MAD_APPOINMENT_DATE.Date == appointment.MAD_APPOINMENT_DATE.Date &&
                    a.MAD_SLOT_ID == appointment.MAD_SLOT_ID &&
                    a.MAD_DOCTOR == appointment.MAD_DOCTOR);

                if (alreadyBooked)
                    return Conflict("You already have an appointment for this timeslot.");

                // Current local time
                var systemTime = DateTime.Now.TimeOfDay;

                // Determine allocated time
                TimeSpan newAllocatedTime;
                if (timeslot.MT_PATIENT_NO == 0)
                {
                    // First appointment: use whichever is later - slot start or current time
                    newAllocatedTime = systemTime > timeslot.MT_START_TIME
                        ? systemTime
                        : timeslot.MT_START_TIME;
                }
                else
                {
                    // Subsequent appointment: 30 mins after last allocated
                    newAllocatedTime = timeslot.MT_ALLOCATED_TIME.HasValue
                        ? timeslot.MT_ALLOCATED_TIME.Value.Add(TimeSpan.FromMinutes(1))
                        : (systemTime > timeslot.MT_START_TIME
                            ? systemTime.Add(TimeSpan.FromMinutes(1))
                            : timeslot.MT_START_TIME.Add(TimeSpan.FromMinutes(1)));
                }

                // Prevent going beyond slot end
                if (newAllocatedTime > timeslot.MT_END_TIME)
                    return BadRequest("No available time in this slot.");

                // Update timeslot for next patient
                timeslot.MT_ALLOCATED_TIME = newAllocatedTime;
                // timeslot.MT_PATIENT_NO += 1;

                // Assign to appointment
                appointment.MAD_ALLOCATED_TIME = newAllocatedTime;
                appointment.MAD_START_TIME = timeslot.MT_START_TIME;
                appointment.MAD_END_TIME = timeslot.MT_END_TIME;

                // Save both
                _context.MED_APPOINMENT_DETAILS.Add(appointment);
                _context.Entry(timeslot).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(GetAppointmentById),
                    new { id = appointment.MAD_APPOINMENT_ID },
                    new
                    {
                        appointment,
                        message = "Appointment created successfully",
                        allocatedTime = newAllocatedTime.ToString(@"hh\:mm")
                    });
            }
        }

        //[HttpPost]
        //public async Task<ActionResult<MED_APPOINMENT_DETAILS>> PostAppointment(MED_APPOINMENT_DETAILS appointment)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    using (var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
        //    {
        //        var timeslot = await _context.MED_TIMESLOT
        //            .FirstOrDefaultAsync(t => t.MT_SLOT_ID == appointment.MAD_SLOT_ID);

        //        if (timeslot == null)
        //            return BadRequest("Invalid time slot selected.");

        //        // Prevent double booking
        //        bool alreadyBooked = await _context.MED_APPOINMENT_DETAILS.AnyAsync(a =>
        //            a.MAD_PATIENT_CODE == appointment.MAD_PATIENT_CODE &&
        //            a.MAD_APPOINMENT_DATE.Date == appointment.MAD_APPOINMENT_DATE.Date &&
        //            a.MAD_SLOT_ID == appointment.MAD_SLOT_ID &&
        //            a.MAD_DOCTOR == appointment.MAD_DOCTOR
        //        );

        //        if (alreadyBooked)
        //            return Conflict("You already have an appointment for this timeslot.");

        //        // Current system time
        //        TimeSpan now = DateTime.Now.TimeOfDay;

        //        // Determine allocated time (NO 30-MIN ADD)
        //        TimeSpan newAllocatedTime;

        //        if (timeslot.MT_PATIENT_NO == 0)
        //        {
        //            // First patient → take current time if after slot start
        //            newAllocatedTime = now > timeslot.MT_START_TIME
        //                ? now
        //                : timeslot.MT_START_TIME;
        //        }
        //        else
        //        {
        //            // Subsequent patients → KEEP allocated time (NO INCREMENT)
        //            if (timeslot.MT_ALLOCATED_TIME.HasValue)
        //                newAllocatedTime = timeslot.MT_ALLOCATED_TIME.Value;
        //            else
        //                newAllocatedTime = timeslot.MT_START_TIME;
        //        }

        //        // Prevent going beyond slot end
        //        if (newAllocatedTime > timeslot.MT_END_TIME)
        //            return BadRequest("No available time in this slot.");

        //        // Update timeslot (NO 30 MIN, NO ADD)
        //        timeslot.MT_ALLOCATED_TIME = newAllocatedTime;
        //        // timeslot.MT_PATIENT_NO not incremented unless you want patient numbering

        //        // Assign appointment values
        //        appointment.MAD_ALLOCATED_TIME = newAllocatedTime;
        //        appointment.MAD_START_TIME = timeslot.MT_START_TIME;
        //        appointment.MAD_END_TIME = timeslot.MT_END_TIME;

        //        // Save
        //        _context.MED_APPOINMENT_DETAILS.Add(appointment);
        //        _context.Entry(timeslot).State = EntityState.Modified;

        //        await _context.SaveChangesAsync();
        //        await tx.CommitAsync();

        //        return CreatedAtAction(nameof(GetAppointmentById),
        //            new { id = appointment.MAD_APPOINMENT_ID },
        //            new
        //            {
        //                appointment,
        //                message = "Appointment created successfully",
        //                allocatedTime = newAllocatedTime.ToString(@"hh\\:mm")
        //            });
        //    }
        //}




        [HttpGet("{id}")]
        public async Task<ActionResult<MED_APPOINMENT_DETAILS>> GetAppointmentById(int id)
        {
            var appointment = await _context.MED_APPOINMENT_DETAILS.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            return appointment;
        }


        [HttpGet("getappoinments/doctor")]
        public async Task<ActionResult<IEnumerable<object>>> GetAppointmentsByDoctorAndDate(string doctor, DateTime date)
        {
            var appointments = await (from appointment in _context.MED_APPOINMENT_DETAILS
                                      join timeslot in _context.MED_TIMESLOT
                                      on appointment.MAD_SLOT_ID equals timeslot.MT_SLOT_ID into ts
                                      from timeslot in ts.DefaultIfEmpty() // Left join to include null if no timeslot is found
                                      where appointment.MAD_DOCTOR == doctor && appointment.MAD_APPOINMENT_DATE.Date == date.Date
                                      select new
                                      {
                                          appointment.MAD_APPOINMENT_ID,
                                          appointment.MAD_FULL_NAME,
                                          appointment.MAD_CONTACT,
                                          appointment.MAD_PATIENT_NO,
                                          appointment.MAD_ALLOCATED_TIME,
                                          appointment.MAD_PATIENT_CODE,
                                          appointment.MAD_APPOINMENT_DATE,
                                          appointment.MAD_SLOT_ID, // Include slot ID from appointment
                                          TimeslotStartTime = timeslot != null ? timeslot.MT_START_TIME : (TimeSpan?)null, // Include timeslot details
                                          TimeslotEndTime = timeslot != null ? timeslot.MT_END_TIME : (TimeSpan?)null,
                                          TimeslotDate = timeslot != null ? timeslot.MT_SLOT_DATE : (DateTime?)null,
                                          TreatmentStatus = _context.MED_TREATMENT_DETAILS
                                              .Any(t => t.MTD_APPOINMENT_ID == appointment.MAD_APPOINMENT_ID) // Check if the appointment has treatment
                                      })
                                      .ToListAsync();

            if (appointments == null || !appointments.Any())
            {
                return NotFound("No appointments found for the selected date.");
            }

            return Ok(appointments);
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<MED_APPOINMENT_DETAILS>>> GetAllAppointments()
        {
            var appointments = await _context.MED_APPOINMENT_DETAILS
                .OrderBy(a => a.MAD_APPOINMENT_DATE)
                .ThenBy(a => a.MAD_PATIENT_NO)
                .ToListAsync();

            if (appointments == null || !appointments.Any())
            {
                return NotFound("No appointments found.");
            }

            return Ok(appointments);
        }


        //Get both active and inactive appointments

        //[HttpGet("appointments/{slotId}")]
        //public async Task<IActionResult> GetAppointmentsByTimeslot(int slotId)
        //{
        //    var appointments = await _context.MED_APPOINMENT_DETAILS
        //        .Where(a => a.MAD_SLOT_ID == slotId)
        //        .Select(a => new
        //        {
        //            a.MAD_APPOINMENT_ID,
        //            a.MAD_FULL_NAME,
        //            a.MAD_CONTACT,
        //            a.MAD_PATIENT_NO,
        //            a.MAD_ALLOCATED_TIME,
        //            a.MAD_PATIENT_CODE,
        //            a.MAD_APPOINMENT_DATE,
        //            a.MAD_STATUS,
        //            IsCompleted = _context.MED_TREATMENT_DETAILS
        //                .Any(t => t.MTD_APPOINMENT_ID == a.MAD_APPOINMENT_ID)
        //        })
        //        .ToListAsync();

        //    if (appointments == null || appointments.Count == 0)
        //    {
        //        return NotFound("No appointments found for the selected timeslot.");
        //    }

        //    return Ok(appointments);
        //}
        [HttpGet("appointments/{slotId}")]
        public async Task<IActionResult> GetAppointmentsByTimeslot(int slotId)
        {
            var appointments = await _context.MED_APPOINMENT_DETAILS
                .Where(a => a.MAD_SLOT_ID == slotId)
                .Select(a => new
                {
                    a.MAD_APPOINMENT_ID,
                    a.MAD_FULL_NAME,
                    a.MAD_CONTACT,
                    a.MAD_PATIENT_NO,
                    a.MAD_ALLOCATED_TIME,
                    a.MAD_PATIENT_CODE,
                    a.MAD_APPOINMENT_DATE,
                    a.MAD_STATUS,
                    a.MAD_PAID_STATUS,
                    TreatmentStatus = _context.MED_TREATMENT_DETAILS
                        .Where(t => t.MTD_APPOINMENT_ID == a.MAD_APPOINMENT_ID)
                        .Select(t => t.MTD_TREATMENT_STATUS)
                        .FirstOrDefault()
                })
                .ToListAsync();

            if (appointments == null || appointments.Count == 0)
            {
                return NotFound("No appointments found for the selected timeslot.");
            }

            return Ok(appointments);
        }


        //[HttpGet("tomorrows-appointments")]
        //public async Task<IActionResult> GetTomorrowsAppointments()
        //{
        //    var tomorrow = DateTime.Today.AddDays(1);

        //    var appointments = await _context.MED_APPOINMENT_DETAILS
        //        .Where(a => a.MAD_APPOINMENT_DATE.Date == tomorrow.Date)
        //        .Select(a => new
        //        {
        //            a.MAD_APPOINMENT_ID,
        //            a.MAD_FULL_NAME,
        //            a.MAD_CONTACT,
        //            a.MAD_PATIENT_NO,
        //            a.MAD_ALLOCATED_TIME,
        //            a.MAD_PATIENT_CODE,
        //            a.MAD_APPOINMENT_DATE,
        //            a.MAD_DOCTOR
        //        })
        //        .ToListAsync();

        //    return Ok(appointments);
        //}


        //Only active active will be showed
        [HttpGet("tomorrows-appointments")]
        public async Task<IActionResult> GetTomorrowsAppointments()
        {
            var tomorrow = DateTime.Today.AddDays(1);

            var appointments = await _context.MED_APPOINMENT_DETAILS
                .Where(a => a.MAD_APPOINMENT_DATE.Date == tomorrow.Date
                            && (a.MAD_STATUS == null || a.MAD_STATUS == "A"))
                .Select(a => new
                {
                    a.MAD_APPOINMENT_ID,
                    a.MAD_FULL_NAME,
                    a.MAD_CONTACT,
                    a.MAD_PATIENT_NO,
                    a.MAD_ALLOCATED_TIME,
                    a.MAD_PATIENT_CODE,
                    a.MAD_APPOINMENT_DATE,
                    a.MAD_DOCTOR,
                    a.MAD_STATUS
                })
                .ToListAsync();

            return Ok(appointments);
        }

        //[HttpPost("send-appointment-confirmation/{appointmentId}")]
        //public async Task<IActionResult> SendAppointmentConfirmation(int appointmentId)
        //{
        //    var appointment = await _context.MED_APPOINMENT_DETAILS
        //        .FirstOrDefaultAsync(a => a.MAD_APPOINMENT_ID == appointmentId);

        //    if (appointment == null)
        //        return NotFound("Appointment not found.");

        //    var timeSlot = await _context.MED_TIMESLOT
        //        .FirstOrDefaultAsync(ts => ts.MT_SLOT_ID == appointment.MAD_SLOT_ID);

        //    if (timeSlot == null)
        //        return NotFound("Time slot not found.");

        //    int displayAppointmentId = timeSlot.MT_PATIENT_NO;
        //    var formattedTime = appointment.MAD_ALLOCATED_TIME?.ToString(@"hh\\:mm") ?? "N/A";
        //    var formattedDate = appointment.MAD_APPOINMENT_DATE.ToString("yyyy-MM-dd");

        //    string messageBody =
        //        $"Dear {appointment.MAD_FULL_NAME},\n\n" +
        //        $"This is a reminder for your appointment with Dr. {appointment.MAD_DOCTOR}\n" +
        //        $"Appointment ID: {displayAppointmentId}\n" +
        //        $"on {formattedDate} at {formattedTime}.\n" +
        //        $"Please arrive 15 minutes early.\n\n" +
        //        $"If you need to reschedule or cancel, contact us at 07777100546.\n\n" +
        //        $"Thank you,\nAURA MEDICATION";

        //    // 🔹 Validate and normalize contact number
        //    string rawContact = appointment.MAD_CONTACT?.Trim() ?? string.Empty;

        //    if (string.IsNullOrEmpty(rawContact))
        //        return BadRequest("Contact number is missing.");

        //    string contact;

        //    // Accept both local (07XXXXXXXX) and international (+94XXXXXXXXX) formats
        //    if (Regex.IsMatch(rawContact, @"^0\d{9}$"))
        //    {
        //        // Keep as-is (07XXXXXXXX) for SMS Gateway compatibility
        //        contact = rawContact;
        //    }
        //    else if (Regex.IsMatch(rawContact, @"^\+94\d{9}$"))
        //    {
        //        // Convert +94 to 0 (since gateway expects 07XXXXXXXX)
        //        contact = "0" + rawContact.Substring(3);
        //    }
        //    else
        //    {
        //        return BadRequest("Invalid contact number format. Use 07XXXXXXXX or +94XXXXXXXXX.");
        //    }

        //    // 🔹 eSystems SMS Gateway Configuration (DO NOT ENCODE '+' or digits)
        //    string smsApiUrl = $"https://esystems.cdl.lk/Backend/SMSGateway/api/SMS/DTSSendMessage" +
        //                       $"?mobileNo={contact}" +
        //                       $"&message={Uri.EscapeDataString(messageBody)}";

        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            var response = await httpClient.GetAsync(smsApiUrl);

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                var errorContent = await response.Content.ReadAsStringAsync();
        //                _logger.LogWarning($"❌ SMS API Error: {response.StatusCode}, Details: {errorContent}");

        //                return StatusCode((int)response.StatusCode, new
        //                {
        //                    message = "Failed to send confirmation message.",
        //                    error = errorContent
        //                });
        //            }
        //        }

        //        _logger.LogInformation($"✅ SMS sent successfully to {contact}");
        //        return Ok(new { message = "Confirmation SMS sent successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"🚨 Exception when sending SMS: {ex.Message}");
        //        return StatusCode(500, new { message = "Error sending SMS.", error = ex.Message });
        //    }
        //}




        //[HttpPost("send-appointment-confirmation/{appointmentId}")]
        //public async Task<IActionResult> SendAppointmentConfirmation(int appointmentId)
        //{
        //    var appointment = await _context.MED_APPOINMENT_DETAILS
        //        .FirstOrDefaultAsync(a => a.MAD_APPOINMENT_ID == appointmentId);

        //    if (appointment == null)
        //    {
        //        return NotFound("Appointment not found.");
        //    }

        //    var formattedTime = appointment.MAD_ALLOCATED_TIME?.ToString(@"hh\:mm");
        //    var formattedDate = appointment.MAD_APPOINMENT_DATE.ToString("yyyy-MM-dd");

        //    string messageBody =
        //        $"Dear {appointment.MAD_FULL_NAME},\n\n" +
        //        $"This is a reminder for your appointment with Dr. {appointment.MAD_DOCTOR}\n" +
        //        $"This is your appointment ID: {appointment.MAD_APPOINMENT_ID}\n" +
        //        $"on {formattedDate} at {formattedTime}.\n" +
        //        $"Please arrive 15 minutes before your scheduled time.\n\n" +
        //        $"If you need to reschedule or cancel, please contact us at 07777100546.\n\n" +
        //        $"Thank you,\n AURA MEDICATION ";

        //    string smsApiUrl = $"https://esystems.cdl.lk/Backend/SMSGateway/api/SMS/DTSSendMessage" +
        //                       $"?mobileNo={appointment.MAD_CONTACT}" +
        //                       $"&message={Uri.EscapeDataString(messageBody)}";

        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            var response = await httpClient.GetAsync(smsApiUrl);

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                var errorContent = await response.Content.ReadAsStringAsync();
        //                _logger.LogWarning($"SMS API returned non-success status code: {response.StatusCode}, Error: {errorContent}");
        //                return StatusCode((int)response.StatusCode, new
        //                {
        //                    message = "Failed to send confirmation message.",
        //                    error = errorContent
        //                });
        //            }
        //        }

        //        return Ok(new { message = "Confirmation message sent successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Exception when sending SMS: {ex.Message}");
        //        return StatusCode(500, new { message = "Error sending confirmation message.", error = ex.Message });
        //    }
        //}





        //[HttpPost("send-appointment-confirmation/{appointmentId}")]
        //public async Task<IActionResult> SendAppointmentConfirmation(int appointmentId)
        //{
        //    var appointment = await _context.MED_APPOINMENT_DETAILS
        //        .FirstOrDefaultAsync(a => a.MAD_APPOINMENT_ID == appointmentId);

        //    if (appointment == null)
        //    {
        //        return NotFound("Appointment not found.");
        //    }

        //    // 🔹 Format TimeSpan safely (hh:mm format)
        //    var formattedTime = appointment.MAD_ALLOCATED_TIME.HasValue
        //        ? appointment.MAD_ALLOCATED_TIME.Value.ToString(@"hh\:mm")
        //        : "N/A";

        //    // 🔹 Format Date
        //    var formattedDate = appointment.MAD_APPOINMENT_DATE.ToString("yyyy-MM-dd");

        //    string messageBody =
        //        $"Dear {appointment.MAD_FULL_NAME},\n\n" +
        //        $"This is a reminder for your appointment with Dr. {appointment.MAD_DOCTOR}\n" +
        //        $"Appointment ID: {appointment.MAD_APPOINMENT_ID}\n" +
        //        $"on {formattedDate} at {formattedTime}.\n" +
        //        $"Please arrive 15 minutes early.\n\n" +
        //        $"If you need to reschedule or cancel, contact us at 07777100546.\n\n" +
        //        $"Thank you,\nAURA MEDICATION";

        //    // 🔹 SMSLenz API credentials (store in appsettings.json ideally)
        //    string userId = "719";
        //    string apiKey = "02c00d54-f83b-4dcc-ac12-d6415a158371";
        //    string senderId = "SMSlenzDEMO"; // Replace with approved sender
        //    string contact = $"+94{appointment.MAD_CONTACT?.TrimStart('0')}"; // format to +94

        //    string smsApiUrl = $"https://smslenz.lk/api/send-sms" +
        //                       $"?user_id={Uri.EscapeDataString(userId)}" +
        //                       $"&api_key={Uri.EscapeDataString(apiKey)}" +
        //                       $"&sender_id={Uri.EscapeDataString(senderId)}" +
        //                       $"&contact={Uri.EscapeDataString(contact)}" +
        //                       $"&message={Uri.EscapeDataString(messageBody)}";

        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            var response = await httpClient.GetAsync(smsApiUrl);

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                var errorContent = await response.Content.ReadAsStringAsync();
        //                _logger.LogWarning($"SMS API returned error: {response.StatusCode}, {errorContent}");
        //                return StatusCode((int)response.StatusCode, new
        //                {
        //                    message = "Failed to send confirmation message.",
        //                    error = errorContent
        //                });
        //            }
        //        }

        //        return Ok(new { message = "Confirmation SMS sent successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Exception when sending SMS: {ex.Message}");
        //        return StatusCode(500, new { message = "Error sending SMS.", error = ex.Message });
        //    }
        //}



        [HttpPost("send-appointment-confirmation/{appointmentId}")]
        public async Task<IActionResult> SendAppointmentConfirmation(int appointmentId)
        {
            var appointment = await _context.MED_APPOINMENT_DETAILS
                .FirstOrDefaultAsync(a => a.MAD_APPOINMENT_ID == appointmentId);

            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            // 🔹 Get Patient Count from Time Slot table


            var timeSlot = await _context.MED_TIMESLOT
        .FirstOrDefaultAsync(ts => ts.MT_SLOT_ID == appointment.MAD_SLOT_ID);
            if (timeSlot == null)
            {
                return NotFound("Time slot not found.");
            }

            // use PATIENT_COUNT instead of appointment.MAD_APPOINMENT_ID
            int displayAppointmentId = timeSlot.MT_PATIENT_NO;
            //int displayAppointmentId = timeSlot.MT_PATIENT_NO + 1;


            // 🔹 Format TimeSpan safely (hh:mm format)
            var formattedTime = appointment.MAD_ALLOCATED_TIME.HasValue
                ? appointment.MAD_ALLOCATED_TIME.Value.ToString(@"hh\:mm")
                : "N/A";

            // 🔹 Format Date
            var formattedDate = appointment.MAD_APPOINMENT_DATE.ToString("yyyy-MM-dd");

            string messageBody =
                $"Dear {appointment.MAD_FULL_NAME},\n\n" +
                //$"This is a reminder for your appointment with {appointment.MAD_DOCTOR}\n" +
                $"This is a reminder for your appointment with Deshamanya Lakmal Perera\n" +
                $"Appointment ID: {displayAppointmentId}\n" +
               // $"on {formattedDate} at {formattedTime}.\n" +
                $"on {formattedDate} \n" +
                //$"Please arrive 15 minutes early.\n\n" + open time's from 2pm  to 9pm. only 30min Sessions are available. 
                $"If you need to reschedule or cancel, contact us at 0114532108 | 0117905805.\n\n" +
                $"Working hours are from 2pm to 9pm. Galle Branch - Sunday From 8:30am to 2:00pm. \n" +
                $"Please note that we currently offer 30-minute sessions only. Thank you for your understanding.. \n\n" +
                $"Thank you,\nAURA MEDITATION";


            string userId = "850 ";
            string apiKey = "c1676e83-bffa-4899-aa47-88394fb72ab2";
            string senderId = "Aura Medi";
            string contact = $"+94{appointment.MAD_CONTACT?.TrimStart('0')}";

            string smsApiUrl = $"https://smslenz.lk/api/send-sms" +
                               $"?user_id={Uri.EscapeDataString(userId)}" +
                               $"&api_key={Uri.EscapeDataString(apiKey)}" +
                               $"&sender_id={Uri.EscapeDataString(senderId)}" +
                               $"&contact={Uri.EscapeDataString(contact)}" +
                               $"&message={Uri.EscapeDataString(messageBody)}";

            //string smsApiUrl = $"https://esystems.cdl.lk/Backend/SMSGateway/api/SMS/DTSSendMessage" +
            //                $"?mobileNo={appointment.MAD_CONTACT}" +
            //                  $"&message={Uri.EscapeDataString(messageBody)}";


            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(smsApiUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"SMS API returned error: {response.StatusCode}, {errorContent}");
                        return StatusCode((int)response.StatusCode, new
                        {
                            message = "Failed to send confirmation message.",
                            error = errorContent
                        });
                    }
                }

                return Ok(new { message = "Confirmation SMS sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception when sending SMS: {ex.Message}");
                return StatusCode(500, new { message = "Error sending SMS.", error = ex.Message });
            }
        }



        //[HttpDelete("cancel-appointment/{id}")]
        //public async Task<IActionResult> CancelAppointment(int id)
        //{
        //    var appointment = await _context.MED_APPOINMENT_DETAILS.FindAsync(id);

        //    if (appointment == null)
        //    {
        //        return NotFound();
        //    }

        //    appointment.MAD_STATUS = "I";

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //        return NoContent();
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}


        [HttpDelete("cancel-appointment/{id}")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.MED_APPOINMENT_DETAILS.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.MAD_STATUS = "I";

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Appointment cancelled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        [HttpGet("Test/{id}")]
        public async Task<IActionResult> Fetchbyid(int id)
        {


            var appoinments = await _context.MED_APPOINMENT_DETAILS
                .Where(a => a.MAD_APPOINMENT_ID == id)
                .FirstOrDefaultAsync();

            if (appoinments == null)
            {
                return NotFound("No appointments found for the selected timeslot.");
            }


            return Ok(appoinments);
        }

        [HttpGet("getappointment/patientcode")]
        public async Task<ActionResult<IEnumerable<object>>> DetailsById(string patientcode)
        {
            if (string.IsNullOrWhiteSpace(patientcode))
            {
                return BadRequest("Patient code cannot be null or empty.");
            }

            var appointments = await _context.MED_APPOINMENT_DETAILS
                               .Where(a => a.MAD_PATIENT_CODE == patientcode)
                               .Select(a => new
                               {
                                   a.MAD_APPOINMENT_ID,
                                   a.MAD_DOCTOR,
                                   a.MAD_APPOINMENT_DATE,
                                   a.MAD_START_TIME,
                                   a.MAD_END_TIME,
                                   a.MAD_ALLOCATED_TIME,
                                   TreatmentStatus = _context.MED_TREATMENT_DETAILS.Any(t => t.MTD_APPOINMENT_ID == a.MAD_APPOINMENT_ID) ? "Completed" : "Pending"
                               })
                               .ToListAsync();

            if (appointments == null || !appointments.Any())
            {
                return NotFound("No appointments found for the given patient code.");
            }

            return Ok(appointments);
        }

        [HttpPut("status/{appointmentID}")]
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentID, [FromBody] string newStatus)
        {
            // Find the appointment by ID
            var appointment = await _context.MED_APPOINMENT_DETAILS
                .FirstOrDefaultAsync(a => a.MAD_APPOINMENT_ID == appointmentID);

            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found." });
            }

            // Update only the status
            appointment.MAD_STATUS = newStatus;
            // appointment.MAD_UPDATED_DATE = DateTime.Now; 

            _context.MED_APPOINMENT_DETAILS.Update(appointment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                AppointmentId = appointment.MAD_APPOINMENT_ID,
                UpdatedStatus = appointment.MAD_STATUS
            });
        }


        [HttpGet("getappointment/email")]
        public async Task<ActionResult<IEnumerable<object>>> getappointmentemail(string email)
        {
            var appointments = await _context.MED_APPOINMENT_DETAILS
                .Where(a => a.MAD_EMAIL == email)
                .Select(a => new
                {
                    a.MAD_APPOINMENT_ID,
                    a.MAD_DOCTOR,
                    a.MAD_APPOINMENT_DATE,
                    a.MAD_START_TIME,
                    a.MAD_END_TIME,
                    a.MAD_ALLOCATED_TIME,
                    TreatmentStatus = _context.MED_TREATMENT_DETAILS.Any(t => t.MTD_APPOINMENT_ID == a.MAD_APPOINMENT_ID) ? "Completed" : "Pending"
                })
                .ToListAsync();

            if (appointments == null || !appointments.Any())
            {
                return NotFound("No appointments found");
            }

            return Ok(appointments);
        }

    }
}
