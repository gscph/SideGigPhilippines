using Foundation;
using MobileAppTheme.Firebase;
using UIKit;

namespace SideGigPh.Maui
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // Phone sign-in: Firebase proves the app with a silent push, so it needs Apple's push token and that push
        // (FirebaseAuthSetup). Firebase Cloud Messaging still gets both through its own hooks.
        [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
        public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) =>
            FirebaseAuthSetup.HandleApnsToken(deviceToken);

        [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
        public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            FirebaseAuthSetup.HandleRemoteNotification(userInfo);
            completionHandler(UIBackgroundFetchResult.NoData);
        }
    }
}
