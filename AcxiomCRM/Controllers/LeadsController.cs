using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Lead;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly IUserService _userService;

        public LeadsController(ILeadService leadService, IUserService userService)
        {
            _leadService = leadService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            LeadStatus? status,
            LeadPriority? priority,
            string? assignedToUserId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CreatedDate",
            bool isAscending = false)
        {
            var pagedResult = await _leadService.GetLeadsAsync(
                User, searchTerm, status, priority, assignedToUserId, page, pageSize, sortBy, isAscending);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.AssignedToUserId = assignedToUserId;
            ViewBag.SortBy = sortBy;
            ViewBag.IsAscending = isAscending;

            var salesUsers = await _userService.GetSalesExecutivesAsync();
            ViewBag.SalesExecutives = new SelectList(salesUsers, "Id", "FullName", assignedToUserId);

            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int id)
        {
            var lead = await _leadService.GetWithDetailsAsync(id, User);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or you do not have permission to view it.";
                return RedirectToAction(nameof(Index));
            }

            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new LeadCreateEditViewModel();
            await PopulateSalesExecutivesDropdown(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                ExpectedValue = model.ExpectedValue,
                Priority = model.Priority,
                Notes = model.Notes,
                AssignedToUserId = model.AssignedToUserId
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdLead) = await _leadService.CreateAsync(lead, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create lead.");
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully with code {lead.LeadCode}.";
            return RedirectToAction(nameof(Details), new { id = createdLead!.LeadId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _leadService.GetByIdAsync(id, User);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or you do not have permission to edit it.";
                return RedirectToAction(nameof(Index));
            }

            var model = new LeadCreateEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                ExpectedValue = lead.ExpectedValue,
                Priority = lead.Priority,
                Notes = lead.Notes,
                AssignedToUserId = lead.AssignedToUserId
            };

            await PopulateSalesExecutivesDropdown(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeadCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadCode = model.LeadCode ?? string.Empty,
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                ExpectedValue = model.ExpectedValue,
                Priority = model.Priority,
                Notes = model.Notes,
                AssignedToUserId = model.AssignedToUserId
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _leadService.UpdateAsync(lead, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update lead.");
                await PopulateSalesExecutivesDropdown(model);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{model.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.LeadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _leadService.DeleteAsync(id, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Lead was deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var lead = await _leadService.GetByIdAsync(id, User);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or you do not have permission to convert it.";
                return RedirectToAction(nameof(Index));
            }

            if (lead.Status == LeadStatus.Converted)
            {
                TempData["ErrorMessage"] = "This lead has already been converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                ExpectedValue = lead.ExpectedValue,
                OpportunityName = $"{(!string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.CompanyName : lead.LeadName)} - Deal",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 5000m,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                CreateOpportunity = true
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConvertViewModel model)
        {
            if (model.CreateOpportunity && model.OpportunityAmount.HasValue && model.OpportunityAmount <= 0)
            {
                ModelState.AddModelError("OpportunityAmount", "Opportunity Amount must be greater than 0.");
            }

            if (model.CreateOpportunity && model.ExpectedCloseDate.HasValue && model.ExpectedCloseDate.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdCustomer, createdOpportunity) = await _leadService.ConvertLeadAsync(
                model.LeadId,
                model.CreateOpportunity,
                model.OpportunityName,
                model.OpportunityAmount,
                model.ExpectedCloseDate,
                User,
                ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to convert lead.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{model.LeadName}' successfully converted to Customer '{createdCustomer!.CustomerName}'!";
            return RedirectToAction("Details", "Customers", new { id = createdCustomer.CustomerId });
        }

        private async Task PopulateSalesExecutivesDropdown(LeadCreateEditViewModel model)
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

