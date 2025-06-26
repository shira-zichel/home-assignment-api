using AutoMapper;
using FluentAssertions;
using HomeAssignment.DTOs;
using HomeAssignment.Models;
using HomeAssignment.Repositories;
using HomeAssignment.Services;
using Moq;
using Org.BouncyCastle.Crypto;
using Xunit;

namespace HomeAssignment.Tests.Services
{
    public class DataServiceTests
    {
        private readonly Mock<IDataRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly DataService _dataService;

        public DataServiceTests()
        {
            _mockRepository = new Mock<IDataRepository>();
            _mockMapper = new Mock<IMapper>();
            _dataService = new DataService(_mockRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnDataItemDto()
        {
            // Arrange
            var dataItem = new DataItem
            {
                Id = 1,
                Value = "Test Data",
                CreatedAt = DateTime.UtcNow
            };
            var expectedDto = new DataItemDto
            {
                Id = 1,
                Value = "Test Data",
                CreatedAt = dataItem.CreatedAt
            };

            _mockRepository.Setup(x => x.GetByIdAsync(1))
                          .ReturnsAsync(dataItem);
            _mockMapper.Setup(x => x.Map<DataItemDto>(dataItem))
                      .Returns(expectedDto);

            // Act
            var result = await _dataService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto);
            _mockRepository.Verify(x => x.GetByIdAsync(1), Times.Once);
            _mockMapper.Verify(x => x.Map<DataItemDto>(dataItem), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetByIdAsync(999))
                          .ReturnsAsync((DataItem)null);

            // Act
            var result = await _dataService.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
            _mockRepository.Verify(x => x.GetByIdAsync(999), Times.Once);
            _mockMapper.Verify(x => x.Map<DataItemDto>(It.IsAny<DataItem>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_WithZeroId_ShouldReturnNull()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetByIdAsync(0))
                          .ReturnsAsync((DataItem)null);

            // Act
            var result = await _dataService.GetByIdAsync(0);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithNegativeId_ShouldReturnNull()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetByIdAsync(-1))
                          .ReturnsAsync((DataItem)null);

            // Act
            var result = await _dataService.GetByIdAsync(-1);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_WithValidDto_ShouldReturnCreatedItem()
        {
            // Arrange
            var createDto = new CreateDataItemDto { Value = "New Item" };
            var dataItem = new DataItem { Value = "New Item" };
            var createdItem = new DataItem
            {
                Id = 1,
                Value = "New Item",
                CreatedAt = DateTime.UtcNow
            };
            var expectedDto = new DataItemDto
            {
                Id = 1,
                Value = "New Item",
                CreatedAt = createdItem.CreatedAt
            };

            _mockMapper.Setup(x => x.Map<DataItem>(createDto))
                      .Returns(dataItem);
            _mockRepository.Setup(x => x.CreateAsync(dataItem))
                          .ReturnsAsync(createdItem);
            _mockMapper.Setup(x => x.Map<DataItemDto>(createdItem))
                      .Returns(expectedDto);

            // Act
            var result = await _dataService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto);
            _mockMapper.Verify(x => x.Map<DataItem>(createDto), Times.Once);
            _mockRepository.Verify(x => x.CreateAsync(dataItem), Times.Once);
            _mockMapper.Verify(x => x.Map<DataItemDto>(createdItem), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithEmptyValue_ShouldStillCreateItem()
        {
            // Arrange
            var createDto = new CreateDataItemDto { Value = "" };
            var dataItem = new DataItem { Value = "" };
            var createdItem = new DataItem
            {
                Id = 1,
                Value = "",
                CreatedAt = DateTime.UtcNow
            };
            var expectedDto = new DataItemDto
            {
                Id = 1,
                Value = "",
                CreatedAt = createdItem.CreatedAt
            };

            _mockMapper.Setup(x => x.Map<DataItem>(createDto))
                      .Returns(dataItem);
            _mockRepository.Setup(x => x.CreateAsync(dataItem))
                          .ReturnsAsync(createdItem);
            _mockMapper.Setup(x => x.Map<DataItemDto>(createdItem))
                      .Returns(expectedDto);

            // Act
            var result = await _dataService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be("");
        }

        [Fact]
        public async Task CreateAsync_WithLongValue_ShouldCreateItem()
        {
            // Arrange
            var longValue = new string('A', 500);
            var createDto = new CreateDataItemDto { Value = longValue };
            var dataItem = new DataItem { Value = longValue };
            var createdItem = new DataItem
            {
                Id = 1,
                Value = longValue,
                CreatedAt = DateTime.UtcNow
            };
            var expectedDto = new DataItemDto
            {
                Id = 1,
                Value = longValue,
                CreatedAt = createdItem.CreatedAt
            };

            _mockMapper.Setup(x => x.Map<DataItem>(createDto))
                      .Returns(dataItem);
            _mockRepository.Setup(x => x.CreateAsync(dataItem))
                          .ReturnsAsync(createdItem);
            _mockMapper.Setup(x => x.Map<DataItemDto>(createdItem))
                      .Returns(expectedDto);

            // Act
            var result = await _dataService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be(longValue);
        }

        [Fact]
        public async Task UpdateAsync_WithExistingId_ShouldReturnUpdatedItem()
        {
            // Arrange
            var updateDto = new UpdateDataItemDto { Value = "Updated Value" };
            var dataItem = new DataItem { Value = "Updated Value" };
            var updatedItem = new DataItem
            {
                Id = 1,
                Value = "Updated Value",
                CreatedAt = DateTime.UtcNow
            };
            var expectedDto = new DataItemDto
            {
                Id = 1,
                Value = "Updated Value",
                CreatedAt = updatedItem.CreatedAt
            };

            _mockMapper.Setup(x => x.Map<DataItem>(updateDto))
                      .Returns(dataItem);
            _mockRepository.Setup(x => x.UpdateAsync(1, dataItem))
                          .ReturnsAsync(updatedItem);
            _mockMapper.Setup(x => x.Map<DataItemDto>(updatedItem))
                      .Returns(expectedDto);

            // Act
            var result = await _dataService.UpdateAsync(1, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto);
            _mockMapper.Verify(x => x.Map<DataItem>(updateDto), Times.Once);
            _mockRepository.Verify(x => x.UpdateAsync(1, dataItem), Times.Once);
            _mockMapper.Verify(x => x.Map<DataItemDto>(updatedItem), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var updateDto = new UpdateDataItemDto { Value = "Updated Value" };
            var dataItem = new DataItem { Value = "Updated Value" };

            _mockMapper.Setup(x => x.Map<DataItem>(updateDto))
                      .Returns(dataItem);
            _mockRepository.Setup(x => x.UpdateAsync(999, dataItem))
                          .ReturnsAsync((DataItem)null);

            // Act
            var result = await _dataService.UpdateAsync(999, updateDto);

            // Assert
            result.Should().BeNull();
            _mockRepository.Verify(x => x.UpdateAsync(999, dataItem), Times.Once);
            _mockMapper.Verify(x => x.Map<DataItemDto>(It.IsAny<DataItem>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllItems()
        {
            // Arrange
            var dataItems = new List<DataItem>
            {
                new DataItem { Id = 1, Value = "Item 1", CreatedAt = DateTime.UtcNow },
                new DataItem { Id = 2, Value = "Item 2", CreatedAt = DateTime.UtcNow }
            };

            var expectedDtos = new List<DataItemDto>
            {
                new DataItemDto { Id = 1, Value = "Item 1", CreatedAt = dataItems[0].CreatedAt },
                new DataItemDto { Id = 2, Value = "Item 2", CreatedAt = dataItems[1].CreatedAt }
            };

            _mockRepository.Setup(x => x.GetAllAsync())
                          .ReturnsAsync(dataItems);
            _mockMapper.Setup(x => x.Map<IEnumerable<DataItemDto>>(dataItems))
                      .Returns(expectedDtos);

            // Act
            var result = await _dataService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expectedDtos);
            _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockMapper.Verify(x => x.Map<IEnumerable<DataItemDto>>(dataItems), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithEmptyRepository_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyItems = new List<DataItem>();
            var emptyDtos = new List<DataItemDto>();

            _mockRepository.Setup(x => x.GetAllAsync())
                          .ReturnsAsync(emptyItems);
            _mockMapper.Setup(x => x.Map<IEnumerable<DataItemDto>>(emptyItems))
                      .Returns(emptyDtos);

            // Act
            var result = await _dataService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var createDto = new CreateDataItemDto { Value = "Test Item" };
            var dataItem = new DataItem { Value = "Test Item" };

            _mockMapper.Setup(x => x.Map<DataItem>(createDto))
                      .Returns(dataItem);
            _mockRepository.Setup(x => x.CreateAsync(dataItem))
                          .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _dataService.CreateAsync(createDto));
        }

        [Fact]
        public async Task GetByIdAsync_WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetByIdAsync(1))
                          .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _dataService.GetByIdAsync(1));
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var updateDto = new UpdateDataItemDto { Value = "Updated Value" };
            var dataItem = new DataItem { Value = "Updated Value" };

            _mockMapper.Setup(x => x.Map<DataItem>(updateDto))
                      .Returns(dataItem);
            _mockRepository.Setup(x => x.UpdateAsync(1, dataItem))
                          .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _dataService.UpdateAsync(1, updateDto));
        }

        [Fact]
        public async Task GetAllAsync_WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetAllAsync())
                          .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _dataService.GetAllAsync());
        }
    }
}
