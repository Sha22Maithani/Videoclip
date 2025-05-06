using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClipsAutomation.Services
{
    public class AssemblyAITranscriptionService : ITranscriptionService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public AssemblyAITranscriptionService(IConfiguration configuration, HttpClient httpClient)
        {
            _apiKey = configuration["AssemblyAI:ApiKey"] ?? throw new ArgumentNullException("AssemblyAI API key is missing");
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_apiKey);
        }

        public async Task<(string transcriptPath, bool wasSuccessful)> TranscribeAsync(string filePath, string outputPath)
        {
            try
            {
                // 1. Upload the file to AssemblyAI
                string uploadUrl = await UploadFileAsync(filePath);
                if (string.IsNullOrEmpty(uploadUrl))
                {
                    return (outputPath, false);
                }

                // 2. Submit transcription request
                string transcriptId = await SubmitTranscriptionRequestAsync(uploadUrl);
                if (string.IsNullOrEmpty(transcriptId))
                {
                    return (outputPath, false);
                }

                // 3. Wait and poll for the transcription to complete
                var transcriptionResult = await WaitForTranscriptionAsync(transcriptId);
                if (transcriptionResult == null)
                {
                    return (outputPath, false);
                }

                // 4. Parse and save the transcript in the format expected by our application
                await SaveTranscriptAsync(transcriptionResult, outputPath);
                
                return (outputPath, true);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in transcription: {ex.Message}");
                return (outputPath, false);
            }
        }

        private async Task<string> UploadFileAsync(string filePath)
        {
            // Read the file as a byte array
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

            // Create the upload request
            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.assemblyai.com/v2/upload")
            {
                Content = new ByteArrayContent(fileBytes)
            };

            // Send the request
            var response = await _httpClient.SendAsync(uploadRequest);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error uploading file: {response.StatusCode}");
                return string.Empty;
            }

            // Parse the response
            string responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseJson);
            return responseObject["upload_url"]?.ToString() ?? string.Empty;
        }

        private async Task<string> SubmitTranscriptionRequestAsync(string uploadUrl)
        {
            // Create the transcription request
            var transcriptionOptions = new
            {
                audio_url = uploadUrl,
                language_model = "assemblyai_default",
                punctuate = true,
                format_text = true,
                speaker_labels = false,
                auto_chapters = true,
                entity_detection = true
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(transcriptionOptions),
                Encoding.UTF8,
                "application/json");

            // Send the request
            var response = await _httpClient.PostAsync("https://api.assemblyai.com/v2/transcript", content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error submitting transcription: {response.StatusCode}");
                return string.Empty;
            }

            // Parse the response
            string responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseJson);
            return responseObject["id"]?.ToString() ?? string.Empty;
        }

        private async Task<JObject> WaitForTranscriptionAsync(string transcriptId)
        {
            string pollingUrl = $"https://api.assemblyai.com/v2/transcript/{transcriptId}";
            int maxAttempts = 60; // 5 minutes with 5-second interval
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                var response = await _httpClient.GetAsync(pollingUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error checking transcription status: {response.StatusCode}");
                    return null;
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                var responseObject = JObject.Parse(responseJson);
                string status = responseObject["status"]?.ToString();

                if (status == "completed")
                {
                    return responseObject;
                }
                else if (status == "error")
                {
                    Console.WriteLine($"Transcription error: {responseObject["error"]}");
                    return null;
                }

                // Wait 5 seconds before checking again
                await Task.Delay(5000);
                attempt++;
            }

            Console.WriteLine("Transcription timed out");
            return null;
        }

        private async Task SaveTranscriptAsync(JObject transcriptionResult, string outputPath)
        {
            // Ensure the output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // Get words with timestamps
            var words = transcriptionResult["words"]?.ToObject<JArray>();
            if (words == null || words.Count == 0)
            {
                // If we don't have word-level timestamps, use the entire transcript
                string fullText = transcriptionResult["text"]?.ToString() ?? "No transcript available";
                await File.WriteAllTextAsync(outputPath, $"00:00:00 {fullText}");
                return;
            }

            // Format the transcript with timestamps in the format expected by our application
            // Format: HH:MM:SS Text
            using (var writer = new StreamWriter(outputPath))
            {
                double lastTimestamp = -1;
                StringBuilder currentLine = new StringBuilder();

                foreach (var word in words)
                {
                    double startMs = word["start"]?.Value<double>() ?? 0;
                    double startSeconds = startMs / 1000;
                    string wordText = word["text"]?.ToString() ?? "";

                    // If this is a new timestamp (or the first word), write the previous line and start a new one
                    if (lastTimestamp != startSeconds && lastTimestamp != -1)
                    {
                        await writer.WriteLineAsync($"{FormatTimestamp(lastTimestamp)} {currentLine}");
                        currentLine.Clear();
                    }

                    // Add the word to the current line
                    if (currentLine.Length > 0)
                    {
                        currentLine.Append(" ");
                    }
                    currentLine.Append(wordText);
                    lastTimestamp = startSeconds;
                }

                // Write the last line
                if (currentLine.Length > 0)
                {
                    await writer.WriteLineAsync($"{FormatTimestamp(lastTimestamp)} {currentLine}");
                }
            }
        }

        private string FormatTimestamp(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
    }
} 