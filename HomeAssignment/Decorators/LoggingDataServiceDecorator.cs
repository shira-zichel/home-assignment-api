using HomeAssignment.DTOs;
using HomeAssignment.Services;
using System.Diagnostics;

namespace HomeAssignment.Decorators
{
    public class LoggingDataServiceDecorator : IDataService
    {
        private readonly IDataService _dataService;
        private readonly ILogger<LoggingDataServiceDecorator> _logger;

        public LoggingDataServiceDecorator(IDataService dataService, ILogger<LoggingDataServiceDecorator> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        public async Task<DataItemDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Starting GetByIdAsync for ID: {Id}", id);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _dataService.GetByIdAsync(id);
                stopwatch.Stop();

                if (result != null)
                {
                    _logger.LogInformation("Successfully retrieved data for ID: {Id} in {ElapsedMs}ms",
                        id, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("No data found for ID: {Id} in {ElapsedMs}ms",
                        id, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error retrieving data for ID: {Id} in {ElapsedMs}ms",
                    id, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<IEnumerable<DataItemDto>> GetAllAsync()
        {
            _logger.LogInformation("Starting GetAllAsync");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _dataService.GetAllAsync();
                stopwatch.Stop();

                _logger.LogInformation("Successfully retrieved {Count} items in {ElapsedMs}ms",
                    result.Count(), stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error retrieving all data in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<DataItemDto> CreateAsync(CreateDataItemDto createDto)
        {
            _logger.LogInformation("Starting CreateAsync for value: {Value}", createDto.Value);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _dataService.CreateAsync(createDto);
                stopwatch.Stop();

                _logger.LogInformation("Successfully created item with ID: {Id} in {ElapsedMs}ms",
                    result.Id, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error creating item in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<DataItemDto?> UpdateAsync(int id, UpdateDataItemDto updateDto)
        {
            _logger.LogInformation("Starting UpdateAsync for ID: {Id}", id);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _dataService.UpdateAsync(id, updateDto);
                stopwatch.Stop();

                if (result != null)
                {
                    _logger.LogInformation("Successfully updated item with ID: {Id} in {ElapsedMs}ms",
                        id, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("No item found to update for ID: {Id} in {ElapsedMs}ms",
                        id, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error updating item with ID: {Id} in {ElapsedMs}ms",
                    id, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        
    }
}