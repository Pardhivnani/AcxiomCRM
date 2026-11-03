using AcxiomCRM.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Opportunity : IValidatableObject
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity name is required.")]
        [MaxLength(150, ErrorMessage = "Opportunity name cannot exceed 150 characters.")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 9999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required(ErrorMessage = "Expected close date is required.")]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; }

        public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [Required(ErrorMessage = "Assigned user is required.")]
        [MaxLength(450)]
        public string AssignedToUserId { get; set; } = string.Empty;
        public virtual ApplicationUser? AssignedTo { get; set; }

        public string? Notes { get; set; }

        [NotMapped]
        public decimal WeightedPipeline => Math.Round(Amount * Probability / 100m, 2);

        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Status == OpportunityStatus.Open && ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                yield return new ValidationResult(
                    "Expected Close Date cannot be in the past.",
                    new[] { nameof(ExpectedCloseDate) });
            }

            if (Amount <= 0)
            {
                yield return new ValidationResult(
                    "Opportunity Amount must be greater than 0.",
                    new[] { nameof(Amount) });
            }

            if (Probability < 0 || Probability > 100)
            {
                yield return new ValidationResult(
                    "Probability must be between 0 and 100.",
                    new[] { nameof(Probability) });
            }
        }
    }
}

