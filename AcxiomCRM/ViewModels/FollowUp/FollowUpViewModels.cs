using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels.FollowUp
{
    public class FollowUpCreateEditViewModel : IValidatableObject
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Display(Name = "Related Opportunity (Optional)")]
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Display(Name = "Follow-up Type")]
        public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

        [Required(ErrorMessage = "Assigned user is required.")]
        [Display(Name = "Assigned Sales Executive")]
        public string AssignedToUserId { get; set; } = string.Empty;

        public List<SelectListItem> CustomersList { get; set; } = new();
        public List<SelectListItem> LeadsList { get; set; } = new();
        public List<SelectListItem> OpportunitiesList { get; set; } = new();
        public List<SelectListItem> SalesExecutivesList { get; set; } = new();

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

    public class FollowUpRescheduleViewModel
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public DateTime CurrentDate { get; set; }

        [Required(ErrorMessage = "New follow-up date is required.")]
        [Display(Name = "New Follow-up Date & Time")]
        public DateTime NewDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Display(Name = "Reschedule Reason / Remarks")]
        public string? Remarks { get; set; }
    }

    public class FollowUpCompleteViewModel
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;

        [Display(Name = "Completion Notes / Outcome Remarks")]
        public string? Remarks { get; set; }
    }
}

