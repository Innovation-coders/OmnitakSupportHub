using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.DTOs;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Support Manager")]
    public class FeedbacksApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public FeedbacksApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Feedback>>> GetAll()
        {
            return await _context.Feedbacks.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Feedback>> Get(int id)
        {
            var item = await _context.Feedbacks.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<Feedback>> Create(FeedbackDto dto)
        {
            var entity = new Feedback(); // Map fields from dto to entity here
            _context.Feedbacks.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.FeedbackID }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, FeedbackDto dto)
        {
            var entity = await _context.Feedbacks.FindAsync(id);
            if (entity == null) return NotFound();

            // Map fields from dto to entity here

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Feedbacks.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Feedbacks.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
