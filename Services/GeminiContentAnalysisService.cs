using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClipsAutomation.Models;
using Newtonsoft.Json;

namespace ClipsAutomation.Services
{
    public class GeminiContentAnalysisService : IContentAnalysisService
    {
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly dynamic _model;

        public GeminiContentAnalysisService(IConfiguration configuration)
        {
            _apiKey = configuration["GoogleGenerativeAI:ApiKey"] ?? throw new ArgumentNullException("Google Generative AI API key is missing");
            _modelName = configuration["GoogleGenerativeAI:Model"] ?? "gemini-1.5-flash";
            
            // Initialize the Gemini model - temporarily using dynamic to resolve the namespace issue
            dynamic googleAI = Activator.CreateInstance(Type.GetType("GenerativeAI.GoogleAI, Google_GenerativeAI"), new object[] { _apiKey });
            _model = googleAI.CreateGenerativeModel(_modelName);
        }

        public async Task<List<VideoSegment>> AnalyzeTranscriptAsync(string transcriptPath, ProcessingOptions options)
        {
            // Read the transcript file
            string[] lines = await File.ReadAllLinesAsync(transcriptPath);
            
            // Parse the transcript into segments
            var segments = ParseTranscript(lines);
            
            // Group segments into chunks suitable for LLM analysis
            var chunks = CreateContentChunks(segments);
            
            // Analyze each chunk with Gemini
            foreach (var chunk in chunks)
            {
                await AnalyzeSegmentsWithGeminiAsync(chunk);
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
        
        private List<VideoSegment> ParseTranscript(string[] lines)
        {
            var segments = new List<VideoSegment>();
            
            // Parse the transcript lines (format: "HH:MM:SS Text")
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
                
                // Create segment (engagement score will be determined by Gemini)
                var segment = new VideoSegment
                {
                    StartTimeSeconds = startTimeSeconds,
                    EndTimeSeconds = endTimeSeconds,
                    TranscriptText = text,
                    EngagementScore = 0, // Will be set by Gemini analysis
                    SelectedForClip = false // Will be set later
                };
                
                segments.Add(segment);
            }
            
            return segments;
        }
        
        private List<List<VideoSegment>> CreateContentChunks(List<VideoSegment> segments)
        {
            // Group segments into chunks to avoid token limits in the LLM
            var chunks = new List<List<VideoSegment>>();
            var currentChunk = new List<VideoSegment>();
            int currentTokenEstimate = 0;
            int maxTokensPerChunk = 4000; // Conservative limit to leave room for model response
            
            foreach (var segment in segments)
            {
                // Rough token estimate (4 characters per token is a common approximation)
                int segmentTokens = segment.TranscriptText.Length / 4;
                
                // If adding this segment would exceed the token limit, start a new chunk
                if (currentTokenEstimate + segmentTokens > maxTokensPerChunk && currentChunk.Count > 0)
                {
                    chunks.Add(currentChunk);
                    currentChunk = new List<VideoSegment>();
                    currentTokenEstimate = 0;
                }
                
                currentChunk.Add(segment);
                currentTokenEstimate += segmentTokens;
            }
            
            // Add the last chunk if it has any segments
            if (currentChunk.Count > 0)
            {
                chunks.Add(currentChunk);
            }
            
            return chunks;
        }
        
        private async Task AnalyzeSegmentsWithGeminiAsync(List<VideoSegment> segments)
        {
            try
            {
                // Format the transcript segments for the model
                StringBuilder promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("Below is a transcript from a YouTube video with timestamps. Analyze each segment and assign an engagement score from 0.0 to 10.0 based on how likely it would make compelling content for a YouTube Short.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Consider the following factors in your scoring:");
                promptBuilder.AppendLine("- Emotional content (excitement, surprise, humor)");
                promptBuilder.AppendLine("- Important facts or revelations");
                promptBuilder.AppendLine("- Controversial or surprising statements");
                promptBuilder.AppendLine("- Well-structured explanations of complex topics");
                promptBuilder.AppendLine("- Quotable moments");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Transcript segments:");
                promptBuilder.AppendLine();
                
                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    promptBuilder.AppendLine($"[{i}] {FormatTimestamp(segment.StartTimeSeconds)} - {FormatTimestamp(segment.EndTimeSeconds)}: \"{segment.TranscriptText}\"");
                }
                
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("For each segment, return a JSON object with the segment index and engagement score. Format your response as follows:");
                promptBuilder.AppendLine("{");
                promptBuilder.AppendLine("  \"scores\": [");
                promptBuilder.AppendLine("    { \"index\": 0, \"score\": 7.5, \"reason\": \"Brief explanation of why this score was assigned\" },");
                promptBuilder.AppendLine("    { \"index\": 1, \"score\": 3.2, \"reason\": \"Brief explanation of why this score was assigned\" }");
                promptBuilder.AppendLine("    // and so on for each segment");
                promptBuilder.AppendLine("  ]");
                promptBuilder.AppendLine("}");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Only provide the JSON object in your response, with no additional text.");
                
                // Request analysis from Gemini
                var response = await _model.GenerateContentAsync(promptBuilder.ToString());
                
                if (response?.Text() != null)
                {
                    // Extract JSON from the response
                    string jsonText = response.Text().Trim();
                    
                    // Remove any markdown code block delimiters if present
                    jsonText = Regex.Replace(jsonText, @"^```json\s*", "", RegexOptions.Multiline);
                    jsonText = Regex.Replace(jsonText, @"^```\s*$", "", RegexOptions.Multiline);
                    
                    try
                    {
                        // Parse the JSON response
                        var result = JsonConvert.DeserializeObject<EngagementAnalysisResult>(jsonText);
                        
                        if (result?.Scores != null)
                        {
                            // Update the engagement scores for each segment
                            foreach (var scoreInfo in result.Scores)
                            {
                                if (scoreInfo.Index >= 0 && scoreInfo.Index < segments.Count)
                                {
                                    segments[scoreInfo.Index].EngagementScore = scoreInfo.Score;
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error parsing Gemini response: {ex.Message}");
                        // Fallback to a basic scoring method
                        AssignBasicScores(segments);
                    }
                }
                else
                {
                    // Fallback to a basic scoring method if Gemini didn't provide a valid response
                    AssignBasicScores(segments);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Gemini analysis: {ex.Message}");
                // Fallback to a basic scoring method
                AssignBasicScores(segments);
            }
        }
        
        private void AssignBasicScores(List<VideoSegment> segments)
        {
            // Keywords that might indicate engaging content (fallback method)
            string[] engagingKeywords = new[]
            {
                "amazing", "wow", "incredible", "unbelievable", "shocking",
                "important", "fascinating", "secret", "reveal", "exclusive",
                "first time", "never before", "breaking", "discover", "learn",
                "best", "worst", "most", "least", "top", "favorite"
            };
            
            foreach (var segment in segments)
            {
                double score = 0;
                
                // Check for engaging keywords
                foreach (var keyword in engagingKeywords)
                {
                    if (segment.TranscriptText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.5;
                    }
                }
                
                // Longer segments may be more substantial
                score += Math.Min(segment.TranscriptText.Length / 100.0, 1.0);
                
                // Questions might be engaging
                if (segment.TranscriptText.Contains('?'))
                {
                    score += 0.5;
                }
                
                // Exclamation may indicate excitement
                if (segment.TranscriptText.Contains('!'))
                {
                    score += 0.3;
                }
                
                // Add some randomness for variety
                score += new Random().NextDouble() * 0.3;
                
                // Normalize to 0-10 scale
                segment.EngagementScore = Math.Min(score, 10.0);
            }
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
        
        private string FormatTimestamp(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
    }
    
    // Classes for deserializing Gemini API response
    public class EngagementAnalysisResult
    {
        [JsonProperty("scores")]
        public List<ScoreInfo> Scores { get; set; }
    }
    
    public class ScoreInfo
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        
        [JsonProperty("score")]
        public double Score { get; set; }
        
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
} 