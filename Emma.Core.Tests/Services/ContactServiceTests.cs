using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Emma.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Services
{
    public class ContactServiceTests : IDisposable
    {
        private readonly Mock<IAppDbContext> _mockContext;
        private readonly Mock<ILogger<ContactService>> _mockLogger;
        private readonly Mock<IContactAccessService> _mockAccessService;
        private readonly ContactService _service;
        private readonly List<Contact> _testContacts;
        private readonly List<User> _testUsers;
        private readonly List<ContactCollaborator> _testCollaborators;
        private readonly List<ContactAssignment> _testAssignments;
        private readonly List<ContactOwnershipTransfer> _testTransfers;

        public ContactServiceTests()
        {
            _mockContext = new Mock<IAppDbContext>();
            _mockLogger = new Mock<ILogger<ContactService>>();
            _mockAccessService = new Mock<IContactAccessService>();

            // Initialize test data
            _testUsers = new List<User>
            {
                new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", IsActive = true },
                new User { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", IsActive = true },
                new User { Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User", IsActive = true, IsAdmin = true }
            };

            _testContacts = new List<Contact>
            {
                new Contact 
                { 
                    Id = Guid.NewGuid(), 
                    FirstName = "Alice", 
                    LastName = "Johnson", 
                    OwnerUserId = _testUsers[0].Id,
                    AssignedToUserId = _testUsers[0].Id,
                    OrganizationId = Guid.NewGuid(),
                    RelationshipState = RelationshipState.Lead,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow
                },
                new Contact 
                { 
                    Id = Guid.NewGuid(), 
                    FirstName = "Bob", 
                    LastName = "Williams",
                    OwnerUserId = _testUsers[1].Id,
                    AssignedToUserId = _testUsers[1].Id,
                    OrganizationId = Guid.NewGuid(),
                    RelationshipState = RelationshipState.Client,
                    IsActive = false,
                    DeletedAt = DateTime.UtcNow,
                    DeletedByUserId = _testUsers[1].Id,
                    DeleteReason = "Test deletion"
                }
            };

            _testCollaborators = new List<ContactCollaborator>
            {
                new ContactCollaborator
                {
                    Id = Guid.NewGuid(),
                    ContactId = _testContacts[0].Id,
                    CollaboratorUserId = _testUsers[1].Id,
                    CollaborationType = ContactCollaborationType.ReadOnly,
                    IsActive = true,
                    AddedAt = DateTime.UtcNow.AddDays(-5),
                    AddedByUserId = _testUsers[0].Id
                }
            };

            _testAssignments = new List<ContactAssignment>
            {
                new ContactAssignment
                {
                    Id = Guid.NewGuid(),
                    ContactId = _testContacts[0].Id,
                    AssignedToUserId = _testUsers[0].Id,
                    AssignedByUserId = _testUsers[0].Id,
                    AssignedAt = DateTime.UtcNow.AddDays(-10),
                    Notes = "Initial assignment"
                }
            };

            _testTransfers = new List<ContactOwnershipTransfer>();

            // Setup DbSet mocks
            SetupMockDbSet(_mockContext.Setup(c => c.Contacts), _testContacts);
            SetupMockDbSet(_mockContext.Setup(c => c.Users), _testUsers);
            SetupMockDbSet(_mockContext.Setup(c => c.ContactCollaborators), _testCollaborators);
            SetupMockDbSet(_mockContext.Setup(c => c.ContactAssignments), _testAssignments);
            SetupMockDbSet(_mockContext.Setup(c => c.ContactOwnershipTransfers), _testTransfers);

            // Setup SaveChanges
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Setup default access control
            _mockAccessService.Setup(s => s.CanAccessContactAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(true);

            _service = new ContactService(
                _mockContext.Object,
                _mockLogger.Object,
                _mockAccessService.Object);
        }

        private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            
            // Setup async methods
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));
                
            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
                
            // Setup Find for entities with Id property
            if (typeof(T).GetProperty("Id") != null)
            {
                mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                    .ReturnsAsync((object[] ids) => data.FirstOrDefault(e => (Guid)e.GetType().GetProperty("Id").GetValue(e) == (Guid)ids[0]));
            }
        }

        public void Dispose()
        {
            // Clean up test data if needed
        }

        #region CRUD Tests

        [Fact]
        public async Task GetContactByIdAsync_WhenContactExists_ReturnsContact()
        {
            // Arrange
            var contact = _testContacts[0];
            var userId = _testUsers[0].Id;

            // Act
            var result = await _service.GetContactByIdAsync(contact.Id, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(contact.Id, result.Id);
            _mockAccessService.Verify(s => s.CanAccessContactAsync(contact.Id, userId), Times.Once);
        }

        [Fact]
        public async Task CreateContactAsync_ValidContact_ReturnsCreatedContact()
        {
            // Arrange
            var userId = _testUsers[0].Id;
            var newContact = new Contact
            {
                FirstName = "New",
                LastName = "Contact",
                OrganizationId = Guid.NewGuid(),
                RelationshipState = RelationshipState.Lead
            };

            // Act
            var result = await _service.CreateContactAsync(newContact, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.CreatedByUserId);
            Assert.True((DateTime.UtcNow - result.CreatedAt).TotalSeconds < 5);
            _mockContext.Verify(c => c.Contacts.Add(It.IsAny<Contact>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactAsync_ValidUpdate_UpdatesContact()
        {
            // Arrange
            var contact = _testContacts[0];
            var userId = _testUsers[0].Id;
            var updatedName = "Updated Name";
            contact.FirstName = updatedName;

            // Act
            var result = await _service.UpdateContactAsync(contact, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updatedName, result.FirstName);
            Assert.Equal(userId, result.UpdatedByUserId);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteContactAsync_ValidId_SoftDeletesContact()
        {
            // Arrange
            var contact = _testContacts[0];
            var userId = _testUsers[0].Id;
            var reason = "Test deletion";

            // Act
            var result = await _service.DeleteContactAsync(contact.Id, userId, reason);

            // Assert
            Assert.True(result);
            Assert.True(contact.IsDeleted);
            Assert.Equal(reason, contact.DeleteReason);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Assignment & Ownership Tests

        [Fact]
        public async Task AssignContactToUserAsync_ValidData_AssignsContact()
        {
            // Arrange
            var contact = _testContacts[0];
            var targetUser = _testUsers[1];
            var userId = _testUsers[0].Id;
            var notes = "Test assignment";

            // Act
            var result = await _service.AssignContactToUserAsync(contact.Id, targetUser.Id, userId, notes);

            // Assert
            Assert.True(result);
            Assert.Equal(targetUser.Id, contact.AssignedToUserId);
            Assert.Equal(2, _testAssignments.Count);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TransferContactOwnershipAsync_ValidData_TransfersOwnership()
        {
            // Arrange
            var contact = _testContacts[0];
            var newOwner = _testUsers[1];
            var userId = _testUsers[0].Id;
            var reason = "Test transfer";
            var previousOwnerId = contact.OwnerUserId;

            // Act
            var result = await _service.TransferContactOwnershipAsync(contact.Id, newOwner.Id, userId, reason);

            // Assert
            Assert.True(result);
            Assert.Equal(newOwner.Id, contact.OwnerUserId);
            Assert.Single(_testTransfers);
            var transfer = _testTransfers[0];
            Assert.Equal(previousOwnerId, transfer.PreviousOwnerId);
            Assert.Equal(newOwner.Id, transfer.NewOwnerId);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Helper Classes

        // Helper class for async enumerable support
        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public T Current => _inner.Current;

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return ValueTask.FromResult(_inner.MoveNext());
            }
        }

        // Helper class for async queryable support
        private class TestAsyncQueryProvider<T> : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            public TestAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new TestAsyncEnumerable<T>(expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new TestAsyncEnumerable<TElement>(expression);
            }

            public object Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return _inner.Execute<TResult>(expression);
            }

            public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
            {
                return new TestAsyncEnumerable<TResult>(expression);
            }

            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            {
                var result = Execute(expression);
                return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                    ?.MakeGenericMethod(typeof(TResult).GetGenericArguments()[0])
                    .Invoke(null, new[] { result });
            }
        }

        // Helper class for async enumerable support
        private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            { }

            public TestAsyncEnumerable(Expression expression)
                : base(expression)
            { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }

            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
        }

        #endregion
    }
}
