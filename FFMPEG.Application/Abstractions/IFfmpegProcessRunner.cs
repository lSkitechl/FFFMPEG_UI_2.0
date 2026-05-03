namespace FFMPEG.Application.Abstractions;

public interface IFfmpegProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string commandLine,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
