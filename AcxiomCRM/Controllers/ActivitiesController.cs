using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IOpportunityService _opportunityService;
        private readonly IUserService _userService;

        public ActivitiesController(
            IActivityService activityService,
            ICustomerService customerService,
            ILeadService leadService,
            IOpportunityService opportunityService,
            IUserService userService)
        {
            _activityService = activityService;
            _customerService = customerService;
            _leadService = leadService;
            _opportunityService = opportunityService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            ActivityType? type,
            ActivityStatus? status,
            string? assignedToUserId,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "ActivityDate",
            bool isAscending = false)
        {
            var pagedResult = await _activityService.GetActivitiesAsync(
                User, searchTerm, type, status, assignedToUserId, startDate, endDate, page, pageSize, sortBy, isAscending);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Type = type;
            ViewBag.Status = status;
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
            var activity = await _activityService.GetByIdAsync(id, User);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(activity);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
        {
            var model = new ActivityCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                ActivityDate = DateTime.UtcNow
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                AssignedToUserId = model.AssignedToUserId,
                Status = model.Status
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdActivity) = await _activityService.CreateAsync(activity, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create activity.");
                await PopulateDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Activity logged successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var activity = await _activityService.GetByIdAsync(id, User);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ActivityCreateEditViewModel
            {
                ActivityId = activity.ActivityId,
                ActivityType = activity.ActivityType,
                Subject = activity.Subject,
                Description = activity.Description,
                ActivityDate = activity.ActivityDate,
                CustomerId = activity.CustomerId,
                LeadId = activity.LeadId,
                OpportunityId = activity.OpportunityId,
                AssignedToUserId = activity.AssignedToUserId,
                Status = activity.Status
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ActivityCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var activity = new Activity
            {
                ActivityId = model.ActivityId,
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                AssignedToUserId = model.AssignedToUserId,
                Status = model.Status
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _activityService.UpdateAsync(activity, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update activity.");
                await PopulateDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Activity updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _activityService.DeleteAsync(id, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Activity deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ActivityCreateEditViewModel model)
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
            model.UsersList = salesUsers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})",
                Selected = u.Id == model.AssignedToUserId
            }).ToList();
        }
    }
}

