using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.FollowUp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IOpportunityService _opportunityService;
        private readonly IUserService _userService;

        public FollowUpsController(
            IFollowUpService followUpService,
            ICustomerService customerService,
            ILeadService leadService,
            IOpportunityService opportunityService,
            IUserService userService)
        {
            _followUpService = followUpService;
            _customerService = customerService;
            _leadService = leadService;
            _opportunityService = opportunityService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            FollowUpStatus? status,
            FollowUpType? type,
            string? assignedToUserId,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "FollowUpDate",
            bool isAscending = true)
        {
            var pagedResult = await _followUpService.GetFollowUpsAsync(
                User, searchTerm, status, type, assignedToUserId, startDate, endDate, page, pageSize, sortBy, isAscending);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Type = type;
            ViewBag.AssignedToUserId = assignedToUserId;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SortBy = sortBy;
            ViewBag.IsAscending = isAscending;

            var salesUsers = await _userService.GetSalesExecutivesAsync();
            ViewBag.SalesExecutives = new SelectList(salesUsers, "Id", "FullName", assignedToUserId);

            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int id)
        {
            var followUp = await _followUpService.GetByIdAsync(id, User);
            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(followUp);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
        {
            var model = new FollowUpCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(1)
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
        {
            if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Remarks = model.Remarks,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdFollowUp) = await _followUpService.CreateAsync(followUp, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create follow-up.");
                await PopulateDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var followUp = await _followUpService.GetByIdAsync(id, User);
            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = new FollowUpCreateEditViewModel
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Subject = followUp.Subject,
                Remarks = followUp.Remarks,
                Status = followUp.Status,
                AssignedToUserId = followUp.AssignedToUserId
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FollowUpCreateEditViewModel model)
        {
            if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var followUp = new FollowUp
            {
                FollowUpId = model.FollowUpId,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Remarks = model.Remarks,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _followUpService.UpdateAsync(followUp, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update follow-up.");
                await PopulateDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Follow-up updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? remarks)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _followUpService.CompleteAsync(id, remarks, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up marked as completed.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Reschedule(int id)
        {
            var followUp = await _followUpService.GetByIdAsync(id, User);
            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = new FollowUpRescheduleViewModel
            {
                FollowUpId = followUp.FollowUpId,
                Subject = followUp.Subject,
                CurrentDate = followUp.FollowUpDate,
                NewDate = DateTime.UtcNow.AddDays(1)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(FollowUpRescheduleViewModel model)
        {
            if (model.NewDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("NewDate", "Follow-up date cannot be earlier than today.");
                return View(model);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _followUpService.RescheduleAsync(model.FollowUpId, model.NewDate, model.Remarks, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to reschedule follow-up.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Follow-up rescheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _followUpService.DeleteAsync(id, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(FollowUpCreateEditViewModel model)
        {
            var customers = await _customerService.GetCustomersAsync(User, null, null, null, 1, 100);
            model.CustomersList = customers.Items.Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = $"{c.CustomerName} ({c.CustomerCode})",
                Selected = c.CustomerId == model.CustomerId
            }).ToList();

            var leads = await _leadService.GetLeadsAsync(User, null, null, null, null, 1, 100);
            model.LeadsList = leads.Items.Select(l => new SelectListItem
            {
                Value = l.LeadId.ToString(),
                Text = $"{l.LeadName} ({l.LeadCode})",
                Selected = l.LeadId == model.LeadId
            }).ToList();

            var opps = await _opportunityService.GetOpportunitiesAsync(User, null, null, null, null, null, 1, 100);
            model.OpportunitiesList = opps.Items.Select(o => new SelectListItem
            {
                Value = o.OpportunityId.ToString(),
                Text = $"{o.OpportunityName} (${o.Amount:N0})",
                Selected = o.OpportunityId == model.OpportunityId
            }).ToList();

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

