using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.ApiControllers
{
    [ApiController]
    [Route("api/opportunities")]
    [Authorize]
    [Produces("application/json")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;

        public OpportunitiesApiController(IOpportunityService opportunityService)
        {
            _opportunityService = opportunityService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOpportunities(
            [FromQuery] string? search,
            [FromQuery] OpportunityStage? stage,
            [FromQuery] OpportunityStatus? status,
            [FromQuery] string? assignedTo,
            [FromQuery] int? customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _opportunityService.GetOpportunitiesAsync(
                User, search, stage, status, assignedTo, customerId, page, pageSize);

            var dtos = result.Items.Select(MapToDto).ToList();

            return Ok(new
            {
                items = dtos,
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages = result.TotalPages
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOpportunity(int id)
        {
            var opp = await _opportunityService.GetByIdAsync(id, User);
            if (opp == null)
            {
                return NotFound(new { message = $"Opportunity with ID {id} was not found or access is denied." });
            }

            return Ok(MapToDto(opp));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] OpportunityCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Enum.TryParse<OpportunityStage>(dto.Stage, true, out var stageVal);

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = stageVal,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                AssignedToUserId = dto.AssignedToUserId ?? string.Empty,
                Notes = dto.Notes
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdOpp) = await _opportunityService.CreateAsync(opp, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return CreatedAtAction(nameof(GetOpportunity), new { id = createdOpp!.OpportunityId }, MapToDto(createdOpp));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOpportunity(int id, [FromBody] OpportunityUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _opportunityService.GetByIdAsync(id, User);
            if (existing == null)
            {
                return NotFound(new { message = $"Opportunity with ID {id} was not found or access is denied." });
            }

            if (!Enum.TryParse<OpportunityStage>(dto.Stage, true, out var stageVal))
                stageVal = existing.Stage;

            if (!Enum.TryParse<OpportunityStatus>(dto.Status, true, out var statusVal))
                statusVal = existing.Status;

            existing.OpportunityName = dto.OpportunityName;
            existing.CustomerId = dto.CustomerId;
            existing.LeadId = dto.LeadId;
            existing.Amount = dto.Amount;
            existing.Stage = stageVal;
            existing.Probability = dto.Probability;
            existing.ExpectedCloseDate = dto.ExpectedCloseDate;
            existing.Status = statusVal;
            if (!string.IsNullOrEmpty(dto.AssignedToUserId))
            {
                existing.AssignedToUserId = dto.AssignedToUserId;
            }
            existing.Notes = dto.Notes;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _opportunityService.UpdateAsync(existing, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(MapToDto(existing));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteOpportunity(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _opportunityService.DeleteAsync(id, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = $"Opportunity {id} deleted successfully." });
        }

        private static OpportunityResponseDto MapToDto(Opportunity o) => new()
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName ?? string.Empty,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage.ToString(),
            Probability = o.Probability,
            WeightedPipeline = o.WeightedPipeline,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status.ToString(),
            CreatedDate = o.CreatedDate,
            AssignedToUserId = o.AssignedToUserId,
            AssignedToName = o.AssignedTo?.FullName,
            Notes = o.Notes
        };
    }
}

