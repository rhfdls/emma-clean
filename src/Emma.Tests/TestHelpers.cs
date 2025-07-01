using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Emma.Tests
{
    public static class TestHelpers
    {
        public static Mock<DbSet<T>> GetMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            
            // Support for async operations
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
                
            // Support for FindAsync
            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .Returns<object[]>(ids => 
                {
                    // This is a simplified implementation - you might need to adjust based on your entity's key
                    var id = ids.FirstOrDefault();
                    if (id == null) return ValueTask.FromResult<T>(null);
                    
                    var property = typeof(T).GetProperty("Id");
                    if (property == null) return ValueTask.FromResult<T>(null);
                    
                    return ValueTask.FromResult(data.FirstOrDefault(e => 
                        property.GetValue(e)?.ToString() == id?.ToString()));
                });
            
            return mockSet;
        }
        
        public static void SetupSaveChanges(this Mock<IAppDbContext> mockContext, int returnValue = 1)
        {
            mockContext.Setup(c => c.SaveChangesAsync(default))
                .ReturnsAsync(returnValue);
        }
        
        public static void SetupUsers(this Mock<IAppDbContext> mockContext, IEnumerable<User> users)
        {
            var mockSet = GetMockDbSet(users.AsQueryable());
            mockContext.Setup(c => c.Users).Returns(mockSet.Object);
        }
        
        public static void SetupTasks(this Mock<IAppDbContext> mockContext, IEnumerable<TaskItem> tasks)
        {
            var mockSet = GetMockDbSet(tasks.AsQueryable());
            mockContext.Setup(c => c.TaskItems).Returns(mockSet.Object);
        }
        
        public static void SetupContacts(this Mock<IAppDbContext> mockContext, IEnumerable<Contact> contacts)
        {
            var mockSet = GetMockDbSet(contacts.AsQueryable());
            mockContext.Setup(c => c.Contacts).Returns(mockSet.Object);
        }
    }
    
    // Helper class to support async operations in tests
    public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current => _inner.Current;
    }
}
