namespace FFFMPEG_UI_2._0.Services;

public interface IFileDialogService
{
    string? SelectInputFile();

    string? SelectOutputDirectory();
}
