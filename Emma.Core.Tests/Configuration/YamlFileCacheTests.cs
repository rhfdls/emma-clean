using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Configuration
{
    public class YamlFileCacheTests : IDisposable
    {
        private readonly Mock<IFileProvider> _fileProviderMock;
        private readonly Mock<IFileInfo> _fileInfoMock;
        private readonly MemoryStream _fileStream;
        private readonly string _filePath = "/test/agent_capabilities.yaml";
        private readonly ILogger<YamlFileCache> _logger;
        private readonly YamlFileCache _cache;

        public YamlFileCacheTests()
        {
            _fileProviderMock = new Mock<IFileProvider>();
            _fileInfoMock = new Mock<IFileInfo>();
            _fileStream = new MemoryStream();
            _logger = new LoggerFactory().CreateLogger<YamlFileCache>();
            
            // Setup default file info
            _fileInfoMock.Setup(f => f.Exists).Returns(true);
            _fileInfoMock.Setup(f => f.CreateReadStream())
                .Returns(() => new MemoryStream(_fileStream.ToArray()));
            _fileInfoMock.Setup(f => f.LastModified).Returns(DateTimeOffset.UtcNow);
            
            _fileProviderMock.Setup(f => f.GetFileInfo(It.IsAny<string>()))
                .Returns(_fileInfoMock.Object);
            
            _cache = new YamlFileCache(
                _fileProviderMock.Object,
                _filePath,
                _logger,
                TimeSpan.FromMilliseconds(100)); // Short cache duration for testing
        }

        [Fact]
        public async Task GetContentAsync_WhenFileExists_ReturnsContent()
        {
            // Arrange
            var expectedContent = "test content";
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes(expectedContent));
            _fileStream.Position = 0;

            // Act
            var (content, wasCached) = await _cache.GetContentAsync();

            // Assert
            Assert.Equal(expectedContent, content);
            Assert.False(wasCached);
        }

        [Fact]
        public async Task GetContentAsync_WhenFileDoesNotExist_ReturnsNull()
        {
            // Arrange
            _fileInfoMock.Setup(f => f.Exists).Returns(false);

            // Act
            var (content, wasCached) = await _cache.GetContentAsync();

            // Assert
            Assert.Null(content);
            Assert.False(wasCached);
        }

        [Fact]
        public async Task GetContentAsync_WhenCalledTwice_ReturnsCachedContent()
        {
            // Arrange
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("test"));
            _fileStream.Position = 0;

            // Act - First call
            var result1 = await _cache.GetContentAsync();
            
            // Modify the file
            _fileStream.SetLength(0);
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("modified"));
            _fileStream.Position = 0;
            
            // Act - Second call (should still return cached content)
            var result2 = await _cache.GetContentAsync();

            // Assert
            Assert.Equal("test", result1.content);
            Assert.False(result1.wasCached);
            Assert.Equal("test", result2.content);
            Assert.True(result2.wasCached);
        }

        [Fact]
        public async Task GetContentAsync_AfterCacheExpiry_ReturnsFreshContent()
        {
            // Arrange - First call
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("first"));
            _fileStream.Position = 0;
            await _cache.GetContentAsync();
            
            // Wait for cache to expire
            await Task.Delay(150);
            
            // Update file content
            _fileStream.SetLength(0);
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("second"));
            _fileStream.Position = 0;
            
            // Update last modified time
            _fileInfoMock.Setup(f => f.LastModified).Returns(DateTimeOffset.UtcNow);

            // Act - Second call after expiry
            var (content, wasCached) = await _cache.GetContentAsync();

            // Assert
            Assert.Equal("second", content);
            Assert.False(wasCached);
        }

        [Fact]
        public async Task GetContentAsync_WhenFileChanges_ReturnsFreshContent()
        {
            // Arrange - First call
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("first"));
            _fileStream.Position = 0;
            await _cache.GetContentAsync();
            
            // Update file content and last modified time
            _fileStream.SetLength(0);
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("second"));
            _fileStream.Position = 0;
            var newLastModified = DateTimeOffset.UtcNow.AddSeconds(10);
            _fileInfoMock.Setup(f => f.LastModified).Returns(newLastModified);

            // Act - Second call with modified file
            var (content, wasCached) = await _cache.GetContentAsync();

            // Assert
            Assert.Equal("second", content);
            Assert.False(wasCached);
        }

        [Fact]
        public async Task RefreshAsync_WhenCalled_ForcesCacheRefresh()
        {
            // Arrange - First call
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("first"));
            _fileStream.Position = 0;
            await _cache.GetContentAsync();
            
            // Update file content
            _fileStream.SetLength(0);
            await _fileStream.WriteAsync(Encoding.UTF8.GetBytes("second"));
            _fileStream.Position = 0;
            
            // Act - Force refresh
            await _cache.RefreshAsync();
            
            // Act - Get content after refresh
            var (content, wasCached) = await _cache.GetContentAsync();

            // Assert
            Assert.Equal("second", content);
            Assert.True(wasCached); // Should be cached after refresh
        }

        [Fact]
        public async Task GetContentAsync_WithCancellation_ThrowsWhenCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _cache.GetContentAsync(cts.Token));
        }

        public void Dispose()
        {
            _fileStream.Dispose();
            _cache.Dispose();
        }
    }
}
