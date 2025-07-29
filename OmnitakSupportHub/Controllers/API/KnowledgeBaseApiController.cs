using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.Dtos;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Support Agent,Support Manager")]
    public class KnowledgeBasesApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public KnowledgeBasesApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KnowledgeBase>>> GetAll()
        {
            return await _context.KnowledgeBase.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<KnowledgeBase>> Get(int id)
        {
            var item = await _context.KnowledgeBase.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<KnowledgeBase>> Create(KnowledgeBaseDto dto)
        {
            var entity = new KnowledgeBase(); // Map fields from dto to entity here
            _context.KnowledgeBase.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.ArticleID }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, KnowledgeBaseDto dto)
        {
            var entity = await _context.KnowledgeBase.FindAsync(id);
            if (entity == null) return NotFound();

            // Map fields from dto to entity here

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.KnowledgeBase.FindAsync(id);
            if (entity == null) return NotFound();

            _context.KnowledgeBase.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
