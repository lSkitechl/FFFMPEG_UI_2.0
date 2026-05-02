using System.Windows;
using FFFMPEG_UI_2._0.ViewModels;

namespace FFFMPEG_UI_2._0;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
