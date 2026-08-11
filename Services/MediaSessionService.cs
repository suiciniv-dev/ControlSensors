using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace ControlSensors.Services;

public class MediaInfoDto {
    public bool IsPlaying { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string CoverBase64 { get; set; } = string.Empty;
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
}

public class MediaSessionService {
    public async Task<MediaInfoDto> GetCurrentMediaAsync() {

        return await Task.Run(async () => {
            var mediaInfo = new MediaInfoDto();

            try {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

                var session = manager?.GetCurrentSession();

                if (session == null || session.GetPlaybackInfo()?.PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing) {
                    var allSessions = manager?.GetSessions();
                    if (allSessions != null) {
                        foreach (var s in allSessions) {
                            if (s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing) {
                                session = s;
                                break;
                            }
                        }
                    }
                }

                if (session != null) {
                    var playbackInfo = session.GetPlaybackInfo();
                    mediaInfo.IsPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                    mediaInfo.AppName = session.SourceAppUserModelId;

                    var timeline = session.GetTimelineProperties();
                    if (timeline != null) {
                        mediaInfo.Position = timeline.Position;
                        mediaInfo.Duration = timeline.EndTime;
                    }

                    var mediaProperties = await session.TryGetMediaPropertiesAsync();
                    if (mediaProperties != null) {
                        mediaInfo.Title = mediaProperties.Title ?? string.Empty;
                        mediaInfo.Artist = mediaProperties.Artist ?? string.Empty;
                        mediaInfo.Album = mediaProperties.AlbumTitle ?? string.Empty;

                        if (mediaProperties.Thumbnail != null) {
                            using var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                            using var memoryStream = new MemoryStream();
                            stream.AsStreamForRead().CopyTo(memoryStream);
                            byte[] bytes = memoryStream.ToArray();
                            if (bytes.Length > 0) {
                                mediaInfo.CoverBase64 = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
                            }
                        }
                    }
                }
            } catch { }

            return mediaInfo;
        });
    }
}