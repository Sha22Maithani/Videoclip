using System.Threading.Tasks;
using ClipsAutomation.Models;

namespace ClipsAutomation.Services
{
    public interface IVideoProcessingService
    {
        /// <summary>
        /// Generates a short clip from the full video based on the clip configuration
        /// </summary>
        /// <param name="videoPath">Path to the source video file</param>
        /// <param name="clipConfig">Configuration for the clip to generate</param>
        /// <param name="outputDirectory">Directory where to save the generated clip</param>
        /// <returns>Path to the generated clip file</returns>
        Task<string> GenerateClipAsync(string videoPath, GeneratedClip clipConfig, string outputDirectory);
        
        /// <summary>
        /// Adds captions to a video clip based on its transcript segments
        /// </summary>
        /// <param name="clipPath">Path to the clip file</param>
        /// <param name="clipConfig">Configuration with transcript segments</param>
        /// <param name="outputDirectory">Directory where to save the captioned clip</param>
        /// <returns>Path to the captioned clip file</returns>
        Task<string> AddCaptionsAsync(string clipPath, GeneratedClip clipConfig, string outputDirectory);
        
        /// <summary>
        /// Enhances a video clip with effects, transforms, and possibly music
        /// </summary>
        /// <param name="clipPath">Path to the clip file</param>
        /// <param name="options">Processing options for enhancements</param>
        /// <param name="outputDirectory">Directory where to save the enhanced clip</param>
        /// <returns>Path to the enhanced clip file</returns>
        Task<string> EnhanceClipAsync(string clipPath, ProcessingOptions options, string outputDirectory);
        
        /// <summary>
        /// Converts a video clip to YouTube Shorts format (vertical, specific resolution)
        /// </summary>
        /// <param name="clipPath">Path to the clip file</param>
        /// <param name="options">Processing options for conversion</param>
        /// <param name="outputDirectory">Directory where to save the converted clip</param>
        /// <returns>Path to the converted clip file</returns>
        Task<string> ConvertToShortsFormatAsync(string clipPath, ProcessingOptions options, string outputDirectory);
    }
} 