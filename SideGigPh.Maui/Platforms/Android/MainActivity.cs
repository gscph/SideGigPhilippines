using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using MobileAppTheme.Firebase;

namespace SideGigPh.Maui
{
    // SingleTop: tapping a notification while the app runs hands the tap to this activity (OnNewIntent) instead of opening a second copy.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // The app is white edge to edge: dark status-bar icons so the clock and battery stay visible.
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (Window is { } window && AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView) is { } bars)
            {
                bars.AppearanceLightStatusBars = true;
            }
        }

        // Google sign-in launches the account-picker as a separate activity and awaits its
        // result. Firebase Google sign-in only completes that awaited Task when the result
        // is forwarded here — without this override SignInWithGoogleAsync() hangs forever (no
        // exception, stuck on "connecting"). Required per the package's Android setup notes.
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            FirebaseAuthSetup.HandleActivityResult(requestCode, resultCode, data);
        }
    }
}
