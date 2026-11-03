using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Tests
{
    public class ValidationTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Opportunity_AmountZeroOrNegative_FailsValidation(decimal amount)
        {
            var opp = new Opportunity
            {
                OpportunityName = "Big Deal",
                CustomerId = 1,
                Amount = amount,
                Stage = OpportunityStage.Qualification,
                Probability = 20,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                AssignedToUserId = "user-123"
            };

            var context = new ValidationContext(opp);
            var results = new List<ValidationResult>();
            opp.Validate(context).ToList().ForEach(r => results.Add(r));
            Validator.TryValidateObject(opp, context, results, validateAllProperties: true);

            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Opportunity Amount must be greater than 0"));
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(101)]
        [InlineData(150)]
        public void Opportunity_ProbabilityOutOfRange_FailsValidation(int prob)
        {
            var opp = new Opportunity
            {
                OpportunityName = "Big Deal",
                CustomerId = 1,
                Amount = 10000m,
                Stage = OpportunityStage.Qualification,
                Probability = prob,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                AssignedToUserId = "user-123"
            };

            var context = new ValidationContext(opp);
            var results = new List<ValidationResult>();
            opp.Validate(context).ToList().ForEach(r => results.Add(r));
            Validator.TryValidateObject(opp, context, results, validateAllProperties: true);

            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Probability must be between 0 and 100"));
        }

        [Fact]
        public void Opportunity_ExpectedCloseDateInPastForActive_FailsValidation()
        {
            var opp = new Opportunity
            {
                OpportunityName = "Big Deal",
                CustomerId = 1,
                Amount = 10000m,
                Stage = OpportunityStage.Qualification,
                Probability = 20,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2), // Yesterday
                Status = OpportunityStatus.Open,
                AssignedToUserId = "user-123"
            };

            var context = new ValidationContext(opp);
            var results = opp.Validate(context).ToList();

            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Expected Close Date cannot be in the past"));
        }

        [Fact]
        public void FollowUp_PlannedDateInPast_FailsValidation()
        {
            var fu = new FollowUp
            {
                Subject = "Client call",
                FollowUpType = FollowUpType.Call,
                FollowUpDate = DateTime.UtcNow.AddDays(-1), // Yesterday
                Status = FollowUpStatus.Planned,
                AssignedToUserId = "user-123"
            };

            var context = new ValidationContext(fu);
            var results = fu.Validate(context).ToList();

            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Follow-up date cannot be earlier than today"));
        }

        [Fact]
        public void Customer_MissingName_FailsValidation()
        {
            var customer = new Customer
            {
                CustomerName = "", // Blank
                Email = "test@company.com",
                Phone = "+1-555-0100"
            };

            var context = new ValidationContext(customer);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(customer, context, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("CustomerName"));
        }

        [Fact]
        public void OpportunityViewModel_ZeroCustomerId_FailsValidation()
        {
            var model = new AcxiomCRM.ViewModels.Opportunity.OpportunityCreateEditViewModel
            {
                OpportunityName = "Cloud Migration Deal",
                CustomerId = 0, // Unselected
                Amount = 15000m,
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                AssignedToUserId = "user-1"
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("CustomerId"));
        }

        [Fact]
        public void OpportunityViewModel_PastExpectedCloseDate_FailsValidation()
        {
            var model = new AcxiomCRM.ViewModels.Opportunity.OpportunityCreateEditViewModel
            {
                OpportunityName = "Cloud Deal",
                CustomerId = 5,
                Amount = 15000m,
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2), // Past
                AssignedToUserId = "user-1",
                Status = OpportunityStatus.Open
            };

            var context = new ValidationContext(model);
            var results = model.Validate(context).ToList();

            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Expected Close Date cannot be in the past"));
        }
    }
}

