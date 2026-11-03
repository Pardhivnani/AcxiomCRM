using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AuditLogsController : Controller
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(
            string? userId,
            string? entityName,
            string? actionName,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 15)
        {
            var pagedResult = await _auditLogService.GetAuditLogsAsync(
                userId, entityName, actionName, fromDate, toDate, page, pageSize);

            ViewBag.UserId = userId;
            ViewBag.EntityName = entityName;
            ViewBag.ActionName = actionName;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _auditLogService.GetByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_AuditDetailsModal", log);
            }

            return View(log);
        }
    }
}

