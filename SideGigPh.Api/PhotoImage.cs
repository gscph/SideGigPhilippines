using ImageMagick;

namespace SideGigPh.Api;

/// <summary>The image types an upload may be. Everything is stored as JPEG.</summary>
public enum PhotoKind
{
    Jpeg,
    Png,
    Heic,
}

/// <summary>
/// Turns every uploaded photo into the one stored shape, the way photo apps like Airbnb do before serving:
/// upright (EXIF rotation baked in), sRGB, at most 1280px on the long side (never enlarged: blowing a small photo up
/// only makes it blurry), JPEG at quality 82, with EXIF and GPS removed. The screens crop it to fit with
/// object-fit: cover (square avatars, square work thumbnails), so any shape of photo fills them.
/// Re-encoding is deterministic, so the same upload still gets the same id (anti-fraud rule 2).
/// </summary>
public static class PhotoImage
{
    // Same as the app's picker: photos are shown on a phone, never bigger than this.
    private const uint MaxSide = 1280;
    private const uint JpegQuality = 82;

    // HEIC/HEIF "brands" in the file header (bytes 8-11, after "ftyp").
    private static readonly string[] HeicBrands = ["heic", "heix", "hevc", "hevx", "heim", "heis", "mif1", "msf1"];

    /// <summary>
    /// The biggest photo accepted, in pixels: a 48 MP phone camera (8064 x 6048) fits with room to spare. Decoding
    /// needs about 8 bytes a pixel, so this keeps one upload under ~0.5 GB of memory.
    /// </summary>
    public const ulong MaxPixels = 64_000_000;

    /// <summary>The longest side accepted (a panorama), checked with <see cref="MaxPixels"/>.</summary>
    public const uint MaxSideAccepted = 16_384;

    static PhotoImage()
    {
        // A backstop for "decompression bombs" (a tiny file that claims to be a gigantic image) in anything that
        // decodes without IsTooBig first. It's JPEG's largest side, above MaxSideAccepted, so that IsTooBig can still
        // read an oversized header and answer "too large" instead of the decoder calling the file broken.
        ResourceLimits.Width = 65_535;
        ResourceLimits.Height = 65_535;
    }

    /// <summary>
    /// True when the image says it is bigger than <see cref="MaxPixels"/> or <see cref="MaxSideAccepted"/>, read from
    /// its header without decoding the pixels. Throws <see cref="MagickException"/> when the header is broken.
    /// </summary>
    public static bool IsTooBig(byte[] bytes, PhotoKind kind)
    {
        var info = new MagickImageInfo(bytes, new MagickReadSettings { Format = FormatOf(kind) });
        return info.Width > MaxSideAccepted || info.Height > MaxSideAccepted || (ulong)info.Width * info.Height > MaxPixels;
    }

    private static MagickFormat FormatOf(PhotoKind kind) => kind switch
    {
        PhotoKind.Png => MagickFormat.Png,
        PhotoKind.Heic => MagickFormat.Heic,
        _ => MagickFormat.Jpeg,
    };

    /// <summary>What the bytes are, from their first bytes (not the Content-Type, which a client can fake), or null.</summary>
    public static PhotoKind? Detect(byte[] bytes)
    {
        if (bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return PhotoKind.Jpeg;
        }

        if (bytes.Length > 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return PhotoKind.Png;
        }

        if (bytes.Length > 12 && System.Text.Encoding.ASCII.GetString(bytes, 4, 4) == "ftyp"
            && HeicBrands.Contains(System.Text.Encoding.ASCII.GetString(bytes, 8, 4)))
        {
            return PhotoKind.Heic;
        }

        return null;
    }

    /// <summary>The JPEG to store. Throws <see cref="MagickException"/> when the file is broken.</summary>
    public static byte[] ToStoredJpeg(byte[] bytes, PhotoKind kind)
    {
        using var image = new MagickImage(bytes, new MagickReadSettings { Format = FormatOf(kind) });

        // iPhones store "rotate when showing" instead of rotated pixels; bake it in before the tag is stripped.
        image.AutoOrient();

        // JPEG has no transparency: a transparent PNG gets a white background instead of black.
        if (image.HasAlpha)
        {
            image.BackgroundColor = MagickColors.White;
            image.Alpha(AlphaAction.Remove);
        }

        if (image.Width > MaxSide || image.Height > MaxSide)
        {
            image.Resize(new MagickGeometry(MaxSide, MaxSide)); // fits inside, keeps the shape
        }

        // iPhone photos are in Display P3; convert to sRGB before the color profile is stripped, or colors go dull.
        if (image.GetColorProfile() is not null)
        {
            image.TransformColorSpace(ColorProfiles.SRGB);
        }

        // Drops EXIF, including the GPS location phones put in photos (a home address, for a worker's job photo).
        image.Strip();

        image.Format = MagickFormat.Jpeg;
        image.Quality = JpegQuality;
        image.Settings.Interlace = Interlace.Plane; // progressive: a blurry whole photo shows first on slow data
        return image.ToByteArray();
    }
}
