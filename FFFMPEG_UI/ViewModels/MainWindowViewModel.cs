using System.IO;
using System.Windows.Input;
using FFMPEG.Application.Abstractions;
using FFMPEG.Core.Commands;
using FFFMPEG_UI_2._0.Commands;
using FFFMPEG_UI_2._0.Services;

namespace FFFMPEG_UI_2._0.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IFfmpegCommandPreviewService _commandPreviewService;
    private readonly IFileDialogService _fileDialogService;
    private string _ffmpegCommand;
    private string _inputFilePath = string.Empty;
    private string _outputDirectoryPath = string.Empty;
    private string _outputFileName = "output.mp4";
    private bool _showFullPath;

    public MainWindowViewModel(
        IFfmpegCommandPreviewService commandPreviewService,
        IFileDialogService fileDialogService)
    {
        _commandPreviewService = commandPreviewService;
        _fileDialogService = fileDialogService;
        ToggleShowFullPathCommand = new RelayCommand(ToggleShowFullPath);
        SelectInputFileCommand = new RelayCommand(SelectInputFile);
        SelectOutputDirectoryCommand = new RelayCommand(SelectOutputDirectory);
        _ffmpegCommand = BuildFfmpegCommand();
    }

    public string Title { get; } = "FFFMPEG UI";

    public string ShowFullPathButtonText => ShowFullPath ? "Show file names" : "Show full path";

    public string InputFileButtonText { get; } = "Input file";

    public string OutputPathButtonText { get; } = "Output path";

    public ICommand ToggleShowFullPathCommand { get; }

    public ICommand SelectInputFileCommand { get; }

    public ICommand SelectOutputDirectoryCommand { get; }

    public string InputFileDisplayPath => FormatDisplayPath(InputFilePath);

    public string OutputDirectoryDisplayPath => FormatDisplayPath(OutputDirectoryPath);

    public string InputFilePath
    {
        get => _inputFilePath;
        set
        {
            if (SetProperty(ref _inputFilePath, value))
            {
                OnPropertyChanged(nameof(InputFileDisplayPath));
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
                OnPropertyChanged(nameof(OutputDirectoryDisplayPath));
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
                OnPropertyChanged(nameof(InputFileDisplayPath));
                OnPropertyChanged(nameof(OutputDirectoryDisplayPath));
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
