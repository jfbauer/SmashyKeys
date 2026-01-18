#if ANDROID
using Android.Media;
#endif

namespace SmashyKeys.Mobile;

public class SoundManager
{
    private readonly Random _random = new();
    
    // Musical notes (frequencies in Hz)
    private readonly int[] _toneTypes = { 0, 1, 2, 3, 4, 5, 6, 7 }; // ToneGenerator types
    
    public void PlayRandomSound()
    {
#if ANDROID
        try
        {
            // Use haptic feedback for tactile response
            var vibrator = (Android.OS.Vibrator?)Android.App.Application.Context.GetSystemService(Android.Content.Context.VibratorService);
            if (vibrator != null && vibrator.HasVibrator)
            {
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    vibrator.Vibrate(Android.OS.VibrationEffect.CreateOneShot(30, Android.OS.VibrationEffect.DefaultAmplitude));
                }
                else
                {
                    #pragma warning disable CA1422
                    vibrator.Vibrate(30);
                    #pragma warning restore CA1422
                }
            }
            
            // Play a short tone
            Task.Run(() =>
            {
                try
                {
                    using var toneGen = new ToneGenerator(Stream.Music, 50); // 50% volume
                    var toneType = (ToneType)_random.Next(0, 8);
                    toneGen.StartTone(toneType, 100); // 100ms duration
                }
                catch
                {
                    // Ignore audio errors
                }
            });
        }
        catch
        {
            // Ignore errors - sound is not critical
        }
#endif
    }
}
