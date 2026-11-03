using AcxiomCRM.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class FollowUp : IValidatableObject
    {
        [Key]
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }
        public virtual Opportunity? Opportunity { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        public DateTime FollowUpDate { get; set; }

        public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

        [Required(ErrorMessage = "Assigned user is required.")]
        [MaxLength(450)]
        public string AssignedToUserId { get; set; } = string.Empty;
        public virtual ApplicationUser? AssignedTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Status == FollowUpStatus.Planned && FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                yield return new ValidationResult(
                    "Follow-up date cannot be earlier than today.",
                    new[] { nameof(FollowUpDate) });
            }
        }
    }
}

