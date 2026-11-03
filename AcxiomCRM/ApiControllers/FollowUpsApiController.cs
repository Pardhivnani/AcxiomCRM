using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.ApiControllers
{
    [ApiController]
    [Route("api/followups")]
    [Authorize]
    [Produces("application/json")]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;

        public FollowUpsApiController(IFollowUpService followUpService)
        {
            _followUpService = followUpService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowUps(
            [FromQuery] string? search,
            [FromQuery] FollowUpStatus? status,
            [FromQuery] FollowUpType? type,
            [FromQuery] string? assignedTo,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _followUpService.GetFollowUpsAsync(
                User, search, status, type, assignedTo, startDate, endDate, page, pageSize);

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

        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] FollowUpCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Enum.TryParse<FollowUpType>(dto.FollowUpType, true, out var typeVal);

            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                OpportunityId = dto.OpportunityId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = typeVal,
                Subject = dto.Subject,
                Remarks = dto.Remarks,
                AssignedToUserId = dto.AssignedToUserId ?? string.Empty,
                Status = FollowUpStatus.Planned
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdFollowUp) = await _followUpService.CreateAsync(followUp, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return StatusCode(StatusCodes.Status201Created, MapToDto(createdFollowUp!));
        }

        private static FollowUpResponseDto MapToDto(FollowUp f) => new()
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType.ToString(),
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status.ToString(),
            AssignedToUserId = f.AssignedToUserId,
            AssignedToName = f.AssignedTo?.FullName,
            CompletedDate = f.CompletedDate
        };
    }
}

