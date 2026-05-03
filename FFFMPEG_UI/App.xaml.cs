using System.Windows;
using FFMPEG.Application.Abstractions;
using FFMPEG.Application.Services;
using FFMPEG.Infrastructure.Processes;
using FFFMPEG_UI_2._0.Services;
using FFFMPEG_UI_2._0.ViewModels;

namespace FFFMPEG_UI_2._0;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IFfmpegCommandPreviewService commandPreviewService = new FfmpegCommandPreviewService();
        IFfmpegProcessRunner processRunner = new FfmpegProcessRunner();
        IFileDialogService fileDialogService = new WindowsFileDialogService();
        var mainWindowViewModel = new MainWindowViewModel(
            commandPreviewService,
            processRunner,
            fileDialogService);

        var mainWindow = new MainWindow(mainWindowViewModel);
        mainWindow.Show();
    }
}
