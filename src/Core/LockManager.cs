using System;
using System.Collections.Concurrent;
using System.Threading.RateLimiting;

public class LockManager : IDisposable {
  private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
  private readonly ConcurrentDictionary<int, int> _reentrantCounts = new ConcurrentDictionary<int, int>();
  private readonly TokenBucketRateLimiter _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions {
    TokenLimit = 5, // max 5 requests per second (adjust as needed)
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 100,
    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
    TokensPerPeriod = 5,
    AutoReplenishment = true
  });

  public async Task<T> ExecuteWithLockAsync<T>(Func<Task<T>> action) {
    var lease = await _rateLimiter.AcquireAsync(1);
    if (!lease.IsAcquired) throw new InvalidOperationException("Rate limit exceeded");

    var threadId = Thread.CurrentThread.ManagedThreadId;
    if (!_reentrantCounts.ContainsKey(threadId)) _reentrantCounts[threadId] = 0;
    if (_reentrantCounts[threadId] == 0) await _semaphore.WaitAsync();
    _reentrantCounts[threadId]++;
    try {
      return await action();
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) _semaphore.Release();
      lease.Dispose();
    }
  }

  public async Task ExecuteWithLockAsync(Func<Task> action) {
    var lease = await _rateLimiter.AcquireAsync(1);
    if (!lease.IsAcquired) throw new InvalidOperationException("Rate limit exceeded");

    var threadId = Thread.CurrentThread.ManagedThreadId;
    if (!_reentrantCounts.ContainsKey(threadId)) _reentrantCounts[threadId] = 0;
    if (_reentrantCounts[threadId] == 0) await _semaphore.WaitAsync();
    _reentrantCounts[threadId]++;
    try {
      await action();
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) _semaphore.Release();
      lease.Dispose();
    }
  }

  public async Task<T> ExecuteWithLockAsync<T>(Func<T> action) {
    var lease = await _rateLimiter.AcquireAsync(1);
    if (!lease.IsAcquired) throw new InvalidOperationException("Rate limit exceeded");

    var threadId = Thread.CurrentThread.ManagedThreadId;
    if (!_reentrantCounts.ContainsKey(threadId)) _reentrantCounts[threadId] = 0;
    if (_reentrantCounts[threadId] == 0) await _semaphore.WaitAsync();
    _reentrantCounts[threadId]++;
    try {
      return await Task.Run(action);
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) _semaphore.Release();
      lease.Dispose();
    }
  }

  public async Task ExecuteWithLockAsync(Action action) {
    var lease = await _rateLimiter.AcquireAsync(1);
    if (!lease.IsAcquired) throw new InvalidOperationException("Rate limit exceeded");

    var threadId = Thread.CurrentThread.ManagedThreadId;
    if (!_reentrantCounts.ContainsKey(threadId)) _reentrantCounts[threadId] = 0;
    if (_reentrantCounts[threadId] == 0) await _semaphore.WaitAsync();
    _reentrantCounts[threadId]++;
    try {
      await Task.Run(action);
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) _semaphore.Release();
      lease.Dispose();
    }
  }

  public void Dispose() {
    _semaphore.Dispose();
    _rateLimiter.Dispose();
  }
}
