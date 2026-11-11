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
/// Turns an uploaded photo into the JPEG that gets stored: iPhone HEIC and PNG are converted,
/// a small JPEG is kept byte-for-byte (so its id matches the SHA-256 the app already computed).
/// </summary>
public static class PhotoImage
{
    // Same as the app's picker: photos are shown on a phone, never bigger than this.
    private const uint MaxSide = 1280;
    private const uint JpegQuality = 85;

    // A JPEG this small is stored as sent; a bigger one is shrunk like the others.
    private const int MaxStoredJpegBytes = 2 * 1024 * 1024;

    // HEIC/HEIF "brands" in the file header (bytes 8-11, after "ftyp").
    private static readonly string[] HeicBrands = ["heic", "heix", "hevc", "hevx", "heim", "heis", "mif1", "msf1"];

    static PhotoImage()
    {
        // Refuse "decompression bombs": a tiny file that claims to be a gigantic image.
        ResourceLimits.Width = 20_000;
        ResourceLimits.Height = 20_000;
    }

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
    public static byte[] ToStoredJpeg(byte[] bytes, PhotoKind kind) =>
        kind == PhotoKind.Jpeg && bytes.Length <= MaxStoredJpegBytes ? bytes : ConvertToJpeg(bytes, kind);

    private static byte[] ConvertToJpeg(byte[] bytes, PhotoKind kind)
    {
        using var image = new MagickImage(bytes, new MagickReadSettings
        {
            Format = kind switch
            {
                PhotoKind.Png => MagickFormat.Png,
                PhotoKind.Heic => MagickFormat.Heic,
                _ => MagickFormat.Jpeg,
            },
        });

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
        return image.ToByteArray();
    }
}
