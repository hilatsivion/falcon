using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                var userEmail = User.Identity.Name; 
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
    }

    public class SaveUserTagsRequest
    {
        public List<string> Tags { get; set; }
    }
}
