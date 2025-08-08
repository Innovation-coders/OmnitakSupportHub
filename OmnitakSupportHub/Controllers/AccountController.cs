using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using OmnitakSupportHub.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers
{
    public class AccountController : Controller
    {

        // Brute Force Protection (simple in-memory lockout)
        private static readonly Dictionary<string, int> FailedLoginAttempts = new();
        private static readonly Dictionary<string, DateTime> LockoutUntil = new();


        private bool IsLockedOut(string username)
        {
            return LockoutUntil.ContainsKey(username) && LockoutUntil[username] > DateTime.UtcNow;
        }

        private void RecordFailedAttempt(string username)
        {
            if (!FailedLoginAttempts.ContainsKey(username))
                FailedLoginAttempts[username] = 0;

            FailedLoginAttempts[username]++;
            if (FailedLoginAttempts[username] >= 5) // Lock after 5 failed attempts
            {
                LockoutUntil[username] = DateTime.UtcNow.AddMinutes(5); // Lock for 5 minutes
                FailedLoginAttempts[username] = 0; // Reset counter
            }
        }

        private void ResetFailedAttempts(string username)
        {
            if (FailedLoginAttempts.ContainsKey(username))
                FailedLoginAttempts[username] = 0;
        }

        private readonly IAuthService _authService;
        private readonly OmnitakContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            OmnitakContext context,
            EmailService emailService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (IsLockedOut(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Account temporarily locked. Try again after 5 minutes.");
                RecordFailedAttempt(model.Email);
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            // Convert ViewModel to Model for AuthService
            var loginModel = new LoginModel
            {
                Email = model.Email,
                Password = model.Password
            };

            var result = await _authService.LoginAsync(loginModel);

            ResetFailedAttempts(model.Email);
            if (result.Success && result.User != null)
            {
                // Create claims for the authenticated user
                var claims = new List<Claim>
                {
                    new Claim("UserID", result.User.UserID.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, result.User.UserID.ToString()),
                    new Claim(ClaimTypes.Name, result.User.FullName),
                    new Claim(ClaimTypes.Email, result.User.Email),
                    new Claim("Department", result.User.Department?.DepartmentName ?? ""),
                    new Claim("RoleID", result.User.RoleID?.ToString() ?? ""),
                    new Claim("TeamID", result.User.TeamID?.ToString() ?? "")
                };

                // Add role-based claims if Role is loaded
                if (result.User.Role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, result.User.Role.RoleName));

                    // Add permission claims
                    if (result.User.Role.CanApproveUsers)
                        claims.Add(new Claim("Permission", "CanApproveUsers"));
                    if (result.User.Role.CanManageTickets)
                        claims.Add(new Claim("Permission", "CanManageTickets"));
                    if (result.User.Role.CanViewAllTickets)
                        claims.Add(new Claim("Permission", "CanViewAllTickets"));
                    if (result.User.Role.CanManageKnowledgeBase)
                        claims.Add(new Claim("Permission", "CanManageKnowledgeBase"));
                    if (result.User.Role.CanViewReports)
                        claims.Add(new Claim("Permission", "CanViewReports"));
                    if (result.User.Role.CanManageTeams)
                        claims.Add(new Claim("Permission", "CanManageTeams"));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);

                // Redirect based on role
                if (result.User.Role?.RoleName == "Administrator")
                {
                    return RedirectToAction("Index", "AdminDashboard");
                }
                else if (result.User.Role?.RoleName == "Support Manager")
                {
                    return RedirectToAction("Index", "ManagerDashboard");
                }
                else if (result.User.Role?.RoleName == "Support Agent")
                {
                    return RedirectToAction("Index", "AgentDashboard");
                }
                else
                {
                    return RedirectToAction("Index", "UserDashboard");
                }
            }

            _logger.LogWarning("Failed login attempt for user: {Email}", model.Email);
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            var departments = _context.Departments.ToList();

            var model = new RegisterViewModel // Create ViewModel
            {
                AvailableDepartments = departments.Select(d => new SelectListItem
                {
                    Value = d.DepartmentName,
                    Text = d.DepartmentName
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model) // Receive ViewModel
        {
            if (!ModelState.IsValid)
            {
                // Repopulate departments on failure
                model.AvailableDepartments = _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentName,
                        Text = d.DepartmentName
                    }).ToList();
                return View(model);
            }

            // Convert ViewModel to Model for service
            var registerModel = new RegisterModel
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Department = model.Department
            };

            var result = await _authService.RegisterAsync(registerModel);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                _emailService.SendRegistrationEmail(model.Email, model.FullName);
                

                return RedirectToAction("Login");
            }

            // Repopulate departments if registration fails
            model.AvailableDepartments = _context.Departments
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentName,
                    Text = d.DepartmentName
                }).ToList();

            _logger.LogWarning("Failed login attempt for user: {Email}", model.Email);
            ModelState.AddModelError("", result.Message);
            return View(model);
        }
        // Forgot Password GET
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Forgot Password POST
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists (replace with your user lookup logic)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    // Generate unique token
                    var token = Guid.NewGuid().ToString();

                    // Store token in database
                    var resetToken = new PasswordResetToken
                    {
                        Email = model.Email,
                        Token = token
                    };

                    _context.PasswordResetTokens.Add(resetToken);
                    await _context.SaveChangesAsync();

                    // Generate reset link
                    var resetLink = Url.Action("ResetPassword", "Account",
                        new { email = model.Email, token = token },
                        protocol: HttpContext.Request.Scheme);

                    // Send email
                    _emailService.SendPasswordResetEmail(model.Email, user.FullName, resetLink);
                }

                // Always show confirmation (security best practice)
                return View("ForgotPasswordConfirmation");
            }
            return View(model);
        }

        // Reset Password GET
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email, string token)
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.Email == email &&
                    t.Token == token &&
                    !t.IsUsed &&
                    t.Expiration > DateTime.UtcNow);

            if (resetToken == null)
            {
                return View("ResetPasswordError");
            }

            return View(new ResetPasswordModel { Email = email, Token = token });
        }

        // Reset Password POST
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.Email == model.Email &&
                    t.Token == model.Token &&
                    !t.IsUsed &&
                    t.Expiration > DateTime.UtcNow);

            if (resetToken == null)
            {
                ModelState.AddModelError("", "Invalid or expired token");
                return View(model);
            }

            // Update user password (replace with your password update logic)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null)
            {
                user.PasswordHash = HashPassword(model.NewPassword); // Implement your hashing
                resetToken.IsUsed = true;
                await _context.SaveChangesAsync();
                return View("ResetPasswordConfirmation");
            }

            ModelState.AddModelError("", "User not found");
            return View(model);
        }

        private string HashPassword(string newPassword)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Access Denied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}