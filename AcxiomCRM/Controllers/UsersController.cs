using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            string? role,
            bool? isActive,
            int page = 1,
            int pageSize = 10)
        {
            var pagedResult = await _userService.GetUsersAsync(searchTerm, role, isActive, page, pageSize);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Role = role;
            ViewBag.IsActive = isActive;
            ViewBag.RolesList = new SelectList(UserRoles.AllRoles, role);

            return View(pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UserCreateViewModel();
            await PopulateManagersDropdown(model.ManagerId);
            ViewBag.RolesList = new SelectList(UserRoles.AllRoles);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateManagersDropdown(model.ManagerId);
                ViewBag.RolesList = new SelectList(UserRoles.AllRoles, model.Role);
                return View(model);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _userService.CreateUserAsync(model, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create user.");
                await PopulateManagersDropdown(model.ManagerId);
                ViewBag.RolesList = new SelectList(UserRoles.AllRoles, model.Role);
                return View(model);
            }

            TempData["SuccessMessage"] = $"User '{model.FullName}' ({model.Email}) created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _userService.GetUserForEditAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateManagersDropdown(model.ManagerId);
            ViewBag.RolesList = new SelectList(UserRoles.AllRoles, model.Role);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateManagersDropdown(model.ManagerId);
                ViewBag.RolesList = new SelectList(UserRoles.AllRoles, model.Role);
                return View(model);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _userService.UpdateUserAsync(model, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update user.");
                await PopulateManagersDropdown(model.ManagerId);
                ViewBag.RolesList = new SelectList(UserRoles.AllRoles, model.Role);
                return View(model);
            }

            TempData["SuccessMessage"] = $"User '{model.FullName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _userService.ToggleActiveStatusAsync(id, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "User status updated.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _userService.UnlockUserAsync(id, User, ip);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "User account unlocked successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var editModel = await _userService.GetUserForEditAsync(id);
            if (editModel == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ResetPasswordAdminViewModel
            {
                UserId = editModel.Id,
                UserEmail = editModel.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, errorMessage) = await _userService.ResetPasswordAsync(model, User, ip);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to reset password.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Password for '{model.UserEmail}' reset successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateManagersDropdown(string? selectedManagerId)
        {
            var managers = await _userService.GetManagersAsync();
            ViewBag.ManagersList = new SelectList(managers, "Id", "FullName", selectedManagerId);
        }
    }
}

