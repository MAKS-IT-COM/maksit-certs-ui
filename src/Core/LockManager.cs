using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class LockManager : IDisposable {
  private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
  private readonly ConcurrentDictionary<int, int> _reentrantCounts = new ConcurrentDictionary<int, int>();

  public async Task<T> ExecuteWithLockAsync<T>(Func<Task<T>> action) {
    var threadId = Thread.CurrentThread.ManagedThreadId;

    if (!_reentrantCounts.ContainsKey(threadId)) {
      _reentrantCounts[threadId] = 0;
    }

    if (_reentrantCounts[threadId] == 0) {
      await _semaphore.WaitAsync();
    }

    _reentrantCounts[threadId]++;
    try {
      return await action();
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) {
        _semaphore.Release();
      }
    }
  }

  public async Task ExecuteWithLockAsync(Func<Task> action) {
    var threadId = Thread.CurrentThread.ManagedThreadId;

    if (!_reentrantCounts.ContainsKey(threadId)) {
      _reentrantCounts[threadId] = 0;
    }

    if (_reentrantCounts[threadId] == 0) {
      await _semaphore.WaitAsync();
    }

    _reentrantCounts[threadId]++;
    try {
      await action();
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) {
        _semaphore.Release();
      }
    }
  }

  public async Task<T> ExecuteWithLockAsync<T>(Func<T> action) {
    var threadId = Thread.CurrentThread.ManagedThreadId;

    if (!_reentrantCounts.ContainsKey(threadId)) {
      _reentrantCounts[threadId] = 0;
    }

    if (_reentrantCounts[threadId] == 0) {
      await _semaphore.WaitAsync();
    }

    _reentrantCounts[threadId]++;
    try {
      return await Task.Run(action);
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) {
        _semaphore.Release();
      }
    }
  }

  public async Task ExecuteWithLockAsync(Action action) {
    var threadId = Thread.CurrentThread.ManagedThreadId;

    if (!_reentrantCounts.ContainsKey(threadId)) {
      _reentrantCounts[threadId] = 0;
    }

    if (_reentrantCounts[threadId] == 0) {
      await _semaphore.WaitAsync();
    }

    _reentrantCounts[threadId]++;
    try {
      await Task.Run(action);
    }
    finally {
      _reentrantCounts[threadId]--;
      if (_reentrantCounts[threadId] == 0) {
        _semaphore.Release();
      }
    }
  }

  public void Dispose() {
    _semaphore.Dispose();
  }
}
