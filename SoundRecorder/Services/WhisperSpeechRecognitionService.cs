using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace SoundRecorder.Services
{

    public class WhisperSpeechRecognitionService : ISpeechRecognitionService
    {
        private WhisperFactory whisperFactory;
        private WhisperProcessor processor;
        private readonly WaveFormat whisperFormat = new WaveFormat(16000, 16, 1);

        public event EventHandler<string> OnTranscriptionResult;
        public event EventHandler<string> OnPartialResult;

        public async Task InitializeAsync()
        {
            var modelPath = "ggml-tiny.bin";
            if (!File.Exists(modelPath))
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Tiny);
                using var fileStream = File.Create(modelPath);
                await modelStream.CopyToAsync(fileStream);
            }

            whisperFactory = WhisperFactory.FromPath(modelPath);
            processor = whisperFactory.CreateBuilder()
                .WithLanguage("zh")
                .WithThreads(Environment.ProcessorCount)
                .Build();
        }

        public async Task<string> TranscribeFileAsync(string filePath)
        {
            const int CHUNK_SIZE = 30;
            using var audioFile = new AudioFileReader(filePath);
            var duration = audioFile.TotalTime.TotalSeconds;
            var chunks = (int)Math.Ceiling(duration / CHUNK_SIZE);

            var transcription = new List<string>();

            for (int i = 0; i < chunks; i++)
            {
                var chunkBuffer = new byte[audioFile.WaveFormat.AverageBytesPerSecond * CHUNK_SIZE];
                var bytesRead = await Task.Run(() => audioFile.Read(chunkBuffer, 0, chunkBuffer.Length));

                if (bytesRead == 0) break;

                var chunkText = await Task.Run(async () =>
                {
                    using var whisperStream = new MemoryStream(chunkBuffer, 0, bytesRead);
                    var segments = processor.ProcessAsync(whisperStream);
                    var texts = new List<string>();
                    await foreach (var segment in segments)
                    {
                        texts.Add(segment.Text);
                    }
                    return string.Join(" ", texts);
                });

                transcription.Add(chunkText);
            }

            return string.Join(" ", transcription);
        }

        public bool ProcessRealTimeAudio(byte[] buffer, int bytesRecorded)
        {
            // Whisper 不支持实时识别，返回 false
            return false;
        }

        public void Dispose()
        {
            processor?.Dispose();
            whisperFactory?.Dispose();
        }
    }

}