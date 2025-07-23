using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    public class RolesApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public RolesApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var item = await _context.Roles.FindAsync(id);
            if (item == null)
                return NotFound();

            return item;
        }

        [HttpPost]
        public async Task<ActionResult<Role>> PostRole(Role item)
        {
            _context.Roles.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRole), new { id = item.RoleID }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutRole(int id, Role item)
        {
            if (id != item.RoleID)
                return BadRequest();

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Roles.Any(e => e.RoleID == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var item = await _context.Roles.FindAsync(id);
            if (item == null)
                return NotFound();

            _context.Roles.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
