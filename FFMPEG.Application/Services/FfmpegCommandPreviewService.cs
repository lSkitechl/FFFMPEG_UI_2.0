using FFMPEG.Application.Abstractions;
using FFMPEG.Core.Commands;

namespace FFMPEG.Application.Services;

public sealed class FfmpegCommandPreviewService : IFfmpegCommandPreviewService
{
    public string BuildPreview(FfmpegCommandDraft draft)
    {
        var input = draft.InputFilePath;
        var output = BuildOutputPath(draft);

        return $"ffmpeg -i {Quote(input)} {Quote(output)}";
    }

    private static string BuildOutputPath(FfmpegCommandDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.OutputDirectoryPath))
        {
            return draft.OutputFileName;
        }

        return Path.Combine(draft.OutputDirectoryPath, draft.OutputFileName);
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }
}
