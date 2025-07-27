using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class AgentDashboardViewModel
    {
        public string AgentName { get; set; }
        public string TeamName { get; set; }
        public List<Ticket> AssignedTickets { get; set; } = new();
        public Dictionary<int, List<ChatMessage>> TicketChats { get; set; } = new();
    }
}
