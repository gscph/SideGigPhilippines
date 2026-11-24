using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Color = Android.Graphics.Color;
using Size = Android.Util.Size;
using View = Android.Views.View;
using Paint = Android.Graphics.Paint;
using RectF = Android.Graphics.RectF;

namespace SideGigPh.Maui.Platforms.Android;

/// <summary>
/// Tarbaho's own selfie camera (<see cref="AndroidPhotoPicker.TakeSelfieAsync"/>): the live front camera only, with no
/// way to switch to the back lens, a dashed oval where the face goes and a short hint over it, one shutter button and
/// Cancel. The phone's camera app could not do this (the person could flip it to the back lens and photograph anything,
/// and no app can draw over it). The app never leaves the screen while the person poses, so a one-time camera
/// permission is not taken back mid-pose.
/// <para>
/// The shutter keeps the frame the person sees (upright, as previewed), at most <see cref="MaxSide"/> pixels, JPEG
/// quality 82 with no metadata, written to <see cref="ExtraOutput"/>; the result is <see cref="Result.Ok"/> with the
/// file there, else <see cref="Result.Canceled"/>. Portrait only.
/// </para>
/// </summary>
[Activity(Theme = "@android:style/Theme.DeviceDefault.NoActionBar.Fullscreen", ScreenOrientation = ScreenOrientation.Portrait, Exported = false)]
public sealed class SelfieCameraActivity : Activity, TextureView.ISurfaceTextureListener
{
    /// <summary>The path the selfie JPEG is written to.</summary>
    public const string ExtraOutput = "selfie.output";

    /// <summary>The hint over the camera, already in the person's language.</summary>
    public const string ExtraHint = "selfie.hint";

    /// <summary>The Cancel button's text.</summary>
    public const string ExtraCancel = "selfie.cancel";

    /// <summary>The shutter button's spoken label.</summary>
    public const string ExtraShutter = "selfie.shutter";

    private const int MaxSide = 1280;
    private const int JpegQuality = 82;
    private const int MaxPreviewSide = 1920;

    private TextureView _preview = null!;
    private View _shutter = null!;
    private Size? _previewSize;
    private string? _cameraId;
    private CameraDevice? _camera;
    private CameraCaptureSession? _session;
    private Handler _handler = null!;
    private bool _taking;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _handler = new Handler(Looper.MainLooper!);
        SetResult(Result.Canceled);

        if (CheckSelfPermission(global::Android.Manifest.Permission.Camera) != Permission.Granted || FrontCamera() is not { } front)
        {
            Finish();
            return;
        }

