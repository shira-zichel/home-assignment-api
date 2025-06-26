using HomeAssignment.DTOs;


namespace HomeAssignment.Services
{
    public interface IDataService
    {
        Task<DataItemDto?> GetByIdAsync(int id);
        Task<IEnumerable<DataItemDto>> GetAllAsync();
        Task<DataItemDto> CreateAsync(CreateDataItemDto createDto);
        Task<DataItemDto?> UpdateAsync(int id, UpdateDataItemDto updateDto);
    }
}