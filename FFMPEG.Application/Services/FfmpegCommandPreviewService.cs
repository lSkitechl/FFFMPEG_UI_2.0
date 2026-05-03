using System.Text;
using FFMPEG.Application.Abstractions;
using FFMPEG.Core.Commands;

namespace FFMPEG.Application.Services;

public sealed class FfmpegCommandPreviewService : IFfmpegCommandPreviewService
{
    private static readonly string[] VideoCodecOptionNames = ["-c:v", "-codec:v"];
    private static readonly string[] AudioCodecOptionNames = ["-c:a", "-codec:a"];
    private static readonly string[] ManagedCodecValues =
    [
        "libx264",
        "libx265",
        "libaom-av1",
        "libvpx-vp9",
        "libvvenc",
        "aac",
        "libfdk_aac",
        "libmp3lame",
        "libopus"
    ];

    private static readonly string[] OptionsWithValues =
    [
        "-i",
        "-c:v",
        "-codec:v",
        "-c:a",
        "-codec:a",
        "-crf",
        "-preset",
        "-profile:v",
        "-level",
        "-b:v",
        "-b:a",
        "-bufsize",
        "-maxrate",
        "-minrate",
        "-vf",
        "-af",
        "-filter:v",
        "-filter:a",
        "-filter_complex",
        "-map",
        "-ss",
        "-to",
        "-t",
        "-r",
        "-s",
        "-ar",
        "-ac",
        "-metadata",
        "-movflags",
        "-f"
    ];

    public string BuildPreview(FfmpegCommandDraft draft)
    {
        var tokens = new List<string> { "ffmpeg" };

        SetOptionValue(tokens, "-i", draft.InputFilePath, insertIndex: 1);
        SetCodecValue(tokens, VideoCodecOptionNames, "-c:v", draft.VideoCodec);
        SetCodecValue(tokens, AudioCodecOptionNames, "-c:a", draft.AudioCodec);
        SetOutput(tokens, previousOutputPath: string.Empty, nextOutputPath: BuildOutputPath(draft));

        return JoinCommandLine(tokens);
    }

    public string UpdatePreview(
        string currentCommandLine,
        FfmpegCommandDraft previousDraft,
        FfmpegCommandDraft nextDraft)
    {
        var tokens = SplitCommandLine(currentCommandLine).ToList();

        if (tokens.Count == 0)
        {
            return BuildPreview(nextDraft);
        }

        SetOptionValue(tokens, "-i", nextDraft.InputFilePath, insertIndex: 1);
        SetCodecValue(tokens, VideoCodecOptionNames, "-c:v", nextDraft.VideoCodec);
        SetCodecValue(tokens, AudioCodecOptionNames, "-c:a", nextDraft.AudioCodec);
        RemoveStandaloneManagedCodecValues(tokens);
        SetOutput(tokens, BuildOutputPath(previousDraft), BuildOutputPath(nextDraft));

        return JoinCommandLine(tokens);
    }

    public FfmpegCommandManagedValues ReadManagedValues(string commandLine)
    {
        var tokens = SplitCommandLine(commandLine);
        var inputPath = ReadOptionValue(tokens, "-i");
        var videoCodec = ReadFirstOptionValue(tokens, VideoCodecOptionNames);
        var audioCodec = ReadFirstOptionValue(tokens, AudioCodecOptionNames);
        var outputIndex = FindLastLikelyOutputIndex(tokens);
        var outputPath = outputIndex >= 0 ? tokens[outputIndex] : null;

        return new FfmpegCommandManagedValues(
            inputPath,
            outputPath,
            videoCodec,
            audioCodec);
    }

    private static void SetOptionValue(
        List<string> tokens,
        string optionName,
        string value,
        int insertIndex)
    {
        var optionIndex = FindOptionIndex(tokens, optionName);

        if (optionIndex >= 0)
        {
            if (optionIndex + 1 < tokens.Count)
            {
                tokens[optionIndex + 1] = value;
            }
            else
            {
                tokens.Add(value);
            }

            return;
        }

        insertIndex = Math.Clamp(insertIndex, 1, tokens.Count);
        tokens.Insert(insertIndex, optionName);
        tokens.Insert(insertIndex + 1, value);
    }

    private static void SetCodecValue(
        List<string> tokens,
        IReadOnlyCollection<string> knownOptionNames,
        string preferredOptionName,
        string codec)
    {
        RemoveOptionPairs(tokens, knownOptionNames);

        if (string.IsNullOrWhiteSpace(codec))
        {
            return;
        }

        var insertIndex = FindOptionIndex(tokens, "-i");
        insertIndex = insertIndex >= 0 ? Math.Min(insertIndex + 2, tokens.Count) : Math.Min(1, tokens.Count);

        tokens.Insert(insertIndex, preferredOptionName);
        tokens.Insert(insertIndex + 1, codec);
    }

