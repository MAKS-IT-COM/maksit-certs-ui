using System;
using System.Threading;
using System.Threading.Tasks;

public class LockManager : IDisposable {
  private readonly SemaphoreSlim _semaphore;

  public LockManager(int initialCount, int maxCount) {
    _semaphore = new SemaphoreSlim(initialCount, maxCount);
  }

  public async Task<T> ExecuteWithLockAsync<T>(Func<Task<T>> action) {
    await _semaphore.WaitAsync();
    try {
      return await action();
    }
    finally {
      _semaphore.Release();
    }
  }

  public async Task ExecuteWithLockAsync(Func<Task> action) {
    await _semaphore.WaitAsync();
    try {
      await action();
    }
    finally {
      _semaphore.Release();
    }
  }

  public async Task<T> ExecuteWithLockAsync<T>(Func<T> action) {
    await _semaphore.WaitAsync();
    try {
      return await Task.Run(action);
    }
    finally {
      _semaphore.Release();
    }
  }

  public async Task ExecuteWithLockAsync(Action action) {
    await _semaphore.WaitAsync();
    try {
      await Task.Run(action);
    }
    finally {
      _semaphore.Release();
    }
  }

  public void Dispose() {
    _semaphore?.Dispose();
  }
}
