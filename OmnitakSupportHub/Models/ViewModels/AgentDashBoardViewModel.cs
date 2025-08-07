using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class AgentDashboardViewModel
    {
        // Agent Information
        public string AgentName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string AgentRole { get; set; } = string.Empty;

        // Ticket Collections
        public List<Ticket> AssignedTickets { get; set; } = new();
        public Dictionary<int, List<ChatMessage>> TicketChats { get; set; } = new();
        public List<TicketTimeline> RecentActivity { get; set; } = new();

        // Dashboard Statistics
        public int TotalTickets { get; set; }
        public int AssignedToMe { get; set; }
        public int InProgress { get; set; }
        public int ResolvedToday { get; set; }
        public int NewTickets { get; set; }
        public int OverdueTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public int PendingUserTickets { get; set; }

        // Performance Metrics
        public double AverageResponseTime { get; set; }
        public double ResolutionRate { get; set; }
        public int TicketsResolvedThisWeek { get; set; }
        public int TicketsResolvedThisMonth { get; set; }

        // Recent Activity and Communication
        public List<ChatMessage> RecentMessages { get; set; } = new();
        public List<Ticket> RecentlyUpdatedTickets { get; set; } = new();

        // Filter and Search Properties
        public string CurrentStatusFilter { get; set; } = string.Empty;
        public string CurrentPriorityFilter { get; set; } = string.Empty;
        public string CurrentSearchTerm { get; set; } = string.Empty;
        public List<string> AvailableStatuses { get; set; } = new();
        public List<string> AvailablePriorities { get; set; } = new();

        // Quick Stats Properties
        public int ActiveTicketsCount => AssignedTickets.Count(t =>
            t.Status?.StatusName != "Resolved" && t.Status?.StatusName != "Closed");

        public int CriticalTicketsCount => AssignedTickets.Count(t =>
            t.Priority?.PriorityName == "High" &&
            t.Status?.StatusName != "Resolved" &&
            t.Status?.StatusName != "Closed");

        // Priority Breakdown
        public int HighPriorityCount => AssignedTickets.Count(t => t.Priority?.PriorityName == "High");
        public int MediumPriorityCount => AssignedTickets.Count(t => t.Priority?.PriorityName == "Medium");
        public int LowPriorityCount => AssignedTickets.Count(t => t.Priority?.PriorityName == "Low");

        // Status Breakdown
        public int NewTicketsCount => AssignedTickets.Count(t => t.Status?.StatusName == "New");
        public int InProgressTicketsCount => AssignedTickets.Count(t => t.Status?.StatusName == "In Progress");
        public int ResolvedTicketsCount => AssignedTickets.Count(t => t.Status?.StatusName == "Resolved");
        public int PendingUserTicketsCount => AssignedTickets.Count(t => t.Status?.StatusName == "Pending User");

        // SLA and Time-based Properties
        public List<Ticket> OverdueTicketsList => GetOverdueTickets();
        public List<Ticket> DueTodayTickets => GetDueTodayTickets();
        public List<Ticket> DueThisWeekTickets => GetDueThisWeekTickets();

        // Performance Indicators
        public string WorkloadStatus => GetWorkloadStatus();
        public string WorkloadStatusClass => GetWorkloadStatusClass();
        public double CompletionPercentage => GetCompletionPercentage();
        public string ResolutionTrend => GetResolutionTrend();

        // Helper Methods
        public string GetPriorityClass(string? priority)
        {
            return priority?.ToLower() switch
            {
                "high" => "priority-high",
                "medium" => "priority-medium",
                "low" => "priority-low",
                _ => "priority-low"
            };
        }

        public string GetStatusClass(string? status)
        {
            return status?.ToLower().Replace(" ", "-") switch
            {
                "new" => "status-new",
                "in-progress" => "status-in-progress",
                "pending-user" => "status-pending-user",
                "resolved" => "status-resolved",
                "closed" => "status-closed",
                _ => "status-new"
            };
        }

        public string GetPriorityIcon(string? priority)
        {
            return priority?.ToLower() switch
            {
                "high" => "bi-exclamation-triangle-fill",
                "medium" => "bi-dash-circle-fill",
                "low" => "bi-check-circle-fill",
                _ => "bi-check-circle"
            };
        }

        public string GetStatusIcon(string? status)
        {
            return status?.ToLower().Replace(" ", "-") switch
            {
                "new" => "bi-circle",
                "in-progress" => "bi-arrow-clockwise",
                "pending-user" => "bi-clock",
                "resolved" => "bi-check-circle-fill",
                "closed" => "bi-x-circle-fill",
                _ => "bi-circle"
            };
        }

        public string GetPriorityBadgeClass(string? priority)
        {
            return priority?.ToLower() switch
            {
                "high" => "badge bg-danger",
                "medium" => "badge bg-warning text-dark",
                "low" => "badge bg-success",
                _ => "badge bg-secondary"
            };
        }

        public string GetStatusBadgeClass(string? status)
        {
            return status?.ToLower().Replace(" ", "-") switch
            {
                "new" => "badge bg-primary",
                "in-progress" => "badge bg-warning text-dark",
                "pending-user" => "badge bg-info",
                "resolved" => "badge bg-success",
                "closed" => "badge bg-secondary",
                _ => "badge bg-secondary"
            };
        }

        public List<Ticket> GetTicketsByPriority(string priority)
        {
            return AssignedTickets.Where(t => t.Priority?.PriorityName == priority).ToList();
        }

        public List<Ticket> GetTicketsByStatus(string status)
        {
            return AssignedTickets.Where(t => t.Status?.StatusName == status).ToList();
        }

        public List<Ticket> GetOverdueTickets()
        {
            var now = DateTime.UtcNow;
            return AssignedTickets.Where(t =>
                t.Status?.StatusName != "Resolved" &&
                t.Status?.StatusName != "Closed" &&
                t.CreatedAt.AddDays(2) < now).ToList(); // Consider 2+ days as overdue
        }

        public List<Ticket> GetDueTodayTickets()
        {
            var today = DateTime.Today;
            return AssignedTickets.Where(t =>
                t.Status?.StatusName != "Resolved" &&
                t.Status?.StatusName != "Closed" &&
                t.CreatedAt.Date.AddDays(1) == today).ToList(); // Due today
        }

        public List<Ticket> GetDueThisWeekTickets()
        {
            var endOfWeek = DateTime.Today.AddDays(7);
            return AssignedTickets.Where(t =>
                t.Status?.StatusName != "Resolved" &&
                t.Status?.StatusName != "Closed" &&
                t.CreatedAt.Date.AddDays(3) <= endOfWeek).ToList(); // Due this week
        }

        public List<Ticket> GetUrgentTickets()
        {
            return AssignedTickets.Where(t =>
                t.Priority?.PriorityName == "High" &&
                t.Status?.StatusName != "Resolved" &&
                t.Status?.StatusName != "Closed").ToList();
        }

        public double GetCompletionPercentage()
        {
            if (AssignedTickets.Count == 0) return 0;
            return Math.Round((double)ResolvedTicketsCount / AssignedTickets.Count * 100, 1);
        }

        public string GetWorkloadStatus()
        {
            var openTickets = AssignedTickets.Count(t =>
                t.Status?.StatusName != "Resolved" &&
                t.Status?.StatusName != "Closed");

            return openTickets switch
            {
                <= 3 => "Light",
                <= 7 => "Moderate",
                <= 12 => "Heavy",
                _ => "Critical"
            };
        }

        public string GetWorkloadStatusClass()
        {
            return GetWorkloadStatus() switch
            {
                "Light" => "text-success",
                "Moderate" => "text-warning",
                "Heavy" => "text-danger",
                "Critical" => "text-danger fw-bold",
                _ => "text-muted"
            };
        }

        public string GetResolutionTrend()
        {
            if (TicketsResolvedThisWeek > TicketsResolvedThisMonth / 4)
                return "Improving";
            else if (TicketsResolvedThisWeek < TicketsResolvedThisMonth / 6)
                return "Declining";
            else
                return "Stable";
        }

        public string GetResolutionTrendClass()
        {
            return GetResolutionTrend() switch
            {
                "Improving" => "text-success",
                "Declining" => "text-danger",
                _ => "text-muted"
            };
        }

        public string GetResolutionTrendIcon()
        {
            return GetResolutionTrend() switch
            {
                "Improving" => "bi-arrow-up-circle-fill",
                "Declining" => "bi-arrow-down-circle-fill",
                _ => "bi-dash-circle-fill"
            };
        }

        // Time-based analysis
        public bool IsTicketOverdue(Ticket ticket)
        {
            var now = DateTime.UtcNow;
            var slaHours = ticket.Priority?.PriorityName switch
            {
                "High" => 4,
                "Medium" => 24,
                "Low" => 72,
                _ => 48
            };

            return ticket.Status?.StatusName != "Resolved" &&
                   ticket.Status?.StatusName != "Closed" &&
                   ticket.CreatedAt.AddHours(slaHours) < now;
        }

        public string GetTicketAgeDisplay(Ticket ticket)
        {
            var age = DateTime.UtcNow - ticket.CreatedAt;

            if (age.TotalDays >= 1)
                return $"{(int)age.TotalDays}d {age.Hours}h";
            else if (age.TotalHours >= 1)
                return $"{(int)age.TotalHours}h {age.Minutes}m";
            else
                return $"{(int)age.TotalMinutes}m";
        }

        public string GetSLAStatus(Ticket ticket)
        {
            var slaHours = ticket.Priority?.PriorityName switch
            {
                "High" => 4,
                "Medium" => 24,
                "Low" => 72,
                _ => 48
            };

            var age = DateTime.UtcNow - ticket.CreatedAt;
            var slaDeadline = ticket.CreatedAt.AddHours(slaHours);
            var timeRemaining = slaDeadline - DateTime.UtcNow;

            if (ticket.Status?.StatusName == "Resolved" || ticket.Status?.StatusName == "Closed")
            {
                return ticket.ClosedAt <= slaDeadline ? "Met" : "Missed";
            }

            if (timeRemaining.TotalHours <= 0)
                return "Overdue";
            else if (timeRemaining.TotalHours <= slaHours * 0.25)
                return "Critical";
            else if (timeRemaining.TotalHours <= slaHours * 0.5)
                return "Warning";
            else
                return "On Track";
        }

        public string GetSLAStatusClass(Ticket ticket)
        {
            return GetSLAStatus(ticket) switch
            {
                "Met" => "text-success",
                "Missed" => "text-danger",
                "Overdue" => "text-danger fw-bold",
                "Critical" => "text-danger",
                "Warning" => "text-warning",
                "On Track" => "text-success",
                _ => "text-muted"
            };
        }

        public string GetTimeRemainingDisplay(Ticket ticket)
        {
            var slaHours = ticket.Priority?.PriorityName switch
            {
                "High" => 4,
                "Medium" => 24,
                "Low" => 72,
                _ => 48
            };

            var slaDeadline = ticket.CreatedAt.AddHours(slaHours);
            var timeRemaining = slaDeadline - DateTime.UtcNow;

            if (timeRemaining.TotalHours <= 0)
            {
                var overdue = DateTime.UtcNow - slaDeadline;
                if (overdue.TotalDays >= 1)
                    return $"Overdue by {(int)overdue.TotalDays}d {overdue.Hours}h";
                else
                    return $"Overdue by {(int)overdue.TotalHours}h {overdue.Minutes}m";
            }

            if (timeRemaining.TotalDays >= 1)
                return $"{(int)timeRemaining.TotalDays}d {timeRemaining.Hours}h remaining";
            else if (timeRemaining.TotalHours >= 1)
                return $"{(int)timeRemaining.TotalHours}h {timeRemaining.Minutes}m remaining";
            else
                return $"{(int)timeRemaining.TotalMinutes}m remaining";
        }

        // Dashboard Analytics
        public Dictionary<string, int> GetTicketDistributionByCategory()
        {
            return AssignedTickets
                .GroupBy(t => t.Category?.CategoryName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Dictionary<string, int> GetTicketDistributionByPriority()
        {
            return AssignedTickets
                .GroupBy(t => t.Priority?.PriorityName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Dictionary<string, int> GetTicketDistributionByStatus()
        {
            return AssignedTickets
                .GroupBy(t => t.Status?.StatusName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        // Performance Insights
        public double GetAverageResolutionTimeInHours()
        {
            var resolvedTickets = AssignedTickets.Where(t =>
                t.Status?.StatusName == "Resolved" && t.ClosedAt.HasValue).ToList();

            if (!resolvedTickets.Any())
                return 0;

            var totalHours = resolvedTickets.Sum(t =>
                (t.ClosedAt!.Value - t.CreatedAt).TotalHours);

            return Math.Round(totalHours / resolvedTickets.Count, 1);
        }

        public string GetPerformanceRating()
        {
            var score = 0;

            // Resolution rate scoring (40% weight)
            if (ResolutionRate >= 90) score += 40;
            else if (ResolutionRate >= 80) score += 32;
            else if (ResolutionRate >= 70) score += 24;
            else if (ResolutionRate >= 60) score += 16;
            else score += 8;

            // Response time scoring (30% weight)
            if (AverageResponseTime <= 2) score += 30;
            else if (AverageResponseTime <= 6) score += 24;
            else if (AverageResponseTime <= 12) score += 18;
            else if (AverageResponseTime <= 24) score += 12;
            else score += 6;

            // Workload management (20% weight)
            var workload = GetWorkloadStatus();
            score += workload switch
            {
                "Light" => 20,
                "Moderate" => 16,
                "Heavy" => 12,
                "Critical" => 8,
                _ => 10
            };

            // SLA compliance (10% weight)
            var slaCompliance = GetSLACompliance();
            if (slaCompliance >= 95) score += 10;
            else if (slaCompliance >= 90) score += 8;
            else if (slaCompliance >= 80) score += 6;
            else if (slaCompliance >= 70) score += 4;
            else score += 2;

            return score switch
            {
                >= 90 => "Excellent",
                >= 80 => "Good",
                >= 70 => "Satisfactory",
                >= 60 => "Needs Improvement",
                _ => "Poor"
            };
        }

        public double GetSLACompliance()
        {
            var totalTickets = AssignedTickets.Count(t =>
                t.Status?.StatusName == "Resolved" || t.Status?.StatusName == "Closed");

            if (totalTickets == 0) return 100.0;

            var metSLA = AssignedTickets.Count(t => GetSLAStatus(t) == "Met");
            return Math.Round((double)metSLA / totalTickets * 100, 1);
        }
    }

    // Additional ViewModels for specific functionalities
    public class TicketUpdateViewModel
    {
        public int TicketId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public string? Priority { get; set; }
    }

    public class AddCommentViewModel
    {
        public int TicketId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;
    }

    public class TicketSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool IncludeResolved { get; set; } = false;
    }

    public class AgentPerformanceViewModel
    {
        public string AgentName { get; set; } = string.Empty;
        public int TotalTicketsResolved { get; set; }
        public double AverageResolutionTime { get; set; }
        public double SLACompliance { get; set; }
        public string PerformanceRating { get; set; } = string.Empty;
        public Dictionary<string, int> TicketsByPriority { get; set; } = new();
        public Dictionary<string, int> TicketsByCategory { get; set; } = new();
        public List<Ticket> RecentlyResolvedTickets { get; set; } = new();
    }
}