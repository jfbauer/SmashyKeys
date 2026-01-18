# 🎨 SmashyKeys

A toddler-friendly fullscreen Windows app where every keypress and mouse click creates colorful chaos! Perfect for little ones who love mashing keyboards.

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## ⬇️ Download

**[📥 Download SmashyKeys.exe](https://github.com/jfbauer/SmashyKeys/releases/latest/download/SmashyKeys.exe)**

Just download and double-click to run! No installation needed.

## ✨ Features

- **Fullscreen colorful fun** - Every keypress creates random shapes, colors, and animations
- **Mouse effects** - Clicking creates expanding rings and particle bursts
- **Floating objects** - Shapes bounce around the screen with physics
- **Blocks escape shortcuts** - Windows key, Alt+Tab, Alt+F4, Escape are all blocked
- **Safe exit for parents** - Hold `Ctrl+Shift+Q` for 2 seconds, or press `Ctrl+Alt+Delete`
- **No cursor** - Hides the mouse cursor for uninterrupted play
- **System beeps** - Occasional fun sounds on keypress

## 🎮 How It Works

1. **Run the app** - It goes fullscreen immediately
2. **Let your toddler mash away!** - Every key and click creates visual effects
3. **To exit (parents only):**
   - Hold `Ctrl+Shift+Q` for 2 seconds, OR
   - Press `Ctrl+Alt+Delete` and choose Task Manager → End Task

## 🔒 Blocked Shortcuts

The app blocks these common escape methods:
- Windows key (both left and right)
- Alt+Tab
- Alt+F4
- Alt+Escape
- Ctrl+Escape
- Escape key

**Note:** `Ctrl+Alt+Delete` cannot be blocked by any Windows application (it's a security feature). This serves as the ultimate parent escape hatch!

## 🛠️ Building from Source

Requirements:
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
# Clone the repository
git clone https://github.com/jfbauer/SmashyKeys.git
cd SmashyKeys

# Build
dotnet build

# Run
dotnet run

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 📁 Project Structure

```
SmashyKeys/
├── App.xaml              # Application entry point
├── MainWindow.xaml       # Main fullscreen window UI
├── MainWindow.xaml.cs    # Window logic, animations, physics
├── KeyboardHook.cs       # Low-level keyboard hook for blocking shortcuts
├── VisualEffect.cs       # Shape and animation generators
└── SmashyKeys.csproj     # Project file
```

## 🎨 Visual Effects

- **Circles, squares, stars, hearts** - Random shapes appear on keypress
- **Letters** - When letter keys are pressed, that letter appears
- **Expanding rings** - On mouse click
- **Particle bursts** - Colorful particles shoot out from clicks
- **Mouse trail** - Subtle dots follow the cursor
- **Floating physics objects** - Shapes bounce with gravity

## ⚠️ Notes

- The app runs **topmost** (always on top of other windows)
- The cursor is hidden while the app is running
- Works best on the primary monitor
- Tested on Windows 10 and 11

## 📄 License

MIT License - Feel free to use, modify, and share!

---

Made with ❤️ for toddlers who love buttons
