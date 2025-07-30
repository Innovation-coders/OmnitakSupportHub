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
    public class SupportTeamsApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public SupportTeamsApiController(OmnitakContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupportTeam>>> GetAll()
        {
            return await _context.SupportTeams.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupportTeam>> Get(int id)
        {
            var item = await _context.SupportTeams.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<SupportTeam>> Create(SupportTeamDto dto)
        {
            var entity = new SupportTeam
            {
                TeamName = dto.TeamName,
                Description = dto.Description,
                Specialization = dto.Specialization,
                TeamLeadID = dto.TeamLeadID
            };

            _context.SupportTeams.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = entity.TeamID }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SupportTeamDto dto)
        {
            var entity = await _context.SupportTeams.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TeamName = dto.TeamName;
            entity.Description = dto.Description;
            entity.Specialization = dto.Specialization;
            entity.TeamLeadID = dto.TeamLeadID;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.SupportTeams.FindAsync(id);
            if (entity == null) return NotFound();

            _context.SupportTeams.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
