using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub;
using OmnitakSupportHub.Models;

[Authorize(Roles = "Support Manager, Administrator")]
public class SupportTeamController : Controller
{
    private readonly OmnitakContext _context;
    public SupportTeamController(OmnitakContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var teams = await _context.SupportTeams.Include(t => t.Users).ToListAsync();
        return View(teams);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(SupportTeam team)
    {
        if (ModelState.IsValid)
        {
            _context.SupportTeams.Add(team);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(team);
    }
}