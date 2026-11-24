using ImageMagick;

namespace SideGigPh.Api;

/// <summary>What <see cref="SelfieJpeg.Check"/> found.</summary>
public enum SelfieVerdict
{
    /// <summary>A whole JPEG within the limits: <see cref="SelfieCheck.Jpeg"/> is what to store.</summary>
    Ok,

    /// <summary>Not a JPEG (a PNG, a HEIC, anything else), or a JPEG a phone wouldn't make (CMYK).</summary>
    NotJpeg,

    /// <summary>Over <see cref="SelfieJpeg.MaxBytes"/>, or a side over <see cref="SelfieJpeg.MaxSide"/> pixels.</summary>
    TooBig,

    /// <summary>Starts like a JPEG but its structure is broken, cut short, or its pixels don't decode.</summary>
    Broken,
}

/// <summary>The verdict and, when <see cref="SelfieVerdict.Ok"/>, the JPEG to store (metadata taken out).</summary>
public sealed record SelfieCheck(SelfieVerdict Verdict, byte[]? Jpeg = null);

/// <summary>
/// A worker's selfie (their profile photo) arrives already prepared by the phone: upright, sRGB, at most
/// <see cref="MaxSide"/> pixels on the long side, JPEG quality about 82, no metadata. So, unlike the other photos
/// (<see cref="PhotoImage.ToStoredJpeg"/>), it is <b>checked, not re-encoded</b>: a real JPEG (its markers walked from
/// start to end, then its pixels decoded once to prove they read), within <see cref="MaxBytes"/> and
/// <see cref="MaxSide"/>, or refused.
/// <para>
/// Privacy: the phone already leaves metadata out, and this takes it out again without touching the pixels, so a bug
/// or a modified app can't store a location. Every APPn segment goes except the JFIF header, an ICC colour profile
/// and Adobe's colour-transform flag (which only say how to show the colours), comments go, and anything after the
/// end-of-image marker (a second picture, a depth map) goes. EXIF, GPS, XMP, IPTC and thumbnails are never stored.
/// </para>
/// </summary>
public static class SelfieJpeg
{
    /// <summary>The longest side a selfie may have: the phone's own limit.</summary>
    public const int MaxSide = 1280;

    /// <summary>
    /// The most bytes a selfie may have. A 1280px JPEG at quality 82 is a few hundred KB; this leaves room for a busy
    /// picture and a colour profile.
    /// </summary>
    public const int MaxBytes = 2 * 1024 * 1024;

    private const byte Mark = 0xFF;
    private const byte StartOfImage = 0xD8;
    private const byte EndOfImage = 0xD9;
    private const byte StartOfScan = 0xDA;
    private const byte Comment = 0xFE;
    private const byte App0 = 0xE0;
    private const byte App2 = 0xE2;
    private const byte App14 = 0xEE;
    private const byte App15 = 0xEF;

    private static readonly byte[] Jfif = "JFIF\0"u8.ToArray();
    private static readonly byte[] IccProfile = "ICC_PROFILE\0"u8.ToArray();
    private static readonly byte[] Adobe = "Adobe"u8.ToArray();

    /// <summary>Checks the upload and, when it passes, gives back the same image data with its metadata taken out.</summary>
    public static SelfieCheck Check(byte[] bytes)
    {
        if (bytes.Length > MaxBytes)
        {
            return new(SelfieVerdict.TooBig);
        }

        if (PhotoImage.Detect(bytes) != PhotoKind.Jpeg)
        {
            return new(SelfieVerdict.NotJpeg);
        }

        var walked = Walk(bytes);
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

    // Walks the markers from start-of-image to end-of-image, copying every segment but the metadata, and reads the
    // frame's size on the way. The entropy-coded data after each start-of-scan is copied byte for byte.
    private static SelfieCheck Walk(byte[] bytes)
    {
        using var kept = new MemoryStream(bytes.Length);
        kept.Write([Mark, StartOfImage]);
        var pos = 2;
        var frames = 0;

        while (true)
        {
            if (pos >= bytes.Length || bytes[pos] != Mark)
            {
                return new(SelfieVerdict.Broken);
            }

            while (pos < bytes.Length && bytes[pos] == Mark)
            {
                pos++; // fill bytes before a marker
            }

            if (pos >= bytes.Length)
            {
                return new(SelfieVerdict.Broken);
            }

            var marker = bytes[pos++];
            if (marker == EndOfImage)
            {
                return new(SelfieVerdict.Broken); // no picture before the end
            }

            if (marker is 0x01 || marker is >= 0xD0 and <= 0xD7)
            {
                kept.Write([Mark, marker]); // markers with no length
                continue;
            }

            if (pos + 2 > bytes.Length)
            {
                return new(SelfieVerdict.Broken);
            }

            var length = (bytes[pos] << 8) | bytes[pos + 1];
            if (length < 2 || pos + length > bytes.Length)
            {
                return new(SelfieVerdict.Broken);
            }

            var payload = bytes.AsSpan(pos + 2, length - 2);
            var segmentEnd = pos + length;

            if (IsFrame(marker))
            {
                if (++frames > 1 || payload.Length < 6)
                {
                    return new(SelfieVerdict.Broken);
                }

                var height = (payload[1] << 8) | payload[2];
                var width = (payload[3] << 8) | payload[4];
                var components = payload[5];
                if (width == 0 || height == 0)
                {
                    return new(SelfieVerdict.Broken);
                }

                if (components is not (1 or 3))
                {
                    return new(SelfieVerdict.NotJpeg);
                }

                if (width > MaxSide || height > MaxSide)
                {
                    return new(SelfieVerdict.TooBig);
                }
            }

            if (Keeps(marker, payload))
            {
                kept.Write([Mark, marker]);
                kept.Write(bytes, pos, length);
            }

            pos = segmentEnd;
            if (marker != StartOfScan)
            {
                continue;
            }

            if (frames == 0)
            {
                return new(SelfieVerdict.Broken); // a scan before the frame says its size
            }

            // The scan's data runs to the next marker that isn't a stuffed 0xFF00 or a restart marker.
            var scanStart = pos;
            while (true)
            {
                if (pos + 1 >= bytes.Length)
                {
                    return new(SelfieVerdict.Broken); // cut short
                }

                if (bytes[pos] == Mark && bytes[pos + 1] is not (0x00 or Mark) && bytes[pos + 1] is not (>= 0xD0 and <= 0xD7))
                {
                    break;
                }

                pos++;
            }

            kept.Write(bytes, scanStart, pos - scanStart);
            if (bytes[pos + 1] == EndOfImage)
            {
                kept.Write([Mark, EndOfImage]); // anything after it (a second picture, a depth map) is left behind
                return new(SelfieVerdict.Ok, kept.ToArray());
            }
        }
    }

    // Start-of-frame markers (C0–CF), except DHT (C4), JPG (C8) and DAC (CC), which share the range.
    private static bool IsFrame(byte marker) => marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC);

    private static bool Keeps(byte marker, ReadOnlySpan<byte> payload) => marker switch
    {
        App0 => payload.StartsWith(Jfif),
        App2 => payload.StartsWith(IccProfile),
        App14 => payload.StartsWith(Adobe),
        >= App0 and <= App15 => false,
        Comment => false,
        _ => true,
    };
}
