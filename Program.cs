using Microsoft.EntityFrameworkCore;
using ClipsAutomation;
using ClipsAutomation.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpClient factory
builder.Services.AddHttpClient();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Register services
builder.Services.AddScoped<ITranscriptionService, AssemblyAITranscriptionService>();
builder.Services.AddScoped<IYouTubeService, YouTubeService>();
builder.Services.AddScoped<IContentAnalysisService, GeminiContentAnalysisService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();

// Add Xabe.FFmpeg configuration
Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.Combine(builder.Environment.ContentRootPath, "ffmpeg"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
