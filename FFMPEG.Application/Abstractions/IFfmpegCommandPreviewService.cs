using FFMPEG.Core.Commands;

namespace FFMPEG.Application.Abstractions;

public interface IFfmpegCommandPreviewService
{
    string BuildPreview(FfmpegCommandDraft draft);
}
