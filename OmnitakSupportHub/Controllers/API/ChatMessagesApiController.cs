using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.Dtos;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Support Agent")]
    public class ChatMessagesApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public ChatMessagesApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChatMessage>>> GetAll()
        {
            return await _context.ChatMessages
                .Include(c => c.User)
                .Include(c => c.Ticket)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChatMessage>> Get(int id)
        {
            var item = await _context.ChatMessages.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<ChatMessage>> Create(ChatMessageDto dto)
        {
            var entity = new ChatMessage           

           {
               TicketID = dto.TicketID,
                UserID = dto.UserID,
                Message = dto.Message,
                SentAt = dto.SentAt,
                ReadAt = dto.ReadAt
            };

            _context.ChatMessages.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.MessageID }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ChatMessageDto dto)
        {
            var entity = await _context.ChatMessages.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TicketID = dto.TicketID;
            entity.UserID = dto.UserID;
            entity.Message = dto.Message;
            entity.SentAt = dto.SentAt;
            entity.ReadAt = dto.ReadAt;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.ChatMessages.FindAsync(id);
            if (entity == null) return NotFound();

            _context.ChatMessages.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
