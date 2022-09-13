﻿using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Splintermate.Delegation.Sinks;

public static class MemorySinkExtensions
{
    const string DefaultOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Writes log events to an in-memory log sink.
    /// </summary>
    /// <param name="sinkConfiguration">Logger sink configuration.</param>
    /// <param name="restrictedToMinimumLevel">The minimum level for
    /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level
    /// to be changed at runtime.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static LoggerConfiguration InMemory(
        this LoggerSinkConfiguration sinkConfiguration,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        string outputTemplate = DefaultOutputTemplate,
        LoggingLevelSwitch levelSwitch = null)
    {
        if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
        if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

        return sinkConfiguration.Sink(MemorySink.Instance, restrictedToMinimumLevel, levelSwitch);
    }
}