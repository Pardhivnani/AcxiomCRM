using AcxiomCRM.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        public ActivityType ActivityType { get; set; } = ActivityType.Task;

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity date is required.")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }
        public virtual Opportunity? Opportunity { get; set; }

        [Required(ErrorMessage = "Assigned user is required.")]
        [MaxLength(450)]
        public string AssignedToUserId { get; set; } = string.Empty;
        public virtual ApplicationUser? AssignedTo { get; set; }

        public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

