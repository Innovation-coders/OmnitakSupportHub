using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.DTOs;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    public class DepartmentsApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public DepartmentsApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetAll()
        {
            return await _context.Departments.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> Get(int id)
        {
            var item = await _context.Departments.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<Department>> Create(DepartmentDto dto)
        {
            var entity = new Department(); // Map fields from dto to entity here
            _context.Departments.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.DepartmentId }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DepartmentDto dto)
        {
            var entity = await _context.Departments.FindAsync(id);
            if (entity == null) return NotFound();

            // Map fields from dto to entity here

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Departments.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Departments.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
