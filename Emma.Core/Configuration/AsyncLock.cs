using System;
using System.Threading;
using System.Threading.Tasks;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// A lightweight asynchronous lock that can be used with async/await.
    /// </summary>
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> _releaser;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLock"/> class.
        /// </summary>
        public AsyncLock()
        {
            _releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        /// <summary>
        /// Asynchronously acquires the lock.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait.</param>
        /// <returns>A task that will complete with a disposable that releases the lock when disposed.</returns>
        public Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            var wait = _semaphore.WaitAsync(cancellationToken);
            return wait.IsCompleted
                ? _releaser
                : wait.ContinueWith(
                    (_, state) => (IDisposable)state!,
                    _releaser.Result,
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        /// <summary>
        /// Synchronously acquires the lock.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait.</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public IDisposable Lock(CancellationToken cancellationToken = default)
        {
            _semaphore.Wait(cancellationToken);
            return _releaser.Result;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="AsyncLock"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        private void Release()
        {
            _semaphore.Release();
        }

        /// <summary>
        /// A disposable that releases the lock when disposed.
        /// </summary>
        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock _asyncLock;
            private bool _disposed;

            public Releaser(AsyncLock asyncLock)
            {
                _asyncLock = asyncLock;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _asyncLock.Release();
                    _disposed = true;
                }
            }
        }
    }
}
