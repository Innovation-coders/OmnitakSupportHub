public class RoleDto
{
    public int RoleID { get; set; }
    public string RoleName { get; set; }
    public string? Description { get; set; }
    public bool CanApproveUsers { get; set; }
    public bool CanManageTickets { get; set; }
    public bool CanViewAllTickets { get; set; }
    public bool CanManageKnowledgeBase { get; set; }

    public bool CanViewReports { get; set; }
    public bool CanManageTeams { get; set; }
}