        (_cameraId, _previewSize) = front;
        SetContentView(BuildScreen());
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (_preview is not null && _preview.IsAvailable)
        {
            OpenCamera();
        }
    }

    protected override void OnPause()
    {
        CloseCamera();
        base.OnPause();
    }

    // The front camera and the preview size used for it (4:3 when the camera offers one); null without a front camera.
    private (string Id, Size Preview)? FrontCamera()
    {
        var manager = (CameraManager)GetSystemService(CameraService)!;
        foreach (var id in manager.GetCameraIdList())
        {
            var characteristics = manager.GetCameraCharacteristics(id);
            if (characteristics.Get(CameraCharacteristics.LensFacing) is not Java.Lang.Integer facing
                || facing.IntValue() != (int)LensFacing.Front
                || characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap) is not StreamConfigurationMap map
                || map.GetOutputSizes(Java.Lang.Class.FromType(typeof(SurfaceTexture))) is not { Length: > 0 } sizes)
            {
                continue;
            }

            var fitting = sizes.Where(s => Math.Max(s.Width, s.Height) <= MaxPreviewSide).ToList();
            var choice = fitting.Where(s => s.Width * 3 == s.Height * 4).OrderByDescending(s => s.Width * s.Height).FirstOrDefault()
                ?? fitting.OrderByDescending(s => s.Width * s.Height).FirstOrDefault()
                ?? sizes.OrderBy(s => s.Width * s.Height).First();
            return (id, choice);
        }

        return null;
    }

    private FrameLayout BuildScreen()
    {
        var size = _previewSize!;
        var screenWidth = Resources!.DisplayMetrics!.WidthPixels;
        // The camera's sizes are landscape; on a portrait screen the frame is taller than wide.
        var previewHeight = (int)(screenWidth * (Math.Max(size.Width, size.Height) / (double)Math.Min(size.Width, size.Height)));

        var root = new FrameLayout(this);
        root.SetBackgroundColor(Color.Black);
        // Android 15 draws every screen edge to edge: kept clear of the status and navigation bars, so the shutter
        // and Cancel are never under the system buttons.
        AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(root, new KeepClearOfBars());

        _preview = new TextureView(this) { SurfaceTextureListener = this };
        root.AddView(_preview, new FrameLayout.LayoutParams(screenWidth, previewHeight, GravityFlags.Center));
        root.AddView(new FaceOvalView(this), new FrameLayout.LayoutParams(screenWidth, previewHeight, GravityFlags.Center));

        var hint = new TextView(this) { Text = Intent?.GetStringExtra(ExtraHint) ?? string.Empty, Gravity = GravityFlags.Center };
        hint.SetTextColor(Color.White);
        hint.SetTextSize(ComplexUnitType.Sp, 20);
        hint.SetShadowLayer(6, 0, 0, Color.Black);
        root.AddView(hint, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, GravityFlags.Top)
        {
            TopMargin = Dp(48),
        });

        _shutter = new View(this) { ContentDescription = Intent?.GetStringExtra(ExtraShutter), Clickable = true, Focusable = true };
        var ring = new GradientDrawable();
        ring.SetShape(ShapeType.Oval);
        ring.SetColor(Color.White);
        ring.SetStroke(Dp(5), Color.LightGray);
        _shutter.Background = ring;
        _shutter.Click += (_, _) => TakePhoto();
        root.AddView(_shutter, new FrameLayout.LayoutParams(Dp(76), Dp(76), GravityFlags.Bottom | GravityFlags.CenterHorizontal)
        {
            BottomMargin = Dp(40),
        });

        var cancel = new TextView(this) { Text = Intent?.GetStringExtra(ExtraCancel) ?? string.Empty, Clickable = true, Focusable = true };
        cancel.SetTextColor(Color.White);
        cancel.SetTextSize(ComplexUnitType.Sp, 18);
        cancel.SetPadding(Dp(20), Dp(16), Dp(20), Dp(16));
        cancel.Click += (_, _) => Finish();
        root.AddView(cancel, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, GravityFlags.Bottom | GravityFlags.Start)
        {
            BottomMargin = Dp(52),
            LeftMargin = Dp(8),
        });

        return root;
    }

    private int Dp(int value) => (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Resources!.DisplayMetrics);

    private void OpenCamera()
    {
        if (_camera is not null || _cameraId is null)
        {
            return;
        }

        try
        {
            var manager = (CameraManager)GetSystemService(CameraService)!;
            manager.OpenCamera(_cameraId, new CameraState(this), _handler);
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void CloseCamera()
    {
        _session?.Close();
        _session = null;
        _camera?.Close();
        _camera = null;
    }

    private void StartPreview(CameraDevice camera)
    {
        _camera = camera;
        var texture = _preview.SurfaceTexture;
        if (texture is null || _previewSize is null)
        {
            return;
        }

        texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
        var surface = new Surface(texture);
        var state = new SessionState(this, surface);
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            camera.CreateCaptureSession(new SessionConfiguration((int)SessionType.Regular, [new OutputConfiguration(surface)], MainExecutor!, state));
        }
        else
        {
            camera.CreateCaptureSession([surface], state, _handler);
        }
    }

    private void ShowPreview(CameraCaptureSession session, Surface surface)
    {
        if (_camera is null)
        {
            session.Close();
            return;
        }

        _session = session;
        var request = _camera.CreateCaptureRequest(CameraTemplate.Preview);
        request.AddTarget(surface);
        session.SetRepeatingRequest(request.Build(), null, _handler);
    }

    // Keeps the frame on screen: what the person saw is what is sent.
    private void TakePhoto()
    {
        if (_taking || _session is null || _preview.Bitmap is not { } frame)
        {
            return;
        }

        _taking = true;
        _shutter.Enabled = false;
        var path = Intent?.GetStringExtra(ExtraOutput);
        Task.Run(() =>
        {
            try
            {
                Save(frame, path ?? throw new InvalidOperationException("No path for the selfie."));
                RunOnUiThread(() =>
                {
                    SetResult(Result.Ok);
                    Finish();
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => Fail(ex));
            }
        });
    }

    private static void Save(Bitmap frame, string path)
    {
        using (frame)
        {
            var scale = Math.Min(1f, MaxSide / (float)Math.Max(frame.Width, frame.Height));
            using var small = scale < 1f
                ? Bitmap.CreateScaledBitmap(frame, Math.Max(1, (int)(frame.Width * scale)), Math.Max(1, (int)(frame.Height * scale)), true)!
                : Bitmap.CreateBitmap(frame);
            using var file = System.IO.File.Create(path);
            small.Compress(Bitmap.CompressFormat.Jpeg!, JpegQuality, file);
        }
    }

    // Logged by the picker (the result is Canceled with nothing written, which it reports as a failed camera).
    private void Fail(Exception ex)
    {
        global::Android.Util.Log.Error("SideGigPh", $"Selfie camera failed: {ex}");
        CloseCamera();
        SetResult(Result.FirstUser);
        Finish();
    }

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height) => OpenCamera();

    bool TextureView.ISurfaceTextureListener.OnSurfaceTextureDestroyed(SurfaceTexture surface) => true;

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
    {
    }

    void TextureView.ISurfaceTextureListener.OnSurfaceTextureUpdated(SurfaceTexture surface)
    {
    }

    private sealed class CameraState(SelfieCameraActivity screen) : CameraDevice.StateCallback
    {
        public override void OnOpened(CameraDevice camera)
        {
            try
            {
                screen.StartPreview(camera);
            }
            catch (Exception ex)
            {
                screen.Fail(ex);
            }
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            camera.Close();
            screen._camera = null;
        }

        public override void OnError(CameraDevice camera, CameraError error)
        {
            camera.Close();
            screen._camera = null;
            screen.Fail(new InvalidOperationException($"Camera error {error}."));
        }
    }

    private sealed class SessionState(SelfieCameraActivity screen, Surface surface) : CameraCaptureSession.StateCallback
    {
        public override void OnConfigured(CameraCaptureSession session)
        {
            try
            {
                screen.ShowPreview(session, surface);
            }
            catch (Exception ex)
            {
                screen.Fail(ex);
            }
        }

        public override void OnConfigureFailed(CameraCaptureSession session) =>
            screen.Fail(new InvalidOperationException("The camera preview could not start."));
    }

    private sealed class KeepClearOfBars : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
    {
        public AndroidX.Core.View.WindowInsetsCompat OnApplyWindowInsets(View view, AndroidX.Core.View.WindowInsetsCompat insets)
        {
            var bars = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars() | AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout());
            view.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
            return AndroidX.Core.View.WindowInsetsCompat.Consumed!;
        }
    }

    // The dashed white oval where the face goes, the same shape as the app's guide picture (SelfieGuide) and the
    // iPhone's camera overlay. Draws only; taps go through.
    private sealed class FaceOvalView : View
    {
        private readonly Paint _paint;

        public FaceOvalView(Context context) : base(context)
        {
            var density = context.Resources!.DisplayMetrics!.Density;
            _paint = new Paint(PaintFlags.AntiAlias) { Color = Color.White, StrokeWidth = 3 * density };
            _paint.SetStyle(Paint.Style.Stroke);
            _paint.SetPathEffect(new DashPathEffect([12 * density, 9 * density], 0));
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            var width = Width * 0.62f;
            var height = width * 1.3f;
            var left = (Width - width) / 2;
            var top = Height * 0.46f - height / 2;
            canvas.DrawOval(new RectF(left, top, left + width, top + height), _paint);
        }
    }
}
