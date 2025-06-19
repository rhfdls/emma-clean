using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Caches YAML file contents with change detection based on file hashing.
    /// </summary>
    internal class YamlFileCache : IDisposable
    {
        private readonly IFileProvider _fileProvider;
        private readonly ILogger<YamlFileCache> _logger;
        private readonly string _filePath;
        private readonly TimeSpan _maxCacheDuration;
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        
        private string? _cachedContent;
        private string? _cachedHash;
        private DateTimeOffset _lastCheckTime = DateTimeOffset.MinValue;
        private bool _disposed;

        public YamlFileCache(
            IFileProvider fileProvider,
            string filePath,
            ILogger<YamlFileCache> logger,
            TimeSpan? maxCacheDuration = null)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxCacheDuration = maxCacheDuration ?? TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Gets the current file content, using cache if valid.
        /// </summary>
        public async Task<(string? content, bool wasCached)> GetContentAsync(CancellationToken cancellationToken = default)
        {
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                // Check if we can use the cached content
                if (IsCacheValid())
                {
                    _logger.LogTrace("Using cached content for {FilePath}", _filePath);
                    return (_cachedContent, true);
                }

                // Read and cache the file content
                _logger.LogDebug("Reading content from {FilePath}", _filePath);
                var fileInfo = _fileProvider.GetFileInfo(_filePath);
                
                if (!fileInfo.Exists)
                {
                    _logger.LogWarning("File not found: {FilePath}", _filePath);
                    _cachedContent = null;
                    _cachedHash = null;
                    return (null, false);
                }

                using var stream = fileInfo.CreateReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                _cachedContent = await reader.ReadToEndAsync(cancellationToken);
                
                // Update cache metadata
                _cachedHash = ComputeHash(_cachedContent);
                _lastCheckTime = DateTimeOffset.UtcNow;
                
                return (_cachedContent, false);
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Forces a refresh of the cached content.
        /// </summary>
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                _lastCheckTime = DateTimeOffset.MinValue;
                _cachedContent = null;
                _cachedHash = null;
                
                // Trigger a fresh read
                await GetContentAsync(cancellationToken);
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private bool IsCacheValid()
        {
            // No content cached
            if (_cachedContent == null || _cachedHash == null)
                return false;
                
            // Cache expired
            if (DateTimeOffset.UtcNow - _lastCheckTime > _maxCacheDuration)
                return false;
                
            // File might have been modified
            var fileInfo = _fileProvider.GetFileInfo(_filePath);
            if (!fileInfo.Exists || fileInfo.LastModified > _lastCheckTime)
                return false;
                
            return true;
        }

        private static string ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashBytes);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cacheLock.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
