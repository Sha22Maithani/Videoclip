using System.Threading.Tasks;
using ClipsAutomation.Models;

namespace ClipsAutomation.Services
{
    public interface IYouTubeService
    {
        /// <summary>
        /// Gets the video information from a YouTube URL
        /// </summary>
        /// <param name="youtubeUrl">The YouTube video URL</param>
        /// <returns>Basic video information</returns>
        Task<VideoInfo> GetVideoInfoAsync(string youtubeUrl);
        
        /// <summary>
        /// Downloads the video from YouTube
        /// </summary>
        /// <param name="videoId">The YouTube video ID</param>
        /// <param name="outputPath">Path where to save the video</param>
        /// <returns>Path to the downloaded video file</returns>
        Task<string> DownloadVideoAsync(string videoId, string outputPath);
        
        /// <summary>
        /// Gets the transcript of a YouTube video
        /// </summary>
        /// <param name="videoId">The YouTube video ID</param>
        /// <param name="outputPath">Path where to save the transcript</param>
        /// <returns>Path to the transcript file and flag indicating if transcript was found</returns>
        Task<(string transcriptPath, bool wasFound)> GetTranscriptAsync(string videoId, string outputPath);
    }

    /// <summary>
    /// Basic information about a YouTube video
    /// </summary>
    public class VideoInfo
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string ChannelTitle { get; set; } = string.Empty;
    }
} 