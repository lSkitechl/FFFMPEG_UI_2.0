using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FFMPEG.Application.Abstractions;

namespace FFMPEG.Infrastructure.Processes;

public sealed class FfmpegProcessRunner : IFfmpegProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string commandLine,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var commandParts = SplitCommandLine(commandLine);

        if (commandParts.Count == 0)
        {
            throw new InvalidOperationException("Command is empty.");
        }

        var executable = commandParts[0];

        if (!IsFfmpegExecutable(executable))
        {
            throw new InvalidOperationException("Only ffmpeg commands can be executed.");
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var argument in commandParts.Skip(1))
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start ffmpeg process.");
        }

        progress?.Report(0);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var outputTask = ReadStreamAsync(
            process.StandardOutput,
            outputBuilder,
            progress: null,
            cancellationToken);
        var errorTask = ReadStreamAsync(
            process.StandardError,
            errorBuilder,
            progress,
            cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(outputTask, errorTask);

        if (process.ExitCode == 0)
        {
            progress?.Report(100);
        }

        return new ProcessRunResult(
            process.ExitCode,
            outputBuilder.ToString(),
            errorBuilder.ToString());
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        StringBuilder builder,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        TimeSpan? duration = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                break;
            }

            builder.AppendLine(line);

            duration ??= TryReadDuration(line);
            var currentTime = TryReadCurrentTime(line);

            if (duration is { TotalSeconds: > 0 } && currentTime is not null)
            {
                var percent = (int)Math.Clamp(
                    currentTime.Value.TotalSeconds / duration.Value.TotalSeconds * 100,
                    0,
                    100);

                progress?.Report(percent);
            }
        }
    }

    private static TimeSpan? TryReadDuration(string line)
    {
        var match = Regex.Match(
            line,
            @"Duration:\s(?<time>\d{2}:\d{2}:\d{2}\.\d{2})",
            RegexOptions.CultureInvariant);

        return match.Success ? ParseTime(match.Groups["time"].Value) : null;
    }

    private static TimeSpan? TryReadCurrentTime(string line)
    {
        var match = Regex.Match(
            line,
            @"time=(?<time>\d{2}:\d{2}:\d{2}(?:\.\d{2})?)",
            RegexOptions.CultureInvariant);

        return match.Success ? ParseTime(match.Groups["time"].Value) : null;
    }

    private static TimeSpan? ParseTime(string value)
    {
        if (TimeSpan.TryParseExact(
            value,
            @"hh\:mm\:ss\.ff",
            CultureInfo.InvariantCulture,
            out var result))
        {
            return result;
        }

        return TimeSpan.TryParseExact(
            value,
            @"hh\:mm\:ss",
            CultureInfo.InvariantCulture,
            out result)
            ? result
            : null;
    }

    private static bool IsFfmpegExecutable(string executable)
    {
        var fileName = Path.GetFileName(executable);

        return string.Equals(fileName, "ffmpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "ffmpeg.exe", StringComparison.OrdinalIgnoreCase);
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
}
