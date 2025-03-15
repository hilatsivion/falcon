using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        [HttpPost("login")]
        public async Task<IActionResult> LogIn([FromBody] LoginRequest request)
        {
            try
            {
                var token = await _authService.LogInAsync(request.Email, request.Password);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            try
            {
                await _authService.SignUpAsync(request.FullName, request.Username, request.Email, request.Password);
                return Ok("User registered successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            try
            {
                // Extract the email from the JWT token
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                    return Unauthorized("Missing or invalid authentication token.");

                var token = authorizationHeader.Replace("Bearer ", "").Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                var emailClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
                if (string.IsNullOrEmpty(emailClaim))
                    return Unauthorized("Invalid token: Email not found.");

                // Call logout service
                await _authService.LogOutAsync(emailClaim);

                return Ok("User logged out successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to log out. Error: {ex.Message}");
            }
        }

        [HttpGet("validate")]
        [Authorize]
        public async Task<IActionResult> AuthenticateUser()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _authService.AuthenticateUserAsync(token);

            if (user == null)
                return Unauthorized("Invalid token");

            return Ok(new { Email = user.Email, Username = user.Username });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                    return Unauthorized("Missing or invalid authentication token.");

                var token = authorizationHeader.Replace("Bearer ", "").Trim();
                var userProfile = await _authService.GetUserProfileAsync(token);

                if (userProfile is null)
                    return Unauthorized("Invalid or expired token.");

                if (userProfile.GetType().GetProperty("Error") != null)
                    return Unauthorized(userProfile);

                return Ok(userProfile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve user profile. Error: {ex.Message}");
            }
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class SignUpRequest
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
