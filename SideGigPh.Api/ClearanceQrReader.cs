using ImageMagick;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using ZXing;
using ZXing.Common;

namespace SideGigPh.Api;

/// <summary>
/// Reads the QR code on a photo of an NBI or police clearance, here on the server (ZXing.Net: free, nothing leaves
/// our server), only to help the operator: what it holds is shown beside the photo, with a link to open when it is an
/// NBI or PNP address (<see cref="ClearanceRules.OfficialLink"/>). It never refuses an upload and never approves
/// anything: a QR can be copied onto a fake paper, and an honest worker's crumpled paper may not read.
/// </summary>
public static class ClearanceQrReader
{
    /// <summary>
    /// <see cref="ScanStatuses.Read"/> with the QR's text, or <see cref="ScanStatuses.Unreadable"/> when no QR code
    /// was found. Never throws. Reads the stored JPEG (upright, already cleaned).
    /// </summary>
    public static IdScan Read(byte[] jpeg)
    {
        try
        {
            using var image = new MagickImage(jpeg);
            image.Grayscale();
            image.Depth = 8;
            var gray = image.ToByteArray(MagickFormat.Gray);
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions { TryHarder = true, PossibleFormats = [BarcodeFormat.QR_CODE] },
            };
            var result = reader.Decode(new RGBLuminanceSource(gray, (int)image.Width, (int)image.Height, RGBLuminanceSource.BitmapFormat.Gray8));
            return result?.Text is { Length: > 0 } text
                ? new IdScan(ScanStatuses.Read, Qr: text.Trim())
                : new IdScan(ScanStatuses.Unreadable);
        }
        catch (Exception ex) when (ex is MagickException or ArgumentException or InvalidOperationException)
        {
            return new IdScan(ScanStatuses.Unreadable);
        }
    }
}
