public class PriorityDto
{
    public int PriorityID { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Weight { get; set; }
    public bool IsActive { get; set; } = true;
}
