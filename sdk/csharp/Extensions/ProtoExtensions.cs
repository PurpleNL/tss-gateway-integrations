using System;
using System.IO;
using Google.Protobuf.Collections;
using TSS.Gateway.Sdk.Models;

namespace TSS.Gateway.Sdk.Extensions
{
    public static partial class ProtoExtensions
    {
        public static double GetProgress(this Asset asset) => asset.DataCase switch
        {
            Asset.DataOneofCase.VideoData => asset.VideoData.Progress,
            Asset.DataOneofCase.SoundData => asset.SoundData.Progress,
            _ => throw new InvalidDataException("Asset is not seekable")
        };

        public static int GetDuration(this Asset asset) => asset.DataCase switch
        {
            Asset.DataOneofCase.VideoData => asset.VideoData.Duration,
            Asset.DataOneofCase.SoundData => asset.SoundData.Duration,
            _ => throw new InvalidDataException("Asset is not seekable")
        };

        public static void SetProgress(this Asset asset, double progress)
        {
            switch (asset.DataCase)
            {
                case Asset.DataOneofCase.VideoData:
                    asset.VideoData.Progress = progress;
                    break;
                case Asset.DataOneofCase.SoundData:
                    asset.SoundData.Progress = progress;
                    break;
                default: throw new InvalidDataException("Asset is not seekable");
            }
        }

        public static bool HasProgress(this Asset asset) => asset.DataCase == Asset.DataOneofCase.VideoData || asset.DataCase == Asset.DataOneofCase.SoundData;

        public static RepeatedField<FileData> GetFileData(this Asset asset) => asset.DataCase switch
        {
            Asset.DataOneofCase.VideoData => asset.VideoData.Files,
            Asset.DataOneofCase.SoundData => new RepeatedField<FileData> {asset.SoundData.File},
            Asset.DataOneofCase.PdfData => new RepeatedField<FileData> {asset.PdfData.File},
            Asset.DataOneofCase.ImageData => asset.ImageData.Files,
            _ => new RepeatedField<FileData>()
        };

        public static bool IsMuteable(this Asset asset) => asset.DataCase == Asset.DataOneofCase.VideoData || asset.DataCase == Asset.DataOneofCase.SoundData || asset.DataCase == Asset.DataOneofCase.UrlData || asset.DataCase == Asset.DataOneofCase.NdiData;

        public static bool CanChangeVolume(this Asset asset) => asset.DataCase == Asset.DataOneofCase.VideoData || asset.DataCase == Asset.DataOneofCase.SoundData || asset.DataCase == Asset.DataOneofCase.NdiData || asset.DataCase == Asset.DataOneofCase.UrlData;

        public static bool IsMuted(this Asset asset) => asset.DataCase switch
        {
            Asset.DataOneofCase.VideoData => asset.VideoData.HasIsMuted && asset.VideoData.IsMuted,
            Asset.DataOneofCase.SoundData => asset.SoundData.HasIsMuted && asset.SoundData.IsMuted,
            Asset.DataOneofCase.UrlData => asset.UrlData.HasIsMuted && asset.UrlData.IsMuted,
            Asset.DataOneofCase.NdiData => asset.NdiData.HasIsMuted && asset.NdiData.IsMuted,
            _ => throw new InvalidDataException("Asset is not muteable")
        };

        public static void SetMuted(this Asset asset, bool muted, float volume = 1f)
        {
            switch (asset.DataCase)
            {
                case Asset.DataOneofCase.VideoData:
                    asset.VideoData.IsMuted = muted;
                    break;
                case Asset.DataOneofCase.SoundData:
                    asset.SoundData.IsMuted = muted;
                    break;
                case Asset.DataOneofCase.UrlData:
                    asset.UrlData.IsMuted = muted;
                    break;
                case Asset.DataOneofCase.NdiData:
                    asset.NdiData.IsMuted = muted;
                    break;
                default: throw new InvalidDataException("Asset is not muteable");
            }
        }

        public static void SetVolume(this Asset asset, float volume = 1f)
        {
            switch (asset.DataCase)
            {
                case Asset.DataOneofCase.VideoData:
                    asset.VideoData.Volume = volume;
                    break;
                case Asset.DataOneofCase.SoundData:
                    asset.SoundData.Volume = volume;
                    break;
                case Asset.DataOneofCase.NdiData:
                    asset.NdiData.Volume = volume;
                    break;
                case Asset.DataOneofCase.UrlData:
                    asset.UrlData.Volume = volume;
                    break;
                default: throw new InvalidDataException("Asset volume can not be changed");
            }
        }

        public static float GetVolume(this Asset asset) => asset.DataCase switch
        {
            Asset.DataOneofCase.VideoData => asset.VideoData.Volume,
            Asset.DataOneofCase.SoundData => asset.SoundData.Volume,
            Asset.DataOneofCase.NdiData => asset.NdiData.Volume,
            Asset.DataOneofCase.UrlData => asset.UrlData.Volume,
            _ => throw new InvalidDataException("Asset does not have volume")
        };

        public static bool IsPlayable(this Asset asset) => asset.DataCase == Asset.DataOneofCase.VideoData || asset.DataCase == Asset.DataOneofCase.SoundData;

        public static bool IsPlaying(this Asset asset) => asset.DataCase switch
        {
            Asset.DataOneofCase.VideoData => asset.VideoData.Play,
            Asset.DataOneofCase.SoundData => asset.SoundData.Play,
            _ => throw new InvalidDataException("Asset is not playable")
        };

        public static void SetPlaying(this Asset asset, bool playing)
        {
            switch (asset.DataCase)
            {
                case Asset.DataOneofCase.VideoData:
                    asset.VideoData.Play = playing;
                    break;
                case Asset.DataOneofCase.SoundData:
                    asset.SoundData.Play = playing;
                    break;
                default: throw new InvalidDataException("Asset is not playable");
            }
        }

        public static void ForEach<T>(this RepeatedField<T> field, Action<T> action)
        {
            foreach (T item in field)
            {
                action(item);
            }
        }
    }
}