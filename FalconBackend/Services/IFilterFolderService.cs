using FalconBackend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FalconBackend.Services
{
    public interface IFilterService
    {
        Task<FilterFolderDto?> CreateFilterAsync(string userEmail, FilterFolderCreateDto createDto);
        Task<IEnumerable<FilterFolderDto>> GetFiltersAsync(string userEmail);
        Task<FilterFolderDto?> GetFilterByIdAsync(string userEmail, int filterId);
        Task<bool> UpdateFilterAsync(string userEmail, int filterId, FilterFolderUpdateDto updateDto);
        Task<bool> DeleteFilterAsync(string userEmail, int filterId);
        Task<IEnumerable<EmailSummaryDto>> GetEmailsByFilterAsync(string userEmail, int filterId);
    }
}