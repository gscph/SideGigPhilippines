using ImageMagick;
using SideGigPh.Api;
using SideGigPh.Contracts.Photos;

namespace SideGigPh.Tests.Unit.Photos;

/// <summary>
/// A worker's selfie is made small on the phone and only checked on the server (<see cref="SelfieJpeg"/>): a whole
/// JPEG within 1280px and 2 MB, stored with the same pixels, and never with EXIF or a location.
/// </summary>
public class SelfieJpegTests
{
    private static byte[] Jpeg(uint width, uint height, Action<MagickImage>? extra = null, bool progressive = false)
    {
        using var image = new MagickImage(MagickColors.SeaGreen, width, height) { Format = MagickFormat.Jpeg, Quality = 82 };
        if (progressive)
        {
            image.Settings.Interlace = Interlace.Plane;
        }

        extra?.Invoke(image);
        return image.ToByteArray();
    }

    private static void AddGps(MagickImage image)
    {
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(14, 1), new Rational(35, 1), new Rational(0, 1)]);
        exif.SetValue(ExifTag.Make, "Phone");
        image.SetProfile(exif);
        image.SetAttribute("comment", "taken at home");
    }

    private static SelfieCheck Passes(byte[] bytes)
    {
        var check = SelfieJpeg.Check(bytes);
        check.Verdict.Should().Be(SelfieVerdict.Ok);
        return check;
    }

    [Theory]
    [InlineData(1280u, 960u, false)]
    [InlineData(960u, 1280u, true)]
    [InlineData(40u, 30u, false)]
    public void PhoneSizedJpeg_IsStoredAsSent(uint width, uint height, bool progressive)
    {
        var sent = Jpeg(width, height, progressive: progressive);

        var stored = Passes(sent).Jpeg!;

        using var image = new MagickImage(stored);
        image.Width.Should().Be(width, "a selfie is never resized");
        image.Height.Should().Be(height);
        stored.Length.Should().BeLessThanOrEqualTo(sent.Length, "nothing is re-encoded, at most metadata comes out");
    }

    [Fact]
    public void ExifAndGps_AreTakenOut_WithoutTouchingThePixels()
    {
        var sent = Jpeg(800, 600, AddGps);
        new MagickImage(sent).GetExifProfile().Should().NotBeNull("the test photo must carry a location to begin with");

        var stored = Passes(sent).Jpeg!;

        using var image = new MagickImage(stored);
        image.GetExifProfile().Should().BeNull();
        image.GetAttribute("comment").Should().BeNull();
        using var original = new MagickImage(sent);
        image.Compare(original, ErrorMetric.Absolute).Should().Be(0, "the image data is copied byte for byte");
    }

    [Fact]
    public void AnythingAfterTheEnd_IsLeftBehind()
    {
        var sent = Jpeg(100, 100);
        byte[] hidden = [.. sent, .. Jpeg(50, 50)];

        Passes(hidden).Jpeg.Should().Equal(Passes(sent).Jpeg);
    }

    private static byte[] Segment(byte marker, byte[] payload) =>
        [0xFF, marker, (byte)((payload.Length + 2) >> 8), (byte)(payload.Length + 2), .. payload];

    // A segment slipped in right after the start-of-image marker.
    private static byte[] WithSegmentFirst(byte[] jpeg, byte[] segment) => [.. jpeg[..2], .. segment, .. jpeg[2..]];

    private static bool Contains(byte[] haystack, byte[] needle) => haystack.AsSpan().IndexOf(needle) >= 0;

    [Theory]
    [InlineData((byte)0xF0)] // a reserved JPEG extension marker
    [InlineData((byte)0xE5)] // an APPn nobody reads
    [InlineData((byte)0xFE)] // a comment
    public void DataHiddenInAnyOtherSegment_IsLeftOut(byte marker)
    {
        var secret = "GPS 14.5995,120.9842"u8.ToArray();

        var stored = Passes(WithSegmentFirst(Jpeg(200, 150), Segment(marker, secret))).Jpeg!;

        Contains(stored, secret).Should().BeFalse();
    }

    [Fact]
    public void JfifThumbnail_IsCutOff()
    {
        var thumbnail = Enumerable.Repeat((byte)0x7A, 3 * 2 * 2).ToArray(); // a 2x2 RGB picture
        byte[] app0 = [.. "JFIF\0"u8, 1, 1, 0, 0, 1, 0, 1, 2, 2, .. thumbnail];
        var sent = WithSegmentFirst(Jpeg(200, 150, image => image.RemoveProfile("app0")), Segment(0xE0, app0));

        var stored = Passes(sent).Jpeg!;

        Contains(stored, thumbnail).Should().BeFalse();
        Contains(stored, [0xFF, 0xE0, 0x00, 0x10, .. "JFIF\0"u8]).Should().BeTrue("the bare 14-byte JFIF header stays");
    }

    [Fact]
    public void ASegmentBetweenTheLastScanAndTheEnd_IsFine()
    {
        var sent = Jpeg(200, 150);
        byte[] withComment = [.. sent[..^2], .. Segment(0xFE, "late note"u8.ToArray()), 0xFF, 0xD9];

        Passes(withComment).Jpeg.Should().Equal(Passes(sent).Jpeg);
    }

    [Theory]
    [InlineData(1281u, 900u)]
    [InlineData(900u, 1281u)]
    [InlineData(4000u, 3000u)]
    public void BiggerThanThePhoneMakesIt_IsTooBig(uint width, uint height) =>
        SelfieJpeg.Check(Jpeg(width, height)).Verdict.Should().Be(SelfieVerdict.TooBig);

    [Fact]
    public void OverTheByteLimit_IsTooBig() =>
        SelfieJpeg.Check([0xFF, 0xD8, 0xFF, .. new byte[SelfieJpegRules.MaxBytes]]).Verdict.Should().Be(SelfieVerdict.TooBig);

    [Fact]
    public void NotAJpeg_IsRefused()
    {
        using var png = new MagickImage(MagickColors.SeaGreen, 100, 100) { Format = MagickFormat.Png };

        SelfieJpeg.Check(png.ToByteArray()).Verdict.Should().Be(SelfieVerdict.NotJpeg);
        SelfieJpeg.Check("hello"u8.ToArray()).Verdict.Should().Be(SelfieVerdict.NotJpeg);
        SelfieJpeg.Check([]).Verdict.Should().Be(SelfieVerdict.NotJpeg);
    }

    [Fact]
    public void CmykJpeg_IsRefused() =>
        SelfieJpeg.Check(Jpeg(100, 100, image => image.ColorSpace = ColorSpace.CMYK)).Verdict.Should().Be(SelfieVerdict.NotJpeg);

    [Fact]
    public void CutShort_IsBroken()
    {
        var sent = Jpeg(400, 300);

        SelfieJpeg.Check(sent[..(sent.Length / 2)]).Verdict.Should().Be(SelfieVerdict.Broken);
        SelfieJpeg.Check(sent[..^2]).Verdict.Should().Be(SelfieVerdict.Broken, "no end-of-image marker");
    }

    [Fact]
    public void JustTheStartMarker_IsBroken() =>
        SelfieJpeg.Check([0xFF, 0xD8, 0xFF, 0xD9]).Verdict.Should().Be(SelfieVerdict.Broken);

    [Fact]
    public void GarbageAfterTheStartMarker_IsBroken() =>
        SelfieJpeg.Check([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0xFF, 1, 2, 3]).Verdict.Should().Be(SelfieVerdict.Broken);
}
