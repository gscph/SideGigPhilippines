using ImageMagick;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The photo check's numbers for one badge proof. <see cref="Sharpness"/> is the variance of the edges (a Laplacian) on
/// a grey copy <see cref="PhotoQuality.MeasureSide"/> pixels long: a sharp card is in the hundreds, a shaken one under
/// ~60. <see cref="GlareShare"/> and <see cref="DarkShare"/> are the parts of the photo (0-1) that are blown out white
/// or nearly black. <see cref="Problems"/> are the <see cref="Contracts.PhotoProblems"/> codes the cut-offs found.
/// </summary>
public sealed record PhotoQualityResult(int Width, int Height, double Sharpness, double GlareShare, double DarkShare, IReadOnlyList<string> Problems);

/// <summary>
/// Looks at a badge proof (an ID, a clearance, a certificate) the moment it arrives, so a worker hears "this looks
/// blurry, take it again?" in seconds instead of an operator's rejection hours later. It is only advice: nothing is
/// refused for it (the worker can always send it anyway), and the numbers are stored so the cut-offs below can be
/// set from real photos and the operator's decisions. Free: Magick.NET, already used for every photo, no vendor.
/// </summary>
public static class PhotoQuality
{
    /// <summary>The grey copy's long side: the same yardstick for every phone, and quick (well under a second).</summary>
    public const int MeasureSide = 1000;

    // First guesses, to be set from data (dbo.WorkerVerifications: PhotoSharpness and PhotoProblems against RejectCode):
    // only advice until then, so a wrong guess costs one "send anyway" tap, never a badge.

    /// <summary>Below this edge variance the photo counts as blurry (shared with the operator screens).</summary>
    public const double BlurryBelow = PhotoProblems.BlurryBelow;

    /// <summary>A pixel this bright (0-255) is clipped: a phone exposes white paper well below it, a flash on laminate reaches it.</summary>
    public const byte GlareFrom = 254;

    /// <summary>More than this part of the photo blown out is glare.</summary>
    public const double GlareAbove = 0.04;

    /// <summary>A pixel this dark (0-255) is in shadow.</summary>
    public const byte DarkUpTo = 40;

    /// <summary>More than this part of the photo in shadow is too dark.</summary>
    public const double DarkAbove = 0.45;

    /// <summary>A photo whose long side is shorter than this can't show an ID's small print.</summary>
    public const int SmallerThan = 900;

    /// <summary>The numbers for a stored proof (a JPEG, upright). Throws <see cref="MagickException"/> for a broken file.</summary>
    public static PhotoQualityResult Measure(byte[] jpeg)
    {
        using var image = new MagickImage(jpeg);
        var width = (int)image.Width;
        var height = (int)image.Height;
        if (Math.Max(width, height) > MeasureSide)
        {
            image.Resize(new MagickGeometry(MeasureSide, MeasureSide));
        }

        image.ColorSpace = ColorSpace.Gray;
        var w = (int)image.Width;
        var h = (int)image.Height;
        using var pixels = image.GetPixels();
        var grey = pixels.ToByteArray(0, 0, (uint)w, (uint)h, "R") ?? [];
        return FromGrey(grey, w, h, width, height);
    }

    /// <summary>The numbers from one grey byte per pixel (row by row), for <see cref="Measure"/> and the tests.</summary>
    public static PhotoQualityResult FromGrey(byte[] grey, int w, int h, int width, int height)
    {
        long glare = 0, dark = 0;
        foreach (var value in grey)
        {
            if (value >= GlareFrom)
            {
                glare++;
            }
            else if (value <= DarkUpTo)
            {
                dark++;
            }
        }

        // The Laplacian (4 neighbours minus 4 x the pixel) on every inner pixel, then its variance.
        double sum = 0, sumSquares = 0;
        long count = 0;
        for (var y = 1; y < h - 1; y++)
        {
            for (var x = 1; x < w - 1; x++)
            {
                var i = (y * w) + x;
                double laplacian = grey[i - 1] + grey[i + 1] + grey[i - w] + grey[i + w] - (4 * grey[i]);
                sum += laplacian;
                sumSquares += laplacian * laplacian;
                count++;
            }
        }

        var sharpness = count == 0 ? 0 : (sumSquares / count) - Math.Pow(sum / count, 2);
        var total = Math.Max(1, grey.Length);
        var glareShare = (double)glare / total;
        var darkShare = (double)dark / total;

        var problems = new List<string>();
        if (Math.Max(width, height) < SmallerThan)
        {
            problems.Add(PhotoProblems.TooSmall);
        }

        if (sharpness < BlurryBelow)
        {
            problems.Add(PhotoProblems.Blurry);
        }

        if (glareShare > GlareAbove)
        {
            problems.Add(PhotoProblems.Glare);
        }

        if (darkShare > DarkAbove)
        {
            problems.Add(PhotoProblems.Dark);
        }

        return new PhotoQualityResult(width, height, Math.Round(sharpness, 1), Math.Round(glareShare, 4), Math.Round(darkShare, 4), problems);
    }
}
