using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PluginConfiguration = Jellyfin.Plugin.StreamJelly.Configuration.PluginConfiguration;

namespace Jellyfin.Plugin.StreamJelly.Api;

/// <summary>
/// Returns detailed information about the streams sent to clients by active playback sessions.
/// </summary>
[ApiController]
[Route("StreamJelly")]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class StreamJellyController : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly ISessionManager _sessionManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ITranscodeManager _transcodeManager;
    private readonly IServerApplicationHost _appHost;
    private readonly ILogger<StreamJellyController> _logger;

    public StreamJellyController(
        ISessionManager sessionManager,
        IMediaSourceManager mediaSourceManager,
        ITranscodeManager transcodeManager,
        IServerApplicationHost appHost,
        ILogger<StreamJellyController> logger)
    {
        _sessionManager = sessionManager;
        _mediaSourceManager = mediaSourceManager;
        _transcodeManager = transcodeManager;
        _appHost = appHost;
        _logger = logger;
    }

    /// <summary>
    /// Gets all sessions that are currently playing something, with source/output stream details.
    /// </summary>
    /// <returns>The sessions payload.</returns>
    [HttpGet("Sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetSessions()
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var jobs = GetActiveTranscodingJobs(out var jobsAvailable);

        var response = new SessionsResponse
        {
            GeneratedAtUtc = DateTime.UtcNow,
            RefreshSeconds = Math.Clamp(config.RefreshSeconds, 1, 60),
            ServerVersion = _appHost.ApplicationVersionString,
            TranscodeJobsAvailable = jobsAvailable
        };

        foreach (var session in _sessionManager.Sessions.Where(s => s.NowPlayingItem is not null))
        {
            try
            {
                response.Sessions.Add(BuildSession(session, jobs, config));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "StreamJelly: failed to inspect session {SessionId}", session.Id);
                response.Sessions.Add(new SessionStreamInfo
                {
                    SessionId = session.Id,
                    UserName = session.UserName,
                    Client = session.Client,
                    DeviceName = session.DeviceName,
                    ItemName = session.NowPlayingItem?.Name + " (hiba: " + ex.Message + ")"
                });
            }
        }

        return new JsonResult(response, _jsonOptions);
    }

    private SessionStreamInfo BuildSession(SessionInfo session, List<TranscodingJob> allJobs, PluginConfiguration config)
    {
        var item = session.NowPlayingItem;
        var state = session.PlayState ?? new PlayerStateInfo();

        var dto = new SessionStreamInfo
        {
            SessionId = session.Id,
            UserName = session.UserName,
            Client = session.Client,
            ApplicationVersion = session.ApplicationVersion,
            DeviceName = session.DeviceName,
            DeviceId = session.DeviceId,
            RemoteEndPoint = session.RemoteEndPoint,
            ItemId = item.Id.ToString("N"),
            BackdropItemId = (item.BackdropImageTags is { Length: > 0 } ? item.Id : item.ParentBackdropItemId ?? item.SeriesId)?.ToString("N"),
            PosterItemId = (item.SeriesId ?? item.Id).ToString("N"),
            ItemName = item.Name,
            ItemType = item.Type.ToString(),
            SeriesName = item.SeriesName,
            SeasonNumber = item.ParentIndexNumber,
            EpisodeNumber = item.IndexNumber,
            ProductionYear = item.ProductionYear,
            PositionTicks = state.PositionTicks,
            RunTimeTicks = item.RunTimeTicks,
            IsPaused = state.IsPaused,
            PlayMethod = state.PlayMethod?.ToString(),
            SelectedAudioIndex = state.AudioStreamIndex,
            SelectedSubtitleIndex = state.SubtitleStreamIndex,
            LastReportUtc = session.LastPlaybackCheckIn == default ? null : session.LastPlaybackCheckIn,
            HasProgressReport = session.LastPlaybackCheckIn != default
        };

        // Transcoding jobs belonging to this device
        var jobs = allJobs
            .Where(j => !string.IsNullOrEmpty(j.DeviceId)
                        && string.Equals(j.DeviceId, session.DeviceId, StringComparison.OrdinalIgnoreCase)
                        && !j.HasExited)
            .Reverse()
            .ToList();

        foreach (var job in jobs)
        {
            dto.Jobs.Add(BuildJob(job, config));
        }

        // Source media
        var source = ResolveMediaSource(session, state, jobs);
        if (source is not null)
        {
            dto.Source = BuildSource(source, state, config);
        }

        // Output towards the client
        var ti = session.TranscodingInfo;
        var mode = state.PlayMethod?.ToString() ?? (ti is null ? "Unknown" : "Transcode");
        dto.Output.Mode = mode;
        if (ti is not null && state.PlayMethod != PlayMethod.DirectPlay)
        {
            dto.Output.Container = ti.Container;
            dto.Output.VideoCodec = ti.VideoCodec;
            dto.Output.AudioCodec = ti.AudioCodec;
            dto.Output.IsVideoDirect = ti.IsVideoDirect;
            dto.Output.IsAudioDirect = ti.IsAudioDirect;
            dto.Output.Bitrate = ti.Bitrate;
            dto.Output.Width = ti.Width;
            dto.Output.Height = ti.Height;
            dto.Output.Framerate = ti.Framerate;
            dto.Output.AudioChannels = ti.AudioChannels;
            dto.Output.CompletionPercentage = ti.CompletionPercentage;
            dto.Output.HardwareAcceleration = ti.HardwareAccelerationType?.ToString();
            dto.Output.TranscodeReasons = SplitFlags(ti.TranscodeReasons.ToString());
        }
        else if (dto.Source is not null)
        {
            // Direct play: the client receives the original file as-is.
            var v = dto.Source.Streams.FirstOrDefault(s => s.Type == nameof(MediaStreamType.Video) && s.IsActive);
            var a = dto.Source.Streams.FirstOrDefault(s => s.Type == nameof(MediaStreamType.Audio) && s.IsActive);
            dto.Output.Container = dto.Source.Container;
            dto.Output.VideoCodec = v?.Codec;
            dto.Output.AudioCodec = a?.Codec;
            dto.Output.IsVideoDirect = true;
            dto.Output.IsAudioDirect = true;
            dto.Output.Bitrate = dto.Source.Bitrate;
            dto.Output.Width = v?.Width;
            dto.Output.Height = v?.Height;
            dto.Output.Framerate = v?.FrameRate;
            dto.Output.AudioChannels = a?.Channels;
        }

        dto.Decisions = BuildDecisions(dto, jobs.Count > 0 ? dto.Jobs[0].Hints : null);
        return dto;
    }

    private MediaSourceInfo? ResolveMediaSource(SessionInfo session, PlayerStateInfo state, List<TranscodingJob> jobs)
    {
        // Prefer the media source the transcoder actually opened (covers Live TV, too).
        var fromJob = jobs.Select(j => j.MediaSource).FirstOrDefault(m => m is not null);
        if (fromJob is not null)
        {
            return fromJob;
        }

        var baseItem = session.FullNowPlayingItem;
        if (baseItem is null)
        {
            return null;
        }

        var sources = _mediaSourceManager.GetStaticMediaSources(baseItem, false);
        if (sources is null || sources.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(state.MediaSourceId))
        {
            var match = sources.FirstOrDefault(s => string.Equals(s.Id, state.MediaSourceId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return sources[0];
    }

    private static SourceInfo BuildSource(MediaSourceInfo src, PlayerStateInfo state, PluginConfiguration config)
    {
        var info = new SourceInfo
        {
            Id = src.Id,
            Name = src.Name,
            Path = config.ShowPaths ? src.Path : null,
            Protocol = src.Protocol.ToString(),
            Container = src.Container,
            Size = src.Size,
            Bitrate = src.Bitrate,
            VideoType = src.VideoType?.ToString(),
            SupportsDirectPlay = src.SupportsDirectPlay,
            SupportsDirectStream = src.SupportsDirectStream,
            SupportsTranscoding = src.SupportsTranscoding
        };

        var streams = src.MediaStreams ?? Array.Empty<MediaStream>();
        var audioIndex = state.AudioStreamIndex ?? src.DefaultAudioStreamIndex
                         ?? streams.FirstOrDefault(s => s.Type == MediaStreamType.Audio && s.IsDefault)?.Index
                         ?? streams.FirstOrDefault(s => s.Type == MediaStreamType.Audio)?.Index;
        var subIndex = state.SubtitleStreamIndex;
        var videoIndex = streams.FirstOrDefault(s => s.Type == MediaStreamType.Video)?.Index;

        foreach (var s in streams.OrderBy(s => s.Index))
        {
            var active = s.Type switch
            {
                MediaStreamType.Video => s.Index == videoIndex,
                MediaStreamType.Audio => s.Index == audioIndex,
                MediaStreamType.Subtitle => subIndex.HasValue && subIndex.Value >= 0 && s.Index == subIndex.Value,
                _ => false
            };

            var si = new StreamInfo
            {
                Index = s.Index,
                Type = s.Type.ToString(),
                IsActive = active,
                DisplayTitle = SafeGet(() => s.DisplayTitle),
                Codec = s.Codec,
                CodecTag = s.CodecTag,
                DeliveryMethod = s.DeliveryMethod?.ToString(),
                Profile = s.Profile,
                Level = s.Level,
                Language = s.Language,
                Title = s.Title,
                BitRate = s.BitRate,
                BitDepth = s.BitDepth,
                Width = s.Width,
                Height = s.Height,
                FrameRate = s.RealFrameRate ?? s.AverageFrameRate,
                IsInterlaced = s.IsInterlaced,
                AspectRatio = s.AspectRatio,
                PixelFormat = s.PixelFormat,
                ColorSpace = s.ColorSpace,
                ColorTransfer = s.ColorTransfer,
                ColorPrimaries = s.ColorPrimaries,
                DvProfile = s.DvProfile,
                Channels = s.Channels,
                ChannelLayout = s.ChannelLayout,
                SampleRate = s.SampleRate,
                IsDefault = s.IsDefault,
                IsForced = s.IsForced,
                IsExternal = s.IsExternal,
                IsHearingImpaired = s.IsHearingImpaired,
                Path = config.ShowPaths && s.IsExternal ? s.Path : null
            };

            if (s.Type == MediaStreamType.Video)
            {
                si.VideoRange = SafeGet(() => s.VideoRange.ToString());
                si.VideoRangeType = SafeGet(() => s.VideoRangeType.ToString());
            }

            info.Streams.Add(si);
        }

        return info;
    }

    private static List<StreamDecision> BuildDecisions(SessionStreamInfo dto, FfmpegHints? hints)
    {
        var list = new List<StreamDecision>();
        var streams = dto.Source?.Streams ?? new List<StreamInfo>();
        var v = streams.FirstOrDefault(s => s.IsActive && s.Type == nameof(MediaStreamType.Video));
        var a = streams.FirstOrDefault(s => s.IsActive && s.Type == nameof(MediaStreamType.Audio));
        var sub = streams.FirstOrDefault(s => s.IsActive && s.Type == nameof(MediaStreamType.Subtitle));
        var mode = dto.Output.Mode;
        var direct = string.Equals(mode, nameof(PlayMethod.DirectPlay), StringComparison.Ordinal);

        if (v is not null || !string.IsNullOrEmpty(dto.Output.VideoCodec))
        {
            var d = new StreamDecision { Type = "Video", SourceIndex = v?.Index, From = Describe(v) };
            if (direct)
            {
                d.Decision = "Direct";
                d.To = d.From;
            }
            else if (dto.Output.IsVideoDirect == true)
            {
                d.Decision = "Copy";
                d.To = v?.Codec;
                d.Note = "Remux: video unchanged, new container (" + dto.Output.Container + ")";
            }
            else
            {
                d.Decision = "Transcode";
                d.To = JoinParts(
                    dto.Output.VideoCodec,
                    dto.Output.Width.HasValue && dto.Output.Height.HasValue ? dto.Output.Width + "x" + dto.Output.Height : null,
                    dto.Output.Framerate.HasValue ? dto.Output.Framerate.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " fps" : null,
                    hints?.VideoEncoder is null ? null : "enc: " + hints.VideoEncoder);
                if (hints?.ToneMapping == true)
                {
                    d.Note = "HDR → SDR tone mapping";
                }
            }

            list.Add(d);
        }

        if (a is not null || !string.IsNullOrEmpty(dto.Output.AudioCodec))
        {
            var d = new StreamDecision { Type = "Audio", SourceIndex = a?.Index, From = Describe(a) };
            if (direct)
            {
                d.Decision = "Direct";
                d.To = d.From;
            }
            else if (dto.Output.IsAudioDirect == true)
            {
                d.Decision = "Copy";
                d.To = a?.Codec;
            }
            else
            {
                d.Decision = "Transcode";
                d.To = JoinParts(
                    dto.Output.AudioCodec,
                    dto.Output.AudioChannels.HasValue ? dto.Output.AudioChannels + " ch" : null,
                    hints?.AudioBitrate is null ? null : hints.AudioBitrate + " bps",
                    hints?.AudioEncoder is null ? null : "enc: " + hints.AudioEncoder);
            }

            list.Add(d);
        }

        if (sub is not null)
        {
            var d = new StreamDecision { Type = "Subtitle", SourceIndex = sub.Index, From = Describe(sub) };
            if (hints?.SubtitleBurnIn == true)
            {
                d.Decision = "BurnIn";
                d.To = "burned into the video";
                d.Note = "The subtitle forces a video transcode";
            }
            else if (direct)
            {
                d.Decision = sub.IsExternal ? "Separate" : "Direct";
                d.To = sub.IsExternal ? "as a separate file" : "in the container";
            }
            else
            {
                d.Decision = "Separate";
                d.To = "separate stream (e.g. WebVTT/SRT/ASS)";
            }

            list.Add(d);
        }

        return list;
    }

    private static string? Describe(StreamInfo? s)
    {
        if (s is null)
        {
            return null;
        }

        return s.Type switch
        {
            "Video" => JoinParts(
                s.Codec,
                s.Profile,
                s.Width.HasValue && s.Height.HasValue ? s.Width + "x" + s.Height : null,
                s.VideoRangeType is not null && s.VideoRangeType != "Unknown" && s.VideoRangeType != "SDR" ? s.VideoRangeType : null,
                s.BitRate.HasValue ? FormatBitrate(s.BitRate.Value) : null),
            "Audio" => JoinParts(
                s.Codec,
                s.Profile,
                s.ChannelLayout ?? (s.Channels.HasValue ? s.Channels + " ch" : null),
                s.Language,
                s.BitRate.HasValue ? FormatBitrate(s.BitRate.Value) : null),
            _ => JoinParts(s.Codec, s.Language, s.IsForced ? "forced" : null, s.IsExternal ? "external" : null)
        };
    }

    private TranscodeJobInfo BuildJob(TranscodingJob job, PluginConfiguration config)
    {
        var info = new TranscodeJobInfo
        {
            Id = job.Id,
            PlaySessionId = job.PlaySessionId,
            Type = job.Type.ToString(),
            OutputPath = config.ShowPaths ? job.Path : null,
            HasExited = job.HasExited,
            IsUserPaused = job.IsUserPaused,
            ActiveRequestCount = job.ActiveRequestCount,
            Framerate = job.Framerate,
            CompletionPercentage = job.CompletionPercentage,
            BitRate = job.BitRate,
            BytesDownloaded = job.BytesDownloaded,
            BytesTranscoded = job.BytesTranscoded,
            TranscodingPositionTicks = job.TranscodingPositionTicks,
            DownloadPositionTicks = job.DownloadPositionTicks,
            ThrottlingEnabled = job.TranscodingThrottler is not null,
            LastPingDate = job.LastPingDate
        };

        string? args = null;
        try
        {
            var p = job.Process;
            if (p is not null)
            {
                args = p.StartInfo.Arguments;
                if (!job.HasExited)
                {
                    info.ProcessId = p.Id;
                }

                if (config.ShowFfmpegCommand)
                {
                    info.FfmpegCommand = (p.StartInfo.FileName + " " + args).Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "StreamJelly: could not read ffmpeg process info");
        }

        if (!string.IsNullOrEmpty(args))
        {
            info.Hints = ParseFfmpegArgs(args);
        }

        return info;
    }

    /// <summary>
    /// TranscodeManager has no public "list all jobs" API, so the private list is read via reflection.
    /// If the internal field is renamed in a future version, the plugin keeps working without job details.
    /// </summary>
    private List<TranscodingJob> GetActiveTranscodingJobs(out bool available)
    {
        available = false;
        try
        {
            var field = _transcodeManager.GetType().GetField("_activeTranscodingJobs", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? _transcodeManager.GetType()
                            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                            .FirstOrDefault(f => typeof(IEnumerable<TranscodingJob>).IsAssignableFrom(f.FieldType));

            if (field?.GetValue(_transcodeManager) is IEnumerable<TranscodingJob> list)
            {
                available = true;
                // TranscodeManager itself locks on the list instance.
                lock (list)
                {
                    return list.ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "StreamJelly: could not enumerate transcoding jobs");
        }

        return new List<TranscodingJob>();
    }

    private static FfmpegHints ParseFfmpegArgs(string args)
    {
        var tokens = Tokenize(args);
        var hints = new FfmpegHints();

        for (var i = 0; i < tokens.Count - 1; i++)
        {
            var key = tokens[i];
            var val = tokens[i + 1];
            switch (key)
            {
                case "-c:v":
                case "-c:v:0":
                case "-codec:v":
                case "-codec:v:0":
                case "-vcodec":
                    hints.VideoEncoder = val;
                    break;
                case "-c:a":
                case "-c:a:0":
                case "-codec:a":
                case "-codec:a:0":
                case "-acodec":
                    hints.AudioEncoder = val;
                    break;
                case "-hwaccel":
                    hints.HwAccel = val;
                    break;
                case "-init_hw_device":
                    hints.HwAccel ??= val;
                    break;
                case "-preset":
                    hints.Preset = val;
                    break;
                case "-crf":
                    hints.Crf = val;
                    break;
                case "-b:v":
                case "-b:v:0":
                    hints.VideoBitrate = val;
                    break;
                case "-maxrate":
                    hints.MaxRate = val;
                    break;
                case "-b:a":
                case "-ab":
                    hints.AudioBitrate = val;
                    break;
                case "-ac":
                    hints.AudioChannels = val;
                    break;
                case "-hls_segment_type":
                    hints.SegmentFormat = val;
                    break;
                case "-hls_time":
                    hints.SegmentLength = val;
                    break;
                case "-map":
                    hints.Maps.Add(val);
                    break;
                case "-vf":
                case "-filter_complex":
                case "-filter:v":
                    if (val.Contains("subtitles=", StringComparison.OrdinalIgnoreCase)
                        || val.Contains("overlay", StringComparison.OrdinalIgnoreCase)
                        || val.Contains("alphasrc", StringComparison.OrdinalIgnoreCase))
                    {
                        hints.SubtitleBurnIn = true;
                    }

                    if (val.Contains("tonemap", StringComparison.OrdinalIgnoreCase))
                    {
                        hints.ToneMapping = true;
                    }

                    break;
            }
        }

        return hints;
    }

    private static List<string> Tokenize(string s)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        foreach (var c in s)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (sb.Length > 0)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }

                continue;
            }

            sb.Append(c);
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result;
    }

    private static List<string> SplitFlags(string flags)
    {
        if (string.IsNullOrWhiteSpace(flags) || flags == "0")
        {
            return new List<string>();
        }

        return flags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string? JoinParts(params string?[] parts)
    {
        var p = parts.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return p.Length == 0 ? null : string.Join(" · ", p);
    }

    private static string FormatBitrate(int bps)
    {
        return bps >= 1_000_000
            ? (bps / 1_000_000d).ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture) + " Mbps"
            : (bps / 1000d).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " kbps";
    }

    private static string? SafeGet(Func<string?> f)
    {
        try
        {
            return f();
        }
        catch
        {
            return null;
        }
    }
}
