namespace SideGigPh.Contracts.Photos;

/// <summary>What <see cref="SelfieJpegRules.Check"/> found.</summary>
public enum SelfieVerdict
{
    /// <summary>A whole JPEG within the limits: <see cref="SelfieCheck.Jpeg"/> is what to store.</summary>
    Ok,

    /// <summary>Not a JPEG (a PNG, a HEIC, anything else), or a JPEG a phone wouldn't make (CMYK).</summary>
    NotJpeg,

    /// <summary>Over <see cref="SelfieJpegRules.MaxBytes"/>, or a side over <see cref="SelfieJpegRules.MaxSide"/> pixels.</summary>
    TooBig,

    /// <summary>Starts like a JPEG but its structure is broken or cut short (on the server: or its pixels don't decode).</summary>
    Broken,
}

/// <summary>The verdict and, when <see cref="SelfieVerdict.Ok"/>, the JPEG to store (metadata taken out).</summary>
public sealed record SelfieCheck(SelfieVerdict Verdict, byte[]? Jpeg = null);

/// <summary>
/// The rules a worker's selfie must meet, the same on the phone (checked before it is sent, so a bad file never goes
/// up) and on the server (<c>SelfieJpeg</c>, which also decodes the pixels once): a whole JPEG, walked marker by marker
/// from start to end, with 1 or 3 colour components, at most <see cref="MaxBytes"/> and <see cref="MaxSide"/> pixels a
/// side. <see cref="Check"/> also gives back the image with its metadata taken out without touching the pixels: only
/// the segments a decoder needs are kept (an allow-list), and of the APPn segments only the bare JFIF header (its
/// thumbnail cut off), an ICC colour profile and Adobe's 12-byte colour flag. Comments, EXIF, GPS, XMP, IPTC, reserved
/// markers and anything after the end-of-image marker (a second picture, a depth map) go.
/// </summary>
public static class SelfieJpegRules
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
    private const byte DefineHuffman = 0xC4;
    private const byte DefineArithmetic = 0xCC;
    private const byte DefineQuantization = 0xDB;
    private const byte DefineRestart = 0xDD;
    private const byte App0 = 0xE0;
    private const byte App2 = 0xE2;
    private const byte App14 = 0xEE;

    // "JFIF", a zero byte, the version, units, densities and the thumbnail's size; Adobe's segment is always 12 bytes.
    private const int JfifHeader = 14;
    private const int AdobeHeader = 12;

    private static readonly byte[] Jfif = "JFIF\0"u8.ToArray();
    private static readonly byte[] IccProfile = "ICC_PROFILE\0"u8.ToArray();
    private static readonly byte[] Adobe = "Adobe"u8.ToArray();

    /// <summary>Checks the bytes and, when they pass, gives back the same image data with its metadata taken out.</summary>
    public static SelfieCheck Check(byte[] bytes)
    {
        if (bytes.Length > MaxBytes)
        {
            return new(SelfieVerdict.TooBig);
        }

        if (bytes.Length < 4 || bytes[0] != Mark || bytes[1] != StartOfImage || bytes[2] != Mark)
        {
            return new(SelfieVerdict.NotJpeg);
        }

        return Walk(bytes);
    }

    // Walks the markers from start-of-image to end-of-image, copying every segment but the metadata, and reads the
    // frame's size on the way. The entropy-coded data after each start-of-scan is copied byte for byte.
    private static SelfieCheck Walk(byte[] bytes)
    {
        using var kept = new MemoryStream(bytes.Length);
        kept.Write([Mark, StartOfImage]);
        var pos = 2;
        var frames = 0;
        var scans = 0;

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
                if (scans == 0)
                {
                    return new(SelfieVerdict.Broken); // no picture before the end
                }

                kept.Write([Mark, EndOfImage]); // anything after it (a second picture, a depth map) is left behind
                return new(SelfieVerdict.Ok, kept.ToArray());
            }

            if (marker is 0x01 || marker is >= 0xD0 and <= 0xD7)
            {
                continue; // markers with no length, meaningless outside scan data: left out
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

            WriteKept(kept, marker, payload);

            pos = segmentEnd;
            if (marker != StartOfScan)
            {
                continue;
            }

            if (frames == 0)
            {
                return new(SelfieVerdict.Broken); // a scan before the frame says its size
            }

            scans++;

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

            kept.Write(bytes, scanStart, pos - scanStart); // the next marker (another table or scan, or the end) is read above
        }
    }

    // Start-of-frame markers (C0–CF), except DHT (C4), JPG (C8) and DAC (CC), which share the range.
    private static bool IsFrame(byte marker) => marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC);

    // An allow-list: the segments a decoder needs (frame, Huffman and quantization tables, restart interval, scan
    // headers) are copied as they are; of the APPn segments only the bare JFIF header (its thumbnail cut off), an ICC
    // colour profile and Adobe's 12-byte colour flag are kept. Everything else (EXIF, XMP, IPTC, comments, reserved
    // markers) is left out.
    private static void WriteKept(MemoryStream kept, byte marker, ReadOnlySpan<byte> payload)
    {
        var keep = marker switch
        {
            _ when IsFrame(marker) => payload,
            DefineHuffman or DefineArithmetic or DefineQuantization or DefineRestart or StartOfScan => payload,
            App0 when payload.Length >= JfifHeader && payload.StartsWith(Jfif) => payload[..JfifHeader],
            App2 when payload.StartsWith(IccProfile) => payload,
            App14 when payload.Length >= AdobeHeader && payload.StartsWith(Adobe) => payload[..AdobeHeader],
            _ => ReadOnlySpan<byte>.Empty,
        };
        if (keep.IsEmpty)
        {
            return;
        }

        var length = keep.Length + 2;
        kept.Write([Mark, marker, (byte)(length >> 8), (byte)length]);
        if (marker == App0)
        {
            kept.Write(keep[..^2]);
            kept.Write([0, 0]); // no thumbnail: a JFIF thumbnail could be another picture
        }
        else
        {
            kept.Write(keep);
        }
    }
}
