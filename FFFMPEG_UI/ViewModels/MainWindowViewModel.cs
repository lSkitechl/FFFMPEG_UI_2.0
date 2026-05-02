using System.Windows.Input;
using FFMPEG.Application.Abstractions;
using FFMPEG.Core.Commands;
using FFFMPEG_UI_2._0.Commands;

namespace FFFMPEG_UI_2._0.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IFfmpegCommandPreviewService _commandPreviewService;
    private string _ffmpegCommand;
    private string _inputFilePath = string.Empty;
    private string _outputDirectoryPath = string.Empty;
    private string _outputFileName = "output.mp4";
    private bool _showFullPath;

    public MainWindowViewModel(IFfmpegCommandPreviewService commandPreviewService)
    {
        _commandPreviewService = commandPreviewService;
        ToggleShowFullPathCommand = new RelayCommand(ToggleShowFullPath);
        _ffmpegCommand = BuildFfmpegCommand();
    }

    public string Title { get; } = "FFFMPEG UI";

    public string ShowFullPathButtonText => ShowFullPath ? "Show file names" : "Show full path";

    public string InputFileButtonText { get; } = "Input file";

    public string OutputPathButtonText { get; } = "Output path";

    public ICommand ToggleShowFullPathCommand { get; }

    public string InputFilePath
    {
        get => _inputFilePath;
        set
        {
            if (SetProperty(ref _inputFilePath, value))
            {
                RefreshFfmpegCommand();
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
                RefreshFfmpegCommand();
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
                RefreshFfmpegCommand();
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
                RefreshFfmpegCommand();
            }
        }
    }

    public string FfmpegCommand
    {
        get => _ffmpegCommand;
        set => SetProperty(ref _ffmpegCommand, value);
    }

    private void ToggleShowFullPath()
    {
        ShowFullPath = !ShowFullPath;
    }

    public void RefreshFfmpegCommand()
    {
        FfmpegCommand = BuildFfmpegCommand();
    }

    public string BuildFfmpegCommand()
    {
        return _commandPreviewService.BuildPreview(new FfmpegCommandDraft
        {
            InputFilePath = InputFilePath,
            OutputDirectoryPath = OutputDirectoryPath,
            OutputFileName = OutputFileName,
            ShowFullPath = ShowFullPath
        });
    }
}
