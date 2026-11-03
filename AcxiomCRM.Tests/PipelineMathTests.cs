using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Tests
{
    public class PipelineMathTests
    {
        [Theory]
        [InlineData(100000, 50, 50000)]
        [InlineData(85000, 20, 17000)]
        [InlineData(120000, 80, 96000)]
        [InlineData(50000, 100, 50000)]
        [InlineData(40000, 0, 0)]
        public void WeightedPipeline_CalculatesCorrectly(decimal amount, int probability, decimal expectedWeighted)
        {
            var opp = new Opportunity
            {
                OpportunityName = "Math Test Deal",
                Amount = amount,
                Probability = probability,
                Stage = OpportunityStage.Qualification,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
            };

            Assert.Equal(expectedWeighted, opp.WeightedPipeline);
        }
    }
}

