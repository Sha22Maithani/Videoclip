using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClipsAutomation.Models;
using ClipsAutomation.Services;
using System.IO;

namespace ClipsAutomation.Controllers
{
    public class VideoProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IYouTubeService _youTubeService;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly IVideoProcessingService _videoProcessingService;

        public VideoProjectsController(
            ApplicationDbContext context,
            IYouTubeService youTubeService,
            IContentAnalysisService contentAnalysisService,
            IVideoProcessingService videoProcessingService)
        {
            _context = context;
            _youTubeService = youTubeService;
            _contentAnalysisService = contentAnalysisService;
            _videoProcessingService = videoProcessingService;
        }

        // GET: VideoProjects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.VideoProjects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            
            return View(projects);
        }

        // GET: VideoProjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var videoProject = await _context.VideoProjects
                .Include(p => p.ProcessingOptions)
                .Include(p => p.VideoSegments)
                .Include(p => p.GeneratedClips)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (videoProject == null)
            {
                return NotFound();
            }

            return View(videoProject);
        }

        // GET: VideoProjects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VideoProjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("YouTubeUrl")] VideoProject videoProject)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get video info from YouTube
                    var videoInfo = await _youTubeService.GetVideoInfoAsync(videoProject.YouTubeUrl);
                    
                    videoProject.VideoId = videoInfo.VideoId;
                    videoProject.VideoTitle = videoInfo.Title;
                    videoProject.DurationSeconds = videoInfo.DurationSeconds;
                    videoProject.Status = ProcessingStatus.Pending;
                    
                    // Create default processing options
                    videoProject.ProcessingOptions = new ProcessingOptions
                    {
                        MaxClipDurationSeconds = 60,
                        MinClipDurationSeconds = 15,
                        AutoGenerateCaptions = true,
                        AutoZoomVertical = true,
                        MaxNumberOfClips = 3
                    };
                    
                    _context.Add(videoProject);
                    await _context.SaveChangesAsync();
                    
                    // Start processing in background
                    // In a real application, this would be handled by a background service
                    // For simplicity, we'll just return the ID for now
                    
                    return RedirectToAction(nameof(Details), new { id = videoProject.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error processing video: {ex.Message}");
                }
            }
            
            return View(videoProject);
        }

        // GET: VideoProjects/ProcessOptions/5
        public async Task<IActionResult> ProcessOptions(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var options = await _context.ProcessingOptions
                .FirstOrDefaultAsync(m => m.VideoProjectId == id);
                
            if (options == null)
            {
                return NotFound();
            }

            return View(options);
        }

        // POST: VideoProjects/ProcessOptions/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOptions(int id, [Bind("Id,VideoProjectId,MaxClipDurationSeconds,MinClipDurationSeconds,AutoGenerateCaptions,AddBackgroundMusic,MusicVolumePercent,AutoZoomVertical,ApplyVideoEnhancements,MaxNumberOfClips")] ProcessingOptions options)
        {
            if (id != options.VideoProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(options);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction(nameof(Details), new { id = options.VideoProjectId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ProcessingOptions.Any(e => e.Id == options.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            return View(options);
        }

        // POST: VideoProjects/StartProcessing/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartProcessing(int id)
        {
            var videoProject = await _context.VideoProjects
                .Include(p => p.ProcessingOptions)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (videoProject == null)
            {
                return NotFound();
            }

            // Update status
            videoProject.Status = ProcessingStatus.DownloadingVideo;
            await _context.SaveChangesAsync();

            // In a real application, this would be handled by a background service
            // For now, we'll just redirect back to the details page
            
            return RedirectToAction(nameof(Details), new { id = videoProject.Id });
        }

        // POST: VideoProjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var videoProject = await _context.VideoProjects
                .Include(p => p.ProcessingOptions)
                .Include(p => p.VideoSegments)
                .Include(p => p.GeneratedClips)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (videoProject != null)
            {
                // Delete associated files
                if (!string.IsNullOrEmpty(videoProject.LocalVideoPath) && System.IO.File.Exists(videoProject.LocalVideoPath))
                {
                    System.IO.File.Delete(videoProject.LocalVideoPath);
                }
                
                if (!string.IsNullOrEmpty(videoProject.LocalTranscriptPath) && System.IO.File.Exists(videoProject.LocalTranscriptPath))
                {
                    System.IO.File.Delete(videoProject.LocalTranscriptPath);
                }
                
                if (videoProject.GeneratedClips != null)
                {
                    foreach (var clip in videoProject.GeneratedClips)
                    {
                        if (!string.IsNullOrEmpty(clip.FilePath) && System.IO.File.Exists(clip.FilePath))
                        {
                            System.IO.File.Delete(clip.FilePath);
                        }
                    }
                }
                
                _context.VideoProjects.Remove(videoProject);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
} 