using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using HomeAssignment.DTOs;
using HomeAssignment.Services;
using System.Security.Claims;

namespace HomeAssignment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DataController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly IValidator<CreateDataItemDto> _createValidator;
        private readonly IValidator<UpdateDataItemDto> _updateValidator;
        private readonly IValidator<int> _idValidator;
        private readonly ILogger<DataController> _logger;

        public DataController(
            IDataService dataService,
            IValidator<CreateDataItemDto> createValidator,
            IValidator<UpdateDataItemDto> updateValidator,
            IValidator<int> idValidator,
            ILogger<DataController> logger)
        {
            _dataService = dataService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _idValidator = idValidator;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetById(int id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var idValidation = await _idValidator.ValidateAsync(id);
            if (!idValidation.IsValid)
            {
                return BadRequest(idValidation.Errors.Select(e => e.ErrorMessage));
            }

            _logger.LogInformation("User {Username} ({Role}) requesting data item with ID: {Id}", username, role, id);

            var itemDto = await _dataService.GetByIdAsync(id);
            if (itemDto == null)
            {
                return NotFound(new { Message = $"Data item with ID {id} not found" });
            }

            return Ok(itemDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateDataItemDto createDto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation("User {Username} ({Role}) attempting to create data item", username, role);

            var validation = await _createValidator.ValidateAsync(createDto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            _logger.LogInformation("Admin user {Username} creating new data item with value: {Value}", username, createDto.Value);

            var createdDto = await _dataService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDataItemDto updateDto)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation("User {Username} ({Role}) attempting to update data item ID: {Id}", username, role, id);

            var idValidation = await _idValidator.ValidateAsync(id);
            if (!idValidation.IsValid)
            {
                return BadRequest(idValidation.Errors.Select(e => e.ErrorMessage));
            }

            var validation = await _updateValidator.ValidateAsync(updateDto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            _logger.LogInformation("Admin user {Username} updating data item ID: {Id} with value: {Value}", username, id, updateDto.Value);

            var updatedDto = await _dataService.UpdateAsync(id, updateDto);
            if (updatedDto == null)
            {
                return NotFound(new { Message = $"Data item with ID {id} not found" });
            }

            return Ok(updatedDto);
        }
    }
}
