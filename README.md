# 🎨 SmashyKeys

A toddler-friendly fullscreen app where every keypress, touch, and click creates colorful chaos! Available for both Windows and Android. Perfect for little ones who love mashing buttons!

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## ⬇️ Download

| Platform | Download |
|----------|----------|
| 🖥️ **Windows** | [📥 SmashyKeys-Windows.exe](https://github.com/jfbauer/SmashyKeys/releases/latest/download/SmashyKeys-Windows.exe) |
| 📱 **Android** | [📥 SmashyKeys-Android.apk](https://github.com/jfbauer/SmashyKeys/releases/latest/download/SmashyKeys-Android.apk) |

**Windows:** Just download and double-click to run! No installation needed.

**Android:** Download the APK and sideload it (enable "Install from unknown sources" in your device settings). See [Android Setup](#-android-setup) below.

## ✨ Features

### Both Platforms
- **Fullscreen colorful fun** - Every input creates random shapes, colors, and animations
- **Floating physics objects** - Shapes bounce around with realistic collision physics
- **No gravity, slow friction** - Objects glide smoothly across the screen
- **Smart object limits** - Automatically manages objects (soft cap: 100, hard cap: 200)
- **Vibrant colors & shapes** - Circles, squares, stars, hearts, and triangles
- **Fun sounds** - Audio and haptic feedback on interactions

### Windows-Specific
- **Keyboard input** - Each keypress spawns 5 colorful objects
- **Mouse effects** - Click to create bursts, scroll to push objects, right-click for tornado vortex
- **Blocks escape shortcuts** - Windows key, Alt+Tab, Alt+F4, Escape are all blocked
- **Visible cursor** - Glowing hollow circle follows mouse
- **Parent exit** - Hold `Ctrl+Shift+Q` for 2 seconds

### Android-Specific
- **Multi-touch support** - Tap anywhere to create effects
- **Touch trails** - Drag your finger to leave colorful trails
- **Immersive fullscreen** - Hides navigation and status bars
- **Back button blocked** - Toddler can't accidentally exit
- **Screen stays on** - Display won't dim during play
- **Secret parent exit** - Tap 5 times rapidly in top-left corner

## 🎮 How It Works

### Windows
1. **Run the app** - It goes fullscreen immediately
2. **Let your toddler mash away!** - Every key and click creates visual effects
3. **To exit (parents only):**
   - Hold `Ctrl+Shift+Q` for 2 seconds, OR
   - Press `Ctrl+Alt+Delete` and choose Task Manager → End Task

### Android
1. **Enable screen pinning** (recommended):
   - Go to Settings → Security → Screen pinning → Enable
   - Open SmashyKeys, then tap the app switcher button and select "Pin"
   - This prevents your toddler from switching apps!
2. **Let your toddler tap away!** - Every touch creates colorful effects
3. **To exit (parents only):**
   - Tap 5 times rapidly in the top-left corner (within 2 seconds), OR
   - Unpin the app (hold Back + App Switcher buttons)

## 📱 Android Setup

Since SmashyKeys isn't on the Play Store, you'll need to sideload the APK:

1. **Download the APK** from the releases page
2. **Enable installation from unknown sources:**
   - Android 8+: Settings → Apps → Special access → Install unknown apps → Allow your browser
   - Older: Settings → Security → Unknown sources → Enable
3. **Open the downloaded APK** and tap Install
4. **Launch SmashyKeys!**

### Recommended: Use Screen Pinning

For the best toddler-proof experience, use Android's built-in screen pinning:

1. Settings → Security → Screen pinning → Enable
2. Open SmashyKeys
3. Tap the app switcher button (square button)
4. Tap the pin icon on SmashyKeys
5. Now the app is locked! Your toddler can't switch away.
6. To unpin: Hold Back + App Switcher buttons together

## 🔒 Blocked Shortcuts (Windows)

The app blocks these common escape methods:
- Windows key (both left and right)
- Alt+Tab
- Alt+F4
- Alt+Escape
- Ctrl+Escape
- Escape key

**Note:** `Ctrl+Alt+Delete` cannot be blocked (it's a Windows security feature). This serves as the ultimate parent escape hatch!

## 🛠️ Building from Source

### Requirements
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- For Android: MAUI workload (`dotnet workload install maui-android`)

### Windows Desktop
```bash
cd src/SmashyKeys.Desktop
dotnet build
dotnet run

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Android Mobile
```bash
cd src/SmashyKeys.Mobile
dotnet build -f net8.0-android

# Publish APK
dotnet publish -c Release -f net8.0-android
```

## 📁 Project Structure

```
SmashyKeys/
├── src/
│   ├── SmashyKeys.Desktop/     # Windows WPF app
│   │   ├── App.xaml
│   │   ├── MainWindow.xaml     # Main fullscreen window
│   │   ├── KeyboardHook.cs     # Low-level keyboard hook
│   │   ├── VisualEffect.cs     # Shape generators
│   │   ├── SoundManager.cs     # Audio generation
│   │   └── Logger.cs           # Debug logging
│   │
│   └── SmashyKeys.Mobile/      # Android MAUI app
│       ├── MainPage.xaml       # Touch-based UI
│       ├── GameDrawable.cs     # Physics & rendering
│       ├── SoundManager.cs     # Haptics & tones
│       └── Platforms/Android/  # Android-specific code
│
└── .github/workflows/
    ├── build-desktop.yml       # Windows build & release
    └── build-mobile.yml        # Android build & release
```

## 🎨 Visual Effects

- **Circles, squares, stars, hearts, triangles** - Random shapes on every input
- **Expanding rings** - Radiate from touch/click points
- **Particle bursts** - Colorful particles explode outward
- **Touch/mouse trails** - Colored dots follow your movement
- **Tornado vortex** (Windows) - Right-click to create a spinning force field
- **Physics collisions** - Objects bounce off each other realistically

## ⚠️ Notes

- **Windows:** Runs topmost (always on top), works best on primary monitor
- **Android:** Requires Android 7.0 (API 24) or higher
- **Both:** The app is designed to be toddler-proof - exits are intentionally hidden!

## 📄 License

MIT License - Feel free to use, modify, and share!

---

Made with ❤️ for toddlers who love buttons
