using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace SoundRecorder.Converters
{
    public class RecordingStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRecording)
            {
                return isRecording ? 
                    new SolidColorBrush(Color.Parse("#FF3B30")) : // 录音时为红色
                    new SolidColorBrush(Color.Parse("#FF9500")); // 未录音时为橙色
            }
            return new SolidColorBrush(Color.Parse("#FF9500"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 