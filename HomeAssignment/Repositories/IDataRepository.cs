using HomeAssignment.Models;


namespace HomeAssignment.Repositories
{
    public interface IDataRepository
    {
        Task<DataItem?> GetByIdAsync(int id);
        Task<IEnumerable<DataItem>> GetAllAsync();
        Task<DataItem> CreateAsync(DataItem dataItem);
        Task<DataItem?> UpdateAsync(int id, DataItem dataItem);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
