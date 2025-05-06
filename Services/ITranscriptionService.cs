using System.Threading.Tasks;

namespace ClipsAutomation.Services
{
    public interface ITranscriptionService
    {
        /// <summary>
        /// Transcribes an audio/video file using speech-to-text technology
        /// </summary>
        /// <param name="filePath">Path to the audio/video file to transcribe</param>
        /// <param name="outputPath">Path where to save the transcript</param>
        /// <returns>Path to the transcript file and flag indicating if transcription was successful</returns>
        Task<(string transcriptPath, bool wasSuccessful)> TranscribeAsync(string filePath, string outputPath);
    }
} 