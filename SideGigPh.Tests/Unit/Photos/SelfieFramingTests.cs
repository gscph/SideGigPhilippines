using FluentAssertions;
using SideGigPh.Maui.Services;
using Xunit;

namespace SideGigPh.Tests.Unit.Photos;

public class SelfieFramingTests
{
    // A portrait camera picture as on a phone: 1080 wide, 4:3.
    private const double Width = 1080;
    private const double Height = 1440;

    private static readonly SelfieOval Oval = SelfieFraming.OvalIn(Width, Height);

    [Fact]
    public void AFaceFillingTheOval_IsGood() =>
        SelfieFraming.Check(Width, Height, new FaceBox(Oval.CentreX, Oval.CentreY, Oval.Width * 0.8)).Should().Be(SelfieFrame.Good);

    [Fact]
    public void NoFace_IsNoFace() =>
        SelfieFraming.Check(Width, Height, null).Should().Be(SelfieFrame.NoFace);

    [Fact]
    public void AFaceOffToTheSide_IsOffCentre() =>
        SelfieFraming.Check(Width, Height, new FaceBox(Oval.CentreX + Oval.Width * 0.4, Oval.CentreY, Oval.Width * 0.8)).Should().Be(SelfieFrame.OffCentre);

    [Fact]
    public void AFaceAboveTheOval_IsOffCentre() =>
        SelfieFraming.Check(Width, Height, new FaceBox(Oval.CentreX, Oval.CentreY - Oval.Height * 0.4, Oval.Width * 0.8)).Should().Be(SelfieFrame.OffCentre);

    [Fact]
    public void ASmallFace_IsTooFar() =>
        SelfieFraming.Check(Width, Height, new FaceBox(Oval.CentreX, Oval.CentreY, Oval.Width * 0.25)).Should().Be(SelfieFrame.TooFar);

    [Fact]
    public void AnArmsLengthSelfie_IsGood() =>
        SelfieFraming.Check(Width, Height, new FaceBox(Oval.CentreX, Oval.CentreY, Oval.Width * 0.4)).Should().Be(SelfieFrame.Good);

    [Fact]
    public void AFaceBiggerThanTheOval_IsTooClose() =>
        SelfieFraming.Check(Width, Height, new FaceBox(Oval.CentreX, Oval.CentreY, Oval.Width * 1.3)).Should().Be(SelfieFrame.TooClose);

    [Fact]
    public void ASlightlyOffFace_StillCounts() =>
        SelfieFraming.Check(Width, Height, new FaceBox(Oval.CentreX + Oval.Width * 0.1, Oval.CentreY + Oval.Height * 0.1, Oval.Width * 0.7)).Should().Be(SelfieFrame.Good);

    [Fact]
    public void TheOval_IsCentredAcrossAndAboveTheMiddle()
    {
        Oval.CentreX.Should().Be(Width / 2);
        Oval.CentreY.Should().BeLessThan(Height / 2);
        Oval.Height.Should().BeGreaterThan(Oval.Width);
    }
}
