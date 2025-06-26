using AutoMapper;
using HomeAssignment.DTOs;
using HomeAssignment.Models;
using HomeAssignment.Repositories;
using HomeAssignment.Services;

namespace HomeAssignment.Services
{
    public class DataService : IDataService
    {
        private readonly IDataRepository _repository;
        private readonly IMapper _mapper;

        public DataService(IDataRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<DataItemDto?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item != null ? _mapper.Map<DataItemDto>(item) : null;
        }

        public async Task<IEnumerable<DataItemDto>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<DataItemDto>>(items);
        }

        public async Task<DataItemDto> CreateAsync(CreateDataItemDto createDto)
        {
            var dataItem = _mapper.Map<DataItem>(createDto);
            var createdItem = await _repository.CreateAsync(dataItem);
            return _mapper.Map<DataItemDto>(createdItem);
        }

        public async Task<DataItemDto?> UpdateAsync(int id, UpdateDataItemDto updateDto)
        {
            var dataItem = _mapper.Map<DataItem>(updateDto);
            var updatedItem = await _repository.UpdateAsync(id, dataItem);
            return updatedItem != null ? _mapper.Map<DataItemDto>(updatedItem) : null;
        }

       
    }
}