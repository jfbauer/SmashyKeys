using System.IO;

namespace SmashyKeys;

/// <summary>
/// Simple file logger for debugging crashes.
/// </summary>
public static class Logger
{
    private static readonly string LogPath;
    private static readonly object Lock = new();

    static Logger()
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        LogPath = Path.Combine(exeDir, "SmashyKeys.log");

        // Clear log on startup
        try
        {
            File.WriteAllText(LogPath, $"=== SmashyKeys Log Started {DateTime.Now} ===\n");
        }
        catch
        {
            // Ignore if we can't write
        }
    }

    public static void Log(string message)
    {
        try
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }

    public static void LogException(string context, Exception ex)
    {
        Log($"EXCEPTION in {context}: {ex.GetType().Name}: {ex.Message}");
        Log($"  Stack: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Log($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }
}
