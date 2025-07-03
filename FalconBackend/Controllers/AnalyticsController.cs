using FalconBackend.Services; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; 
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Get email category breakdown for the current month as percentages
        /// </summary>
        /// <returns>Array of category objects with name and percentage value</returns>
        /// <response code="200">Returns the category breakdown data</response>
        /// <response code="401">If the user is not authenticated or token is invalid</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("email-category-breakdown")]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmailCategoryBreakdown()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var categoryBreakdown = await _analyticsService.GetEmailCategoryBreakdownAsync(userEmail);

                return Ok(categoryBreakdown);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching email category breakdown for user {User.FindFirstValue(ClaimTypes.Email) ?? "UNKNOWN"}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving email category breakdown.");
            }
        }

        /// <summary>
        /// Get emails by time of day analytics for the current week (last 7 days)
        /// </summary>
        /// <returns>Array of time range objects with range and average count</returns>
        /// <response code="200">Returns the time of day analytics data</response>
        /// <response code="401">If the user is not authenticated or token is invalid</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("emails-by-time-of-day")]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmailsByTimeOfDay()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var timeOfDayData = await _analyticsService.GetEmailsByTimeOfDayAsync(userEmail);

                return Ok(timeOfDayData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching emails by time of day for user {User.FindFirstValue(ClaimTypes.Email) ?? "UNKNOWN"}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving emails by time of day analytics.");
            }
        }

        /// <summary>
        /// Get top 5 email senders for the last 7 days
        /// </summary>
        /// <returns>Array of sender objects with sender email and count</returns>
        /// <response code="200">Returns the top senders data</response>
        /// <response code="401">If the user is not authenticated or token is invalid</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("top-senders")]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTopSenders()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var topSendersData = await _analyticsService.GetTopSendersAsync(userEmail);

                return Ok(topSendersData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching top senders for user {User.FindFirstValue(ClaimTypes.Email) ?? "UNKNOWN"}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving top senders analytics.");
            }
        }
    }
}