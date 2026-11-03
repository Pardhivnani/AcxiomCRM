using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Opportunity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IUserService _userService;

        public OpportunitiesController(
            IOpportunityService opportunityService,
            ICustomerService customerService,
            ILeadService leadService,
            IUserService userService)
        {
            _opportunityService = opportunityService;
            _customerService = customerService;
            _leadService = leadService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            OpportunityStage? stage,
            OpportunityStatus? status,
            string? assignedToUserId,
            int? customerId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CreatedDate",
            bool isAscending = false)
        {
            var pagedResult = await _opportunityService.GetOpportunitiesAsync(
                User, searchTerm, stage, status, assignedToUserId, customerId, page, pageSize, sortBy, isAscending);

            var (totalPipeline, weightedPipeline, totalCount) = await _opportunityService.GetPipelineSummaryAsync(User);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Stage = stage;
            ViewBag.Status = status;
            ViewBag.AssignedToUserId = assignedToUserId;
            ViewBag.CustomerId = customerId;
            ViewBag.SortBy = sortBy;
            ViewBag.IsAscending = isAscending;

            ViewBag.TotalPipeline = totalPipeline;
            ViewBag.WeightedPipeline = weightedPipeline;
            ViewBag.TotalCount = totalCount;

            var salesUsers = await _userService.GetSalesExecutivesAsync();
            ViewBag.SalesExecutives = new SelectList(salesUsers, "Id", "FullName", assignedToUserId);

            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int id)
        {
            var opp = await _opportunityService.GetWithDetailsAsync(id, User);
            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or you do not have permission to view it.";
                return RedirectToAction(nameof(Index));
            }

            return View(opp);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var model = new OpportunityCreateEditViewModel
            {
                CustomerId = customerId ?? 0,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                Probability = 10,
                Stage = OpportunityStage.Qualification
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
        {
            if (model.CustomerId <= 0)
            {
                ModelState.AddModelError("CustomerId", "Please select a valid customer.");
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == OpportunityStatus.Open && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId,
                Notes = model.Notes
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage, createdOpp) = await _opportunityService.CreateAsync(opp, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create opportunity.");
                await PopulateDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = createdOpp!.OpportunityId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var opp = await _opportunityService.GetByIdAsync(id, User);
            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or you do not have permission to edit it.";
                return RedirectToAction(nameof(Index));
            }

            var model = new OpportunityCreateEditViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedToUserId = opp.AssignedToUserId,
                Notes = opp.Notes
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OpportunityCreateEditViewModel model)
        {
            if (model.CustomerId <= 0)
            {
                ModelState.AddModelError("CustomerId", "Please select a valid customer.");
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == OpportunityStatus.Open && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId,
                Notes = model.Notes
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _opportunityService.UpdateAsync(opp, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update opportunity.");
                await PopulateDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{model.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStage(int id, OpportunityStage newStage)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _opportunityService.UpdateStageAsync(id, newStage, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"Opportunity stage updated to {newStage}.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _opportunityService.DeleteAsync(id, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Opportunity was deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(OpportunityCreateEditViewModel model)
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

