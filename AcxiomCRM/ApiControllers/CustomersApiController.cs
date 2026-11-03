using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.ApiControllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    [Produces("application/json")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersApiController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] string? search,
            [FromQuery] CustomerStatus? status,
            [FromQuery] string? assignedTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _customerService.GetCustomersAsync(
                User, search, status, assignedTo, page, pageSize);

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
        public async Task<IActionResult> GetCustomer(int id)
        {
            var customer = await _customerService.GetByIdAsync(id, User);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found or access is denied." });
            }

            return Ok(MapToDto(customer));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                AssignedToUserId = dto.AssignedToUserId,
                Status = CustomerStatus.Active
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdCustomer) = await _customerService.CreateAsync(customer, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return CreatedAtAction(nameof(GetCustomer), new { id = createdCustomer!.CustomerId }, MapToDto(createdCustomer));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _customerService.GetByIdAsync(id, User);
            if (existing == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found or access is denied." });
            }

            if (!Enum.TryParse<CustomerStatus>(dto.Status, true, out var statusVal))
            {
                statusVal = CustomerStatus.Active;
            }

            existing.CustomerName = dto.CustomerName;
            existing.Email = dto.Email;
            existing.Phone = dto.Phone;
            existing.CompanyName = dto.CompanyName;
            existing.Address = dto.Address;
            existing.City = dto.City;
            existing.State = dto.State;
            existing.Status = statusVal;
            existing.AssignedToUserId = dto.AssignedToUserId;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _customerService.UpdateAsync(existing, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(MapToDto(existing));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _customerService.DeleteOrDeactivateAsync(id, User, ip);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = $"Customer {id} deleted or deactivated successfully." });
        }

        private static CustomerResponseDto MapToDto(Customer c) => new()
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status.ToString(),
            CreatedDate = c.CreatedDate,
            AssignedToUserId = c.AssignedToUserId,
            AssignedToName = c.AssignedTo?.FullName
        };
    }
}

