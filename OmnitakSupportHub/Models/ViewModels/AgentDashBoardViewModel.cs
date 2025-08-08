// File: Models/ViewModels/AgentDashboardViewModel.cs
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

        // Agent-Friendly Metrics (What Should Stay)
        public int AssignedToMe { get; set; }
        public int InProgress { get; set; }
        public int ResolvedToday { get; set; }
        public int NewTickets { get; set; }
        public int OverdueTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public int PendingUserTickets { get; set; }

        // Simple Performance Indicators (Not Stressful Percentages)
        public int TicketsResolvedThisWeek { get; set; }
        public double AverageResponseTime { get; set; }

        // Recent Activity and Communication
        public List<ChatMessage> RecentMessages { get; set; } = new();
        public List<Ticket> RecentlyUpdatedTickets { get; set; } = new();

        // Filter and Search Properties
        public string CurrentStatusFilter { get; set; } = string.Empty;
        public string CurrentPriorityFilter { get; set; } = string.Empty;
        public string CurrentSearchTerm { get; set; } = string.Empty;
        public List<string> AvailableStatuses { get; set; } = new();
        public List<string> AvailablePriorities { get; set; } = new();

        // ✅ AGENT-FRIENDLY METHODS (Keep Simple & Positive)

        /// <summary>
        /// Simple workload awareness without stress
        /// </summary>
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

        /// <summary>
        /// Motivational trend indicator
        /// </summary>
        public string GetResolutionTrend()
        {
            // Simple logic: if resolved more this week than weekly average, it's improving
            var weeklyAverage = TicketsResolvedThisWeek > 0 ? TicketsResolvedThisWeek : 1;

            if (ResolvedToday >= 2) return "Improving";
            if (ResolvedToday >= 1) return "Steady";
            return "Starting";
        }

        public string GetResolutionTrendClass()
        {
            return GetResolutionTrend() switch
            {
                "Improving" => "text-success",
                "Steady" => "text-primary",
                "Starting" => "text-muted",
                _ => "text-muted"
            };
        }

        public string GetResolutionTrendIcon()
        {
            return GetResolutionTrend() switch
            {
                "Improving" => "bi-trending-up",
                "Steady" => "bi-dash-circle",
                "Starting" => "bi-hourglass",
                _ => "bi-dash"
            };
        }

        /// <summary>
        /// Simple SLA awareness without anxiety
        /// </summary>
        public string GetSLAStatus()
        {
            var overdueCount = GetOverdueTicketsCount();
            var criticalCount = GetCriticalTicketsCount();

            if (overdueCount == 0 && criticalCount <= 1) return "On Track";
            if (overdueCount <= 1) return "Watch Timing";
            return "Needs Attention";
        }

        public string GetSLAStatusClass()
        {
            return GetSLAStatus() switch
            {
                "On Track" => "text-success",
                "Watch Timing" => "text-warning",
                "Needs Attention" => "text-danger",
                _ => "text-muted"
            };
        }

        /// <summary>
        /// Overall performance status without numbers/percentages
        /// </summary>
        public string GetPerformanceStatus()
        {
            var workload = GetWorkloadStatus();
            var sla = GetSLAStatus();
            var trend = GetResolutionTrend();

            // Simple logic for agent awareness
            if (sla == "On Track" && (trend == "Improving" || trend == "Steady"))
                return "Meeting Expectations";

            if (sla == "Watch Timing" || workload == "Heavy")
                return "Focus Needed";

            if (sla == "Needs Attention" || workload == "Critical")
                return "Priority Attention";

            return "On Track";
        }

        public string GetPerformanceStatusClass()
        {
            return GetPerformanceStatus() switch
            {
                "Meeting Expectations" => "text-success",
                "Focus Needed" => "text-warning",
                "Priority Attention" => "text-danger",
                "On Track" => "text-primary",
                _ => "text-muted"
            };
        }

        // Helper Methods
        public int GetActiveTicketsCount() => AssignedTickets.Count(t =>
            t.Status?.StatusName != "Resolved" && t.Status?.StatusName != "Closed");

        public int GetCriticalTicketsCount() => AssignedTickets.Count(t =>
            t.Priority?.PriorityName == "High" &&
            t.Status?.StatusName != "Resolved" &&
            t.Status?.StatusName != "Closed");

        public int GetOverdueTicketsCount()
        {
            return AssignedTickets.Count(t => {
                if (t.Status?.StatusName == "Resolved" || t.Status?.StatusName == "Closed")
                    return false;

                var slaHours = t.Priority?.PriorityName switch
                {
                    "High" => 4,
                    "Medium" => 24,
                    "Low" => 72,
                    _ => 48
                };

                var slaDeadline = t.CreatedAt.AddHours(slaHours);
                return DateTime.UtcNow > slaDeadline;
            });
        }

        // Quick Stats Properties
        public int HighPriorityCount => AssignedTickets.Count(t => t.Priority?.PriorityName == "High");
        public int MediumPriorityCount => AssignedTickets.Count(t => t.Priority?.PriorityName == "Medium");
        public int LowPriorityCount => AssignedTickets.Count(t => t.Priority?.PriorityName == "Low");

        // Distribution Methods (for simple visualization, not stressful metrics)
        public Dictionary<string, int> GetTicketDistributionByStatus()
        {
            return AssignedTickets
                .GroupBy(t => t.Status?.StatusName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Dictionary<string, int> GetTicketDistributionByPriority()
        {
            return AssignedTickets
                .GroupBy(t => t.Priority?.PriorityName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        // Utility methods for ticket management
        public List<Ticket> GetTicketsNeedingAttention()
        {
            return AssignedTickets.Where(t =>
                t.Priority?.PriorityName == "High" &&
                t.Status?.StatusName != "Resolved" &&
                t.Status?.StatusName != "Closed").ToList();
        }

        public bool HasUrgentTickets() => GetCriticalTicketsCount() > 0 || GetOverdueTicketsCount() > 0;
    }

    // Additional ViewModels for ticket operations
    public class TicketUpdateViewModel
    {
        public int TicketId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? UpdateNote { get; set; }
    }

    public class QuickCommentViewModel
    {
        public int TicketId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}