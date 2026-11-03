using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;

        public CustomersController(ICustomerService customerService, IUserService userService)
        {
            _customerService = customerService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            CustomerStatus? status,
            string? assignedToUserId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CustomerName",
            bool isAscending = true)
        {
            var pagedResult = await _customerService.GetCustomersAsync(
                User, searchTerm, status, assignedToUserId, page, pageSize, sortBy, isAscending);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.AssignedToUserId = assignedToUserId;
            ViewBag.SortBy = sortBy;
            ViewBag.IsAscending = isAscending;

            var salesUsers = await _userService.GetSalesExecutivesAsync();
            ViewBag.SalesExecutives = new SelectList(salesUsers, "Id", "FullName", assignedToUserId);

            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetWithDetailsAsync(id, User);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or you do not have permission to view it.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CustomerDetailsViewModel
            {
                Customer = customer,
                TotalOpportunityValue = customer.Opportunities.Sum(o => o.Amount),
                ActiveDealsCount = customer.Opportunities.Count(o => o.Status == OpportunityStatus.Open)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerCreateEditViewModel();
            await PopulateSalesExecutivesDropdown(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdCustomer) = await _customerService.CreateAsync(customer, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create customer.");
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully with code {customer.CustomerCode}.";
            return RedirectToAction(nameof(Details), new { id = createdCustomer!.CustomerId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetByIdAsync(id, User);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or you do not have permission to edit it.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CustomerCreateEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                AssignedToUserId = customer.AssignedToUserId
            };

            await PopulateSalesExecutivesDropdown(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerCode = model.CustomerCode ?? string.Empty,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _customerService.UpdateAsync(customer, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update customer.");
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _customerService.DeleteOrDeactivateAsync(id, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Customer was deleted or deactivated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSalesExecutivesDropdown(CustomerCreateEditViewModel model)
        {
            var salesUsers = await _userService.GetSalesExecutivesAsync();
            model.SalesExecutivesList = salesUsers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})",
                Selected = u.Id == model.AssignedToUserId
            }).ToList();
        }
    }
}

