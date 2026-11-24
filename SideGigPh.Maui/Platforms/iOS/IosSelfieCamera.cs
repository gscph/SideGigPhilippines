using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using ImageIO;
using SideGigPh.Maui.Services;
using UIKit;
using Vision;

namespace SideGigPh.Maui.Platforms.iOS;

/// <summary>
/// Tarbaho's own selfie camera on iPhone (<see cref="IosPhotoPicker.TakeSelfieAsync"/>), the twin of Android's
/// <c>SelfieCameraActivity</c>: the live front camera only (no lens switch), an oval where the face goes, a hint, one
/// shutter and Cancel. Apple's Vision face detection (on the phone, nothing sent) looks at the picture about five times a
/// second: the oval and the hint turn green and the shutter works only while one face fills the middle of the oval
/// (<see cref="SelfieFraming"/>); otherwise they are red and the hint says what to do. The shutter keeps the next frame
/// whose face is still right, upright and as previewed (mirrored); the picker makes it small.
/// </summary>
internal sealed class IosSelfieCamera : UIViewController
{
    private static readonly TimeSpan CheckEvery = TimeSpan.FromMilliseconds(200);

    private readonly string[] _hints;
    private readonly UIColor _good;
    private readonly UIColor _bad;
    private readonly string _cancelText;
    private readonly string _shutterText;
    private readonly TaskCompletionSource<UIImage?> _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly AVCaptureSession _session = new();
    private readonly DispatchQueue _queue = new("com.tarbaho.selfie");
    private readonly CAShapeLayer _oval = new();
    private readonly UILabel _hint = new();
    private readonly UIButton _shutter = new();
    private readonly UIButton _cancel = new(UIButtonType.System);
    private AVCaptureVideoPreviewLayer? _preview;
    private FrameReader? _reader;
    private DateTime _lastCheck = DateTime.MinValue;
    private volatile bool _capture;
    private SelfieFrame _frame = SelfieFrame.NoFace;
    private bool _finished;
    private Exception? _failure;

    public IosSelfieCamera(string[] hints, string goodColor, string badColor, string cancelText, string shutterText)
    {
        _hints = hints;
        _good = ColorOf(goodColor);
        _bad = ColorOf(badColor);
        _cancelText = cancelText;
        _shutterText = shutterText;
        // Full screen, so it can't be swiped away without Cancel (which would leave the selfie button waiting).
        ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
    }

    /// <summary>The selfie, or null when the person cancelled.</summary>
    public Task<UIImage?> Result => _done.Task;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.Black;
        try
        {
            StartSession();
        }
        catch (Exception ex)
        {
            _failure = ex; // answered once the screen is up (ViewDidAppear): UIKit can't dismiss it while it loads
            return;
        }

