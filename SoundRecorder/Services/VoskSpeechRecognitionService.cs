using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NAudio.Wave;
using Vosk;
namespace SoundRecorder.Services
{
    public class VoskSpeechRecognitionService : ISpeechRecognitionService
    {
        private Model model;
        private VoskRecognizer recognizer;

        public event EventHandler<string> OnTranscriptionResult;
        public event EventHandler<string> OnPartialResult;

        public async Task InitializeAsync()
        {
            var modelPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SoundRecorder",
                "vosk-model-small-cn"
            );

            if (!Directory.Exists(modelPath))
            {
                await DownloadAndExtractModelAsync(modelPath);
            }

            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
            recognizer.SetMaxAlternatives(0);
            recognizer.SetWords(true);
        }

        private async Task DownloadAndExtractModelAsync(string modelPath)
        {
            var modelZipPath = Path.Combine(
                Path.GetDirectoryName(modelPath),
                "vosk-model-small-cn.zip"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(modelZipPath));

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("https://alphacephei.com/vosk/models/vosk-model-small-cn-0.22.zip");
                using (var fs = new FileStream(modelZipPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(modelZipPath, Path.GetDirectoryName(modelPath));
            File.Delete(modelZipPath);
        }

        public async Task<string> TranscribeFileAsync(string filePath)
        {
            try
            {
                // 创建新的识别器实例，避免和实时识别冲突
                using var fileRecognizer = new VoskRecognizer(model, 16000.0f);
                fileRecognizer.SetMaxAlternatives(0);
                fileRecognizer.SetWords(true);

                using var audioFile = new AudioFileReader(filePath);
                
                // 如果需要重采样
                WaveFormat targetFormat = new WaveFormat(16000, 16, 1);
                using var resampler = new MediaFoundationResampler(audioFile, targetFormat);
                resampler.ResamplerQuality = 60; // 高质量重采样

                var buffer = new byte[4096];
                var transcription = new StringBuilder();
                int bytesRead;

                while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (fileRecognizer.AcceptWaveform(buffer, bytesRead))
                    {
                        var result = fileRecognizer.Result();
                        var text = JsonDocument.Parse(result)
                            .RootElement
                            .GetProperty("text")
                            .GetString();

                        if (!string.IsNullOrEmpty(text))
                        {
                            transcription.AppendLine(text).Append(" ");
                        }
                    }
                }

                // 获取最后的识别结果
                var finalResult = fileRecognizer.FinalResult();
                var finalText = JsonDocument.Parse(finalResult)
                    .RootElement
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrEmpty(finalText))
                {
                    transcription.AppendLine(finalText);
                }

                var resultText = transcription.ToString().Trim();
               
                return resultText.Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"转写文件失败: {ex.Message}");
                return string.Empty;
            }
        }

        public bool ProcessRealTimeAudio(byte[] buffer, int bytesRecorded)
        {
            try
            {
                if (recognizer.AcceptWaveform(buffer, bytesRecorded))
                {
                    var result = recognizer.Result();
                    var text = JsonDocument.Parse(result)
                        .RootElement
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrEmpty(text))
                    {
                        OnTranscriptionResult?.Invoke(this, text);
                    }
                }
                else
                {
                    var partial = recognizer.PartialResult();
                    var text = JsonDocument.Parse(partial)
                        .RootElement
                        .GetProperty("partial")
                        .GetString();

                    if (!string.IsNullOrEmpty(text))
                    {
                        OnPartialResult?.Invoke(this, text);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            recognizer?.Dispose();
            model?.Dispose();
        }
    }

}