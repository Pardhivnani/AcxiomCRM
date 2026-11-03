using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels.Opportunity
{
    public class OpportunityCreateEditViewModel : IValidatableObject
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity name is required.")]
        [MaxLength(150, ErrorMessage = "Opportunity name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid customer.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Range(0.01, 9999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Deal Amount ($)")]
        public decimal Amount { get; set; }

        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Probability (%)")]
        public int Probability { get; set; } = 10;

        [Required(ErrorMessage = "Expected close date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

        [Required(ErrorMessage = "Assigned user is required.")]
        [Display(Name = "Assigned Sales Executive")]
        public string AssignedToUserId { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public List<SelectListItem> CustomersList { get; set; } = new();
        public List<SelectListItem> LeadsList { get; set; } = new();
        public List<SelectListItem> SalesExecutivesList { get; set; } = new();

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

