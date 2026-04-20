using System.Threading;

namespace AutoSavingAlarm;

internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;

    private SingleInstanceGuard(Mutex mutex, bool isPrimaryInstance)
    {
        _mutex = mutex;
        IsPrimaryInstance = isPrimaryInstance;
    }

    public bool IsPrimaryInstance { get; }

    public static SingleInstanceGuard Acquire(string mutexName)
    {
        bool createdNew;
        Mutex mutex = new(initiallyOwned: true, mutexName, out createdNew);
        return new SingleInstanceGuard(mutex, createdNew);
    }

    public void Dispose()
    {
        if (IsPrimaryInstance)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
