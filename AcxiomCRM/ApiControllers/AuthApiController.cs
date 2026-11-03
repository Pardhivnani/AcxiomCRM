using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.ApiControllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;

        public AuthApiController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Authenticates a user and starts an authenticated session.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                await _auditLogService.LogAsync(
                    null,
                    request.Email,
                    "FailedLogin",
                    "Auth",
                    null,
                    null,
                    "Login rejected: Invalid credentials or deactivated account.",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Failed");

                return Unauthorized(new LoginResponseDto
                {
                    Succeeded = false,
                    Message = "Invalid login credentials or account is deactivated."
                });
            }

            var result = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                await _auditLogService.LogAsync(
                    user.Id,
                    user.Email,
                    "Login",
                    "Auth",
                    user.Id,
                    null,
                    "User logged in via REST API.",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Success");

                return Ok(new LoginResponseDto
                {
                    Succeeded = true,
                    Message = "Authentication successful.",
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Roles = roles.ToList()
                });
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
                    "Account locked due to multiple failed login attempts.",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Failed");

                return StatusCode(StatusCodes.Status423Locked, new LoginResponseDto
                {
                    Succeeded = false,
                    Message = "Account is temporarily locked out due to multiple failed login attempts. Please try again later."
                });
            }

            await _auditLogService.LogAsync(
                user.Id,
                user.Email,
                "FailedLogin",
                "Auth",
                user.Id,
                null,
                "Invalid password attempt.",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "Failed");

            return Unauthorized(new LoginResponseDto
            {
                Succeeded = false,
                Message = "Invalid email or password."
            });
        }

        /// <summary>
        /// Logs out the currently authenticated user.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
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
                    "User logged out via REST API.",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Success");
            }

            return Ok(new { message = "Logged out successfully." });
        }
    }
}

