using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClipsAutomation.Models;
using Xabe.FFmpeg;
using System.Collections.Generic;

namespace ClipsAutomation.Services
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly string _tempPath;
        private readonly string _clipsPath;
        private readonly IConfiguration _configuration;

        public VideoProcessingService(IConfiguration configuration)
        {
            _configuration = configuration;
            _tempPath = Path.Combine(
                Environment.CurrentDirectory, 
                configuration["Storage:TempPath"] ?? "wwwroot\\temp"
            );
            _clipsPath = Path.Combine(
                Environment.CurrentDirectory, 
                configuration["Storage:ClipsPath"] ?? "wwwroot\\clips"
            );
            
            // Ensure directories exist
            Directory.CreateDirectory(_tempPath);
            Directory.CreateDirectory(_clipsPath);
            
            // In a production environment, you'd want to ensure FFmpeg is installed
            // Note: FFmpegDownloader functionality is removed
        }

        public async Task<string> GenerateClipAsync(string videoPath, GeneratedClip clipConfig, string outputDirectory)
        {
            if (clipConfig.IncludedSegments == null || !clipConfig.IncludedSegments.Any())
            {
                throw new InvalidOperationException("No segments provided for clip generation");
            }

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Create a unique output filename
            string outputFilename = $"clip_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.mp4";
            string outputPath = Path.Combine(outputDirectory, outputFilename);

            // Get the first and last segment to determine clip boundaries
            var orderedSegments = clipConfig.IncludedSegments.OrderBy(s => s.StartTimeSeconds).ToList();
            var firstSegment = orderedSegments.First();
            var lastSegment = orderedSegments.Last();

            // Calculate start and end times
            var startTime = TimeSpan.FromSeconds(firstSegment.StartTimeSeconds);
            var duration = TimeSpan.FromSeconds(lastSegment.EndTimeSeconds - firstSegment.StartTimeSeconds);

            try
            {
                // Get media info
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(videoPath);

                // Extract clip from the video
                IConversion conversion = await FFmpeg.Conversions.FromSnippet.Split(
                    videoPath,
                    outputPath,
                    startTime,
                    duration
                );

                // Add conversion parameters
                conversion
                    .SetPreset(ConversionPreset.UltraFast) // For testing; use slower presets for better quality
                    .SetOutput(outputPath);

                // Start conversion
                await conversion.Start();

                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating clip: {ex.Message}", ex);
            }
        }

        public async Task<string> AddCaptionsAsync(string clipPath, GeneratedClip clipConfig, string outputDirectory)
        {
            if (clipConfig.IncludedSegments == null || !clipConfig.IncludedSegments.Any())
            {
                return clipPath; // No captions to add
            }

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Create a unique output filename
            string outputFilename = $"captioned_{Path.GetFileNameWithoutExtension(clipPath)}_{Guid.NewGuid():N}.mp4";
            string outputPath = Path.Combine(outputDirectory, outputFilename);

            try
            {
                // In a real implementation, you would:
                // 1. Generate an SRT file from the segments
                // 2. Use FFmpeg to burn the captions into the video
                
                // For simplicity, we'll just simulate by copying the file
                // In real application, use FFmpeg to burn captions
                File.Copy(clipPath, outputPath, true);
                
                // Here's a placeholder for how you might actually do it with FFmpeg:
                /*
                // Create SRT file
                string srtPath = Path.Combine(_tempPath, $"{Guid.NewGuid():N}.srt");
                await GenerateSrtFile(clipConfig.IncludedSegments, srtPath, clipConfig);
                
                // Burn subtitles into the video
                IConversion conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{clipPath}\"")
                    .AddParameter($"-vf \"subtitles='{srtPath}'\"")
                    .SetOutput(outputPath);
                
                await conversion.Start();
                
                // Delete temporary SRT file
                if (File.Exists(srtPath))
                {
                    File.Delete(srtPath);
                }
                */
                
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding captions: {ex.Message}", ex);
            }
        }

        public async Task<string> EnhanceClipAsync(string clipPath, ProcessingOptions options, string outputDirectory)
        {
            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Create a unique output filename
            string outputFilename = $"enhanced_{Path.GetFileNameWithoutExtension(clipPath)}_{Guid.NewGuid():N}.mp4";
            string outputPath = Path.Combine(outputDirectory, outputFilename);

            try
            {
                // This would be a full implementation with FFmpeg to enhance video
                // For example, adding color correction, audio normalization, etc.
                
                // For simplicity, we'll just simulate this by copying the file
                // In a real application, you'd run FFmpeg commands to enhance the video
                File.Copy(clipPath, outputPath, true);
                
                // Here's a placeholder for how you might enhance a video:
                /*
                // Apply video enhancements
                var parameters = new List<string>();
                parameters.Add($"-i \"{clipPath}\"");
                
                if (options.ApplyVideoEnhancements)
                {
                    // Add enhancements like color correction, sharpening, etc.
                    parameters.Add("-vf \"eq=brightness=0.05:saturation=1.3,unsharp=5:5:1.0:5:5:0.0\"");
                }
                
                // Normalize audio
                parameters.Add("-af \"loudnorm=I=-16:TP=-1.5:LRA=11\"");
                
                // If adding background music
                if (options.AddBackgroundMusic)
                {
                    // You would need to implement logic to select an appropriate music file
                    string musicFile = "path_to_music_file.mp3";
                    
                    // Mix original audio with background music at specified volume
                    double musicVolume = options.MusicVolumePercent / 100.0;
                    parameters.Add($"-i \"{musicFile}\" -filter_complex \"[0:a]volume=1[a1];[1:a]volume={musicVolume}[a2];[a1][a2]amix=inputs=2:duration=longest\"");
                }
                
                // Output file
                parameters.Add($"-c:v libx264 -preset medium -crf 23 \"{outputPath}\"");
                
                // Create and run the conversion
                IConversion conversion = FFmpeg.Conversions.New();
                foreach (var param in parameters)
                {
                    conversion.AddParameter(param);
                }
                
                await conversion.Start();
                */
                
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error enhancing clip: {ex.Message}", ex);
            }
        }

        public async Task<string> ConvertToShortsFormatAsync(string clipPath, ProcessingOptions options, string outputDirectory)
        {
            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Create a unique output filename
            string outputFilename = $"shorts_{Path.GetFileNameWithoutExtension(clipPath)}_{Guid.NewGuid():N}.mp4";
            string outputPath = Path.Combine(outputDirectory, outputFilename);

            try
            {
                // Get media info to check current aspect ratio
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(clipPath);
                var videoStream = mediaInfo.VideoStreams.First();
                
                double aspectRatio = (double)videoStream.Width / videoStream.Height;
                
                // Shorts are typically in vertical format (9:16 aspect ratio)
                if (aspectRatio > 0.65)  // If it's not already vertical enough
                {
                    // For horizontal video, need to convert to vertical
                    IConversion conversion = FFmpeg.Conversions.New();
                    
                    if (options.AutoZoomVertical)
                    {
                        // Center crop the video and scale to 9:16 ratio (1080x1920)
                        conversion.AddParameter($"-i \"{clipPath}\"")
                            .AddParameter("-vf \"crop=ih*(9/16):ih,scale=1080:1920:force_original_aspect_ratio=decrease,pad=1080:1920:(ow-iw)/2:(oh-ih)/2\"")
                            .AddParameter("-c:a copy")
                            .AddParameter("-c:v libx264 -preset fast -crf 22")
                            .SetOutput(outputPath);
                    }
                    else
                    {
                        // Add black bars (letterbox) to maintain aspect ratio
                        conversion.AddParameter($"-i \"{clipPath}\"")
                            .AddParameter("-vf \"scale=1080:1920:force_original_aspect_ratio=decrease,pad=1080:1920:(ow-iw)/2:(oh-ih)/2:black\"")
                            .AddParameter("-c:a copy")
                            .AddParameter("-c:v libx264 -preset fast -crf 22")
                            .SetOutput(outputPath);
                    }
                    
                    await conversion.Start();
                }
                else
                {
                    // If it's already close to vertical, just ensure the dimensions are correct
                    IConversion conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-i \"{clipPath}\"")
                        .AddParameter("-vf \"scale=1080:1920:force_original_aspect_ratio=decrease,pad=1080:1920:(ow-iw)/2:(oh-ih)/2:black\"")
                        .AddParameter("-c:a copy")
                        .AddParameter("-c:v libx264 -preset fast -crf 22")
                        .SetOutput(outputPath);
                        
                    await conversion.Start();
                }
                
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting to Shorts format: {ex.Message}", ex);
            }
        }

        // Helper method to generate SRT file from segments (placeholder)
        private async Task GenerateSrtFile(IEnumerable<VideoSegment> segments, string srtPath, GeneratedClip clipConfig)
        {
            double firstSegmentStartTime = segments.Min(s => s.StartTimeSeconds);
            
            using (var writer = new StreamWriter(srtPath))
            {
                int index = 1;
                foreach (var segment in segments.OrderBy(s => s.StartTimeSeconds))
                {
                    // Adjust times to be relative to the clip start
                    double startTime = segment.StartTimeSeconds - firstSegmentStartTime;
                    double endTime = segment.EndTimeSeconds - firstSegmentStartTime;
                    
                    // Format times as SRT timestamps: HH:MM:SS,mmm
                    string startTimeStr = FormatSrtTime(startTime);
                    string endTimeStr = FormatSrtTime(endTime);
                    
                    // Write SRT entry
                    await writer.WriteLineAsync(index.ToString());
                    await writer.WriteLineAsync($"{startTimeStr} --> {endTimeStr}");
                    await writer.WriteLineAsync(segment.TranscriptText);
                    await writer.WriteLineAsync(); // Empty line between entries
                    
                    index++;
                }
            }
        }

        // Format time as SRT timestamp (HH:MM:SS,mmm)
        private string FormatSrtTime(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
        }
    }
} 