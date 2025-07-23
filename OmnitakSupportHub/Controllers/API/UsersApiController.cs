using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    public class UsersApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public UsersApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(int id)
        {
            var item = await _context.Users.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<User>> Create(UserDto dto)
        {
            var entity = new User(); // Map fields from dto to entity here
            _context.Users.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.UserID }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UserDto dto)
        {
            var entity = await _context.Users.FindAsync(id);
            if (entity == null) return NotFound();

            // Map fields from dto to entity here

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Users.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Users.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
