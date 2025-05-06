using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClipsAutomation.Models;

namespace ClipsAutomation.Services
{
    public class ContentAnalysisService : IContentAnalysisService
    {
        // In a real-world implementation, this would use ML.NET for more sophisticated analysis
        // For demonstration, we'll use a simple approach
        
        // Keywords that might indicate engaging content
        private readonly string[] _engagingKeywords = new[]
        {
            "amazing", "wow", "incredible", "unbelievable", "shocking",
            "important", "fascinating", "secret", "reveal", "exclusive",
            "first time", "never before", "breaking", "discover", "learn",
            "best", "worst", "most", "least", "top", "favorite"
        };

        public async Task<List<VideoSegment>> AnalyzeTranscriptAsync(string transcriptPath, ProcessingOptions options)
        {
            // In a real implementation, this would use ML.NET for more sophisticated analysis
            // For now, we'll use a simple approach parsing the transcript and scoring based on keywords
            
            var segments = new List<VideoSegment>();
            
            // Read transcript file
            string[] lines = await File.ReadAllLinesAsync(transcriptPath);
            
            // Simple parsing of transcript lines (assumes format: "HH:MM:SS Text")
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Skip header or empty lines
                if (string.IsNullOrWhiteSpace(line) || !line.Contains(' '))
                    continue;
                
                // Try to parse timestamp
                var parts = line.Split(' ', 2);
                if (parts.Length < 2)
                    continue;
                
                var timestampStr = parts[0];
                var text = parts[1];
                
                // Calculate start time in seconds
                double startTimeSeconds = ParseTimestamp(timestampStr);
                
                // Calculate end time (either next timestamp or +10 seconds)
                double endTimeSeconds;
                if (i < lines.Length - 1 && lines[i + 1].Contains(' '))
                {
                    var nextTimestampStr = lines[i + 1].Split(' ', 2)[0];
                    endTimeSeconds = ParseTimestamp(nextTimestampStr);
                }
                else
                {
                    endTimeSeconds = startTimeSeconds + 10; // Default 10-second segment
                }
                
                // Calculate engagement score based on keywords and other factors
                double engagementScore = CalculateEngagementScore(text);
                
                // Create segment
                var segment = new VideoSegment
                {
                    StartTimeSeconds = startTimeSeconds,
                    EndTimeSeconds = endTimeSeconds,
                    TranscriptText = text,
                    EngagementScore = engagementScore,
                    SelectedForClip = false // Will be set later
                };
                
                segments.Add(segment);
            }
            
            return segments;
        }

        public Task<List<VideoSegment>> SelectEngagingSegmentsAsync(List<VideoSegment> segments, ProcessingOptions options)
        {
            // Sort segments by engagement score in descending order
            var sortedSegments = segments
                .OrderByDescending(s => s.EngagementScore)
                .ToList();
            
            // Create clips based on minimum and maximum durations
            var selectedSegments = new List<VideoSegment>();
            double totalDuration = 0;
            int clipCount = 0;
            
            foreach (var segment in sortedSegments)
            {
                double segmentDuration = segment.EndTimeSeconds - segment.StartTimeSeconds;
                
                // Skip very short segments
                if (segmentDuration < 3) 
                    continue;
                
                // If this segment would make a clip too long, skip it
                if (segmentDuration > options.MaxClipDurationSeconds)
                    continue;
                
                // If we've reached the maximum number of clips, stop
                if (clipCount >= options.MaxNumberOfClips)
                    break;
                
                // Mark segment as selected
                segment.SelectedForClip = true;
                selectedSegments.Add(segment);
                totalDuration += segmentDuration;
                clipCount++;
            }
            
            return Task.FromResult(selectedSegments);
        }

        public Task<List<GeneratedClip>> CreateClipConfigurationsAsync(List<VideoSegment> selectedSegments, VideoProject videoProject)
        {
            var clips = new List<GeneratedClip>();
            
            // Group nearby segments for each clip
            var currentSegments = new List<VideoSegment>();
            double currentStartTime = 0;
            double currentDuration = 0;
            int clipIndex = 1;
            
            foreach (var segment in selectedSegments.OrderBy(s => s.StartTimeSeconds))
            {
                double segmentDuration = segment.EndTimeSeconds - segment.StartTimeSeconds;
                
                // If this is the first segment, or if it's close to the previous one, add it to the current group
                if (currentSegments.Count == 0 || segment.StartTimeSeconds - currentStartTime - currentDuration < 5)
                {
                    if (currentSegments.Count == 0)
                    {
                        currentStartTime = segment.StartTimeSeconds;
                    }
                    
                    currentSegments.Add(segment);
                    currentDuration = segment.EndTimeSeconds - currentStartTime;
                }
                else
                {
                    // Create a clip from the current group
                    CreateClip(clips, currentSegments, videoProject, clipIndex++);
                    
                    // Start a new group
                    currentSegments = new List<VideoSegment> { segment };
                    currentStartTime = segment.StartTimeSeconds;
                    currentDuration = segmentDuration;
                }
            }
            
            // Create a clip from the last group if any
            if (currentSegments.Count > 0)
            {
                CreateClip(clips, currentSegments, videoProject, clipIndex);
            }
            
            return Task.FromResult(clips);
        }
        
        private void CreateClip(List<GeneratedClip> clips, List<VideoSegment> segments, VideoProject videoProject, int index)
        {
            if (segments.Count == 0)
                return;
                
            var firstSegment = segments.First();
            var lastSegment = segments.Last();
            
            var clipTitle = videoProject.VideoTitle != null
                ? $"{videoProject.VideoTitle} - Clip {index}"
                : $"Clip {index}";
                
            var clip = new GeneratedClip
            {
                VideoProjectId = videoProject.Id,
                Title = clipTitle,
                FilePath = "", // Will be set during processing
                DurationSeconds = lastSegment.EndTimeSeconds - firstSegment.StartTimeSeconds,
                CreatedAt = DateTime.UtcNow,
                HasCaptions = false, // Will be set during processing
                UploadStatus = "Not Uploaded",
                IncludedSegments = segments
            };
            
            clips.Add(clip);
        }

        private double ParseTimestamp(string timestamp)
        {
            // Parse timestamp format (e.g., "00:01:23" or "01:23")
            var match = Regex.Match(timestamp, @"(?:(\d+):)?(\d+):(\d+)");
            
            if (match.Success)
            {
                int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                int minutes = int.Parse(match.Groups[2].Value);
                int seconds = int.Parse(match.Groups[3].Value);
                
                return hours * 3600 + minutes * 60 + seconds;
            }
            
            return 0;
        }

        private double CalculateEngagementScore(string text)
        {
            // A simple scoring algorithm based on keywords and text properties
            double score = 0;
            
            // Check for engaging keywords
            foreach (var keyword in _engagingKeywords)
            {
                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.5;
                }
            }
            
            // Longer segments may be more substantial
            score += Math.Min(text.Length / 100.0, 1.0);
            
            // Questions might be engaging
            if (text.Contains('?'))
            {
                score += 0.5;
            }
            
            // Exclamation may indicate excitement
            if (text.Contains('!'))
            {
                score += 0.3;
            }
            
            // Add some randomness for variety
            score += new Random().NextDouble() * 0.3;
            
            return score;
        }
    }
} 