using FFMPEG.Core.Commands;

namespace FFMPEG.Application.Abstractions;

public interface IFfmpegCommandPreviewService
{
    string BuildPreview(FfmpegCommandDraft draft);

    string UpdatePreview(
        string currentCommandLine,
        FfmpegCommandDraft previousDraft,
        FfmpegCommandDraft nextDraft);

    FfmpegCommandManagedValues ReadManagedValues(string commandLine);
}

public sealed record FfmpegCommandManagedValues(
    string? InputFilePath,
    string? OutputPath,
    string? VideoCodec,
    string? AudioCodec);
