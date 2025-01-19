using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using VoiceRecorder.Models;
using SoundRecorder.ViewModels;
using SoundRecorder.Helpers;

namespace SoundRecorder.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void ShowRecordingOptions(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var recording = (RecordingItem)button.DataContext;

        var menu = new MenuFlyout();
        
        var openFolderItem = new MenuItem { Header = "打开所在文件夹" };
        openFolderItem.Click += (s, e) => recording.OpenContainingFolder();
        
        var deleteItem = new MenuItem { Header = "删除", Foreground = new SolidColorBrush(Colors.Red) };
        deleteItem.Click += async (s, e) =>
        {
            var result = await MessageBox.Show(
                "确定要删除这条录音吗？此操作不可撤销。",
                "删除录音",
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBox.MessageBoxResult.Yes)
            {
                recording.Delete();
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.Recordings.Remove(recording);
                }
            }
        };

        menu.Items.Add(openFolderItem);
        menu.Items.Add(deleteItem);
        
        menu.ShowAt(button);
    }
}
