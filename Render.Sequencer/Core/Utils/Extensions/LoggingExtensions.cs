using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Render.Sequencer.Core.Utils.Extensions;

internal static class LoggingExtensions
{
    private const string Info = nameof(Info);

    [Conditional("DEBUG")]
    public static void Trace(
        this object? caller,
        string message = "",
        [CallerMemberName] string memberName = "")
    {
        string callerType = caller?.GetType().Name ?? "Caller is Null!";
        string delimiter = string.IsNullOrEmpty(message) ? string.Empty : ":";
        string fullMessage = $"{callerType}.{memberName}{delimiter} {message}";

        DebugWriteLine(fullMessage);
    }

    [Conditional("DEBUG")]
    public static void Log(
        this object? caller,
        string message = "")
    {
        DebugWriteLine(message);
    }

    [Conditional("DEBUG")]
    private static void DebugWriteLine(string? message)
    {
        Debug.WriteLine(message, Info);
    }
}
