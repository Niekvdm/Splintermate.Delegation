using Serilog.Core;
using Serilog.Events;

namespace Splintermate.Delegation.Sinks;

public class MemorySink : ILogEventSink, IDisposable
{
    private static readonly MemorySink LocalInstance = new();
    private readonly List<LogEvent> _logEvents;
    private readonly object _snapShotLock = new();

    public MemorySink()
        : this(new List<LogEvent>())
    {
    }

    protected MemorySink(List<LogEvent> logEvents) => _logEvents = logEvents;

    public static MemorySink Instance => LocalInstance;

    public List<LogEvent> LogEvents => _logEvents;

    public void Dispose() => _logEvents.Clear();

    public virtual void Emit(LogEvent logEvent)
    {
        lock (_snapShotLock)
            _logEvents.Add(logEvent);
    }

    public MemorySink Snapshot()
    {
        return null;
    }
}