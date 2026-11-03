using AcxiomCRM.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead name is required.")]
        [MaxLength(150, ErrorMessage = "Lead name cannot exceed 150 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        [MaxLength(100)]
        public string Source { get; set; } = "Website";

        public LeadStatus Status { get; set; } = LeadStatus.New;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999999.99, ErrorMessage = "Expected value must be a non-negative amount.")]
        public decimal ExpectedValue { get; set; } = 0m;

        public LeadPriority Priority { get; set; } = LeadPriority.Medium;

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(450)]
        public string? AssignedToUserId { get; set; }
        public virtual ApplicationUser? AssignedTo { get; set; }

        // If converted to customer
        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}

