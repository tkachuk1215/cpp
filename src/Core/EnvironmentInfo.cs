using System.Runtime.InteropServices;

namespace Core;

public sealed record EnvironmentReport(
    string OsDescription,
    string FrameworkDescription,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string BaseDirectory,
    string BuildNote);

public static class EnvironmentInfo
{
    // Додаткове завдання №3: перевірка версії TFM при компіляції
#if NET10_0_OR_GREATER
    private const string CurrentBuildNote = "Збірка під net10.0 (NET10_0_OR_GREATER)";
#else
    private const string CurrentBuildNote = "Збірка під net8.0";
#endif

    public static EnvironmentReport Collect() => new(
        RuntimeInformation.OSDescription,
        RuntimeInformation.FrameworkDescription,
        RuntimeInformation.ProcessArchitecture.ToString(),
        DetectRid(),
        RuntimeInformation.RuntimeIdentifier,
        AppContext.BaseDirectory,
        CurrentBuildNote);

    private static string DetectRid()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux)   ? "linux" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX)     ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64   => "x64",
            Architecture.X86   => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm   => "arm",
            _                  => "unknown"
        };

        return $"{os}-{arch}";
    }
}
