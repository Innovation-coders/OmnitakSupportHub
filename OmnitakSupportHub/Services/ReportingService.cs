using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Services
{
    public interface IReportingService
    {
        Task<ManagerAnalytics> GetManagerAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<TicketVolumeData>> GetTicketVolumeDataAsync(int days = 30);
        Task<List<TeamPerformanceData>> GetTeamPerformanceAsync();
        Task<List<CategoryDistributionData>> GetCategoryDistributionAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<SLAPerformanceData> GetSLAPerformanceAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AgentWorkloadData>> GetAgentWorkloadAsync();
        Task<List<ResolutionTrendData>> GetResolutionTrendAsync(int days = 30);
    }

    public class ReportingService : IReportingService
    {
        private readonly OmnitakContext _context;

        public ReportingService(OmnitakContext context)
        {
            _context = context;
        }

        public async Task<ManagerAnalytics> GetManagerAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var tickets = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            var resolvedTickets = tickets.Where(t => t.Status.StatusName == "Resolved" || t.Status.StatusName == "Closed").ToList();
            var overdueTickets = GetOverdueTickets(tickets);

            return new ManagerAnalytics
            {
                TotalTickets = tickets.Count,
                OpenTickets = tickets.Count(t => t.Status.StatusName == "Open" || t.Status.StatusName == "Assigned"),
                InProgressTickets = tickets.Count(t => t.Status.StatusName == "In Progress"),
                ResolvedTickets = resolvedTickets.Count,
                OverdueTickets = overdueTickets.Count,
                HighPriorityTickets = tickets.Count(t => t.Priority?.PriorityName == "High"),
                AverageResolutionHours = CalculateAverageResolutionTime(resolvedTickets),
                SLAComplianceRate = CalculateSLACompliance(resolvedTickets),
                TotalActiveAgents = await _context.Users.CountAsync(u => u.IsActive && u.Role.RoleName == "Support Agent"),
                TicketsPerAgent = await CalculateTicketsPerAgent(tickets),
                CategoryBreakdown = GroupTicketsByCategory(tickets),
                PriorityBreakdown = GroupTicketsByPriority(tickets),
                StatusBreakdown = GroupTicketsByStatus(tickets),
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };
        }

        public async Task<List<TicketVolumeData>> GetTicketVolumeDataAsync(int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var volumeData = new List<TicketVolumeData>();

            for (int i = days; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i).Date;
                var nextDate = date.AddDays(1);

                var created = await _context.Tickets.CountAsync(t => t.CreatedAt >= date && t.CreatedAt < nextDate);
                var resolved = await _context.Tickets.CountAsync(t => t.ClosedAt >= date && t.ClosedAt < nextDate);

                volumeData.Add(new TicketVolumeData
                {
                    Date = date.ToString("MMM dd"),
                    Created = created,
                    Resolved = resolved
                });
            }

            return volumeData;
        }

        public async Task<List<TeamPerformanceData>> GetTeamPerformanceAsync()
        {
            var teams = await _context.SupportTeams
                .Include(t => t.Users)
                    .ThenInclude(m => m.AssignedTickets)
                        .ThenInclude(ticket => ticket.Status)
                .ToListAsync();

            var performanceData = new List<TeamPerformanceData>();

            foreach (var team in teams)
            {
                var allTeamTickets = team.Users.SelectMany(m => m.AssignedTickets).ToList();
                var resolvedTickets = allTeamTickets.Where(t => t.Status?.StatusName == "Resolved" || t.Status?.StatusName == "Closed").ToList();

                performanceData.Add(new TeamPerformanceData
                {
                    TeamName = team.TeamName,
                    TotalTickets = allTeamTickets.Count,
                    ResolvedTickets = resolvedTickets.Count,
                    ActiveTickets = allTeamTickets.Count(t => t.Status?.StatusName != "Resolved" && t.Status?.StatusName != "Closed"),
                    AverageResolutionHours = CalculateAverageResolutionTime(resolvedTickets),
                    TeamMemberCount = team.Users.Count
                });
            }

            return performanceData;
        }

        public async Task<List<CategoryDistributionData>> GetCategoryDistributionAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var categoryData = await _context.Tickets
                .Include(t => t.Category)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .GroupBy(t => t.Category.CategoryName)
                .Select(g => new CategoryDistributionData
                {
                    CategoryName = g.Key,
                    Count = g.Count(),
                    AverageResolutionHours = g.Where(t => t.ClosedAt.HasValue)
                                            .Select(t => (t.ClosedAt.Value - t.CreatedAt).TotalHours)
                                            .DefaultIfEmpty(0)
                                            .Average()
                })
                .OrderByDescending(c => c.Count)
                .ToListAsync();

            // Calculate percentages
            var total = categoryData.Sum(c => c.Count);
            foreach (var item in categoryData)
            {
                item.Percentage = total > 0 ? (double)item.Count / total * 100 : 0;
            }

            return categoryData;
        }

        public async Task<SLAPerformanceData> GetSLAPerformanceAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var resolvedTickets = await _context.Tickets
                .Include(t => t.Priority)
                .Where(t => t.ClosedAt.HasValue && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            var slaResults = new List<SLAResult>();

            foreach (var ticket in resolvedTickets)
            {
                var slaHours = GetSLAHours(ticket.Priority?.PriorityName);
                var slaDeadline = ticket.CreatedAt.AddHours(slaHours);
                var metSLA = ticket.ClosedAt <= slaDeadline;

                slaResults.Add(new SLAResult
                {
                    Priority = ticket.Priority?.PriorityName ?? "Unknown",
                    MetSLA = metSLA,
                    ResolutionHours = (ticket.ClosedAt.Value - ticket.CreatedAt).TotalHours
                });
            }

            return new SLAPerformanceData
            {
                TotalTickets = resolvedTickets.Count,
                TicketsMetSLA = slaResults.Count(r => r.MetSLA),
                OverallCompliance = slaResults.Any() ? (double)slaResults.Count(r => r.MetSLA) / slaResults.Count * 100 : 0,
                HighPriorityCompliance = CalculatePriorityCompliance(slaResults, "High"),
                MediumPriorityCompliance = CalculatePriorityCompliance(slaResults, "Medium"),
                LowPriorityCompliance = CalculatePriorityCompliance(slaResults, "Low")
            };
        }

        public async Task<List<AgentWorkloadData>> GetAgentWorkloadAsync()
        {
            return await _context.Users
                .Include(u => u.AssignedTickets)
                    .ThenInclude(t => t.Status)
                .Include(u => u.AssignedTickets)
                    .ThenInclude(t => t.Priority)
                .Where(u => u.Role.RoleName == "Support Agent" && u.IsActive)
                .Select(u => new AgentWorkloadData
                {
                    AgentName = u.FullName,
                    TotalAssigned = u.AssignedTickets.Count,
                    OpenTickets = u.AssignedTickets.Count(t => t.Status.StatusName == "Open" || t.Status.StatusName == "Assigned"),
                    InProgressTickets = u.AssignedTickets.Count(t => t.Status.StatusName == "In Progress"),
                    ResolvedToday = u.AssignedTickets.Count(t => t.ClosedAt.HasValue && t.ClosedAt.Value.Date == DateTime.UtcNow.Date),
                    HighPriorityTickets = u.AssignedTickets.Count(t => t.Priority.PriorityName == "High" &&
                                                                     t.Status.StatusName != "Resolved" &&
                                                                     t.Status.StatusName != "Closed")
                })
                .OrderByDescending(a => a.TotalAssigned)
                .ToListAsync();
        }

        public async Task<List<ResolutionTrendData>> GetResolutionTrendAsync(int days = 30)
        {
            var trendData = new List<ResolutionTrendData>();

            for (int i = days; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i).Date;
                var nextDate = date.AddDays(1);

                var resolvedTickets = await _context.Tickets
                    .Include(t => t.Priority)
                    .Where(t => t.ClosedAt >= date && t.ClosedAt < nextDate)
                    .ToListAsync();

                var avgResolutionTime = resolvedTickets.Any()
                    ? resolvedTickets.Average(t => (t.ClosedAt.Value - t.CreatedAt).TotalHours)
                    : 0;

                var slaCompliance = CalculateSLACompliance(resolvedTickets);

                trendData.Add(new ResolutionTrendData
                {
                    Date = date.ToString("MMM dd"),
                    AverageResolutionHours = avgResolutionTime,
                    TicketsResolved = resolvedTickets.Count,
                    SLACompliance = slaCompliance
                });
            }

            return trendData;
        }

        // Helper methods
        private List<Ticket> GetOverdueTickets(List<Ticket> tickets)
        {
            return tickets.Where(t =>
            {
                if (t.Status?.StatusName == "Resolved" || t.Status?.StatusName == "Closed")
                    return false;

                var slaHours = GetSLAHours(t.Priority?.PriorityName);
                var slaDeadline = t.CreatedAt.AddHours(slaHours);
                return DateTime.UtcNow > slaDeadline;
            }).ToList();
        }

        private double CalculateAverageResolutionTime(List<Ticket> resolvedTickets)
        {
            if (!resolvedTickets.Any()) return 0;
            return resolvedTickets.Average(t => (t.ClosedAt.Value - t.CreatedAt).TotalHours);
        }

        private double CalculateSLACompliance(List<Ticket> resolvedTickets)
        {
            if (!resolvedTickets.Any()) return 100;

            var compliantCount = resolvedTickets.Count(t =>
            {
                var slaHours = GetSLAHours(t.Priority?.PriorityName);
                var slaDeadline = t.CreatedAt.AddHours(slaHours);
                return t.ClosedAt <= slaDeadline;
            });

            return (double)compliantCount / resolvedTickets.Count * 100;
        }

        private async Task<double> CalculateTicketsPerAgent(List<Ticket> tickets)
        {
            var agentCount = await _context.Users.CountAsync(u => u.IsActive && u.Role.RoleName == "Support Agent");
            return agentCount > 0 ? (double)tickets.Count / agentCount : 0;
        }

        private Dictionary<string, int> GroupTicketsByCategory(List<Ticket> tickets)
        {
            return tickets.GroupBy(t => t.Category?.CategoryName ?? "Unknown")
                         .ToDictionary(g => g.Key, g => g.Count());
        }

        private Dictionary<string, int> GroupTicketsByPriority(List<Ticket> tickets)
        {
            return tickets.GroupBy(t => t.Priority?.PriorityName ?? "Unknown")
                         .ToDictionary(g => g.Key, g => g.Count());
        }

        private Dictionary<string, int> GroupTicketsByStatus(List<Ticket> tickets)
        {
            return tickets.GroupBy(t => t.Status?.StatusName ?? "Unknown")
                         .ToDictionary(g => g.Key, g => g.Count());
        }

        private double CalculatePriorityCompliance(List<SLAResult> results, string priority)
        {
            var priorityTickets = results.Where(r => r.Priority == priority).ToList();
            if (!priorityTickets.Any()) return 100;

            return (double)priorityTickets.Count(r => r.MetSLA) / priorityTickets.Count * 100;
        }

        private int GetSLAHours(string? priorityName)
        {
            return priorityName switch
            {
                "High" => 4,
                "Medium" => 24,
                "Low" => 72,
                _ => 48
            };
        }
    }

    // Data classes for Manager Dashboard
    public class ManagerAnalytics
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int OverdueTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public double AverageResolutionHours { get; set; }
        public double SLAComplianceRate { get; set; }
        public int TotalActiveAgents { get; set; }
        public double TicketsPerAgent { get; set; }
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
        public Dictionary<string, int> PriorityBreakdown { get; set; } = new();
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class TicketVolumeData
    {
        public string Date { get; set; } = string.Empty;
        public int Created { get; set; }
        public int Resolved { get; set; }
    }

    public class TeamPerformanceData
    {
        public string TeamName { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ActiveTickets { get; set; }
        public double AverageResolutionHours { get; set; }
        public int TeamMemberCount { get; set; }
    }

    public class CategoryDistributionData
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public double AverageResolutionHours { get; set; }
    }

    public class SLAPerformanceData
    {
        public int TotalTickets { get; set; }
        public int TicketsMetSLA { get; set; }
        public double OverallCompliance { get; set; }
        public double HighPriorityCompliance { get; set; }
        public double MediumPriorityCompliance { get; set; }
        public double LowPriorityCompliance { get; set; }
    }

    public class AgentWorkloadData
    {
        public string AgentName { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedToday { get; set; }
        public int HighPriorityTickets { get; set; }
    }

    public class ResolutionTrendData
    {
        public string Date { get; set; } = string.Empty;
        public double AverageResolutionHours { get; set; }
        public int TicketsResolved { get; set; }
        public double SLACompliance { get; set; }
    }

    public class SLAResult
    {
        public string Priority { get; set; } = string.Empty;
        public bool MetSLA { get; set; }
        public double ResolutionHours { get; set; }
    }
}