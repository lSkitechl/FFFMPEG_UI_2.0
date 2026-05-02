using System.Windows;
using FFMPEG.Application.Abstractions;
using FFMPEG.Application.Services;
using FFFMPEG_UI_2._0.Services;
using FFFMPEG_UI_2._0.ViewModels;

namespace FFFMPEG_UI_2._0;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IFfmpegCommandPreviewService commandPreviewService = new FfmpegCommandPreviewService();
        IFileDialogService fileDialogService = new WindowsFileDialogService();
        var mainWindowViewModel = new MainWindowViewModel(commandPreviewService, fileDialogService);

        var mainWindow = new MainWindow(mainWindowViewModel);
        mainWindow.Show();
    }
}
