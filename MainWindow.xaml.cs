using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SmashyKeys;

public partial class MainWindow : Window
{
    private KeyboardHook? _keyboardHook;
    private readonly DispatcherTimer _floatingTimer;
    private readonly DispatcherTimer _backgroundTimer;
    private readonly DispatcherTimer _cleanupTimer;
    private readonly List<FloatingObject> _floatingObjects = new();
    private readonly Random _random = new();
    private bool _welcomeHidden;
    private int _backgroundColorIndex;

    private readonly Color[] _backgroundColors =
    {
        Color.FromRgb(26, 26, 46),    // Dark blue
        Color.FromRgb(30, 20, 50),    // Dark purple
        Color.FromRgb(20, 40, 40),    // Dark teal
        Color.FromRgb(40, 20, 30),    // Dark red
        Color.FromRgb(20, 30, 45),    // Navy
    };

    public MainWindow()
    {
        InitializeComponent();

        // Timer for animating floating objects
        _floatingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
        };
        _floatingTimer.Tick += FloatingTimer_Tick;

        // Timer for background color transitions
        _backgroundTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _backgroundTimer.Tick += BackgroundTimer_Tick;

        // Timer for cleaning up excess objects
        _cleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _cleanupTimer.Tick += CleanupTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Install keyboard hook
        _keyboardHook = new KeyboardHook();
        _keyboardHook.KeyPressed += OnKeyPressed;
        _keyboardHook.SecretExitTriggered += OnSecretExit;
        _keyboardHook.Install();

        // Start timers
        _floatingTimer.Start();
        _backgroundTimer.Start();
        _cleanupTimer.Start();

        // Create some initial floating objects
        for (int i = 0; i < 5; i++)
        {
            CreateFloatingObject();
        }

