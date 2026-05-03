namespace FFMPEG.Core.Commands;

public sealed class FfmpegCommandDraft
{
    public string InputFilePath { get; init; } = string.Empty;

    public string OutputDirectoryPath { get; init; } = string.Empty;

    public string OutputFileName { get; init; } = "output.mp4";

    public string VideoCodec { get; init; } = string.Empty;

    public string AudioCodec { get; init; } = string.Empty;

    public bool ShowFullPath { get; init; }
}
