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
    public class StatussApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public StatussApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Status>>> GetAll()
        {
            return await _context.Statuses.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Status>> Get(int id)
        {
            var item = await _context.Statuses.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<Status>> Create(StatusDto dto)
        {
            var entity = new Status
            {
                StatusName = dto.StatusName,
                IsActive = dto.IsActive
            };

            _context.Statuses.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.StatusID }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, StatusDto dto)
        {
            var entity = await _context.Statuses.FindAsync(id);
            if (entity == null) return NotFound();

            entity.StatusName = dto.StatusName;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Statuses.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Statuses.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
