namespace SideGigPh.Maui.Services;

/// <summary>Where the face is against the selfie camera's oval (<see cref="SelfieFraming.Check"/>).</summary>
public enum SelfieFrame
{
    /// <summary>No face found.</summary>
    NoFace,

    /// <summary>A face, but not in the middle of the oval.</summary>
    OffCentre,

    /// <summary>A face in the oval, too small: move closer.</summary>
    TooFar,

    /// <summary>A face bigger than the oval: move the phone back.</summary>
    TooClose,

    /// <summary>The face fills the oval: the shutter works.</summary>
    Good,
}

/// <summary>A face the phone's detector found, in the camera picture's pixels as shown on screen.</summary>
public readonly record struct FaceBox(double CentreX, double CentreY, double Width);

/// <summary>The dashed oval on the selfie camera, in the camera picture's pixels.</summary>
public readonly record struct SelfieOval(double CentreX, double CentreY, double Width, double Height);

/// <summary>
/// The selfie camera's one rule, the same on Android (<c>SelfieCameraActivity</c>) and iOS (<c>IosSelfieCamera</c>):
/// the shutter works only while one face sits in the middle of the oval and roughly fills it. The oval is drawn from
/// <see cref="OvalIn"/>, so the picture and the rule never disagree.
/// </summary>
public static class SelfieFraming
{
    /// <summary>The oval's width as a share of the picture's width.</summary>
    public const double OvalWidthShare = 0.62;

    /// <summary>The oval's height over its width (a face is taller than wide).</summary>
    public const double OvalAspect = 1.3;

    /// <summary>The oval's centre, down the picture (a little above the middle, like a face in a portrait).</summary>
    public const double OvalCentreDown = 0.46;

    /// <summary>How far the face's centre may be from the oval's, as a share of the oval's half-size.</summary>
    public const double MaxOffCentre = 0.35;

    /// <summary>The narrowest face that counts, as a share of the oval's width.</summary>
    public const double MinFaceShare = 0.5;

    /// <summary>The widest face that counts, as a share of the oval's width.</summary>
    public const double MaxFaceShare = 1.1;

    public static SelfieOval OvalIn(double width, double height)
    {
        var ovalWidth = width * OvalWidthShare;
        return new SelfieOval(width / 2, height * OvalCentreDown, ovalWidth, ovalWidth * OvalAspect);
    }

    public static SelfieFrame Check(double width, double height, FaceBox? face)
    {
        if (face is not { } found || width <= 0 || height <= 0 || found.Width <= 0)
        {
            return SelfieFrame.NoFace;
        }

        var oval = OvalIn(width, height);
        var dx = (found.CentreX - oval.CentreX) / (oval.Width / 2);
        var dy = (found.CentreY - oval.CentreY) / (oval.Height / 2);
        if (dx * dx + dy * dy > MaxOffCentre * MaxOffCentre)
        {
            return SelfieFrame.OffCentre;
        }

        var share = found.Width / oval.Width;
        return share < MinFaceShare ? SelfieFrame.TooFar
            : share > MaxFaceShare ? SelfieFrame.TooClose
            : SelfieFrame.Good;
    }
}
