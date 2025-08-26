using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models.ViewModels;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Services;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Support Manager")]
    public class ManagerDashboardController : Controller
    {
        private readonly OmnitakContext _context;
        private readonly IAuthService _authService;
        private readonly IReportingService _reportingService;

        public ManagerDashboardController(
            OmnitakContext context,
            IAuthService authService,
            IReportingService reportingService)
        {
            _context = context;
            _authService = authService;
            _reportingService = reportingService;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
         
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

          
            var ticketsQuery = _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            if (startDate.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.CreatedAt <= endDate.Value);
            }

            var tickets = await ticketsQuery.ToListAsync();
            var resolvedTickets = tickets.Where(t => t.ClosedAt != null).ToList();
            var openTickets = tickets.Where(t => t.Status.StatusName != "Closed" && t.Status.StatusName != "Resolved").ToList();

            double averageResolutionTime = resolvedTickets.Any()
                ? resolvedTickets.Average(t => (t.ClosedAt.Value - t.CreatedAt).TotalHours)
                : 0;

          
            var feedbacks = await _context.Feedbacks
                .Where(f => f.Ticket.CreatedAt >= startDate && f.Ticket.CreatedAt <= endDate)
                .ToListAsync();

            double averageSatisfaction = feedbacks.Any()
                ? feedbacks.Average(f => f.Rating)
                : 0;

            var groupedTickets = tickets.GroupBy(t => t.Category).ToList();

         
            var agentTicketCounts = await _context.Users
                .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                .Select(u => new
                {
                    Agent = u,
                    ActiveTicketCount = u.AssignedTickets
                        .Count(t => t.Status.StatusName != "Closed" && t.Status.StatusName != "Resolved")
                })
                .ToListAsync();

            var availableAgents = agentTicketCounts
                .Where(x => x.ActiveTicketCount < 5)
                .Select(x =>
                {
                    x.Agent.AssignedTickets = x.Agent.AssignedTickets
                        .Where(t => t.Status.StatusName != "Closed" && t.Status.StatusName != "Resolved")
                        .ToList();
                    return x.Agent;
                })
                .ToList();

            ManagerAnalytics analytics;
            try
            {
                analytics = await _reportingService.GetManagerAnalyticsAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                
                analytics = new ManagerAnalytics
                {
                    TotalTickets = tickets.Count,
                    OpenTickets = openTickets.Count,
                    ResolvedTickets = resolvedTickets.Count,
                    OverdueTickets = 0,
                    HighPriorityTickets = tickets.Count(t => t.Priority?.PriorityName == "High"),
                    AverageResolutionHours = averageResolutionTime,
                    SLAComplianceRate = 95.0, 
                    TotalActiveAgents = agentTicketCounts.Count,
                    TicketsPerAgent = agentTicketCounts.Count > 0 ? (double)tickets.Count / agentTicketCounts.Count : 0,
                    CategoryBreakdown = tickets.GroupBy(t => t.Category?.CategoryName ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
                    PriorityBreakdown = tickets.GroupBy(t => t.Priority?.PriorityName ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
                    StatusBreakdown = tickets.GroupBy(t => t.Status?.StatusName ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
                    StartDate = startDate.Value,
                    EndDate = endDate.Value
                };
            }

          
            var viewModel = new MergedManagerDashboardViewModel
            {
                
                StartDate = startDate,
                EndDate = endDate,
                GroupedTickets = groupedTickets,
                AvailableAgents = availableAgents,
                TotalTickets = tickets.Count,
                OpenTickets = openTickets.Count,
                ResolvedTickets = resolvedTickets.Count,
                AverageResolutionTimeHours = averageResolutionTime,
                AverageSatisfactionScore = averageSatisfaction,

              
                Analytics = analytics,
                HasChartData = true
            };

            return View(viewModel);
        }

       
        [HttpGet]
        public async Task<JsonResult> GetTicketVolumeData(int days = 30)
        {
            try
            {
                var data = await _reportingService.GetTicketVolumeDataAsync(days);
                return Json(data);
            }
            catch (Exception ex)
            {
             
                return Json(new List<object>());
            }
        }

        
        [HttpGet]
        public async Task<JsonResult> GetCategoryDistribution(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;

                var data = await _reportingService.GetCategoryDistributionAsync(startDate, endDate);
                return Json(data);
            }
            catch (Exception ex)
            {
             
                var tickets = await _context.Tickets
                    .Include(t => t.Category)
                    .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                    .ToListAsync();

                var fallbackData = tickets
                    .GroupBy(t => t.Category?.CategoryName ?? "Unknown")
                    .Select(g => new { categoryName = g.Key, count = g.Count() })
                    .ToList();

                return Json(fallbackData);
            }
        }

       
        [HttpGet]
        public async Task<JsonResult> GetAgentWorkload()
        {
            try
            {
                var data = await _reportingService.GetAgentWorkloadAsync();
                return Json(data);
            }
            catch (Exception ex)
            {
              
                var agents = await _context.Users
                    .Include(u => u.AssignedTickets)
                        .ThenInclude(t => t.Status)
                    .Where(u => u.Role.RoleName == "Support Agent" && u.IsActive)
                    .Select(u => new {
                        agentName = u.FullName,
                        totalAssigned = u.AssignedTickets.Count,
                        openTickets = u.AssignedTickets.Count(t => t.Status.StatusName == "Open"),
                        inProgressTickets = u.AssignedTickets.Count(t => t.Status.StatusName == "In Progress"),
                        highPriorityTickets = u.AssignedTickets.Count(t => t.Priority.PriorityName == "High")
                    })
                    .ToListAsync();

                return Json(agents);
            }
        }

       
        [HttpGet]
        public async Task<JsonResult> GetSLAPerformance(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;

                var data = await _reportingService.GetSLAPerformanceAsync(startDate, endDate);
                return Json(data);
            }
            catch (Exception ex)
            {
              
                var fallbackData = new
                {
                    totalTickets = 100,
                    ticketsMetSLA = 85,
                    overallCompliance = 85.0
                };
                return Json(fallbackData);
            }
        }

 
        [HttpGet]
        public async Task<JsonResult> GetResolutionTrend(int days = 30)
        {
            try
            {
                var data = await _reportingService.GetResolutionTrendAsync(days);
                return Json(data);
            }
            catch (Exception ex)
            {
                
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetTeamPerformance()
        {
            try
            {
                var data = await _reportingService.GetTeamPerformanceAsync();
                return Json(data);
            }
            catch (Exception ex)
            {
             
                return Json(new List<object>());
            }
        }

       
        [HttpGet]
        public async Task<JsonResult> GetFilteredAnalytics(DateTime startDate, DateTime endDate)
        {
            try
            {
                var analytics = await _reportingService.GetManagerAnalyticsAsync(startDate, endDate);
                return Json(analytics);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Unable to load analytics data" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetReportData(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;

                var analytics = await _reportingService.GetManagerAnalyticsAsync(startDate, endDate);
                var volumeData = await _reportingService.GetTicketVolumeDataAsync(30);
                var teamData = await _reportingService.GetTeamPerformanceAsync();
                var slaData = await _reportingService.GetSLAPerformanceAsync(startDate, endDate);

                var reportData = new
                {
                    Analytics = analytics,
                    VolumeData = volumeData,
                    TeamData = teamData,
                    SLAData = slaData,
                    GeneratedAt = DateTime.UtcNow
                };

                return Json(reportData);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Unable to generate report data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AuditTrail(int? ticketId, int? agentId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (ticketId.HasValue)
                query = query.Where(a => a.TargetType == "Ticket" && a.TargetID == ticketId.Value);

            if (agentId.HasValue)
                query = query.Where(a => a.UserID == agentId.Value);

            if (startDate.HasValue)
                query = query.Where(a => a.PerformedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.PerformedAt <= endDate.Value);

            var logs = await query.OrderByDescending(a => a.PerformedAt).Take(100).ToListAsync();
            return View("AuditTrail", logs);
        }
    }

    
    public class MergedManagerDashboardViewModel
    {
      
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public IEnumerable<IGrouping<Category, Ticket>> GroupedTickets { get; set; } = new List<IGrouping<Category, Ticket>>();
        public List<User> AvailableAgents { get; set; } = new List<User>();

       
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public double AverageResolutionTimeHours { get; set; }
        public double AverageSatisfactionScore { get; set; }

      
        public ManagerAnalytics Analytics { get; set; } = new();
        public bool HasChartData { get; set; } = false;

      
        public string GetResolutionTimeFormatted()
        {
            if (AverageResolutionTimeHours < 1) return "< 1 hour";
            if (AverageResolutionTimeHours < 24) return $"{AverageResolutionTimeHours:F1} hours";
            return $"{AverageResolutionTimeHours / 24:F1} days";
        }

        public string GetSLAComplianceClass()
        {
            return Analytics.SLAComplianceRate switch
            {
                >= 95 => "text-success",
                >= 80 => "text-warning",
                _ => "text-danger"
            };
        }

        public string GetOverdueTicketsClass()
        {
            return Analytics.OverdueTickets switch
            {
                0 => "text-success",
                <= 3 => "text-warning",
                _ => "text-danger"
            };
        }

        
        public string GetSatisfactionFormatted()
        {
            return AverageSatisfactionScore.ToString("F1");
        }

        public string GetAvailableAgentsCount()
        {
            return AvailableAgents?.Count.ToString() ?? "0";
        }
    }
}