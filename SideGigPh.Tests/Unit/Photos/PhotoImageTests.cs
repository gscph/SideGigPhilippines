using ImageMagick;
using SideGigPh.Api;

namespace SideGigPh.Tests.Unit.Photos;

/// <summary>
/// The server re-encodes every uploaded photo the same way (<see cref="PhotoImage"/>): small enough for prepaid data,
/// upright, and with the GPS location removed (a worker's job photo must not reveal a customer's home). It must also
/// be deterministic, because the duplicate-photo check (anti-fraud rule 2) hashes the result.
/// </summary>
public class PhotoImageTests
{
    private static byte[] Image(uint width, uint height, MagickFormat format, bool withGps = false)
    {
        using var image = new MagickImage(MagickColors.SeaGreen, width, height) { Format = format };
        if (withGps)
        {
            var exif = new ExifProfile();
            exif.SetValue(ExifTag.GPSLatitudeRef, "N");
            exif.SetValue(ExifTag.Make, "Phone");
            image.SetProfile(exif);
        }

        return image.ToByteArray();
    }

    [Fact]
    public void Detect_ReadsTheKindFromTheBytes_NotTheFileName()
    {
        PhotoImage.Detect(Image(10, 10, MagickFormat.Jpeg)).Should().Be(PhotoKind.Jpeg);
        PhotoImage.Detect(Image(10, 10, MagickFormat.Png)).Should().Be(PhotoKind.Png);
        PhotoImage.Detect([0, 0, 0, 0, .."ftypheic"u8, 0, 0, 0, 0]).Should().Be(PhotoKind.Heic);
        PhotoImage.Detect("GIF89a not a photo"u8.ToArray()).Should().BeNull();
        PhotoImage.Detect([]).Should().BeNull();
    }

    // A "decompression bomb": a small file whose header claims a gigantic picture. It's refused from the header,
    // before any pixels are decoded, while a real 48 MP phone photo's size passes.
    [Fact]
    public void IsTooBig_ReadsTheSizeFromTheHeader()
    {
        PhotoImage.IsTooBig(PngClaiming(30_000, 30_000), PhotoKind.Png).Should().BeTrue();
        PhotoImage.IsTooBig(PngClaiming(12_000, 8_000), PhotoKind.Png).Should().BeTrue("96 MP is over the pixel limit");
        PhotoImage.IsTooBig(PngClaiming(20_000, 100), PhotoKind.Png).Should().BeTrue("one side is over the limit");
        PhotoImage.IsTooBig(PngClaiming(8_064, 6_048), PhotoKind.Png).Should().BeFalse("a 48 MP camera photo fits");
        PhotoImage.IsTooBig(Image(300, 200, MagickFormat.Jpeg), PhotoKind.Jpeg).Should().BeFalse();
    }

    /// <summary>A real 1x1 PNG whose header (IHDR) is rewritten to claim <paramref name="width"/> x <paramref name="height"/>.</summary>
    private static byte[] PngClaiming(uint width, uint height)
    {
        var png = Image(1, 1, MagickFormat.Png);
        // Signature (8) + IHDR length (4) + "IHDR" (4), then width and height (big-endian), then the chunk's CRC.
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), height);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(29), Crc32(png.AsSpan(12, 17)));
        return png;
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    [Fact]
    public void ToStoredJpeg_BigPhoto_ShrinksToAtMost1280_KeepingItsShape()
    {
        var stored = PhotoImage.ToStoredJpeg(Image(3000, 1500, MagickFormat.Jpeg), PhotoKind.Jpeg);

        using var image = new MagickImage(stored);
        image.Width.Should().Be(1280);
        image.Height.Should().Be(640);
        image.Format.Should().Be(MagickFormat.Jpeg);
    }

    [Fact]
    public void ToStoredJpeg_SmallPhoto_IsNeverEnlarged()
    {
        using var image = new MagickImage(PhotoImage.ToStoredJpeg(Image(400, 300, MagickFormat.Png), PhotoKind.Png));

        image.Width.Should().Be(400);
        image.Format.Should().Be(MagickFormat.Jpeg, "PNG screenshots are stored as JPEG");
    }

    [Fact]
    public void ToStoredJpeg_RemovesExifAndGps()
    {
        using var image = new MagickImage(PhotoImage.ToStoredJpeg(Image(200, 200, MagickFormat.Jpeg, withGps: true), PhotoKind.Jpeg));

        image.GetExifProfile().Should().BeNull();
    }

    [Fact]
    public void ToStoredJpeg_TransparentPng_GetsAWhiteBackground()
    {
        using var transparent = new MagickImage(MagickColors.Transparent, 50, 50) { Format = MagickFormat.Png };

        using var image = new MagickImage(PhotoImage.ToStoredJpeg(transparent.ToByteArray(), PhotoKind.Png));

        var pixel = image.GetPixels().GetPixel(10, 10).ToColor()!;
        pixel.R.Should().BeGreaterThan(250);
        pixel.G.Should().BeGreaterThan(250);
    }

    [Fact]
    public void ToStoredJpeg_SameUpload_GivesTheSameBytes_SoDuplicatesAreCaught()
    {
        var upload = Image(1600, 1200, MagickFormat.Jpeg);

        PhotoImage.ToStoredJpeg(upload, PhotoKind.Jpeg).Should().Equal(PhotoImage.ToStoredJpeg(upload, PhotoKind.Jpeg));
    }

    [Fact]
    public void ToStoredJpeg_PhotoWithAColourProfile_IsConvertedToSrgbAndTheProfileDropped()
    {
        // iPhones shoot in a wide colour space; converting before stripping keeps colours from going dull.
        using var wide = new MagickImage(MagickColors.Orange, 100, 100) { Format = MagickFormat.Jpeg };
        wide.SetProfile(ColorProfiles.AdobeRGB1998);

        using var image = new MagickImage(PhotoImage.ToStoredJpeg(wide.ToByteArray(), PhotoKind.Jpeg));

        image.GetColorProfile().Should().BeNull();
        image.ColorSpace.Should().Be(ColorSpace.sRGB);
    }

    [Theory]
    [InlineData(PhotoKind.Jpeg)]
    [InlineData(PhotoKind.Heic)]
    public void ToStoredJpeg_BrokenFile_Throws(PhotoKind kind) =>
        FluentActions.Invoking(() => PhotoImage.ToStoredJpeg([0xFF, 0xD8, 0xFF, 1, 2, 3], kind))
            .Should().Throw<MagickException>("the upload answers broken-image instead of crashing");
}
