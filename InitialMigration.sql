USE [master]
GO

-- Create the database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ClipsAutomation')
BEGIN
    CREATE DATABASE [ClipsAutomation]
END
GO

USE [ClipsAutomation]
GO

-- Create VideoProjects table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VideoProjects]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VideoProjects](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [YouTubeUrl] [nvarchar](max) NOT NULL,
        [VideoTitle] [nvarchar](max) NULL,
        [VideoId] [nvarchar](max) NULL,
        [DurationSeconds] [int] NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [Status] [int] NOT NULL,
        [StatusMessage] [nvarchar](max) NULL,
        [HasTranscript] [bit] NOT NULL,
        [LocalVideoPath] [nvarchar](max) NULL,
        [LocalTranscriptPath] [nvarchar](max) NULL,
        CONSTRAINT [PK_VideoProjects] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
END
GO

-- Create ProcessingOptions table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProcessingOptions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ProcessingOptions](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [VideoProjectId] [int] NOT NULL,
        [MaxClipDurationSeconds] [int] NOT NULL,
        [MinClipDurationSeconds] [int] NOT NULL,
        [AutoGenerateCaptions] [bit] NOT NULL,
        [AddBackgroundMusic] [bit] NOT NULL,
        [MusicVolumePercent] [int] NOT NULL,
        [AutoZoomVertical] [bit] NOT NULL,
        [ApplyVideoEnhancements] [bit] NOT NULL,
        [MaxNumberOfClips] [int] NOT NULL,
        CONSTRAINT [PK_ProcessingOptions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProcessingOptions_VideoProjects] FOREIGN KEY([VideoProjectId]) REFERENCES [dbo].[VideoProjects] ([Id]) ON DELETE CASCADE
    )

    CREATE UNIQUE INDEX [IX_ProcessingOptions_VideoProjectId] ON [dbo].[ProcessingOptions]([VideoProjectId])
END
GO

-- Create VideoSegments table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VideoSegments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VideoSegments](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [VideoProjectId] [int] NOT NULL,
        [StartTimeSeconds] [float] NOT NULL,
        [EndTimeSeconds] [float] NOT NULL,
        [TranscriptText] [nvarchar](max) NOT NULL,
        [EngagementScore] [float] NOT NULL,
        [SelectedForClip] [bit] NOT NULL,
        CONSTRAINT [PK_VideoSegments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_VideoSegments_VideoProjects] FOREIGN KEY([VideoProjectId]) REFERENCES [dbo].[VideoProjects] ([Id]) ON DELETE CASCADE
    )

    CREATE INDEX [IX_VideoSegments_VideoProjectId] ON [dbo].[VideoSegments]([VideoProjectId])
END
GO

-- Create GeneratedClips table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GeneratedClips]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[GeneratedClips](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [VideoProjectId] [int] NOT NULL,
        [Title] [nvarchar](max) NOT NULL,
        [FilePath] [nvarchar](max) NOT NULL,
        [DurationSeconds] [float] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [HasCaptions] [bit] NOT NULL,
        [UploadStatus] [nvarchar](max) NOT NULL,
        CONSTRAINT [PK_GeneratedClips] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_GeneratedClips_VideoProjects] FOREIGN KEY([VideoProjectId]) REFERENCES [dbo].[VideoProjects] ([Id]) ON DELETE CASCADE
    )

    CREATE INDEX [IX_GeneratedClips_VideoProjectId] ON [dbo].[GeneratedClips]([VideoProjectId])
END
GO

-- Create many-to-many relationship table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GeneratedClipVideoSegment]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[GeneratedClipVideoSegment](
        [GeneratedClipId] [int] NOT NULL,
        [IncludedSegmentsId] [int] NOT NULL,
        CONSTRAINT [PK_GeneratedClipVideoSegment] PRIMARY KEY CLUSTERED ([GeneratedClipId], [IncludedSegmentsId]),
        CONSTRAINT [FK_GeneratedClipVideoSegment_GeneratedClips] FOREIGN KEY([GeneratedClipId]) REFERENCES [dbo].[GeneratedClips] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GeneratedClipVideoSegment_VideoSegments] FOREIGN KEY([IncludedSegmentsId]) REFERENCES [dbo].[VideoSegments] ([Id])
    )

    CREATE INDEX [IX_GeneratedClipVideoSegment_IncludedSegmentsId] ON [dbo].[GeneratedClipVideoSegment]([IncludedSegmentsId])
END
GO

-- Add EFMigrationsHistory table to record migrations
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory](
        [MigrationId] [nvarchar](150) NOT NULL,
        [ProductVersion] [nvarchar](32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationId] ASC)
    )
END
GO

-- Insert migration record
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20250506000000_InitialCreate')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20250506000000_InitialCreate', '8.0.1')
END
GO 