using ImageMagick;
using SideGigPh.Contracts.Photos;

namespace SideGigPh.Api;

/// <summary>
/// A worker's selfie (their profile photo) arrives already prepared by the phone: upright, sRGB, at most
/// <see cref="SelfieJpegRules.MaxSide"/> pixels on the long side, JPEG quality about 82, no metadata. So, unlike the
/// other photos (<see cref="PhotoImage.ToStoredJpeg"/>), it is <b>checked, not re-encoded</b>: the same rules the phone
/// checks before sending (<see cref="SelfieJpegRules"/>), then its pixels decoded once to prove they read.
/// <para>
/// Privacy: the phone already leaves metadata out, and <see cref="SelfieJpegRules.Check"/> takes it out again without
/// touching the pixels (an allow-list of segments), so a bug or a modified app can't store a location. EXIF, GPS, XMP,
/// IPTC and thumbnails are never stored.
/// </para>
/// </summary>
public static class SelfieJpeg
{
    /// <summary>Checks the upload and, when it passes, gives back the same image data with its metadata taken out.</summary>
    public static SelfieCheck Check(byte[] bytes)
    {
        var walked = SelfieJpegRules.Check(bytes);
        if (walked.Verdict != SelfieVerdict.Ok)
        {
            return walked;
        }

        try
        {
            // Decoded once to prove the pixels read; nothing is written back.
            using var image = new MagickImage(walked.Jpeg!, new MagickReadSettings { Format = MagickFormat.Jpeg });
            return image.Width is 0 || image.Height is 0 ? new(SelfieVerdict.Broken) : walked;
        }
        catch (MagickException)
        {
            return new(SelfieVerdict.Broken);
        }
    }
}
