using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.DTOs;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Support Agent")]
    public class TicketTimelinesApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public TicketTimelinesApiController(OmnitakContext context)
        {
            _context = context;
        }

        // GET: api/TicketTimelines
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketTimeline>>> GetAll()
        {
            return await _context.TicketTimelines
                .Include(t => t.Ticket)
                .ToListAsync();
        }

        // GET: api/TicketTimelines/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketTimeline>> Get(int id)
        {
            var item = await _context.TicketTimelines
                .Include(t => t.Ticket)
                .FirstOrDefaultAsync(t => t.TimelineID == id);

            if (item == null)
                return NotFound();

            return item;
        }

        // POST: api/TicketTimelines
        [HttpPost]
        public async Task<ActionResult<TicketTimeline>> Create(TicketTimelineDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(dto.TicketID);
            if (ticket == null)
                return BadRequest("Invalid Ticket ID");

            var entity = new TicketTimeline
            {
                TicketID = dto.TicketID,
                Ticket = ticket,
                ExpectedResolution = dto.ExpectedResolution,
                ActualResolution = dto.ActualResolution
            };

            _context.TicketTimelines.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = entity.TimelineID }, entity);
        }

        // PUT: api/TicketTimelines/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TicketTimelineDto dto)
        {
            var entity = await _context.TicketTimelines.FindAsync(id);
            if (entity == null)
                return NotFound();

            var ticket = await _context.Tickets.FindAsync(dto.TicketID);
            if (ticket == null)
                return BadRequest("Invalid Ticket ID");

            entity.TicketID = dto.TicketID;
            entity.Ticket = ticket;
            entity.ExpectedResolution = dto.ExpectedResolution;
            entity.ActualResolution = dto.ActualResolution;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/TicketTimelines/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.TicketTimelines.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.TicketTimelines.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
