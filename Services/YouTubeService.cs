using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClipsAutomation.Models;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace ClipsAutomation.Services
{
    public class YouTubeService : IYouTubeService
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly ITranscriptionService _transcriptionService;
        private readonly string _tempPath;

        public YouTubeService(
            IConfiguration configuration, 
            ITranscriptionService transcriptionService)
        {
            _youtubeClient = new YoutubeClient();
            _transcriptionService = transcriptionService;
            _tempPath = Path.Combine(
                Environment.CurrentDirectory,
                configuration["Storage:TempPath"] ?? "wwwroot\\temp"
            );

            // Ensure temp directory exists
            Directory.CreateDirectory(_tempPath);
        }

        public async Task<VideoInfo> GetVideoInfoAsync(string youtubeUrl)
        {
            // Extract video ID from URL
            string videoId = ExtractVideoId(youtubeUrl);
            
            if (string.IsNullOrEmpty(videoId))
            {
                throw new ArgumentException("Invalid YouTube URL");
            }

            // Get video info using YoutubeExplode
            var video = await _youtubeClient.Videos.GetAsync(videoId);

            return new VideoInfo
            {
                VideoId = videoId,
                Title = video.Title,
                Description = video.Description,
                DurationSeconds = (int)video.Duration.GetValueOrDefault().TotalSeconds,
                ThumbnailUrl = video.Thumbnails.FirstOrDefault()?.Url,
                ChannelTitle = video.Author.ChannelTitle
            };
        }

        public async Task<string> DownloadVideoAsync(string videoId, string outputPath)
        {
            // Make sure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // Get stream manifest 
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);

            // Get best quality muxed stream
            var streamInfo = streamManifest
                .GetMuxedStreams()
                .OrderByDescending(s => s.VideoQuality)
                .FirstOrDefault();

            if (streamInfo == null)
            {
                // If no muxed stream is available, try getting audio and video separately
                // and combining them later with FFmpeg (not implemented here)
                throw new InvalidOperationException("No suitable stream found for download");
            }

            // Download the stream to a file
            await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, outputPath);
            
            return outputPath;
        }

        public async Task<(string transcriptPath, bool wasFound)> GetTranscriptAsync(string videoId, string outputPath)
        {
            try
            {
                // First check if we have a cached version of the transcript
                if (File.Exists(outputPath))
                {
                    return (outputPath, true);
                }
                
                // Download the video to a temporary file for transcription
                string tempVideoPath = Path.Combine(_tempPath, $"{videoId}_temp.mp4");
                await DownloadVideoAsync(videoId, tempVideoPath);
                
                // Use the transcription service to transcribe the video
                var result = await _transcriptionService.TranscribeAsync(tempVideoPath, outputPath);
                
                // Clean up the temporary video file
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error fetching transcript: {ex.Message}");
                return (outputPath, false);
            }
        }

        private string ExtractVideoId(string youtubeUrl)
        {
            // Patterns to extract video ID from different YouTube URL formats
            var videoIdPatterns = new[]
            {
                @"youtu\.be\/([a-zA-Z0-9_-]{11})", // youtu.be/{id}
                @"youtube\.com\/watch\?v=([a-zA-Z0-9_-]{11})", // youtube.com/watch?v={id}
                @"youtube\.com\/embed\/([a-zA-Z0-9_-]{11})", // youtube.com/embed/{id}
                @"youtube\.com\/v\/([a-zA-Z0-9_-]{11})" // youtube.com/v/{id}
            };

            foreach (var pattern in videoIdPatterns)
            {
                var match = Regex.Match(youtubeUrl, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }

            return string.Empty;
        }
    }
} 