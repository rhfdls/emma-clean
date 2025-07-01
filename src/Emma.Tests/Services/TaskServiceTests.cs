using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Emma.Models.Enums;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Tests.Services
{
    public class TaskServiceTests : IDisposable
    {
        private readonly Mock<IAppDbContext> _mockContext;
        private readonly Mock<IContactAccessService> _mockContactAccessService;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly TaskService _taskService;
        private readonly Mock<DbSet<TaskItem>> _mockTaskSet;
        private readonly Mock<DbSet<Contact>> _mockContactSet;
        private readonly Mock<DbSet<User>> _mockUserSet;
        private readonly List<TaskItem> _tasks;
        private readonly List<Contact> _contacts;
        private readonly List<User> _users;

        public TaskServiceTests()
        {
            _mockContext = new Mock<IAppDbContext>();
            _mockContactAccessService = new Mock<IContactAccessService>();
            _mockUserContext = new Mock<IUserContext>();
            var mockLogger = new Mock<ILogger<TaskService>>();
            
            // Setup test data
            _users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Email = "test1@example.com", FirstName = "Test", LastName = "User1" },
                new User { Id = Guid.NewGuid(), Email = "test2@example.com", FirstName = "Test", LastName = "User2" },
                new User { Id = Guid.NewGuid(), Email = "system@emma.ai", FirstName = "System", LastName = "User" }
            };
            
            _mockUserContext.Setup(u => u.SystemUserId).Returns(_users[2].Id);
            
            _contacts = new List<Contact>
            {
                new Contact { Id = "contact1", FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
                new Contact { Id = "contact2", FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" }
            };
            
            _tasks = new List<TaskItem>
            {
                new TaskItem 
                { 
                    Id = "task1", 
                    Title = "Follow up with John", 
                    Description = "Call about the property", 
                    ContactId = "contact1",
                    AssignedToUserId = _users[0].Id,
                    Status = TaskStatus.Pending,
                    DueDate = DateTime.UtcNow.AddDays(1),
                    CreatedByUserId = _users[0].Id
                },
                new TaskItem 
                { 
                    Id = "task2", 
                    Title = "Send documents to Jane", 
                    Description = "Email the signed paperwork", 
                    ContactId = "contact2",
                    AssignedToUserId = _users[1].Id,
                    Status = TaskStatus.InProgress,
                    DueDate = DateTime.UtcNow.AddDays(-1), // Overdue
                    CreatedByUserId = _users[1].Id
                },
                new TaskItem 
                { 
                    Id = "task3", 
                    Title = "Schedule showing", 
                    Description = "Find time for property viewing", 
                    ContactId = "contact1",
                    Status = TaskStatus.Completed,
                    DueDate = DateTime.UtcNow.AddDays(-2),
                    CreatedByUserId = _users[0].Id,
                    CompletedAt = DateTime.UtcNow.AddDays(-1)
                },
                new TaskItem 
                { 
                    Id = "task4", 
                    Title = "Review offer", 
                    Description = "Review and respond to the offer", 
                    ContactId = "contact2",
                    Status = TaskStatus.PendingApproval,
                    DueDate = DateTime.UtcNow.AddDays(2),
                    CreatedByUserId = _users[1].Id,
                    NbaRecommendationId = "nba1",
                    RequiresApproval = true
                }
            };
            
            // Setup mock DbSets
            _mockTaskSet = TestHelpers.GetMockDbSet(_tasks.AsQueryable());
            _mockContactSet = TestHelpers.GetMockDbSet(_contacts.AsQueryable());
            _mockUserSet = TestHelpers.GetMockDbSet(_users.AsQueryable());
            
            _mockContext.Setup(c => c.TaskItems).Returns(_mockTaskSet.Object);
            _mockContext.Setup(c => c.Contacts).Returns(_mockContactSet.Object);
            _mockContext.Setup(c => c.Users).Returns(_mockUserSet.Object);
            
            // Setup default access control - user 0 can access contact1, user 1 can access contact2
            _mockContactAccessService
                .Setup(s => s.CanAccessContactAsync(It.IsAny<string>(), _users[0].Id))
                .ReturnsAsync((string contactId, Guid _) => contactId == "contact1");
                
            _mockContactAccessService
                .Setup(s => s.CanAccessContactAsync(It.IsAny<string>(), _users[1].Id))
                .ReturnsAsync((string contactId, Guid _) => contactId == "contact2");
                
            _mockContactAccessService
                .Setup(s => s.GetAccessibleContactIdsAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid userId) => 
                    userId == _users[0].Id ? new[] { "contact1" } : 
                    userId == _users[1].Id ? new[] { "contact2" } : 
                    Array.Empty<string>());
            
            // Setup SaveChanges to update the in-memory collections
            _mockContext.Setup(c => c.SaveChangesAsync(default))
                .Callback(() => { })
                .ReturnsAsync(1);
            
            _taskService = new TaskService(
                _mockContext.Object, 
                _mockContactAccessService.Object, 
                mockLogger.Object,
                _mockUserContext.Object);
        }
        
        public void Dispose()
        {
            // Clean up if needed
        }
        
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_WhenUserHasAccess()
        {
            // Arrange
            var taskId = "task1";
            var userId = _users[0].Id;
            
            // Act
            var result = await _taskService.GetTaskByIdAsync(taskId, userId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(taskId, result.Id);
            _mockContactAccessService.Verify(
                s => s.CanAccessContactAsync(result.ContactId, userId), 
                Times.Once);
        }
        
        [Fact]
        public async Task GetTaskByIdAsync_ShouldThrow_WhenUserNoAccess()
        {
            // Arrange
            var taskId = "task1";
            var userId = _users[1].Id; // User 1 doesn't have access to contact1
            
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _taskService.GetTaskByIdAsync(taskId, userId));
        }
        
        [Fact]
        public async Task CreateTaskAsync_ShouldCreateTask_WithValidData()
        {
            // Arrange
            var newTask = new TaskItem
            {
                Title = "New Task",
                Description = "Test description",
                ContactId = "contact1",
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = TaskStatus.Pending,
                Priority = TaskPriority.High
            };
            var userId = _users[0].Id;
            
            // Setup to capture the added task
            TaskItem? addedTask = null;
            _mockTaskSet.Setup(m => m.Add(It.IsAny<TaskItem>()))
                .Callback<TaskItem>(t => addedTask = t);
            
            // Act
            var result = await _taskService.CreateTaskAsync(newTask, userId);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(string.Empty, result.Id);
            Assert.Equal(userId, result.CreatedByUserId);
            Assert.NotNull(addedTask);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }
        
        [Fact]
        public async Task UpdateTaskStatusAsync_ShouldUpdateStatus_WhenUserHasAccess()
        {
            // Arrange
            var taskId = "task1";
            var newStatus = TaskStatus.InProgress;
            var userId = _users[0].Id;
            var notes = "Starting work on this";
            
            // Act
            var result = await _taskService.UpdateTaskStatusAsync(taskId, newStatus, userId, notes);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(newStatus, result.Status);
            Assert.NotNull(result.Metadata["StatusHistory"]);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }
        
        [Fact]
        public async Task AssignTaskAsync_ShouldAssignTask_WhenUserHasAccess()
        {
            // Arrange
            var taskId = "task1";
            var assigneeId = _users[1].Id; // Different user
            var assignedById = _users[0].Id;
            
            // Act
            var result = await _taskService.AssignTaskAsync(taskId, assigneeId, assignedById, "Reassigning");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(assigneeId, result.AssignedToUserId);
            Assert.NotNull(result.Metadata["AssignmentHistory"]);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }
        
        [Fact]
        public async Task GetOverdueTasksAsync_ShouldReturnOnlyAccessibleTasks()
        {
            // Arrange
            var userId = _users[1].Id; // User 1 has access to contact2
            
            // Act
            var result = await _taskService.GetOverdueTasksAsync(userId);
            
            // Assert
            Assert.Single(result); // Should only return task2 which is overdue and accessible
            Assert.Equal("task2", result.First().Id);
        }
        
        [Fact]
        public async Task CreateTaskFromNbaRecommendation_ShouldCreateTask_WithNbaMetadata()
        {
            // Arrange
            var recommendationId = "nba-rec-123";
            var contactId = "contact1";
            var title = "NBA Recommended Task";
            var description = "Generated by NBA system";
            var dueDate = DateTime.UtcNow.AddDays(3);
            var confidenceScore = 0.85;
            
            // Setup to capture the added task
            TaskItem? addedTask = null;
            _mockTaskSet.Setup(m => m.Add(It.IsAny<TaskItem>()))
                .Callback<TaskItem>(t => addedTask = t);
            
            // Act
            var result = await _taskService.CreateTaskFromNbaRecommendationAsync(
                recommendationId, contactId, title, description, dueDate, confidenceScore, true);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(TaskStatus.PendingApproval, result.Status);
            Assert.True(result.RequiresApproval);
            Assert.Equal(confidenceScore, result.ConfidenceScore);
            Assert.NotNull(result.Metadata["NbaRecommendation"]);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }
    }
}
