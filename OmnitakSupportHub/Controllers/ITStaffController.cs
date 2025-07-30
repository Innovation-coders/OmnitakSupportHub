using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.ViewModels;
using System.Linq;
using Microsoft.AspNetCore.Authorization;


namespace OmnitakSupportHub.Controllers
{

    [Authorize(Roles = "Administrator, Support Manager")]
    public class ITStaffController : Controller
    {
        private readonly OmnitakContext _context;

        public ITStaffController(OmnitakContext context)
        {
            _context = context;
        }

        // GET: ITStaff
        public IActionResult Index()
        {
            var itDepartmentId = _context.Departments
                .Where(d => d.DepartmentName == "IT")
                .Select(d => d.DepartmentId)
                .FirstOrDefault();

            var staffList = _context.Users
                .Include(u => u.Team)
                .Include(u => u.Department)
                .Where(u => u.IsActive && u.DepartmentId == itDepartmentId)
                .ToList();

            return View(staffList);
        }

        // GET: ITStaff/Create
        public IActionResult Create()
        {
            var itDepartmentId = _context.Departments
                .Where(d => d.DepartmentName == "IT")
                .Select(d => d.DepartmentId)
                .FirstOrDefault();

            var viewModel = new ITStaffSpecializationViewModel
            {
                AvailableUsers = _context.Users
                    .Where(u => u.IsActive && u.DepartmentId == itDepartmentId)
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserID.ToString(),
                        Text = u.FullName
                    }).ToList(),

                AvailableSupportTeams = _context.SupportTeams
                    .Select(t => new SelectListItem
                    {
                        Value = t.TeamID.ToString(),
                        Text = t.TeamName
                    }).ToList()
            };

            return View(viewModel);
        }

        // POST: ITStaff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ITStaffSpecializationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableUsers = _context.Users
                    .Where(u => u.IsActive && u.Department.DepartmentName == "IT")
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserID.ToString(),
                        Text = u.FullName
                    }).ToList();

                model.AvailableSupportTeams = _context.SupportTeams
                    .Select(t => new SelectListItem
                    {
                        Value = t.TeamID.ToString(),
                        Text = t.TeamName
                    }).ToList();

                return View(model);
            }

            int teamId = model.SelectedSupportTeamId;

            if (!string.IsNullOrWhiteSpace(model.NewSupportTeamName))
            {
                var newTeam = new SupportTeam
                {
                    TeamName = model.NewSupportTeamName,
                    Specialization = model.NewSupportTeamName
                };

                _context.SupportTeams.Add(newTeam);
                _context.SaveChanges();
                teamId = newTeam.TeamID;
            }

            var user = _context.Users.FirstOrDefault(u => u.UserID == model.SelectedUserId);
            if (user != null)
            {
                user.TeamID = teamId;
                _context.SaveChanges();
            }

            TempData["Success"] = "IT Staff specialization assigned successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ITStaff/Edit/5
        public IActionResult Edit(int id)
        {
            var user = _context.Users
                .Include(u => u.Team)
                .Include(u => u.Department)
                .FirstOrDefault(u => u.UserID == id);

            if (user == null || user.Department?.DepartmentName != "IT" || !user.IsActive)
                return NotFound();

            var viewModel = new ITStaffSpecializationViewModel
            {
                SelectedUserId = user.UserID,
                SelectedSupportTeamId = user.TeamID ?? 0,
                AvailableUsers = new List<SelectListItem> {
                    new SelectListItem { Value = user.UserID.ToString(), Text = user.FullName }
                },
                AvailableSupportTeams = _context.SupportTeams
                    .Select(t => new SelectListItem
                    {
                        Value = t.TeamID.ToString(),
                        Text = t.TeamName
                    }).ToList()
            };

            return View(viewModel);
        }

        // POST: ITStaff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ITStaffSpecializationViewModel model)
        {
            if (id != model.SelectedUserId) return NotFound();

            if (!ModelState.IsValid)
            {
                model.AvailableSupportTeams = _context.SupportTeams
                    .Select(t => new SelectListItem
                    {
                        Value = t.TeamID.ToString(),
                        Text = t.TeamName
                    }).ToList();

                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null) return NotFound();

            int teamId = model.SelectedSupportTeamId;

            if (!string.IsNullOrWhiteSpace(model.NewSupportTeamName))
            {
                var newTeam = new SupportTeam
                {
                    TeamName = model.NewSupportTeamName,
                    Specialization = model.NewSupportTeamName
                };

                _context.SupportTeams.Add(newTeam);
                _context.SaveChanges();
                teamId = newTeam.TeamID;
            }

            user.TeamID = teamId;
            _context.SaveChanges();

            TempData["Success"] = "IT Staff specialization updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ITStaff/Delete/5
        public IActionResult Delete(int id)
        {
            var user = _context.Users
                .Include(u => u.Team)
                .Include(u => u.Department)
                .FirstOrDefault(u => u.UserID == id);

            if (user == null || user.Department?.DepartmentName != "IT")
                return NotFound();

            return View(user);
        }

        // POST: ITStaff/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserID == id);

            if (user != null)
            {
                user.TeamID = null; // unassign team
                _context.SaveChanges();
            }

            TempData["Success"] = "IT Staff specialization removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
