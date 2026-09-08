using System;
using System.Collections.Generic;

// ReSharper disable UnusedMember.Global

namespace TSS.Gateway.Sdk
{
    public class AssetException : Exception
    {
        public int Code { get; }

        private static readonly Dictionary<int, string> CodeNameMap = new Dictionary<int, string>
        {
            {801, "UnsupportedCodecBitdepthChromakeyCombination"},
            {802, "UnsupportedVideoTooSmall"},
            {803, "UnsupportedVideoTooBig"},
            {810, "UnsupportedImageFormat"},
            {811, "UnsupportedImageTooBig"},
            {820, "CouldNotLoadUrl"},
            {821, "InvalidUrl"},
            {830, "UnsupportedAudioFormat"},
            {899, "GeneralException"}
        };

        public AssetException(int code) : base(GetNameByCode(code)) => Code = code;

        // video
        public static AssetException UnsupportedCodecBitdepthChromakeyCombination => new AssetException(801);
        public static AssetException UnsupportedVideoTooSmall => new AssetException(802);
        public static AssetException UnsupportedVideoTooBig => new AssetException(803);

        // image
        public static AssetException UnsupportedImageFormat => new AssetException(810);
        public static AssetException UnsupportedImageTooBig => new AssetException(811);

        // url
        public static AssetException CouldNotLoadUrl => new AssetException(820);
        public static AssetException InvalidUrl => new AssetException(821);

        // audio
        public static AssetException UnsupportedAudioFormat => new AssetException(830);

        // general
        public static AssetException GeneralException => new AssetException(899);

        public static string GetNameByCode(int code) => CodeNameMap.GetValueOrDefault(code, "UnknownException");
    }
}