using FFMPEG.Application.Abstractions;
using FFMPEG.Core.Commands;

namespace FFMPEG.Application.Services;

public sealed class FfmpegCommandPreviewService : IFfmpegCommandPreviewService
{
    public string BuildPreview(FfmpegCommandDraft draft)
    {
        var input = FormatPath(draft.InputFilePath, draft.ShowFullPath);
        var output = FormatPath(BuildOutputPath(draft), draft.ShowFullPath);

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

    private static string FormatPath(string path, bool showFullPath)
    {
        if (string.IsNullOrWhiteSpace(path) || showFullPath)
        {
            return path;
        }

        return Path.GetFileName(path);
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }
}
