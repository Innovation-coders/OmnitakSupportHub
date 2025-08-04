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

        // Dashboard Statistics
        public int TotalTickets { get; set; }
        public int AssignedToMe { get; set; }
        public int InProgress { get; set; }
        public int ResolvedToday { get; set; }
        public int NewTickets { get; set; }
        public int OverdueTickets { get; set; }

        // Performance Metrics
        public double AverageResponseTime { get; set; }
        public double ResolutionRate { get; set; }
        public int TicketsResolvedThisWeek { get; set; }
        public int TicketsResolvedThisMonth { get; set; }

        // Recent Activity
        public List<ChatMessage> RecentMessages { get; set; } = new();
        public List<Ticket> RecentlyUpdatedTickets { get; set; } = new();

        // Priority Breakdown
        public int HighPriorityTickets => AssignedTickets.Count(t => t.Priority?.PriorityName == "High");
        public int MediumPriorityTickets => AssignedTickets.Count(t => t.Priority?.PriorityName == "Medium");
        public int LowPriorityTickets => AssignedTickets.Count(t => t.Priority?.PriorityName == "Low");

        // Status Breakdown
        public int NewTicketsCount => AssignedTickets.Count(t => t.Status?.StatusName == "New");
        public int InProgressTicketsCount => AssignedTickets.Count(t => t.Status?.StatusName == "In Progress");
        public int ResolvedTicketsCount => AssignedTickets.Count(t => t.Status?.StatusName == "Resolved");

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
                "resolved" => "status-resolved",
                _ => "status-new"
            };
        }

        public string GetPriorityIcon(string? priority)
        {
            return priority?.ToLower() switch
            {
                "high" => "bi-exclamation-triangle",
                "medium" => "bi-dash-circle",
                "low" => "bi-check-circle",
                _ => "bi-check-circle"
            };
        }

        public string GetStatusIcon(string? status)
        {
            return status?.ToLower() switch
            {
                "new" => "bi-circle",
                "in progress" => "bi-arrow-clockwise",
                "resolved" => "bi-check-circle",
                _ => "bi-circle"
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
                t.CreatedAt.AddDays(2) < now).ToList(); // Consider 2+ days as overdue
        }

        public List<Ticket> GetUrgentTickets()
        {
            return AssignedTickets.Where(t =>
                t.Priority?.PriorityName == "High" &&
                t.Status?.StatusName != "Resolved").ToList();
        }

        public double GetCompletionPercentage()
        {
            if (AssignedTickets.Count == 0) return 0;
            return Math.Round((double)ResolvedTicketsCount / AssignedTickets.Count * 100, 1);
        }

        public string GetWorkloadStatus()
        {
            var openTickets = AssignedTickets.Count(t => t.Status?.StatusName != "Resolved");
            return openTickets switch
            {
                <= 5 => "Light",
                <= 10 => "Moderate",
                <= 15 => "Heavy",
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
    }

    // Additional ViewModels for specific functionalities
    public class TicketUpdateViewModel
    {
        public int TicketId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Comment { get; set; }
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
    }
}