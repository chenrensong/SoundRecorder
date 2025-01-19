using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace VoiceRecorder.Models
{
    public partial class RecordingItem : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private DateTime createdAt;

        [ObservableProperty]
        private TimeSpan duration;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private TimeSpan currentPosition;

        [ObservableProperty]
        private string transcriptionText = string.Empty;

        [ObservableProperty]
        private bool isTranscribing;

        [ObservableProperty]
        private string currentTranscriptionSegment = string.Empty;

        [ObservableProperty]
        private bool isTranscriptionVisible;

        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;

        public event Action<RecordingItem> TranscriptionCompleted;

        private void OnTranscriptionCompleted()
        {
            TranscriptionCompleted?.Invoke(this);
        }

        partial void OnTranscriptionTextChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                OnTranscriptionCompleted();
            }
        }

        public void Play()
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                audioFile = new AudioFileReader(FilePath);
                outputDevice.Init(audioFile);
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            outputDevice.Play();
            IsPlaying = true;

            // 开始更新播放位置的计时器
            StartPositionTimer();
        }

        public void Pause()
        {
            outputDevice?.Pause();
            IsPlaying = false;
            StopPositionTimer();
        }

        public void Stop()
        {
            outputDevice?.Stop();
            IsPlaying = false;
            StopPositionTimer();
            CurrentPosition = TimeSpan.Zero;
        }

        private System.Timers.Timer positionTimer;

        private void StartPositionTimer()
        {
            positionTimer?.Dispose();
            positionTimer = new System.Timers.Timer(100); // 每100ms更新一次
            positionTimer.Elapsed += (s, e) =>
            {
                if (audioFile != null)
                {
                    CurrentPosition = audioFile.CurrentTime;
                    UpdateCurrentTranscriptionSegment();
                }
            };
            positionTimer.Start();
        }

        private void StopPositionTimer()
        {
            positionTimer?.Stop();
            positionTimer?.Dispose();
            positionTimer = null;
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            IsPlaying = false;
            StopPositionTimer();
            CurrentPosition = TimeSpan.Zero;
            if (audioFile != null)
            {
                audioFile.Position = 0;
            }
        }

        public void Dispose()
        {
            StopPositionTimer();
            outputDevice?.Dispose();
            audioFile?.Dispose();
            outputDevice = null;
            audioFile = null;
        }

        public void Delete()
        {
            Stop();
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除文件失败: {ex.Message}");
            }
        }

        public void OpenContainingFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{FilePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开文件夹失败: {ex.Message}");
            }
        }

        private void UpdateCurrentTranscriptionSegment()
        {
            // 这里可以根据 CurrentPosition 来更新当前显示的文字段落
            // 具体实现取决于您如何存储时间戳和文字的对应关系
            CurrentTranscriptionSegment = TranscriptionText;
        }

        [RelayCommand]
        private void ToggleTranscriptionVisibility()
        {
            IsTranscriptionVisible = !IsTranscriptionVisible;
        }
    }
} 