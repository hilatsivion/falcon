using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FalconBackend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("save-tags")]
        public async Task<IActionResult> SaveUserTags([FromBody] SaveUserTagsRequest request)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized("Invalid user token");

                await _userService.SaveUserTagsAsync(userEmail, request.Tags);
                return Ok("User tags saved successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to save user tags. Error: {ex.Message}");
            }
        }

        // Create a new tag (User-created tags)
        [HttpPost("create")]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
        {
            try
            {
                // Ensure user is authenticated
                if (User.Identity == null || string.IsNullOrEmpty(User.Identity.Name))
                    return Unauthorized("Invalid user token");

                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrWhiteSpace(request.TagName))
                    return BadRequest("Tag name cannot be empty.");

                await _userService.CreateUserTagAsync(userEmail, request.TagName);

                return Ok(new { message = $"Tag '{request.TagName}' created successfully." });
            }
            catch (Exception ex) when (ex.Message.Contains("Tag already exists"))
            {
                return BadRequest(new { error = "Tag already exists." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create tag.", details = ex.Message });
            }
        }


        // Get all tags (System-defined & User-created)
        [HttpGet("tags")]
        public async Task<IActionResult> GetAllTags()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized("Invalid user token");

                var tags = await _userService.GetAllTagsAsync(userEmail);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve tags. Error: {ex.Message}");
            }
        }

        [HttpGet("mailaccounts")] 
        public async Task<IActionResult> GetUserMailAccounts()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var mailAccounts = await _userService.GetUserMailAccountsAsync(userEmail);
                return Ok(mailAccounts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to retrieve user mail accounts.");
            }
        }


        //remove later

        [HttpPost("initialize-account")]
        [Authorize]
        public async Task<IActionResult> InitializeAccountData()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email); 
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                Console.WriteLine($"--- Endpoint /initialize-account called for user: {userEmail} ---");

                await _userService.InitializeDummyUserDataAsync(userEmail);

                Console.WriteLine($"--- Endpoint /initialize-account finished for user: {userEmail} ---");
                return Ok(new { message = "Account initialization process completed." }); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Error in /initialize-account for user {User.FindFirstValue(ClaimTypes.Email)}: {ex.Message} ---");
                // Log the exception ex
                return StatusCode(500, $"Failed to initialize account data. Error: {ex.Message}");
            }
        }
    }
}
