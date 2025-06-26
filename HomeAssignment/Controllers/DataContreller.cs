using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using FluentValidation;
using HomeAssignment.DTOs;
using HomeAssignment.Services;
using System.Security.Claims;

namespace HomeAssignment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 ALL endpoints require authentication
    public class DataController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDataItemDto> _createValidator;
        private readonly IValidator<UpdateDataItemDto> _updateValidator;
        private readonly IValidator<int> _idValidator;
        private readonly ILogger<DataController> _logger;

        public DataController(
            IDataService dataService,
            IMapper mapper,
            IValidator<CreateDataItemDto> createValidator,
            IValidator<UpdateDataItemDto> updateValidator,
            IValidator<int> idValidator,
            ILogger<DataController> logger)
        {
            _dataService = dataService;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _idValidator = idValidator;
            _logger = logger;
        }

        /// <summary>
        /// Get data item by ID (Available to both Admin and User roles)
        /// </summary>
        /// <param name="id">Item ID</param>
        /// <returns>Data item if found</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")] // 🔒 Both roles can read
        public async Task<IActionResult> GetById(int id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Validate ID
            var idValidation = await _idValidator.ValidateAsync(id);
            if (!idValidation.IsValid)
            {
                return BadRequest(idValidation.Errors.Select(e => e.ErrorMessage));
            }

            _logger.LogInformation("User {Username} ({Role}) requesting data item with ID: {Id}", username, role, id);

            var item = await _dataService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { Message = $"Data item with ID {id} not found" });
            }

            var itemDto = _mapper.Map<DataItemDto>(item);
            return Ok(itemDto);
        }

        /// <summary>
        /// Create a new data item (Admin only)
        /// </summary>
        /// <param name="createDto">Data item to create</param>
        /// <returns>Created data item</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")] // 🔒 Only Admin can create
        public async Task<IActionResult> Create([FromBody] CreateDataItemDto createDto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation("User {Username} ({Role}) attempting to create data item", username, role);

            // Validate input
            var validation = await _createValidator.ValidateAsync(createDto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            _logger.LogInformation("Admin user {Username} creating new data item with value: {Value}", username, createDto.Value);

            var createdItem = await _dataService.CreateAsync(createDto);
            var itemDto = _mapper.Map<DataItemDto>(createdItem);

            return CreatedAtAction(nameof(GetById), new { id = itemDto.Id }, itemDto);
        }

        /// <summary>
        /// Update an existing data item (Admin only)
        /// </summary>
        /// <param name="id">Item ID to update</param>
        /// <param name="updateDto">Updated data</param>
        /// <returns>Updated data item</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // 🔒 Only Admin can update
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDataItemDto updateDto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation("User {Username} ({Role}) attempting to update data item ID: {Id}", username, role, id);

            // Validate ID
            var idValidation = await _idValidator.ValidateAsync(id);
            if (!idValidation.IsValid)
            {
                return BadRequest(idValidation.Errors.Select(e => e.ErrorMessage));
            }

            // Validate input
            var validation = await _updateValidator.ValidateAsync(updateDto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            _logger.LogInformation("Admin user {Username} updating data item ID: {Id} with value: {Value}", username, id, updateDto.Value);

            var updatedItem = await _dataService.UpdateAsync(id, updateDto);
            if (updatedItem == null)
            {
                return NotFound(new { Message = $"Data item with ID {id} not found" });
            }

            var itemDto = _mapper.Map<DataItemDto>(updatedItem);
            return Ok(itemDto);
        }
    }
}