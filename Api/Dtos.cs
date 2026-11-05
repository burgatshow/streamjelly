using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.StreamJelly.Api;

#pragma warning disable SA1402, CA2227, CA1002 // simple DTOs in one file

public class SessionsResponse
{
    public DateTime GeneratedAtUtc { get; set; }

    public int RefreshSeconds { get; set; }

    public string ServerVersion { get; set; } = string.Empty;

    public bool TranscodeJobsAvailable { get; set; }

    public List<SessionStreamInfo> Sessions { get; set; } = new();
}

public class SessionStreamInfo
{
    public string SessionId { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Client { get; set; }

    public string? ApplicationVersion { get; set; }

    public string? DeviceName { get; set; }

    public string? DeviceId { get; set; }

    public string? RemoteEndPoint { get; set; }

    public string? ItemId { get; set; }

    public string? BackdropItemId { get; set; }

    public string? PosterItemId { get; set; }

    public string? ItemName { get; set; }

    public string? ItemType { get; set; }

    public string? SeriesName { get; set; }

    public int? SeasonNumber { get; set; }

    public int? EpisodeNumber { get; set; }

    public int? ProductionYear { get; set; }

    public long? PositionTicks { get; set; }

    public long? RunTimeTicks { get; set; }

    public bool IsPaused { get; set; }

    public string? PlayMethod { get; set; }

    public int? SelectedAudioIndex { get; set; }

    public int? SelectedSubtitleIndex { get; set; }

    public DateTime? LastReportUtc { get; set; }

    public bool HasProgressReport { get; set; }

    public SourceInfo? Source { get; set; }

    public OutputInfo Output { get; set; } = new();

    public List<StreamDecision> Decisions { get; set; } = new();

    public List<TranscodeJobInfo> Jobs { get; set; } = new();
}

public class SourceInfo
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Path { get; set; }

    public string? Protocol { get; set; }

    public string? Container { get; set; }

    public long? Size { get; set; }

    public int? Bitrate { get; set; }

    public string? VideoType { get; set; }

    public bool SupportsDirectPlay { get; set; }

    public bool SupportsDirectStream { get; set; }

    public bool SupportsTranscoding { get; set; }

    public List<StreamInfo> Streams { get; set; } = new();
}

public class StreamInfo
{
    public int Index { get; set; }

    public string Type { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string? DisplayTitle { get; set; }

    public string? Codec { get; set; }

    public string? CodecTag { get; set; }

    public string? DeliveryMethod { get; set; }

    public string? Profile { get; set; }

    public double? Level { get; set; }

    public string? Language { get; set; }

    public string? Title { get; set; }

    public int? BitRate { get; set; }

    public int? BitDepth { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public float? FrameRate { get; set; }

    public bool IsInterlaced { get; set; }

    public string? AspectRatio { get; set; }

    public string? PixelFormat { get; set; }

    public string? VideoRange { get; set; }

    public string? VideoRangeType { get; set; }

    public string? ColorSpace { get; set; }

    public string? ColorTransfer { get; set; }

    public string? ColorPrimaries { get; set; }

    public int? DvProfile { get; set; }

    public int? Channels { get; set; }

    public string? ChannelLayout { get; set; }

    public int? SampleRate { get; set; }

    public bool IsDefault { get; set; }

    public bool IsForced { get; set; }

    public bool IsExternal { get; set; }

    public bool IsHearingImpaired { get; set; }

    public string? Path { get; set; }
}

public class OutputInfo
{
    /// <summary>Gets or sets the delivery mode: DirectPlay, DirectStream or Transcode.</summary>
    public string? Mode { get; set; }

    public string? Container { get; set; }

    public string? VideoCodec { get; set; }

    public string? AudioCodec { get; set; }

    public bool? IsVideoDirect { get; set; }

    public bool? IsAudioDirect { get; set; }

    public int? Bitrate { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public float? Framerate { get; set; }

    public int? AudioChannels { get; set; }

    public double? CompletionPercentage { get; set; }

    public string? HardwareAcceleration { get; set; }

    public List<string> TranscodeReasons { get; set; } = new();
}

public class StreamDecision
{
    public string Type { get; set; } = string.Empty;

    public int? SourceIndex { get; set; }

    /// <summary>Gets or sets the decision: Direct, Copy, Transcode, BurnIn, Separate, None.</summary>
    public string Decision { get; set; } = string.Empty;

    public string? From { get; set; }

    public string? To { get; set; }

    public string? Note { get; set; }
}

public class TranscodeJobInfo
{
    public string? Id { get; set; }

    public string? PlaySessionId { get; set; }

    public string? Type { get; set; }

    public string? OutputPath { get; set; }

    public int? ProcessId { get; set; }

    public bool HasExited { get; set; }

    public bool IsUserPaused { get; set; }

    public int ActiveRequestCount { get; set; }

    public float? Framerate { get; set; }

    public double? CompletionPercentage { get; set; }

    public int? BitRate { get; set; }

    public long BytesDownloaded { get; set; }

    public long? BytesTranscoded { get; set; }

    public long? TranscodingPositionTicks { get; set; }

    public long? DownloadPositionTicks { get; set; }

    public bool ThrottlingEnabled { get; set; }

    public DateTime LastPingDate { get; set; }

    public string? FfmpegCommand { get; set; }

    public FfmpegHints? Hints { get; set; }
}

public class FfmpegHints
{
    public string? VideoEncoder { get; set; }

    public string? AudioEncoder { get; set; }

    public string? HwAccel { get; set; }

    public string? Preset { get; set; }

    public string? Crf { get; set; }

    public string? VideoBitrate { get; set; }

    public string? MaxRate { get; set; }

    public string? AudioBitrate { get; set; }

    public string? AudioChannels { get; set; }

    public string? SegmentFormat { get; set; }

    public string? SegmentLength { get; set; }

    public bool SubtitleBurnIn { get; set; }

    public bool ToneMapping { get; set; }

    public List<string> Maps { get; set; } = new();
}
