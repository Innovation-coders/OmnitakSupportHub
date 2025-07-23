using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class TicketDto
    {
        public int TicketID { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status ID is required.")]
        public int StatusID { get; set; }

        [Required(ErrorMessage = "PriorityID is required.")]
        public int PriorityID { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "CreatedBy is required.")]
        public int CreatedBy { get; set; }
        public string? StatusName { get; set; } = string.Empty;
        public string? PriorityName { get; set; } = string.Empty;
        public string? CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? AssignedTo { get; set; }

        public int? TeamID { get; set; }
    }
}
