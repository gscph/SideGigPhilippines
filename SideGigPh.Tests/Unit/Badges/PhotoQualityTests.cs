using ImageMagick;
using ImageMagick.Drawing;
using SideGigPh.Api;
using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.Badges;

/// <summary>
/// The server's photo check on badge proofs (<see cref="PhotoQuality"/>): a sharp, well-lit document passes, and a
/// blurry, glaring, dark or tiny one is named so the worker can be asked "take it again?". Only advice, never a refusal.
/// </summary>
public class PhotoQualityTests
{
    // Black and white stripes 4px wide: plenty of edges, like the print on an ID, and mid-grey overall.
    private static byte[] Stripes(int w, int h)
    {
        var grey = new byte[w * h];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                grey[(y * w) + x] = (byte)((x / 4 % 2 == 0) ? 60 : 200);
            }
        }

        return grey;
    }

    private static byte[] Flat(int w, int h, byte value) => Enumerable.Repeat(value, w * h).ToArray();

    [Fact]
    public void ASharpWellLitDocument_HasNoProblems()
    {
        var result = PhotoQuality.FromGrey(Stripes(200, 100), 200, 100, 2000, 1000);

        result.Problems.Should().BeEmpty();
        result.Sharpness.Should().BeGreaterThan(PhotoQuality.BlurryBelow);
    }

    [Fact]
    public void APhotoWithNoEdges_IsBlurry()
    {
        PhotoQuality.FromGrey(Flat(200, 100, 128), 200, 100, 2000, 1000).Problems.Should().Equal(PhotoProblems.Blurry);
    }

    [Fact]
    public void ABlownOutPatch_IsGlare()
    {
        var grey = Stripes(200, 100);
        Array.Fill(grey, (byte)255, 0, 200 * 10); // the top tenth white from a flash

        PhotoQuality.FromGrey(grey, 200, 100, 2000, 1000).Problems.Should().Contain(PhotoProblems.Glare);
    }

    [Fact]
    public void AMostlyDarkPhoto_IsDark()
    {
        var grey = Stripes(200, 100);
        Array.Fill(grey, (byte)10, 0, 200 * 60); // a shadow over most of it

        PhotoQuality.FromGrey(grey, 200, 100, 2000, 1000).Problems.Should().Contain(PhotoProblems.Dark);
    }

    [Fact]
    public void ATinyPhoto_IsTooSmall()
    {
        PhotoQuality.FromGrey(Stripes(200, 100), 200, 100, 640, 320).Problems.Should().Equal(PhotoProblems.TooSmall);
    }

    [Fact]
    public void OnARealJpeg_BlurringItIsCaught()
    {
        using var sharp = new MagickImage(new MagickColor("#E6E6E6"), 1600, 1000) { Format = MagickFormat.Jpeg };
        // Light grey paper (a phone exposes white below clipping) with rows of small black blocks like lines of print (no fonts needed).
        var print = new Drawables().FillColor(MagickColors.Black);
        for (var line = 0; line < 12; line++)
        {
            for (var letter = 0; letter < 60; letter++)
            {
                var x = 60 + (letter * 24);
                var y = 80 + (line * 70);
                print.Rectangle(x, y, x + 12, y + 28);
            }
        }

        print.Draw(sharp);
        var sharpBytes = sharp.ToByteArray();
        using var blurred = new MagickImage(sharpBytes);
        blurred.Blur(0, 8);

        var good = PhotoQuality.Measure(sharpBytes);
        var bad = PhotoQuality.Measure(blurred.ToByteArray());

        good.Problems.Should().NotContain(PhotoProblems.Blurry);
        bad.Problems.Should().Contain(PhotoProblems.Blurry);
        good.Sharpness.Should().BeGreaterThan(bad.Sharpness);
        (good.Width, good.Height).Should().Be((1600, 1000), "the numbers are about the photo as stored, not the grey copy");
    }
}