        // Focus window to capture all input
        Activate();
        Focus();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _keyboardHook?.Dispose();
        _floatingTimer.Stop();
        _backgroundTimer.Stop();
        _cleanupTimer.Stop();
    }

    private void OnKeyPressed(int keyCode)
    {
        Dispatcher.Invoke(() =>
        {
            HideWelcome();

            // Create visual effect for keypress
            VisualEffects.CreateKeyPressEffect(EffectsCanvas, keyCode);

            // Occasionally add a new floating object
            if (_random.Next(3) == 0 && _floatingObjects.Count < 20)
            {
                CreateFloatingObject();
            }

            // Make existing floating objects bounce
            foreach (var obj in _floatingObjects)
            {
                obj.VelocityY -= _random.Next(2, 5);
            }

            // Play a beep (optional - uses system beep)
            if (_random.Next(2) == 0)
            {
                Console.Beep(_random.Next(200, 800), 50);
            }
        });
    }

    private void OnSecretExit()
    {
        Dispatcher.Invoke(() =>
        {
            _keyboardHook?.Uninstall();
            Application.Current.Shutdown();
        });
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        HideWelcome();

        var position = e.GetPosition(EffectsCanvas);
        VisualEffects.CreateClickEffect(EffectsCanvas, position);

        // Add floating object at click position
        if (_floatingObjects.Count < 20)
        {
            CreateFloatingObject(position);
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        // Create subtle trail effect on mouse move (throttled)
        if (_random.Next(5) == 0)
        {
            var position = e.GetPosition(EffectsCanvas);
            var color = VisualEffects.GetRandomColor();
            var dot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(color),
                Opacity = 0.5
            };

            Canvas.SetLeft(dot, position.X - 5);
            Canvas.SetTop(dot, position.Y - 5);
            EffectsCanvas.Children.Add(dot);

            // Fade out
            var fadeAnim = new DoubleAnimation(0.5, 0, TimeSpan.FromMilliseconds(500));
            fadeAnim.Completed += (s, ev) => EffectsCanvas.Children.Remove(dot);
            dot.BeginAnimation(OpacityProperty, fadeAnim);
        }
    }

    private void HideWelcome()
    {
        if (!_welcomeHidden)
        {
            _welcomeHidden = true;
            var fadeOut = new DoubleAnimation(0.8, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => WelcomePanel.Visibility = Visibility.Collapsed;
            WelcomePanel.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    private void CreateFloatingObject(Point? position = null)
    {
        var size = _random.Next(40, 100);
        var color = VisualEffects.GetRandomColor();

        FrameworkElement shape;
        var shapeType = _random.Next(4);

        switch (shapeType)
        {
            case 0:
                shape = new Ellipse { Width = size, Height = size, Fill = new SolidColorBrush(color) };
                break;
            case 1:
                shape = new Rectangle { Width = size, Height = size, Fill = new SolidColorBrush(color), RadiusX = 8, RadiusY = 8 };
                break;
            case 2:
                shape = CreateStar(color, size);
                break;
            default:
                shape = new TextBlock
                {
                    Text = ((char)_random.Next(65, 91)).ToString(),
                    FontSize = size * 0.8,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(color)
                };
                break;
        }

        var x = position?.X ?? _random.NextDouble() * ActualWidth;
        var y = position?.Y ?? _random.NextDouble() * ActualHeight;

        Canvas.SetLeft(shape, x);
        Canvas.SetTop(shape, y);
        FloatingCanvas.Children.Add(shape);

        var floatingObj = new FloatingObject
        {
            Element = shape,
            X = x,
            Y = y,
            VelocityX = (_random.NextDouble() - 0.5) * 4,
            VelocityY = (_random.NextDouble() - 0.5) * 4,
            Size = size,
            RotationSpeed = (_random.NextDouble() - 0.5) * 3,
            Rotation = 0
        };

        // Add rotation transform
        shape.RenderTransform = new RotateTransform(0, size / 2.0, size / 2.0);
        shape.RenderTransformOrigin = new Point(0.5, 0.5);

        _floatingObjects.Add(floatingObj);
    }

    private Polygon CreateStar(Color color, int size)
    {
        var points = new PointCollection();
        var outerRadius = size / 2.0;
        var innerRadius = size / 4.0;

        for (int i = 0; i < 10; i++)
        {
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            var angle = i * 36 - 90;
            var rad = angle * Math.PI / 180;
            points.Add(new Point(
                size / 2.0 + radius * Math.Cos(rad),
                size / 2.0 + radius * Math.Sin(rad)
            ));
        }

        return new Polygon
        {
            Points = points,
            Fill = new SolidColorBrush(color),
            Width = size,
            Height = size
        };
    }

    private void FloatingTimer_Tick(object? sender, EventArgs e)
    {
        foreach (var obj in _floatingObjects.ToList())
        {
            // Apply gravity
            obj.VelocityY += 0.1;

            // Apply velocity
            obj.X += obj.VelocityX;
            obj.Y += obj.VelocityY;

            // Bounce off walls
            if (obj.X <= 0 || obj.X >= ActualWidth - obj.Size)
            {
                obj.VelocityX *= -0.8;
                obj.X = Math.Clamp(obj.X, 0, ActualWidth - obj.Size);
            }

            if (obj.Y >= ActualHeight - obj.Size)
            {
                obj.VelocityY *= -0.7;
                obj.Y = ActualHeight - obj.Size;

                // Add some horizontal movement on bounce
                obj.VelocityX += (_random.NextDouble() - 0.5) * 2;
            }

            if (obj.Y < 0)
            {
                obj.VelocityY *= -0.8;
                obj.Y = 0;
            }

            // Apply friction
            obj.VelocityX *= 0.995;

            // Update rotation
            obj.Rotation += obj.RotationSpeed;

            // Update position
            Canvas.SetLeft(obj.Element, obj.X);
            Canvas.SetTop(obj.Element, obj.Y);

            // Update rotation
            if (obj.Element.RenderTransform is RotateTransform rotateTransform)
            {
                rotateTransform.Angle = obj.Rotation;
            }
        }
    }

    private void BackgroundTimer_Tick(object? sender, EventArgs e)
    {
        _backgroundColorIndex = (_backgroundColorIndex + 1) % _backgroundColors.Length;
        var nextIndex = (_backgroundColorIndex + 1) % _backgroundColors.Length;

        var color1 = _backgroundColors[_backgroundColorIndex];
        var color2 = _backgroundColors[nextIndex];

        var colorAnim1 = new ColorAnimation(color1, TimeSpan.FromSeconds(4));
        var colorAnim2 = new ColorAnimation(color2, TimeSpan.FromSeconds(4));

        BackgroundGradient.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, colorAnim1);
        BackgroundGradient.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, colorAnim2);
    }

    private void CleanupTimer_Tick(object? sender, EventArgs e)
    {
        // Remove excess floating objects
        while (_floatingObjects.Count > 15)
        {
            var obj = _floatingObjects[0];
            FloatingCanvas.Children.Remove(obj.Element);
            _floatingObjects.RemoveAt(0);
        }

        // Clean up any leftover effect elements (shouldn't happen but just in case)
        while (EffectsCanvas.Children.Count > 100)
        {
            EffectsCanvas.Children.RemoveAt(0);
        }
    }

    private class FloatingObject
    {
        public required FrameworkElement Element { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public int Size { get; set; }
        public double Rotation { get; set; }
        public double RotationSpeed { get; set; }
    }
}
