using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Plugins;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using OmnitakSupportHub.Services;
using LoginRequest = OmnitakSupportHub.Models.ViewModels.LoginRequest;

namespace OmnitakSupportHub.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtService _jwtService;

        public AuthApiController(IAuthService authService, JwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            var loginModel = new LoginModel
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _authService.LoginAsync(loginModel);

            if (!result.Success || result.User == null)
                return Unauthorized(new { message = "Invalid credentials" });

            return new LoginResponse
            {
                Token = _jwtService.GenerateToken(result.User),
                Expiration = DateTime.UtcNow.AddMinutes(60),
                User = new UserDto
                {
                    UserID = result.User.UserID,
                    Email = result.User.Email,
                    FullName = result.User.FullName
                }
            };
        }
    }
}