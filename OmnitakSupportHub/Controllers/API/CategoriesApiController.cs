using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;


namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Support Manager")]
    public class CategorysApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public CategorysApiController(OmnitakContext context)
        {
            _context = context;
        }

        // GET: api/Categorys
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetAll()
        {
            return await _context.Categories.ToListAsync();
        }

        // GET: api/Categorys/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> Get(int id)
        {
            var item = await _context.Categories.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        // POST: api/Categorys
        [HttpPost]
        public async Task<ActionResult<Category>> Create(CategoryDto dto)
        {
            var entity = new Category
            {
                CategoryName = dto.CategoryName,
                Description = dto.Description,
                IsActive = dto.IsActive
            };

            _context.Categories.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = entity.CategoryID }, entity);
        }

        // PUT: api/Categorys/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CategoryDto dto)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null) return NotFound();

            entity.CategoryName = dto.CategoryName;
            entity.Description = dto.Description;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Categorys/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Categories.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
