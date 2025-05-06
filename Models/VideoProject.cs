using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClipsAutomation.Models
{
    public enum ProcessingStatus
    {
        Pending,
        DownloadingVideo,
        ExtractingTranscript,
        AnalyzingContent,
        GeneratingClips,
        Completed,
        Failed
    }

    public class VideoProject
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "YouTube URL")]
        public string YouTubeUrl { get; set; } = string.Empty;

        [Display(Name = "Video Title")]
        public string? VideoTitle { get; set; }

        [Display(Name = "Video ID")]
        public string? VideoId { get; set; }

        [Display(Name = "Duration (seconds)")]
        public int? DurationSeconds { get; set; }

        [Display(Name = "Creation Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Processing Status")]
        public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

        [Display(Name = "Processing Message")]
        public string? StatusMessage { get; set; }

        [Display(Name = "Has Transcript")]
        public bool HasTranscript { get; set; }

        [Display(Name = "Local Video Path")]
        public string? LocalVideoPath { get; set; }

        [Display(Name = "Local Transcript Path")]
        public string? LocalTranscriptPath { get; set; }

        // Navigation properties
        public virtual ICollection<VideoSegment>? VideoSegments { get; set; }
        public virtual ICollection<GeneratedClip>? GeneratedClips { get; set; }
        public virtual ProcessingOptions? ProcessingOptions { get; set; }
    }

    public class VideoSegment
    {
        public int Id { get; set; }
        public int VideoProjectId { get; set; }
        
        [Display(Name = "Start Time (seconds)")]
        public double StartTimeSeconds { get; set; }
        
        [Display(Name = "End Time (seconds)")]
        public double EndTimeSeconds { get; set; }
        
        [Display(Name = "Transcript Text")]
        public string TranscriptText { get; set; } = string.Empty;
        
        [Display(Name = "Engagement Score")]
        public double EngagementScore { get; set; }
        
        [Display(Name = "Selected for Clip")]
        public bool SelectedForClip { get; set; }

        // Navigation property
        public virtual VideoProject? VideoProject { get; set; }
    }

    public class GeneratedClip
    {
        public int Id { get; set; }
        public int VideoProjectId { get; set; }
        
        [Display(Name = "Clip Title")]
        public string Title { get; set; } = string.Empty;
        
        [Display(Name = "File Path")]
        public string FilePath { get; set; } = string.Empty;
        
        [Display(Name = "Duration (seconds)")]
        public double DurationSeconds { get; set; }
        
        [Display(Name = "Creation Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Has Captions")]
        public bool HasCaptions { get; set; }
        
        [Display(Name = "Upload Status")]
        public string UploadStatus { get; set; } = "Not Uploaded";

        // Navigation property
        public virtual VideoProject? VideoProject { get; set; }
        
        // List of segments included in this clip
        public virtual ICollection<VideoSegment>? IncludedSegments { get; set; }
    }

    public class ProcessingOptions
    {
        public int Id { get; set; }
        public int VideoProjectId { get; set; }
        
        [Display(Name = "Maximum Clip Duration (seconds)")]
        [Range(15, 60)]
        public int MaxClipDurationSeconds { get; set; } = 60;
        
        [Display(Name = "Minimum Clip Duration (seconds)")]
        [Range(5, 30)]
        public int MinClipDurationSeconds { get; set; } = 15;
        
        [Display(Name = "Auto-Generate Captions")]
        public bool AutoGenerateCaptions { get; set; } = true;
        
        [Display(Name = "Add Background Music")]
        public bool AddBackgroundMusic { get; set; }
        
        [Display(Name = "Music Volume (%)")]
        [Range(0, 100)]
        public int MusicVolumePercent { get; set; } = 30;
        
        [Display(Name = "Auto-Zoom for Vertical Format")]
        public bool AutoZoomVertical { get; set; } = true;
        
        [Display(Name = "Apply Video Enhancements")]
        public bool ApplyVideoEnhancements { get; set; } = true;
        
        [Display(Name = "Maximum Number of Clips")]
        [Range(1, 10)]
        public int MaxNumberOfClips { get; set; } = 3;

        // Navigation property
        public virtual VideoProject? VideoProject { get; set; }
    }
} 