    private static void SetOutput(
        List<string> tokens,
        string previousOutputPath,
        string nextOutputPath)
    {
        if (!string.IsNullOrWhiteSpace(previousOutputPath))
        {
            var previousOutputIndex = tokens.FindIndex(token => token == previousOutputPath);

            if (previousOutputIndex >= 0)
            {
                tokens[previousOutputIndex] = nextOutputPath;
                return;
            }
        }

        var outputIndex = FindLastLikelyOutputIndex(tokens);

        if (outputIndex >= 0)
        {
            tokens[outputIndex] = nextOutputPath;
            return;
        }

        tokens.Add(nextOutputPath);
    }

    private static int FindLastLikelyOutputIndex(IReadOnlyList<string> tokens)
    {
        for (var index = tokens.Count - 1; index >= 1; index--)
        {
            if (tokens[index].StartsWith('-'))
            {
                continue;
            }

            if (IsKnownOptionValue(tokens, index))
            {
                continue;
            }

            if (ManagedCodecValues.Contains(tokens[index]))
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static bool IsKnownOptionValue(IReadOnlyList<string> tokens, int index)
    {
        if (index <= 1)
        {
            return false;
        }

        var previousToken = tokens[index - 1];

        return OptionsWithValues.Contains(previousToken);
    }

    private static void RemoveOptionPairs(
        List<string> tokens,
        IReadOnlyCollection<string> optionNames)
    {
        for (var index = tokens.Count - 1; index >= 1; index--)
        {
            if (!optionNames.Contains(tokens[index]))
            {
                continue;
            }

            var count = index + 1 < tokens.Count ? 2 : 1;
            tokens.RemoveRange(index, count);
        }
    }

    private static void RemoveStandaloneManagedCodecValues(List<string> tokens)
    {
        for (var index = tokens.Count - 1; index >= 1; index--)
        {
            if (!ManagedCodecValues.Contains(tokens[index]))
            {
                continue;
            }

            if (IsKnownOptionValue(tokens, index))
            {
                continue;
            }

            tokens.RemoveAt(index);
        }
    }

    private static int FindOptionIndex(
        IReadOnlyList<string> tokens,
        string optionName)
    {
        for (var index = 1; index < tokens.Count; index++)
        {
            if (tokens[index] == optionName)
            {
                return index;
            }
        }

        return -1;
    }

    private static string? ReadOptionValue(
        IReadOnlyList<string> tokens,
        string optionName)
    {
        var optionIndex = FindOptionIndex(tokens, optionName);

        return optionIndex >= 0 && optionIndex + 1 < tokens.Count
            ? tokens[optionIndex + 1]
            : null;
    }

    private static string? ReadFirstOptionValue(
        IReadOnlyList<string> tokens,
        IReadOnlyCollection<string> optionNames)
    {
        for (var index = 1; index < tokens.Count - 1; index++)
        {
            if (optionNames.Contains(tokens[index]))
            {
                return tokens[index + 1];
            }
        }

        return null;
    }

    private static string BuildOutputPath(FfmpegCommandDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.OutputDirectoryPath))
        {
            return draft.OutputFileName;
        }

        return Path.Combine(draft.OutputDirectoryPath, draft.OutputFileName);
    }

    private static IReadOnlyList<string> SplitCommandLine(string commandLine)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var isInQuotes = false;

        foreach (var character in commandLine)
        {
            if (character == '"')
            {
                isInQuotes = !isInQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !isInQuotes)
            {
                AddCurrentPart(result, current);
                continue;
            }

            current.Append(character);
        }

        AddCurrentPart(result, current);
        return result;
    }

    private static void AddCurrentPart(List<string> result, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        result.Add(current.ToString());
        current.Clear();
    }

    private static string JoinCommandLine(IEnumerable<string> tokens)
    {
        return string.Join(' ', tokens.Select(QuoteIfNeeded));
    }

    private static string QuoteIfNeeded(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        return value.Any(char.IsWhiteSpace) || IsLikelyPath(value)
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;
    }

    private static bool IsLikelyPath(string value)
    {
        return value.Contains('\\')
            || value.Contains('/')
            || IsWindowsDrivePath(value);
    }

    private static bool IsWindowsDrivePath(string value)
    {
        return value.Length >= 3
            && char.IsLetter(value[0])
            && value[1] == ':'
            && (value[2] == '\\' || value[2] == '/');
    }
}
