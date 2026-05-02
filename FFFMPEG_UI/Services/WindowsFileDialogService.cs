using Microsoft.Win32;

namespace FFFMPEG_UI_2._0.Services;

public sealed class WindowsFileDialogService : IFileDialogService
{
    public string? SelectInputFile()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select input file"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectOutputDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select output directory"
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
