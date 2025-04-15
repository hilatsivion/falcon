using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FalconBackend.Models;
using FalconBackend.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace FalconBackend.Controllers
{
    [Route("api/mail/filters")]
    [ApiController]
    [Authorize]
    public class FilterController : ControllerBase
    {
        private readonly IFilterService _filterService;

        public FilterController(IFilterService filterService) { _filterService = filterService; }

        private string GetCurrentUserEmail()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userIdClaim)) { throw new UnauthorizedAccessException("User email claim not found."); }
            return userIdClaim;
        }

        [HttpPost]
        public async Task<ActionResult<FilterFolderDto>> CreateFilter([FromBody] FilterFolderCreateDto createDto)
        {
            var userEmail = GetCurrentUserEmail();
            var createdFilterDto = await _filterService.CreateFilterAsync(userEmail, createDto);
            if (createdFilterDto == null) { return BadRequest("Could not create filter."); }
            return CreatedAtAction(nameof(GetFilterById), new { id = createdFilterDto.FilterFolderId }, createdFilterDto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FilterFolderDto>>> GetFilters()
        {
            var userEmail = GetCurrentUserEmail();
            var filters = await _filterService.GetFiltersAsync(userEmail);
            return Ok(filters);
        }

        [HttpGet("{id}", Name = "GetFilterById")]
        [ActionName(nameof(GetFilterById))]
        public async Task<ActionResult<FilterFolderDto>> GetFilterById(int id)
        {
            var userEmail = GetCurrentUserEmail();
            var filterDto = await _filterService.GetFilterByIdAsync(userEmail, id);
            if (filterDto == null)
            { 
                return NotFound();
            }
            return Ok(filterDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFilter(int id)
        {
            var userEmail = GetCurrentUserEmail();
            var success = await _filterService.DeleteFilterAsync(userEmail, id);
            if (!success) { return NotFound(); }
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFilter(int id, [FromBody] FilterFolderUpdateDto updateDto)
        {
            var userEmail = GetCurrentUserEmail();
            var success = await _filterService.UpdateFilterAsync(userEmail, id, updateDto);
            if (!success) { return NotFound("Filter not found, user mismatch, or update conflict."); }
            return NoContent();
        }

        [HttpGet("{id}/emails")]
        public async Task<ActionResult<IEnumerable<EmailSummaryDto>>> GetEmailsByFilter(int id)
        {
            var userEmail = GetCurrentUserEmail();
            var emails = await _filterService.GetEmailsByFilterAsync(userEmail, id);
            return Ok(emails);
        }
    }
}