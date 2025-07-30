using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.Dtos;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Support Manager")]
    public class PrioritysApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public PrioritysApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Priority>>> GetAll()
        {
            return await _context.Priorities.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Priority>> Get(int id)
        {
            var item = await _context.Priorities.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<Priority>> Create(PriorityDto dto)
        {
            var entity = new Priority(); // Map fields from dto to entity here
            _context.Priorities.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.PriorityID }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PriorityDto dto)
        {
            var entity = await _context.Priorities.FindAsync(id);
            if (entity == null) return NotFound();

            // Map fields from dto to entity here

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Priorities.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Priorities.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
