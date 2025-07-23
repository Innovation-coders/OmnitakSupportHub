using System;
using System.Collections.Generic;
using System.Linq;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class ManagerDashboardViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public IEnumerable<IGrouping<Category, Ticket>> GroupedTickets { get; set; }
            = new List<IGrouping<Category, Ticket>>();

        public List<User> AvailableAgents { get; set; } = null!;
    }
}
