namespace FFMPEG.Application.Abstractions;

public interface IFfmpegProcessRunner
{
    Task<int> RunAsync(string arguments, CancellationToken cancellationToken = default);
}
