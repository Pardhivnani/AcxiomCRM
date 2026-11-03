using AcxiomCRM.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required.")]
        [MaxLength(150, ErrorMessage = "Customer name cannot exceed 150 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }
        public virtual ApplicationUser? CreatedBy { get; set; }

        [MaxLength(450)]
        public string? AssignedToUserId { get; set; }
        public virtual ApplicationUser? AssignedTo { get; set; }

        public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}

