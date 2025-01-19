using System;
using System.Threading.Tasks;

namespace SoundRecorder.Services
{
    public interface ISpeechRecognitionService : IDisposable
    {
        Task InitializeAsync();
        Task<string> TranscribeFileAsync(string filePath);
        bool ProcessRealTimeAudio(byte[] buffer, int bytesRecorded);
        event EventHandler<string> OnTranscriptionResult;
        event EventHandler<string> OnPartialResult;
    }
}