using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/tickets")]
    [ApiController]
    //[Authorize(Roles = "Administrator,Support Agent,Support Manager")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    public class TicketsApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public TicketsApiController(OmnitakContext context)
        {
            _context = context;
        }

        // GET: api/tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets()
        {
            var TicketDto = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Select(t => new TicketDto
                {
                    TicketID = t.TicketID,
                    Title = t.Title,
                    Description = t.Description,
                    StatusName = t.Status.StatusName,
                    PriorityName = t.Priority.PriorityName,
                    CategoryName = t.Category.CategoryName,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(TicketDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetTicket(int id)
        {
            var TicketDto = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.TicketID == id);

            if (TicketDto == null)
                return NotFound();

            var dto = new TicketDto
            {
                TicketID = TicketDto.TicketID,
                Title = TicketDto.Title,
                Description = TicketDto.Description,
                StatusName = TicketDto.Status?.StatusName ?? "N/A",
                PriorityName = TicketDto.Priority?.PriorityName ?? "N/A",
                CategoryName = TicketDto.Category?.CategoryName ?? "N/A",
                CreatedAt = TicketDto.CreatedAt
            };

            return Ok(TicketDto);
        }



        // POST: api/tickets
        [HttpPost]
        public async Task<ActionResult<Ticket>> CreateTicket(TicketDto dto)
        {
            // ModelState check added
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ticket = new Ticket
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.TicketID }, ticket);
        }

        // PUT: api/tickets/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, TicketDto dto)
        {
            // ModelState check added
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Title = dto.Title;
            ticket.Description = dto.Description;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/tickets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
