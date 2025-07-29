using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.Dtos;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    public class PasswordResetsApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public PasswordResetsApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PasswordReset>>> GetAll()
        {
            return await _context.PasswordResets.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PasswordReset>> Get(Guid id)
        {
            var item = await _context.PasswordResets.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<PasswordReset>> Create(PasswordResetDto dto)
        {
            var entity = new PasswordReset(); // Map fields from dto to entity here
            _context.PasswordResets.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.Token }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, PasswordResetDto dto)
        {
            var entity = await _context.PasswordResets.FindAsync(id);
            if (entity == null) return NotFound();

            // Map fields from dto to entity here

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.PasswordResets.FindAsync(id);
            if (entity == null) return NotFound();

            _context.PasswordResets.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
