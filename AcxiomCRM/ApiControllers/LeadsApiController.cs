using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.ApiControllers
{
    [ApiController]
    [Route("api/leads")]
    [Authorize]
    [Produces("application/json")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsApiController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeads(
            [FromQuery] string? search,
            [FromQuery] LeadStatus? status,
            [FromQuery] LeadPriority? priority,
            [FromQuery] string? assignedTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _leadService.GetLeadsAsync(
                User, search, status, priority, assignedTo, page, pageSize);

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
        public async Task<IActionResult> GetLead(int id)
        {
            var lead = await _leadService.GetByIdAsync(id, User);
            if (lead == null)
            {
                return NotFound(new { message = $"Lead with ID {id} was not found or access is denied." });
            }

            return Ok(MapToDto(lead));
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] LeadCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Enum.TryParse<LeadPriority>(dto.Priority, true, out var priorityVal);

            var lead = new Lead
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                ExpectedValue = dto.ExpectedValue,
                Priority = priorityVal,
                Notes = dto.Notes,
                AssignedToUserId = dto.AssignedToUserId,
                Status = LeadStatus.New
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdLead) = await _leadService.CreateAsync(lead, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return CreatedAtAction(nameof(GetLead), new { id = createdLead!.LeadId }, MapToDto(createdLead));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLead(int id, [FromBody] LeadUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _leadService.GetByIdAsync(id, User);
            if (existing == null)
            {
                return NotFound(new { message = $"Lead with ID {id} was not found or access is denied." });
            }

            if (!Enum.TryParse<LeadStatus>(dto.Status, true, out var statusVal))
                statusVal = existing.Status;

            if (!Enum.TryParse<LeadPriority>(dto.Priority, true, out var priorityVal))
                priorityVal = existing.Priority;

            existing.LeadName = dto.LeadName;
            existing.Email = dto.Email;
            existing.Phone = dto.Phone;
            existing.CompanyName = dto.CompanyName;
            existing.Source = dto.Source;
            existing.Status = statusVal;
            existing.Priority = priorityVal;
            existing.ExpectedValue = dto.ExpectedValue;
            existing.Notes = dto.Notes;
            existing.AssignedToUserId = dto.AssignedToUserId;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _leadService.UpdateAsync(existing, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(MapToDto(existing));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLead(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _leadService.DeleteAsync(id, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = $"Lead {id} deleted successfully." });
        }

        private static LeadResponseDto MapToDto(Lead l) => new()
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status.ToString(),
            ExpectedValue = l.ExpectedValue,
            Priority = l.Priority.ToString(),
            Notes = l.Notes,
            CreatedDate = l.CreatedDate,
            AssignedToUserId = l.AssignedToUserId,
            AssignedToName = l.AssignedTo?.FullName,
            CustomerId = l.CustomerId
        };
    }
}

