using System;
using Google.Protobuf.Collections;
using TSS.Gateway.Sdk.Models;

namespace TSS.Gateway.Sdk.Extensions
{
    public static partial class ProtoExtensions
    {
        public static void CopyFrom(this Asset asset, Asset other)
        {
            if (other == null)
            {
                return;
            }

            asset.Uuid = other.Uuid;
            asset.Title = other.Title;
            asset.Layer = other.Layer;
            asset.Opacity = other.Opacity;
            asset.Draggable = other.Draggable;
            asset.RoundedCorners = other.RoundedCorners;
            asset.Visible = other.Visible;

            asset.Rectangle.CopyFrom(other.Rectangle);
            asset.Margins.CopyFrom(other.Margins);

            switch (other.DataCase)
            {
                case Asset.DataOneofCase.UrlData:
                    asset.UrlData = other.UrlData.Clone();
                    break;
                case Asset.DataOneofCase.VideoData:
                    asset.VideoData = other.VideoData.Clone();
                    break;
                case Asset.DataOneofCase.TextData:
                    asset.TextData = other.TextData.Clone();
                    break;
                case Asset.DataOneofCase.SoundData:
                    asset.SoundData = other.SoundData.Clone();
                    break;
                case Asset.DataOneofCase.PdfData:
                    asset.PdfData = other.PdfData.Clone();
                    break;
                case Asset.DataOneofCase.NdiData:
                    asset.NdiData = other.NdiData.Clone();
                    break;
                case Asset.DataOneofCase.ImageData:
                    asset.ImageData = other.ImageData.Clone();
                    break;
            }
        }

        public static void CopyFileData(this Asset asset, RepeatedField<FileData> other)
        {
            switch (asset.DataCase)
            {
                case Asset.DataOneofCase.VideoData:
                    for (int i = 0; i < other.Count; i++)
                    {
                        asset.VideoData.Files[i].CopyFrom(other[i]);
                    }

                    break;
                case Asset.DataOneofCase.SoundData:
                    asset.SoundData.File.CopyFrom(other[0]);
                    break;
                case Asset.DataOneofCase.PdfData:
                    asset.PdfData.File.CopyFrom(other[0]);
                    break;
                case Asset.DataOneofCase.ImageData:
                    for (int i = 0; i < other.Count; i++)
                    {
                        asset.ImageData.Files[i].CopyFrom(other[i]);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void CopyFrom(this FileData fileData, FileData other)
        {
            fileData.Name = other.Name;
            fileData.Uuid = other.Uuid;
            fileData.Version = other.Version;
        }

        public static void CopyFrom(this Rectangle rectangle, Rectangle other)
        {
            if (other == null)
            {
                return;
            }

            rectangle.X = other.X;
            rectangle.Y = other.Y;
            rectangle.Width = other.Width;
            rectangle.Height = other.Height;
        }

        public static void CopyFrom(this Margins margins, Margins other)
        {
            if (other == null)
            {
                return;
            }

            margins.Left = other.Left;
            margins.Top = other.Top;
            margins.Right = other.Right;
            margins.Bottom = other.Bottom;
        }
    }
}