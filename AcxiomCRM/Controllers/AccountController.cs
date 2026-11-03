using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (user == null)
            {
                await _auditLogService.LogAsync(
                    null,
                    model.Email,
                    "FailedLogin",
                    "Auth",
                    null,
                    null,
                    "Login failed: User email not found.",
                    ip,
                    "Failed");

                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                await _auditLogService.LogAsync(
                    user.Id,
                    user.Email,
                    "FailedLogin",
                    "Auth",
                    user.Id,
                    null,
                    "Login attempt on deactivated account.",
                    ip,
                    "Denied");

                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(
                    user.Id,
                    user.Email,
                    "Login",
                    "Auth",
                    user.Id,
                    null,
                    "User signed in successfully.",
                    ip,
                    "Success");

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync(
                    user.Id,
                    user.Email,
                    "Lockout",
                    "Auth",
                    user.Id,
                    null,
                    "Account locked due to consecutive failed password attempts.",
                    ip,
                    "Failed");

                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts. Please try again after 15 minutes or contact support.");
                return View(model);
            }

            await _auditLogService.LogAsync(
                user.Id,
                user.Email,
                "FailedLogin",
                "Auth",
                user.Id,
                null,
                "Password authentication failed.",
                ip,
                "Failed");

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Department = model.Department,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign default role SalesExecutive to self-registered users
                await _userManager.AddToRoleAsync(user, UserRoles.SalesExecutive);

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditLogService.LogAsync(
                    user.Id,
                    user.Email,
                    "Register",
                    "Auth",
                    user.Id,
                    null,
                    $"New self-registered user '{user.FullName}' registered as SalesExecutive.",
                    ip,
                    "Success");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _signInManager.SignOutAsync();

            if (user != null)
            {
                await _auditLogService.LogAsync(
                    user.Id,
                    user.Email,
                    "Logout",
                    "Auth",
                    user.Id,
                    null,
                    "User signed out.",
                    ip,
                    "Success");
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditLogService.LogAsync(
                    user.Id,
                    user.Email,
                    "ChangePassword",
                    "Auth",
                    user.Id,
                    null,
                    "User successfully changed their password.",
                    ip,
                    "Success");

                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}

