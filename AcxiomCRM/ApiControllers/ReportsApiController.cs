using AcxiomCRM.DTOs;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.ApiControllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    [Produces("application/json")]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IOpportunityService _opportunityService;

        public ReportsApiController(
            IReportService reportService,
            IOpportunityService opportunityService)
        {
            _reportService = reportService;
            _opportunityService = opportunityService;
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? userId)
        {
            var filter = new ReportFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                UserId = userId
            };

            var stagesData = await _reportService.GetPipelineReportAsync(filter, User);
            var (totalPipeline, weightedPipeline, totalCount) = await _opportunityService.GetPipelineSummaryAsync(User);

            var response = new PipelineReportResponseDto
            {
                TotalPipelineValue = totalPipeline,
                WeightedPipelineValue = weightedPipeline,
                TotalOpportunities = totalCount,
                Stages = stagesData.Select(s => new StageSummaryDto
                {
                    Stage = s.Stage,
                    Count = s.OpportunityCount,
                    Amount = s.TotalAmount,
                    WeightedAmount = s.WeightedAmount
                }).ToList()
            };

            return Ok(response);
        }
    }
}

