using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using VoiceRecorder.Models;
using Whisper.net;
using Whisper.net.Ggml;
using System.Text.Json;
using System.Linq;
using Vosk;
using System.Net.Http;
using System.IO.Compression;
using SoundRecorder.Services;

namespace SoundRecorder.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private WaveInEvent waveIn;
        private WaveFileWriter writer;
        private string currentRecordingPath;
        private Timer durationTimer;
        private readonly ObservableCollection<double> _waveformData;
        private readonly Random _random = new Random(); // 临时使用随机数模拟波形
        private DispatcherTimer? _waveformTimer;
        //[ObservableProperty]

        private bool isRecording = false;

        [ObservableProperty]
        private TimeSpan recordingDuration = TimeSpan.Zero;

        [ObservableProperty]
        private string transcriptionText = string.Empty;

        public ObservableCollection<double> WaveformData => _waveformData;

        public ObservableCollection<RecordingItem> Recordings { get; } = new();

        private ISpeechRecognitionService voskService;

        private WaveFormat whisperFormat = new WaveFormat(16000, 16, 1);

        private readonly Queue<byte[]> audioQueue = new Queue<byte[]>();
        private bool isProcessing = false;
        private readonly int BUFFER_SECONDS = 5; // 累积5秒再处理
        private DateTime lastProcessTime = DateTime.MinValue;

        private readonly Queue<byte[]> transcriptionQueue = new Queue<byte[]>();
        private Task? processingTask;

        private readonly string recordingsJsonPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SoundRecorder",
            "recordings.json"
        );

        // 添加 MemoryStream 字段
        private MemoryStream audioBuffer;

        public MainViewModel()
        {
            _waveformData = new ObservableCollection<double>();
            // 初始化30个波形条
            for (int i = 0; i < 30; i++)
            {
                _waveformData.Add(10);
            }

            durationTimer = new Timer(1000);
            durationTimer.Elapsed += (s, e) =>
            {
                RecordingDuration = RecordingDuration.Add(TimeSpan.FromSeconds(1));
            };

            // 加载保存的录音列表
            LoadRecordings();

            voskService = new VoskSpeechRecognitionService();

            voskService.OnTranscriptionResult += (s, text) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TranscriptionText = text + " ";
                });
            };

            voskService.OnPartialResult += (s, text) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdatePartialTranscription(text);
                });
            };

            InitializeServicesAsync().ConfigureAwait(false);
        }

        private void LoadRecordings()
        {
            try
            {
                if (File.Exists(recordingsJsonPath))
                {
                    var json = File.ReadAllText(recordingsJsonPath);
                    var recordingInfos = JsonSerializer.Deserialize<List<RecordingInfo>>(json);

                    foreach (var info in recordingInfos)
                    {
                        if (File.Exists(info.FilePath))
                        {
                            var recording = new RecordingItem
                            {
                                Name = info.Name,
                                FilePath = info.FilePath,
                                CreatedAt = info.CreatedAt,
                                Duration = info.Duration,
                                TranscriptionText = info.TranscriptionText
                            };
                            Recordings.Add(recording);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载录音列表失败: {ex.Message}");
            }
        }

        private void SaveRecordings()
        {
            try
            {
                var directory = Path.GetDirectoryName(recordingsJsonPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var recordingInfos = Recordings.Select(r => new RecordingInfo
                {
                    Name = r.Name,
                    FilePath = r.FilePath,
                    CreatedAt = r.CreatedAt,
                    Duration = r.Duration,
                    TranscriptionText = r.TranscriptionText
                }).ToList();

                var json = JsonSerializer.Serialize(recordingInfos, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(recordingsJsonPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存录音列表失败: {ex.Message}");
            }
        }

        private async Task InitializeServicesAsync()
        {
            await voskService.InitializeAsync();
        }

        [RelayCommand]
        private void ToggleRecording()
        {
            if (!IsRecording)
            {
                StartRecording();
            }
            else
            {
                PauseRecording();
            }
        }

        [RelayCommand]
        private void StopRecording()
        {
            if (IsRecording)
            {
                FinishRecording();
            }
        }

        [RelayCommand]
        private void CancelRecording()
        {
            if (IsRecording)
            {
                CleanupRecording(true);
            }
        }

        private void StartRecording()
        {
            try
            {
                currentRecordingPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav"
                );

                waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1),
                    BufferMilliseconds = 50,
                    DeviceNumber = 0 // 确保使用默认录音设备
                };

                // 确保目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(currentRecordingPath));

                writer = new WaveFileWriter(currentRecordingPath, waveIn.WaveFormat);

                waveIn.DataAvailable += OnDataAvailable;
                waveIn.RecordingStopped += (s, e) =>
                {
                    // 处理录音停止事件
                    if (e.Exception != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"录音错误: {e.Exception.Message}");
                    }
                };

                waveIn.StartRecording();

                IsRecording = true;
                RecordingDuration = TimeSpan.Zero;
                durationTimer.Start();

                // 初始化波形为底线
                for (int i = 0; i < WaveformData.Count; i++)
                {
                    WaveformData[i] = 2;
                }

                audioBuffer = new MemoryStream();
                TranscriptionText = string.Empty;
                transcriptionQueue.Clear();
                processingTask = null;
                lastProcessTime = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动录音失败: {ex.Message}");
                //throw; // 在开发阶段抛出异常以便调试
            }
        }

        private void PauseRecording()
        {
            waveIn?.StopRecording();
            durationTimer.Stop();
        }

        private async void FinishRecording()
        {
            if (IsRecording)
            {
                // 保存当前的录音时长
                var duration = RecordingDuration;

                // 停止录音并保存
                waveIn?.StopRecording();
                writer?.Close();

                // 创建新的录音记录
                var recordingItem = new RecordingItem
                {
                    Name = Path.GetFileName(currentRecordingPath),
                    FilePath = currentRecordingPath,
                    CreatedAt = DateTime.Now,
                    Duration = duration
                };

                // 添加到录音列表
                Recordings.Add(recordingItem);

                Task.Run(async () =>
                {
                    // 开始转写
                    await TranscribeRecordingAsync(recordingItem);
                });

                // 保存录音列表
                SaveRecordings();

                // 清理资源
                CleanupRecording(false);
            }
        }

        private async Task TranscribeRecordingAsync(RecordingItem recording)
        {
            try
            {
                recording.IsTranscribing = true;
                recording.TranscriptionText = await voskService.TranscribeFileAsync(recording.FilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"转写错误: {ex.Message}");
            }
            finally
            {
                recording.IsTranscribing = false;
            }
        }

        private void CleanupRecording(bool deleteFile)
        {
            durationTimer.Stop();
            waveIn?.StopRecording();
            waveIn?.Dispose();
            writer?.Dispose();

            if (deleteFile && File.Exists(currentRecordingPath))
            {
                File.Delete(currentRecordingPath);
            }

            IsRecording = false;
            RecordingDuration = TimeSpan.Zero;

            for (int i = 0; i < WaveformData.Count; i++)
            {
                WaveformData[i] = 2;
            }

            audioBuffer?.Dispose();
            audioBuffer = null;
            transcriptionQueue.Clear();
            processingTask = null;
            lastProcessTime = DateTime.MinValue;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                writer?.Write(e.Buffer, 0, e.BytesRecorded);

                voskService.ProcessRealTimeAudio(e.Buffer, e.BytesRecorded);

                // 波形显示相关代码
                float max = 0;
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                    float sample32 = sample / 32768f;
                    if (Math.Abs(sample32) > max) max = Math.Abs(sample32);
                }

                double height = 2 + (max * 98);
                UpdateWaveform(height);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理音频数据错误: {ex.Message}");
            }
        }

        private void UpdatePartialTranscription(string text)
        {
            try
            {
                var lines = TranscriptionText.Split('\n').ToList();
                if (lines.Count > 0)
                {
                    lines[lines.Count - 1] = text;
                    TranscriptionText = string.Join("\n", lines);
                }
                else
                {
                    TranscriptionText = text;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新转写文本错误: {ex.Message}");
            }
        }

        private void TTS(WaveInEventArgs e)
        {
            // 将数据写入 MemoryStream
            audioBuffer?.Write(e.Buffer, 0, e.BytesRecorded);

            // 检查缓冲区大小
            if (audioBuffer?.Length >= waveIn.WaveFormat.AverageBytesPerSecond * 1 && // 1秒的数据
                (DateTime.Now - lastProcessTime).TotalSeconds >= 1)
            {
                // 获取当前缓冲区的数据
                var audioData = audioBuffer.ToArray();

                // 清空缓冲区，准备接收新数据
                audioBuffer.SetLength(0);
                lastProcessTime = DateTime.Now;

                // 将数据加入队列
                transcriptionQueue.Enqueue(audioData);

                // 如果没有正在处理的任务，启动新的处理任务
                if (processingTask == null || processingTask.IsCompleted)
                {
                    processingTask = Task.Run(ProcessTranscriptionQueueAsync);
                }
            }
        }

        private void UpdateWaveform(double level)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                WaveformData.RemoveAt(0);
                WaveformData.Add(level);
            });
        }

        private void StopWaveformAnimation()
        {
            // 重置波形为底线
            for (int i = 0; i < WaveformData.Count; i++)
            {
                WaveformData[i] = 2;
            }
        }

        private async Task TranscribeAudioAsync(byte[] audioData)
        {
            try
            {
                // 将原始音频数据转换为 Wave 格式
                using var originalStream = new MemoryStream(audioData);
                using var reader = new RawSourceWaveStream(originalStream, waveIn.WaveFormat);

                // 转换为 16kHz, 16-bit, mono 格式
                using var convertedStream = new MemoryStream();
                using (var writer = new WaveFileWriter(convertedStream, whisperFormat))
                {
                    using var resampler = new MediaFoundationResampler(reader, whisperFormat);
                    resampler.ResamplerQuality = 60;

                    byte[] buffer = new byte[4096];
                    int read;
                    while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, read);
                    }
                }

                // 获取转换后的音频数据
                var convertedData = convertedStream.ToArray();

                // 使用 ProcessRealTimeAudio 进行实时转写
                if (voskService.ProcessRealTimeAudio(convertedData, convertedData.Length))
                {
                    // 处理结果会通过事件回调
                    System.Diagnostics.Debug.WriteLine("音频数据处理成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("音频数据处理失败");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"转写错误: {ex.Message}");
            }
        }

        private async Task ProcessTranscriptionQueueAsync()
        {
            while (transcriptionQueue.Count > 0)
            {
                var audioData = transcriptionQueue.Dequeue();
                try
                {
                    await TranscribeAudioAsync(audioData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"转写错误: {ex.Message}");
                }
            }
        }

        public bool IsRecording
        {
            get => isRecording;
            set
            {
                if (isRecording != value)
                {
                    isRecording = value;
                    if (!isRecording)
                    {
                        StopWaveformAnimation();
                    }
                    OnPropertyChanged();
                }
            }
        }

        // 在删除录音时也需要更新保存的列表
        public void DeleteRecording(RecordingItem recording)
        {
            recording.Delete();
            Recordings.Remove(recording);
            SaveRecordings();
        }
    }

    // 用于序列化的数据类
    public class RecordingInfo
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public string TranscriptionText { get; set; }
    }
}