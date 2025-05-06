using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace ClipsAutomation.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YouTubeUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasTranscript = table.Column<bool>(type: "bit", nullable: false),
                    LocalVideoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalTranscriptPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedClips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoProjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationSeconds = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HasCaptions = table.Column<bool>(type: "bit", nullable: false),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedClips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedClips_VideoProjects_VideoProjectId",
                        column: x => x.VideoProjectId,
                        principalTable: "VideoProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoProjectId = table.Column<int>(type: "int", nullable: false),
                    MaxClipDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    MinClipDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    AutoGenerateCaptions = table.Column<bool>(type: "bit", nullable: false),
                    AddBackgroundMusic = table.Column<bool>(type: "bit", nullable: false),
                    MusicVolumePercent = table.Column<int>(type: "int", nullable: false),
                    AutoZoomVertical = table.Column<bool>(type: "bit", nullable: false),
                    ApplyVideoEnhancements = table.Column<bool>(type: "bit", nullable: false),
                    MaxNumberOfClips = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingOptions_VideoProjects_VideoProjectId",
                        column: x => x.VideoProjectId,
                        principalTable: "VideoProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoProjectId = table.Column<int>(type: "int", nullable: false),
                    StartTimeSeconds = table.Column<double>(type: "float", nullable: false),
                    EndTimeSeconds = table.Column<double>(type: "float", nullable: false),
                    TranscriptText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EngagementScore = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    SelectedForClip = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoSegments_VideoProjects_VideoProjectId",
                        column: x => x.VideoProjectId,
                        principalTable: "VideoProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedClipVideoSegment",
                columns: table => new
                {
                    GeneratedClipId = table.Column<int>(type: "int", nullable: false),
                    IncludedSegmentsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedClipVideoSegment", x => new { x.GeneratedClipId, x.IncludedSegmentsId });
                    table.ForeignKey(
                        name: "FK_GeneratedClipVideoSegment_GeneratedClips_GeneratedClipId",
                        column: x => x.GeneratedClipId,
                        principalTable: "GeneratedClips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GeneratedClipVideoSegment_VideoSegments_IncludedSegmentsId",
                        column: x => x.IncludedSegmentsId,
                        principalTable: "VideoSegments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedClips_VideoProjectId",
                table: "GeneratedClips",
                column: "VideoProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedClipVideoSegment_IncludedSegmentsId",
                table: "GeneratedClipVideoSegment",
                column: "IncludedSegmentsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOptions_VideoProjectId",
                table: "ProcessingOptions",
                column: "VideoProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoSegments_VideoProjectId",
                table: "VideoSegments",
                column: "VideoProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedClipVideoSegment");

            migrationBuilder.DropTable(
                name: "ProcessingOptions");

            migrationBuilder.DropTable(
                name: "GeneratedClips");

            migrationBuilder.DropTable(
                name: "VideoSegments");

            migrationBuilder.DropTable(
                name: "VideoProjects");
        }
    }
} 