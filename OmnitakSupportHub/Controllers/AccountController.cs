using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using OmnitakSupportHub.Services;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers
{
    public class AccountController : Controller
    {
        // ==============================
        // Brute Force Protection (simple in-memory lockout)
        // ==============================
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

        // ==============================
        // Dependencies
        // ==============================
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

        // ==============================
        // Login
        // ==============================
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

            var loginModel = new LoginModel
            {
                Email = model.Email,
                Password = model.Password
            };

            var result = await _authService.LoginAsync(loginModel);

            if (result.Success && result.User != null)
            {
                ResetFailedAttempts(model.Email);

                // Build claims
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

                // Add role and permissions
                if (result.User.Role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, result.User.Role.RoleName));

                    if (result.User.Role.CanApproveUsers) claims.Add(new Claim("Permission", "CanApproveUsers"));
                    if (result.User.Role.CanManageTickets) claims.Add(new Claim("Permission", "CanManageTickets"));
                    if (result.User.Role.CanViewAllTickets) claims.Add(new Claim("Permission", "CanViewAllTickets"));
                    if (result.User.Role.CanManageKnowledgeBase) claims.Add(new Claim("Permission", "CanManageKnowledgeBase"));
                    if (result.User.Role.CanViewReports) claims.Add(new Claim("Permission", "CanViewReports"));
                    if (result.User.Role.CanManageTeams) claims.Add(new Claim("Permission", "CanManageTeams"));
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
                return result.User.Role?.RoleName switch
                {
                    "Administrator" => RedirectToAction("Index", "AdminDashboard"),
                    "Support Manager" => RedirectToAction("Index", "ManagerDashboard"),
                    "Support Agent" => RedirectToAction("Index", "AgentDashboard"),
                    _ => RedirectToAction("Index", "UserDashboard")
                };
            }

            RecordFailedAttempt(model.Email);
            _logger.LogWarning("Failed login attempt for user: {Email}", model.Email);
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        // ==============================
        // Register
        // ==============================
        [HttpGet]
        public IActionResult Register()
        {
            var departments = _context.Departments.ToList();

            var model = new RegisterViewModel
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
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableDepartments = _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentName,
                        Text = d.DepartmentName
                    }).ToList();
                return View(model);
            }

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

            model.AvailableDepartments = _context.Departments
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentName,
                    Text = d.DepartmentName
                }).ToList();

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        // ==============================
        // Forgot Password
        // ==============================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    var token = Guid.NewGuid().ToString();

                    var resetToken = new PasswordResetToken
                    {
                        Email = model.Email,
                        Token = token
                    };

                    _context.PasswordResetTokens.Add(resetToken);
                    await _context.SaveChangesAsync();

                    var resetLink = Url.Action("ResetPassword", "Account",
                        new { email = model.Email, token = token },
                        protocol: HttpContext.Request.Scheme);

                    _emailService.SendPasswordResetEmail(model.Email, user.FullName, resetLink);
                }

                return View("ForgotPasswordConfirmation");
            }
            return View(model);
        }

        // ==============================
        // Reset Password
        // ==============================
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null)
            {
                user.PasswordHash = HashPassword(model.NewPassword); // TODO: implement hashing
                resetToken.IsUsed = true;
                await _context.SaveChangesAsync();
                return View("ResetPasswordConfirmation");
            }

            ModelState.AddModelError("", "User not found");
            return View(model);
        }

        private string HashPassword(string newPassword)
        {
            throw new NotImplementedException("⚠️ Implement password hashing here!");
        }

        // ==============================
        // Logout
        // ==============================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ==============================
        // Access Denied
        // ==============================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
