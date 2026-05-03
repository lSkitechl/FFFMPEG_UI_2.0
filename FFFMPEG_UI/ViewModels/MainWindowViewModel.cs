using System.IO;
using System.Windows.Input;
using FFMPEG.Application.Abstractions;
using FFMPEG.Core.Commands;
using FFFMPEG_UI_2._0.Commands;
using FFFMPEG_UI_2._0.Services;

namespace FFFMPEG_UI_2._0.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string VideoCodecPlaceholder = "Video Codec";
    private const string AudioCodecPlaceholder = "Audio Codec";

    private readonly IFfmpegCommandPreviewService _commandPreviewService;
    private readonly IFfmpegProcessRunner _processRunner;
    private readonly IFileDialogService _fileDialogService;
    private FfmpegCommandDraft _lastCommandDraft;
    private string _ffmpegCommand;
    private string _executionStatus = "Ready";
    private string _executionOutput = string.Empty;
    private string _inputFilePath = string.Empty;
    private string _outputDirectoryPath = string.Empty;
    private string _outputFileName = "output.mp4";
    private string _selectedVideoCodec = VideoCodecPlaceholder;
    private string _selectedAudioCodec = AudioCodecPlaceholder;
    private int _executionProgress;
    private bool _isExecuting;
    private bool _isRefreshingCommandFromControls;
    private bool _isSyncingControlsFromCommand;
    private bool _showFullPath;

    public MainWindowViewModel(
        IFfmpegCommandPreviewService commandPreviewService,
        IFfmpegProcessRunner processRunner,
        IFileDialogService fileDialogService)
    {
        _commandPreviewService = commandPreviewService;
        _processRunner = processRunner;
        _fileDialogService = fileDialogService;
        ToggleShowFullPathCommand = new RelayCommand(ToggleShowFullPath);
        SelectInputFileCommand = new RelayCommand(SelectInputFile);
        SelectOutputDirectoryCommand = new RelayCommand(SelectOutputDirectory);
        ExecuteFfmpegCommandCommand = new AsyncRelayCommand(
            ExecuteFfmpegCommandAsync,
            () => !IsExecuting && !string.IsNullOrWhiteSpace(FfmpegCommand));
        _lastCommandDraft = BuildCommandDraft();
        _ffmpegCommand = _commandPreviewService.BuildPreview(_lastCommandDraft);
    }

    public string Title { get; } = "FFFMPEG UI";

    public string ShowFullPathButtonText => ShowFullPath ? "Show file names" : "Show full path";

    public string InputFileButtonText { get; } = "Input file";

    public string OutputPathButtonText { get; } = "Output path";

    public IReadOnlyList<string> VideoCodecOptions { get; } =
    [
        VideoCodecPlaceholder,
        "h264",
        "h265",
        "AV1",
        "VP9",
        "VVENC"
    ];

    public IReadOnlyList<string> AudioCodecOptions { get; } =
    [
        AudioCodecPlaceholder,
        "AAC",
        "libfdk_aac",
        "MP3",
        "Opus"
    ];

    public ICommand ToggleShowFullPathCommand { get; }

    public ICommand SelectInputFileCommand { get; }

    public ICommand SelectOutputDirectoryCommand { get; }

    public ICommand ExecuteFfmpegCommandCommand { get; }

    public string InputFileDisplayPath => FormatDisplayPath(InputFilePath);

    public string OutputDirectoryDisplayPath => FormatDisplayPath(OutputDirectoryPath);

    public string SelectedVideoCodec
    {
        get => _selectedVideoCodec;
        set
        {
            if (SetProperty(ref _selectedVideoCodec, value))
            {
                RefreshFfmpegCommandFromControlChange();
            }
        }
    }

    public string SelectedAudioCodec
    {
        get => _selectedAudioCodec;
        set
        {
            if (SetProperty(ref _selectedAudioCodec, value))
            {
                RefreshFfmpegCommandFromControlChange();
            }
        }
    }

    public string InputFilePath
    {
        get => _inputFilePath;
        set
        {
            if (SetProperty(ref _inputFilePath, value))
            {
                OnPropertyChanged(nameof(InputFileDisplayPath));
                RefreshFfmpegCommandFromControlChange();
            }
        }
    }

    public string OutputDirectoryPath
    {
        get => _outputDirectoryPath;
        set
        {
            if (SetProperty(ref _outputDirectoryPath, value))
            {
                OnPropertyChanged(nameof(OutputDirectoryDisplayPath));
                RefreshFfmpegCommandFromControlChange();
            }
        }
    }

    public string OutputFileName
    {
        get => _outputFileName;
        set
        {
            if (SetProperty(ref _outputFileName, value))
            {
                RefreshFfmpegCommandFromControlChange();
            }
        }
    }

    public bool ShowFullPath
    {
        get => _showFullPath;
        private set
        {
            if (SetProperty(ref _showFullPath, value))
            {
                OnPropertyChanged(nameof(ShowFullPathButtonText));
                OnPropertyChanged(nameof(InputFileDisplayPath));
                OnPropertyChanged(nameof(OutputDirectoryDisplayPath));
                RefreshFfmpegCommandFromControlChange();
            }
        }
    }

    public string FfmpegCommand
    {
        get => _ffmpegCommand;
        set
        {
            if (SetProperty(ref _ffmpegCommand, value))
            {
                RaiseExecuteCommandCanExecuteChanged();

                if (!_isRefreshingCommandFromControls)
                {
                    SyncControlsFromCommand();
                }
            }
        }
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                RaiseExecuteCommandCanExecuteChanged();
            }
        }
    }

    public string ExecutionStatus
    {
        get => _executionStatus;
        private set => SetProperty(ref _executionStatus, value);
    }

    public string ExecutionOutput
    {
        get => _executionOutput;
        private set => SetProperty(ref _executionOutput, value);
    }

    public int ExecutionProgress
    {
        get => _executionProgress;
        private set
        {
            if (SetProperty(ref _executionProgress, value))
            {
                OnPropertyChanged(nameof(ExecutionProgressText));
            }
        }
    }

    public string ExecutionProgressText => $"Progress: {ExecutionProgress}%";

    private void ToggleShowFullPath()
    {
        ShowFullPath = !ShowFullPath;
    }

    private void SelectInputFile()
    {
        var selectedPath = _fileDialogService.SelectInputFile();

        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            InputFilePath = selectedPath;
        }
    }

    private void SelectOutputDirectory()
    {
        var selectedPath = _fileDialogService.SelectOutputDirectory();

        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            OutputDirectoryPath = selectedPath;
        }
    }

    private async Task ExecuteFfmpegCommandAsync()
    {
        IsExecuting = true;
        ExecutionStatus = "Executing ffmpeg...";
        ExecutionOutput = string.Empty;
        ExecutionProgress = 0;

        try
        {
            var progress = new Progress<int>(value => ExecutionProgress = value);
            var result = await _processRunner.RunAsync(FfmpegCommand, progress);

            ExecutionOutput = BuildExecutionOutput(result);
            ExecutionStatus = result.ExitCode == 0
                ? "Command completed successfully."
                : $"Command failed. Exit code: {result.ExitCode}.";
            ExecutionProgress = result.ExitCode == 0 ? 100 : ExecutionProgress;
        }
        catch (Exception ex)
        {
            ExecutionStatus = "Command failed before ffmpeg finished.";
            ExecutionOutput = ex.Message;
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public void RefreshFfmpegCommand()
    {
        var nextDraft = BuildCommandDraft();

        _isRefreshingCommandFromControls = true;

        try
        {
            FfmpegCommand = _commandPreviewService.UpdatePreview(
                FfmpegCommand,
                _lastCommandDraft,
                nextDraft);
            _lastCommandDraft = nextDraft;
        }
        finally
        {
            _isRefreshingCommandFromControls = false;
        }
    }

    public string BuildFfmpegCommand()
    {
        return _commandPreviewService.BuildPreview(BuildCommandDraft());
    }

    private FfmpegCommandDraft BuildCommandDraft()
    {
        return new FfmpegCommandDraft
        {
            InputFilePath = InputFilePath,
            OutputDirectoryPath = OutputDirectoryPath,
            OutputFileName = OutputFileName,
            VideoCodec = MapVideoCodec(SelectedVideoCodec),
            AudioCodec = MapAudioCodec(SelectedAudioCodec),
            ShowFullPath = ShowFullPath
        };
    }

    private static string MapVideoCodec(string selectedCodec)
    {
        return selectedCodec switch
        {
            "h264" => "libx264",
            "h265" => "libx265",
            "AV1" => "libaom-av1",
            "VP9" => "libvpx-vp9",
            "VVENC" => "libvvenc",
            _ => string.Empty
        };
    }

    private static string MapAudioCodec(string selectedCodec)
    {
        return selectedCodec switch
        {
            "AAC" => "aac",
            "libfdk_aac" => "libfdk_aac",
            "MP3" => "libmp3lame",
            "Opus" => "libopus",
            _ => string.Empty
        };
    }

    private void RefreshFfmpegCommandFromControlChange()
    {
        if (!_isSyncingControlsFromCommand)
        {
            RefreshFfmpegCommand();
        }
    }

    private void SyncControlsFromCommand()
    {
        _isSyncingControlsFromCommand = true;

        try
        {
            var managedValues = _commandPreviewService.ReadManagedValues(FfmpegCommand);

            SyncPathValues(managedValues);
            SyncCodecValues(managedValues);
            _lastCommandDraft = BuildCommandDraft();
        }
        finally
        {
            _isSyncingControlsFromCommand = false;
        }
    }

    private void SyncPathValues(FfmpegCommandManagedValues managedValues)
    {
        InputFilePath = managedValues.InputFilePath ?? string.Empty;

        if (string.IsNullOrWhiteSpace(managedValues.OutputPath))
        {
            OutputDirectoryPath = string.Empty;
            OutputFileName = string.Empty;
            return;
        }

        OutputDirectoryPath = Path.GetDirectoryName(managedValues.OutputPath) ?? string.Empty;
        OutputFileName = Path.GetFileName(managedValues.OutputPath);
    }

    private void SyncCodecValues(FfmpegCommandManagedValues managedValues)
    {
        SelectedVideoCodec = MapVideoCodecToSelection(managedValues.VideoCodec);
        SelectedAudioCodec = MapAudioCodecToSelection(managedValues.AudioCodec);
    }

    private static string MapVideoCodecToSelection(string? codec)
    {
        return codec switch
        {
            "libx264" => "h264",
            "libx265" => "h265",
            "libaom-av1" => "AV1",
            "libvpx-vp9" => "VP9",
            "libvvenc" => "VVENC",
            _ => VideoCodecPlaceholder
        };
    }

    private static string MapAudioCodecToSelection(string? codec)
    {
        return codec switch
        {
            "aac" => "AAC",
            "libfdk_aac" => "libfdk_aac",
            "libmp3lame" => "MP3",
            "libopus" => "Opus",
            _ => AudioCodecPlaceholder
        };
    }

    private static string BuildExecutionOutput(ProcessRunResult result)
    {
        var output = string.Join(
            Environment.NewLine + Environment.NewLine,
            new[] { result.StandardError, result.StandardOutput }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(output)
            ? $"ffmpeg finished with exit code {result.ExitCode}."
            : output;
    }

    private void RaiseExecuteCommandCanExecuteChanged()
    {
        if (ExecuteFfmpegCommandCommand is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private string FormatDisplayPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || ShowFullPath)
        {
            return path;
        }

        var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fileName = Path.GetFileName(trimmedPath);

        return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
    }
}