        BuildScreen();
    }

    private void StartSession()
    {
        var device = AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaTypes.Video.GetConstant()!, AVCaptureDevicePosition.Front)
            ?? throw new InvalidOperationException("This phone has no front camera.");
        var input = AVCaptureDeviceInput.FromDevice(device, out var error)
            ?? throw new InvalidOperationException($"The front camera could not be opened: {error?.LocalizedDescription}");

        _session.BeginConfiguration();
        _session.SessionPreset = AVCaptureSession.Preset1280x720;
        _session.AddInput(input);
        var output = new AVCaptureVideoDataOutput
        {
            AlwaysDiscardsLateVideoFrames = true,
            WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }.Dictionary,
        };
        _reader = new FrameReader(this);
        output.SetSampleBufferDelegate(_reader, _queue);
        _session.AddOutput(output);
        // Frames come upright and mirrored, like the preview, so the face check and the photo match what is on screen.
        if (output.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!) is { } connection)
        {
            if (OperatingSystem.IsIOSVersionAtLeast(17))
            {
                if (connection.IsVideoRotationAngleSupported(90))
                {
                    connection.VideoRotationAngle = 90;
                }
            }
            else if (connection.SupportsVideoOrientation)
            {
                connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
            }

            if (connection.SupportsVideoMirroring)
            {
                connection.AutomaticallyAdjustsVideoMirroring = false;
                connection.VideoMirrored = true;
            }
        }

        _session.CommitConfiguration();
    }

    private void BuildScreen()
    {
        _preview = new AVCaptureVideoPreviewLayer(_session) { VideoGravity = AVLayerVideoGravity.ResizeAspect };
        View!.Layer.AddSublayer(_preview);
        _oval.FillColor = UIColor.Clear.CGColor;
        _oval.LineWidth = 5;
        View.Layer.AddSublayer(_oval);

        _hint.TextAlignment = UITextAlignment.Center;
        _hint.TextColor = UIColor.White;
        _hint.Font = UIFont.BoldSystemFontOfSize(20)!;
        _hint.Lines = 2;
        _hint.ClipsToBounds = true;
        _hint.Layer.CornerRadius = 8;
        View.AddSubview(_hint);

        _shutter.BackgroundColor = UIColor.White;
        _shutter.AccessibilityLabel = _shutterText;
        _shutter.Layer.BorderWidth = 5;
        _shutter.Layer.BorderColor = UIColor.LightGray.CGColor;
        _shutter.TouchUpInside += (_, _) => TakePhoto();
        View.AddSubview(_shutter);

        _cancel.SetTitle(_cancelText, UIControlState.Normal);
        _cancel.SetTitleColor(UIColor.White, UIControlState.Normal);
        _cancel.TitleLabel!.Font = UIFont.SystemFontOfSize(18)!;
        _cancel.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
        _cancel.TouchUpInside += (_, _) => Finish(null);
        View.AddSubview(_cancel);

        ShowFrame(SelfieFrame.NoFace);
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        var bounds = View!.Bounds;
        var safe = View.SafeAreaInsets;
        const double shutterSize = 76;

        // The 720 × 1280 picture, as wide as the screen and in the middle of the space between the hint and the shutter.
        double width = bounds.Width;
        var height = width * 16 / 9;
        var top = Math.Max(safe.Top, (bounds.Height - height) / 2);
        var preview = new CGRect(0, top, width, height);
        if (_preview is not null)
        {
            _preview.Frame = preview;
        }

        var oval = SelfieFraming.OvalIn(width, height);
        _oval.Path = UIBezierPath.FromOval(new CGRect(oval.CentreX - oval.Width / 2, top + oval.CentreY - oval.Height / 2, oval.Width, oval.Height)).CGPath;

        _hint.Frame = new CGRect(16, safe.Top + 16, bounds.Width - 32, 56);
        var shutterTop = bounds.Height - safe.Bottom - shutterSize - 24;
        _shutter.Frame = new CGRect((bounds.Width - shutterSize) / 2, shutterTop, shutterSize, shutterSize);
        _shutter.Layer.CornerRadius = (System.Runtime.InteropServices.NFloat)(shutterSize / 2);
        _cancel.Frame = new CGRect(16, shutterTop, 140, shutterSize);
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
        if (_failure is { } failure)
        {
            _finished = true;
            DismissViewController(true, () => _done.TrySetException(failure));
            return;
        }

        _queue.DispatchAsync(() =>
        {
            if (!_session.Running)
            {
                _session.StartRunning();
            }
        });
    }

    public override void ViewWillDisappear(bool animated)
    {
        base.ViewWillDisappear(animated);
        _queue.DispatchAsync(() =>
        {
            if (_session.Running)
            {
                _session.StopRunning();
            }
        });
    }

    private void TakePhoto()
    {
        if (_finished || _frame != SelfieFrame.Good)
        {
            return;
        }

        _shutter.Enabled = false;
        _capture = true;
    }

    // On the camera queue: checks the face a few times a second, and keeps the frame once the shutter was pressed.
    private void OnFrame(CMSampleBuffer sample)
    {
        var now = DateTime.UtcNow;
        if (!_capture && now - _lastCheck < CheckEvery)
        {
            return;
        }

        _lastCheck = now;
        using var pixels = sample.GetImageBuffer() as CVPixelBuffer;
        if (pixels is null)
        {
            return;
        }

        double width = pixels.Width;
        double height = pixels.Height;
        var frame = SelfieFraming.Check(width, height, FindFace(pixels, width, height));

        UIImage? photo = null;
        if (_capture)
        {
            _capture = false;
            if (frame == SelfieFrame.Good)
            {
                photo = ImageOf(pixels);
            }
        }

        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            if (photo is not null)
            {
                Finish(photo);
                return;
            }

            ShowFrame(frame);
        });
    }

    // The biggest face Vision finds, in the frame's pixels (Vision's box is 0–1 with its origin at the bottom left).
    private static FaceBox? FindFace(CVPixelBuffer pixels, double width, double height)
    {
        using var request = new VNDetectFaceRectanglesRequest(null);
        using var handler = new VNImageRequestHandler(pixels, CGImagePropertyOrientation.Up, new VNImageOptions());
        if (!handler.Perform([request], out _))
        {
            return null;
        }

        var face = request.GetResults<VNFaceObservation>()?.OrderByDescending(found => found.BoundingBox.Width).FirstOrDefault();
        if (face is null)
        {
            return null;
        }

        var box = face.BoundingBox;
        return new FaceBox((box.X + box.Width / 2) * width, (1 - box.Y - box.Height / 2) * height, box.Width * width);
    }

    private static UIImage ImageOf(CVPixelBuffer pixels)
    {
        using var image = CIImage.FromImageBuffer(pixels);
        using var context = CIContext.FromOptions(null);
        using var picture = context.CreateCGImage(image, image.Extent)
            ?? throw new InvalidOperationException("The camera frame could not be read.");
        return new UIImage(picture);
    }

    // Green and the shutter on while the face is right; red, the shutter dimmed and off, and what to do otherwise.
    private void ShowFrame(SelfieFrame frame)
    {
        if (_finished)
        {
            return;
        }

        _frame = frame;
        var good = frame == SelfieFrame.Good;
        var color = good ? _good : _bad;
        _hint.Text = (int)frame < _hints.Length ? _hints[(int)frame] : string.Empty;
        _hint.BackgroundColor = color;
        _oval.StrokeColor = color.CGColor;
        _oval.LineDashPattern = good ? null : [new NSNumber(10), new NSNumber(8)];
        _shutter.Enabled = good;
        _shutter.Alpha = good ? 1 : (System.Runtime.InteropServices.NFloat)0.4;
    }

    private void Finish(UIImage? photo)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        DismissViewController(true, () => _done.TrySetResult(photo));
    }

    private static UIColor ColorOf(string hex)
    {
        var value = Convert.ToInt32(hex.TrimStart('#'), 16);
        return UIColor.FromRGB((value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF);
    }

    private sealed class FrameReader(IosSelfieCamera camera) : AVCaptureVideoDataOutputSampleBufferDelegate
    {
        public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            try
            {
                camera.OnFrame(sampleBuffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Selfie face check failed: {ex.Message}");
            }
            finally
            {
                // Frames come from a small pool: one kept alive stops the camera.
                sampleBuffer.Dispose();
            }
        }
    }
}
