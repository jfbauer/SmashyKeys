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
    private SoundManager? _soundManager;
    private readonly DispatcherTimer _physicsTimer;
    private readonly DispatcherTimer _backgroundTimer;
    private readonly DispatcherTimer _cleanupTimer;
    private readonly List<FloatingObject> _floatingObjects = new();
    private readonly Random _random = new();
    private bool _welcomeHidden;
    private int _backgroundColorIndex;

    // Physics constants
    private const double Friction = 0.995;          // Very slow decay (0.995 = objects keep moving a long time)
    private const double CollisionElasticity = 0.8; // How bouncy collisions are
    private const double SpawnForce = 8.0;          // Force applied to nearby objects when spawning
    private const double SpawnForceRadius = 150.0;  // How far the spawn force reaches
    private const double MinVelocity = 0.1;         // Below this, velocity is zeroed

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

        // Physics timer at 60fps
        _physicsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _physicsTimer.Tick += PhysicsTimer_Tick;

        // Background color transition timer
        _backgroundTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _backgroundTimer.Tick += BackgroundTimer_Tick;

        // Cleanup timer
        _cleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _cleanupTimer.Tick += CleanupTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Logger.Log("Window_Loaded started");

        try
        {
            // Initialize sound manager
            Logger.Log("Initializing sound manager...");
            _soundManager = new SoundManager();
            Logger.Log("Sound manager initialized");

            // Install keyboard hook
            Logger.Log("Installing keyboard hook...");
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyPressed += OnKeyPressed;
            _keyboardHook.SecretExitTriggered += OnSecretExit;
            _keyboardHook.Install();
            Logger.Log("Keyboard hook installed");

            // Start timers
            _physicsTimer.Start();
            _backgroundTimer.Start();
            _cleanupTimer.Start();
            Logger.Log("Timers started");

            // Create some initial floating objects
            for (int i = 0; i < 8; i++)
            {
                CreateFloatingObject(
                    _random.NextDouble() * ActualWidth,
                    _random.NextDouble() * ActualHeight,
                    _random.Next(30, 60),
                    applySpawnForce: false
                );
            }
            Logger.Log("Initial floating objects created");

            // Center cursor initially
            Canvas.SetLeft(CursorCircle, ActualWidth / 2 - 25);
            Canvas.SetTop(CursorCircle, ActualHeight / 2 - 25);

            // Focus window
            Activate();
            Focus();
            Logger.Log("Window_Loaded completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogException("Window_Loaded", ex);
            throw;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _keyboardHook?.Dispose();
        _physicsTimer.Stop();
        _backgroundTimer.Stop();
        _cleanupTimer.Stop();
    }

    private void OnKeyPressed(int keyCode)
    {
        try
        {
            Logger.Log($"OnKeyPressed called with keyCode: {keyCode}");

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    HideWelcome();

                    // Create 5 smaller floating objects at random positions
                    for (int i = 0; i < 5; i++)
                    {
                        var x = _random.NextDouble() * ActualWidth;
                        var y = _random.NextDouble() * ActualHeight;
                        CreateFloatingObject(x, y, _random.Next(25, 45), applySpawnForce: true);
                    }

                    // Play a sound
                    _soundManager?.PlayRandomSound();

                    Logger.Log("OnKeyPressed completed");
                }
                catch (Exception ex)
                {
                    Logger.LogException("OnKeyPressed dispatcher callback", ex);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogException("OnKeyPressed", ex);
        }
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

        var position = e.GetPosition(FloatingCanvas);

        // Create visual burst effect
        VisualEffects.CreateClickEffect(EffectsCanvas, position);

        // Create a larger object at click position
        CreateFloatingObject(position.X, position.Y, _random.Next(40, 70), applySpawnForce: true);

        // Play sound
        _soundManager?.PlayRandomSound();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        // Update cursor position
        var position = e.GetPosition(CursorCanvas);
        Canvas.SetLeft(CursorCircle, position.X - 25);
        Canvas.SetTop(CursorCircle, position.Y - 25);

        // Create subtle trail effect (throttled)
        if (_random.Next(8) == 0)
        {
            var color = VisualEffects.GetRandomColor();
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(color),
                Opacity = 0.4
            };

            Canvas.SetLeft(dot, position.X - 4);
            Canvas.SetTop(dot, position.Y - 4);
            EffectsCanvas.Children.Add(dot);

            // Fade out
            var fadeAnim = new DoubleAnimation(0.4, 0, TimeSpan.FromMilliseconds(400));
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

    private void CreateFloatingObject(double x, double y, int size, bool applySpawnForce)
    {
        var color = VisualEffects.GetRandomColor();

        FrameworkElement shape;
        var shapeType = _random.Next(4);

        switch (shapeType)
        {
            case 0:
                shape = new Ellipse { Width = size, Height = size, Fill = new SolidColorBrush(color) };
                break;
            case 1:
                shape = new Rectangle
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(color),
                    RadiusX = size * 0.15,
                    RadiusY = size * 0.15
                };
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

        // Clamp position to stay within bounds
        x = Math.Clamp(x, size / 2.0, ActualWidth - size / 2.0);
        y = Math.Clamp(y, size / 2.0, ActualHeight - size / 2.0);

        Canvas.SetLeft(shape, x - size / 2.0);
        Canvas.SetTop(shape, y - size / 2.0);
        FloatingCanvas.Children.Add(shape);

        // Random initial velocity
        var angle = _random.NextDouble() * Math.PI * 2;
        var speed = _random.NextDouble() * 3 + 1;

        var floatingObj = new FloatingObject
        {
            Element = shape,
            X = x,
            Y = y,
            VelocityX = Math.Cos(angle) * speed,
            VelocityY = Math.Sin(angle) * speed,
            Radius = size / 2.0,
            Mass = size, // Larger objects are heavier
            Rotation = 0,
            RotationSpeed = (_random.NextDouble() - 0.5) * 4
        };

        // Add rotation transform
        shape.RenderTransform = new RotateTransform(0, size / 2.0, size / 2.0);
        shape.RenderTransformOrigin = new Point(0.5, 0.5);

        _floatingObjects.Add(floatingObj);

        // Apply force to nearby objects (push them away)
        if (applySpawnForce)
        {
            ApplySpawnForce(floatingObj);
        }
    }

    private void ApplySpawnForce(FloatingObject newObj)
    {
        foreach (var obj in _floatingObjects)
        {
            if (obj == newObj) continue;

            var dx = obj.X - newObj.X;
            var dy = obj.Y - newObj.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < SpawnForceRadius && distance > 0.1)
            {
                // Normalize direction
                var nx = dx / distance;
                var ny = dy / distance;

                // Force decreases with distance
                var forceMagnitude = SpawnForce * (1 - distance / SpawnForceRadius);

                // Apply force (lighter objects get pushed more)
                var acceleration = forceMagnitude / (obj.Mass * 0.1);
                obj.VelocityX += nx * acceleration;
                obj.VelocityY += ny * acceleration;
            }
        }
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

    private void PhysicsTimer_Tick(object? sender, EventArgs e)
    {
        // First, handle collisions between objects
        HandleCollisions();

        // Then update positions
        foreach (var obj in _floatingObjects)
        {
            // Apply friction (slow velocity decay)
            obj.VelocityX *= Friction;
            obj.VelocityY *= Friction;

            // Zero out very small velocities
            if (Math.Abs(obj.VelocityX) < MinVelocity) obj.VelocityX = 0;
            if (Math.Abs(obj.VelocityY) < MinVelocity) obj.VelocityY = 0;

            // Apply velocity
            obj.X += obj.VelocityX;
            obj.Y += obj.VelocityY;

            // Bounce off walls (keep objects fully on screen)
            var minX = obj.Radius;
            var maxX = ActualWidth - obj.Radius;
            var minY = obj.Radius;
            var maxY = ActualHeight - obj.Radius;

            if (obj.X < minX)
            {
                obj.X = minX;
                obj.VelocityX = Math.Abs(obj.VelocityX) * CollisionElasticity;
            }
            else if (obj.X > maxX)
            {
                obj.X = maxX;
                obj.VelocityX = -Math.Abs(obj.VelocityX) * CollisionElasticity;
            }

            if (obj.Y < minY)
            {
                obj.Y = minY;
                obj.VelocityY = Math.Abs(obj.VelocityY) * CollisionElasticity;
            }
            else if (obj.Y > maxY)
            {
                obj.Y = maxY;
                obj.VelocityY = -Math.Abs(obj.VelocityY) * CollisionElasticity;
            }

            // Update rotation
            obj.Rotation += obj.RotationSpeed;
            obj.RotationSpeed *= 0.998; // Rotation also slows down

            // Update visual position
            Canvas.SetLeft(obj.Element, obj.X - obj.Radius);
            Canvas.SetTop(obj.Element, obj.Y - obj.Radius);

            // Update rotation
            if (obj.Element.RenderTransform is RotateTransform rotateTransform)
            {
                rotateTransform.Angle = obj.Rotation;
            }
        }
    }

    private void HandleCollisions()
    {
        for (int i = 0; i < _floatingObjects.Count; i++)
        {
            for (int j = i + 1; j < _floatingObjects.Count; j++)
            {
                var a = _floatingObjects[i];
                var b = _floatingObjects[j];

                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var minDist = a.Radius + b.Radius;

                if (distance < minDist && distance > 0.01)
                {
                    // Collision detected - resolve it

                    // Normalize collision normal
                    var nx = dx / distance;
                    var ny = dy / distance;

                    // Relative velocity
                    var dvx = a.VelocityX - b.VelocityX;
                    var dvy = a.VelocityY - b.VelocityY;

                    // Relative velocity along collision normal
                    var dvn = dvx * nx + dvy * ny;

                    // Only resolve if objects are moving toward each other
                    if (dvn > 0)
                    {
                        // Calculate impulse (considering mass)
                        var totalMass = a.Mass + b.Mass;
                        var impulse = (2 * dvn) / totalMass * CollisionElasticity;

                        // Apply impulse
                        a.VelocityX -= impulse * b.Mass * nx;
                        a.VelocityY -= impulse * b.Mass * ny;
                        b.VelocityX += impulse * a.Mass * nx;
                        b.VelocityY += impulse * a.Mass * ny;

                        // Add some spin from collision
                        a.RotationSpeed += (_random.NextDouble() - 0.5) * 2;
                        b.RotationSpeed += (_random.NextDouble() - 0.5) * 2;
                    }

                    // Separate overlapping objects
                    var overlap = minDist - distance;
                    var separationX = nx * overlap * 0.5;
                    var separationY = ny * overlap * 0.5;

                    a.X -= separationX;
                    a.Y -= separationY;
                    b.X += separationX;
                    b.Y += separationY;
                }
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
        // Remove excess floating objects (keep max 40)
        while (_floatingObjects.Count > 40)
        {
            var obj = _floatingObjects[0];
            FloatingCanvas.Children.Remove(obj.Element);
            _floatingObjects.RemoveAt(0);
        }

        // Clean up leftover effect elements
        while (EffectsCanvas.Children.Count > 80)
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
        public double Radius { get; set; }
        public double Mass { get; set; }
        public double Rotation { get; set; }
        public double RotationSpeed { get; set; }
    }
}
