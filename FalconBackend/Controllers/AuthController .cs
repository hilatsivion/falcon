using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FalconBackend.Models;
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
        public async Task<ActionResult<LoginResponseDto>> LogIn([FromBody] LoginRequest request)
        {
            try
            {
                var loginResponse = await _authService.LogInAsync(request.Email, request.Password);
                return Ok(loginResponse);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("signup")]
        public async Task<ActionResult<LoginResponseDto>> SignUp([FromBody] SignUpRequest request)
        {
            try
            {
                var loginResponse = await _authService.SignUpAsync(request.FullName, request.Username, request.Email, request.Password);
                // Return 201 Created with the response DTO
                return Ok(loginResponse);
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
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (email == null)
                return Unauthorized("Invalid token (email missing)");

            var user = await _authService.GetUserByEmailAsync(email); // <- you can add this simple method

            if (user == null)
                return Unauthorized("User not found");

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
}
