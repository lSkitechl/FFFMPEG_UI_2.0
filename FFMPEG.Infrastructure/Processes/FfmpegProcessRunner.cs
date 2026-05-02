using System.Diagnostics;
using FFMPEG.Application.Abstractions;

namespace FFMPEG.Infrastructure.Processes;

public sealed class FfmpegProcessRunner : IFfmpegProcessRunner
{
    public async Task<int> RunAsync(string arguments, CancellationToken cancellationToken = default)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
        {
            throw new InvalidOperationException("Failed to start ffmpeg process.");
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
