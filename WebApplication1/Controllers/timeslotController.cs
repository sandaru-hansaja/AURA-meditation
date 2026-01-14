using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeslotController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TimeslotController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Timeslot/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MED_TIMESLOT>> GetTimeslot(int id)
        {
            var timeslot = await _context.MED_TIMESLOT.FindAsync(id);

            if (timeslot == null)
            {
                return NotFound();
            }

            return Ok(timeslot);
        }

        // GET: api/Timeslot
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetAllTimeslots()
        {
            var timeslots = await _context.MED_TIMESLOT.ToListAsync();
            return Ok(timeslots);
        }



        [HttpGet("Doctor/{doctorName}")]
        public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetTimeslotsByDoctor(string doctorName)
        {
            var timeslots = await _context.MED_TIMESLOT
                                          .Where(t => t.MT_DOCTOR == doctorName)
                                          .OrderByDescending(t => t.MT_SLOT_DATE) // Assuming MT_DATE or similar column exists
                                          .ToListAsync();

            if (timeslots == null || !timeslots.Any())
            {
                return NotFound($"No timeslots found for doctor: {doctorName}");
            }

            return Ok(timeslots);
        }
        [HttpGet("Doctorid/{userid}")]
        public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetTimeslotsByDoctorid(string userid)
        {
            var timeslots = await _context.MED_TIMESLOT
                                          .Where(t => t.MT_USER_ID == userid && t.MT_TIMESLOT_STATUS != "I")
                                          /*.OrderByDescending(t => t.MT_SLOT_DATE)*/ // Assuming MT_DATE or similar column exists
                                          .ToListAsync();

            if (timeslots == null || !timeslots.Any())
            {
                return NotFound($"No timeslots found for doctor: ");
            }

            return Ok(timeslots);
        }


        [HttpGet("active-timeslots")]
        public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetActiveTimeslots()
        {
            var timeslots = await _context.MED_TIMESLOT
                .Where(t => t.MT_TIMESLOT_STATUS != "I")
                .ToListAsync();

            return Ok(timeslots);
        }


        [HttpGet("active-timeslots-bydates")]
        public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetActiveTimeslotsByDate([FromQuery] DateTime? date)
        {
            var query = _context.MED_TIMESLOT
                .Where(t => t.MT_TIMESLOT_STATUS != "I");

            //  apply date filter if provided
            if (date.HasValue)
            {
                query = query.Where(t => t.MT_SLOT_DATE == date.Value);
            }

            var timeslots = await query.ToListAsync();

            return Ok(timeslots);
        }


        [HttpGet("timeslotcard/{date}/{name}")]
        public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetTimeslotsByDate(string date, string name)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return BadRequest("Invalid date format.");
            }
            var timeslots = await _context.MED_TIMESLOT
                                           .Where(t => t.MT_SLOT_DATE.Date == parsedDate.Date && t.MT_DOCTOR == name)
                                           .ToListAsync();

            if (timeslots == null || !timeslots.Any())
            {
                return NotFound("No timeslots available for the selected date.");
            }

            return Ok(timeslots);
        }
        //[HttpGet("timeslotcard/{date}")]
        //public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetTimeslotsByDate(string date)
        //{
        //    // Parse the input date
        //    if (!DateTime.TryParse(date, out DateTime parsedDate))
        //    {
        //        return BadRequest("Invalid date format.");
        //    }

        //    // Query full timeslot details for the given date
        //    var timeslots = await _context.MED_TIMESLOT
        //                                  .Where(t => t.MT_SLOT_DATE.Date == parsedDate.Date)
        //                                  .ToListAsync();

        //    if (!timeslots.Any())
        //    {
        //        return NotFound("No timeslots available for the selected date.");
        //    }

        //    return Ok(timeslots);
        //}


        [HttpGet("timeslotcard/{date}")]
        public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetTimeslotsByDate(string date)
        {
            // Parse the input date
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            // Query only active, non-deleted timeslots for the given date
            var timeslots = await _context.MED_TIMESLOT
                .Where(t => t.MT_SLOT_DATE.Date == parsedDate.Date &&
                            t.MT_TIMESLOT_STATUS == "A" &&   // Only active slots
                            (t.MT_DELETE_STATUS == null)) // Not deleted
                .ToListAsync();

            if (!timeslots.Any())
            {
                return NotFound("No active timeslots available for the selected date.");
            }

            return Ok(timeslots);
        }


        //[HttpGet("timeslotcard/{date}/{name?}")]
        //public async Task<ActionResult<IEnumerable<MED_TIMESLOT>>> GetTimeslotsByDate(string date, string name = null)
        //{
        //    if (!DateTime.TryParse(date, out DateTime parsedDate))
        //    {
        //        return BadRequest("Invalid date format.");
        //    }

        //    var query = _context.MED_TIMESLOT
        //                      .Where(t => t.MT_SLOT_DATE.Date == parsedDate.Date);

        //    if (!string.IsNullOrEmpty(name))
        //    {
        //        query = query.Where(t => t.MT_DOCTOR == name);
        //    }

        //    var timeslots = await query.ToListAsync();

        //    if (timeslots == null || !timeslots.Any())
        //    {
        //        return NotFound("No timeslots available for the selected date.");
        //    }

        //    return Ok(timeslots);
        //}



        // POST: api/Timeslot
        [HttpPost]
        public async Task<ActionResult<MED_TIMESLOT>> PostTimeslot(MED_TIMESLOT timeslot)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.MED_TIMESLOT.Add(timeslot);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTimeslot), new { id = timeslot.MT_SLOT_ID }, timeslot);
        }



        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTimeslot(int id)
        {
            var timeslot = await _context.MED_TIMESLOT.FindAsync(id);
            if (timeslot == null)
            {
                return NotFound();
            }
            _context.MED_TIMESLOT.Remove(timeslot);
            await _context.SaveChangesAsync();
            return NoContent();
        }



        [HttpPut("update-status/{id}")]
        public async Task<ActionResult> UpdateTime(int id)
        {
            var timeslot = await _context.MED_TIMESLOT.FindAsync(id);

            if (timeslot == null)
            {
                return NotFound("Timeslot not found.");
            }

            timeslot.MT_TIMESLOT_STATUS = "I";
            timeslot.MT_DELETE_STATUS = "y";

            await _context.SaveChangesAsync();

            return Ok("Timeslot status updated successfully.");
        }



        //[HttpPost("incrementSeat")]
        //public async Task<IActionResult> IncrementSeat([FromQuery] int id)
        //{
        //    var timeslot = await _context.MED_TIMESLOT.FindAsync(id);

        //    if (timeslot == null)
        //        return NotFound();

        //    if (DateTime.Today > timeslot.MT_SLOT_DATE.Date)
        //        return BadRequest("The timeslot date has passed.");

        //    if (timeslot.MT_MAXIMUM_PATIENTS.HasValue &&
        //        timeslot.MT_PATIENT_NO >= timeslot.MT_MAXIMUM_PATIENTS)
        //        return BadRequest("No more seats available.");

        //    // 🔹 Determine current time (for today’s date)
        //    var currentTime = DateTime.Now.TimeOfDay;

        //    // 🔹 If no appointments yet and current time is AFTER slot start, start from now
        //    if (timeslot.MT_PATIENT_NO == 0)
        //    {
        //        if (currentTime > timeslot.MT_START_TIME && timeslot.MT_SLOT_DATE.Date == DateTime.Today)
        //        {
        //            timeslot.MT_ALLOCATED_TIME = currentTime;
        //        }
        //        else
        //        {
        //            timeslot.MT_ALLOCATED_TIME = timeslot.MT_START_TIME;
        //        }
        //    }
        //    else
        //    {
        //        //  Increment by  minutes for each new patient
        //        timeslot.MT_ALLOCATED_TIME += TimeSpan.FromMinutes(1);
        //    }

        //    //  Increment seat count
        //    timeslot.MT_PATIENT_NO += 1;

        //    _context.Entry(timeslot).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!TimeslotExists(id))
        //            return NotFound();
        //        else
        //            throw;
        //    }

        //    return Ok(new
        //    {
        //        message = "Seat incremented successfully",
        //        timeslotId = timeslot.MT_SLOT_ID,
        //        currentPatients = timeslot.MT_PATIENT_NO,
        //        allocatedTime = timeslot.MT_ALLOCATED_TIME.HasValue
        //        ? timeslot.MT_ALLOCATED_TIME.Value.ToString(@"hh\:mm")
        //        : "N/A",

        //        slotDate = timeslot.MT_SLOT_DATE
        //    });
        //}


        [HttpPost("incrementSeat")]
        public async Task<IActionResult> IncrementSeat([FromQuery] int id)
        {
            var timeslot = await _context.MED_TIMESLOT.FindAsync(id);

            if (timeslot == null)
                return NotFound("Timeslot not found.");

            if (DateTime.Today > timeslot.MT_SLOT_DATE.Date)
                return BadRequest("The timeslot date has passed.");

            if (timeslot.MT_MAXIMUM_PATIENTS.HasValue &&
                timeslot.MT_PATIENT_NO >= timeslot.MT_MAXIMUM_PATIENTS)
                return BadRequest("No more seats available.");

            // Get current time
            TimeSpan now = DateTime.Now.TimeOfDay;

            // ----- FIRST PATIENT LOGIC -----
            if (timeslot.MT_PATIENT_NO == 0)
            {
                if (timeslot.MT_SLOT_DATE.Date == DateTime.Today && now > timeslot.MT_START_TIME)
                {
                    // First patient today AND current time is after slot start
                    timeslot.MT_ALLOCATED_TIME = now;
                }
                else
                {
                    // Before today or before slot start → default to slot start time
                    timeslot.MT_ALLOCATED_TIME = timeslot.MT_START_TIME;
                }
            }
            else
            {
                // ----- SUBSEQUENT PATIENTS -----
                if (!timeslot.MT_ALLOCATED_TIME.HasValue)
                    timeslot.MT_ALLOCATED_TIME = timeslot.MT_START_TIME;

                timeslot.MT_ALLOCATED_TIME = timeslot.MT_ALLOCATED_TIME.Value.Add(TimeSpan.FromMinutes(1));
            }

            // Ensure allocated time does NOT exceed slot end
            if (timeslot.MT_ALLOCATED_TIME > timeslot.MT_END_TIME)
                return BadRequest("Allocated time exceeds timeslot end time. No seats left.");

            // Increment patient count
            timeslot.MT_PATIENT_NO++;

            _context.Entry(timeslot).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TimeslotExists(id))
                    return NotFound("Timeslot no longer exists.");
                else
                    throw;
            }

            return Ok(new
            {
                message = "Seat incremented successfully",
                timeslotId = timeslot.MT_SLOT_ID,
                currentPatients = timeslot.MT_PATIENT_NO,
                allocatedTime = timeslot.MT_ALLOCATED_TIME?.ToString(@"hh\:mm"),
                slotDate = timeslot.MT_SLOT_DATE.ToString("yyyy-MM-dd")
            });
        }


        private bool TimeslotExists(int id)
        {
            return _context.MED_TIMESLOT.Any(e => e.MT_SLOT_ID == id);
        }
    }
}
