using System.Collections.Generic;
using System.Threading.Tasks;
using ClipsAutomation.Models;

namespace ClipsAutomation.Services
{
    public interface IContentAnalysisService
    {
        /// <summary>
        /// Analyzes a transcript file to identify engaging segments
        /// </summary>
        /// <param name="transcriptPath">Path to the transcript file</param>
        /// <param name="options">Processing options for analysis</param>
        /// <returns>List of video segments with engagement scores</returns>
        Task<List<VideoSegment>> AnalyzeTranscriptAsync(string transcriptPath, ProcessingOptions options);
        
        /// <summary>
        /// Selects the most engaging segments based on the analysis and processing options
        /// </summary>
        /// <param name="segments">List of analyzed video segments</param>
        /// <param name="options">Processing options for selection</param>
        /// <returns>List of segments selected for clip generation</returns>
        Task<List<VideoSegment>> SelectEngagingSegmentsAsync(List<VideoSegment> segments, ProcessingOptions options);
        
        /// <summary>
        /// Creates clip configuration based on selected segments
        /// </summary>
        /// <param name="selectedSegments">List of selected video segments</param>
        /// <param name="videoProject">The video project being processed</param>
        /// <returns>List of clip configurations ready for processing</returns>
        Task<List<GeneratedClip>> CreateClipConfigurationsAsync(List<VideoSegment> selectedSegments, VideoProject videoProject);
    }
} 