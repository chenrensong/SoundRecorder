using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace SoundRecorder.Helpers
{
    public class MessageBox
    {
        public enum MessageBoxButtons
        {
            Ok,
            YesNo
        }

        public enum MessageBoxResult
        {
            Ok,
            Yes,
            No
        }

        public static async Task<MessageBoxResult> Show(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            var window = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var content = new StackPanel { Spacing = 20, Margin = new Thickness(20) };
            content.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };

            var tcs = new TaskCompletionSource<MessageBoxResult>();

            if (buttons == MessageBoxButtons.YesNo)
            {
                var yesButton = new Button { Content = "确定" };
                var noButton = new Button { Content = "取消" };

                yesButton.Click += (s, e) => { window.Close(); tcs.SetResult(MessageBoxResult.Yes); };
                noButton.Click += (s, e) => { window.Close(); tcs.SetResult(MessageBoxResult.No); };

                buttonPanel.Children.Add(yesButton);
                buttonPanel.Children.Add(noButton);
            }
            else
            {
                var okButton = new Button { Content = "确定" };
                okButton.Click += (s, e) => { window.Close(); tcs.SetResult(MessageBoxResult.Ok); };
                buttonPanel.Children.Add(okButton);
            }

            content.Children.Add(buttonPanel);
            window.Content = content;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await window.ShowDialog(desktop.MainWindow);
            }
            
            return await tcs.Task;
        }
    }
} 