using FalconBackend.Services; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; 
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FalconBackend.Controllers
{
    [ApiController] 
    [Route("api/analytics")] 
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsService _analyticsService; 

        public AnalyticsController(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAnalytics()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var analyticsData = await _analyticsService.GetAnalyticsForUserAsync(userEmail);

                 if (analyticsData == null)
                        {
                            return NotFound("Analytics data not found for this user.");
                        }

                    // Return the fetched analytics data with a 200 OK status
                    return Ok(analyticsData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching analytics for user {User.FindFirstValue(ClaimTypes.Email) ?? "UNKNOWN"}: {ex.Message}");

                return StatusCode(500, "An error occurred while retrieving analytics data.");
            }
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> RecordHeartbeat()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                await _analyticsService.UpdateTimeSpentAsync(userEmail);


                return Ok(new { message = "Heartbeat recorded." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recording heartbeat for user {User.FindFirstValue(ClaimTypes.Email) ?? "UNKNOWN"}: {ex.Message}");
                // Log the exception ex more formally if needed
                return StatusCode(500, "An error occurred while recording heartbeat.");
            }
        }
    }
}