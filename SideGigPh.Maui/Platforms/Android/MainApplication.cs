using Android.App;
using Android.Runtime;

namespace SideGigPh.Maui
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // Java crashes and how the last runs ended (native crash, ANR, memory) go into the error reports.
        public override void OnCreate()
        {
            base.OnCreate();
            if (IPlatformApplication.Current?.Services.GetService(typeof(SideGigPh.Maui.Services.Diagnostics.ErrorLog))
                is SideGigPh.Maui.Services.Diagnostics.ErrorLog log)
            {
                SideGigPh.Maui.Platforms.Android.AndroidCrashCapture.Install(this, log);
            }
        }
    }
}
