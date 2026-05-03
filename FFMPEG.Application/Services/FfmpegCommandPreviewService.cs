using FFMPEG.Application.Abstractions;
using FFMPEG.Core.Commands;

namespace FFMPEG.Application.Services;

public sealed class FfmpegCommandPreviewService : IFfmpegCommandPreviewService
{
    public string BuildPreview(FfmpegCommandDraft draft)
    {
        var input = draft.InputFilePath;
        var codecArguments = BuildCodecArguments(draft);
        var output = BuildOutputPath(draft);

        return $"ffmpeg -i {Quote(input)}{codecArguments} {Quote(output)}";
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

    private static string BuildCodecArguments(FfmpegCommandDraft draft)
    {
        var arguments = new List<string>();

        if (!string.IsNullOrWhiteSpace(draft.VideoCodec))
        {
            arguments.Add($"-c:v {draft.VideoCodec}");
        }

        if (!string.IsNullOrWhiteSpace(draft.AudioCodec))
        {
            arguments.Add($"-c:a {draft.AudioCodec}");
        }

        return arguments.Count == 0 ? string.Empty : $" {string.Join(' ', arguments)}";
    }
}
