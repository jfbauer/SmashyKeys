using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace SmashyKeys.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Landscape,
    LaunchMode = LaunchMode.SingleTop)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Make fullscreen and immersive
        if (Window != null)
        {
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            // Hide system UI - use legacy method which works across all versions
            HideSystemUI();
        }
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Re-hide system UI when resuming
        HideSystemUI();
    }

    private void HideSystemUI()
    {
        if (Window == null) return;

#pragma warning disable CS0618, CA1422 // Using obsolete API for compatibility
        Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
            SystemUiFlags.Fullscreen |
            SystemUiFlags.HideNavigation |
            SystemUiFlags.ImmersiveSticky |
            SystemUiFlags.LayoutFullscreen |
            SystemUiFlags.LayoutHideNavigation |
            SystemUiFlags.LayoutStable);
#pragma warning restore CS0618, CA1422
    }

    // Override back button behavior
    public override void OnBackPressed()
    {
        // Don't call base - we handle this in the Page
        // The MainPage.OnBackButtonPressed will be called instead
    }
}
