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

    // Active vortex (right-click tornado effect)
    private Point? _vortexCenter;
    private DateTime _vortexStartTime;
    private const double VortexDuration = 2.0;      // Seconds the vortex lasts
    private const double VortexRadius = 300.0;      // Range of the vortex effect
    private const double VortexStrength = 15.0;     // How strong the pull/spin is

    // Physics constants
    private const double Friction = 0.995;          // Very slow decay
    private const double CollisionElasticity = 0.8; // How bouncy collisions are
    private const double SpawnForce = 12.0;         // Force applied to nearby objects when spawning (increased)
    private const double SpawnForceRadius = 200.0;  // How far the spawn force reaches (increased)
    private const double MinVelocity = 0.1;         // Below this, velocity is zeroed
    private const double ScrollForce = 6.0;         // Force applied by scroll wheel
    private const double ScrollVariance = 0.4;      // Angular variance for scroll (in radians, ~23 degrees)

    // Object limits
    private const int SoftCap = 100;                // Above this, only remove slow objects
    private const int HardCap = 200;                // Above this, force remove oldest
    private const double SlowSpeedThreshold = 0.5;  // Objects slower than this are "slow"

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
            Interval = TimeSpan.FromSeconds(1)
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
            for (int i = 0; i < 12; i++)
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
        // Only handle left click here (right click has its own handler)
        if (e.ChangedButton != MouseButton.Left) return;

        HideWelcome();

        var position = e.GetPosition(FloatingCanvas);

        // Create visual burst effect
        VisualEffects.CreateClickEffect(EffectsCanvas, position);

        // Create a larger object at click position
        CreateFloatingObject(position.X, position.Y, _random.Next(40, 70), applySpawnForce: true);

        // Play sound
        _soundManager?.PlayRandomSound();
    }

    private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        HideWelcome();

        var position = e.GetPosition(FloatingCanvas);

        // Start a vortex/tornado at this position
        _vortexCenter = position;
        _vortexStartTime = DateTime.Now;

        // Create a visual indicator for the vortex
        CreateVortexVisual(position);

        // Play sound
        _soundManager?.PlayRandomSound();

        Logger.Log($"Vortex started at {position}");
    }

    private void CreateVortexVisual(Point position)
    {
        // Create expanding rings to show the vortex
        for (int i = 0; i < 3; i++)
        {
            var ring = new Ellipse
            {
                Width = 40,
                Height = 40,
                Stroke = new SolidColorBrush(VisualEffects.GetRandomColor()),
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Opacity = 0.8
            };

            Canvas.SetLeft(ring, position.X - 20);
            Canvas.SetTop(ring, position.Y - 20);
            EffectsCanvas.Children.Add(ring);

            // Animate expanding outward
            var delay = TimeSpan.FromMilliseconds(i * 200);
            var duration = TimeSpan.FromMilliseconds(1500);

            var widthAnim = new DoubleAnimation(40, VortexRadius * 2, duration) { BeginTime = delay };
            var heightAnim = new DoubleAnimation(40, VortexRadius * 2, duration) { BeginTime = delay };
            var opacityAnim = new DoubleAnimation(0.8, 0, duration) { BeginTime = delay };

            var translate = new TranslateTransform();
            ring.RenderTransform = translate;

            var moveXAnim = new DoubleAnimation(0, -VortexRadius + 20, duration) { BeginTime = delay };
            var moveYAnim = new DoubleAnimation(0, -VortexRadius + 20, duration) { BeginTime = delay };

            opacityAnim.Completed += (s, ev) => EffectsCanvas.Children.Remove(ring);

            ring.BeginAnimation(WidthProperty, widthAnim);
            ring.BeginAnimation(HeightProperty, heightAnim);
            ring.BeginAnimation(OpacityProperty, opacityAnim);
            translate.BeginAnimation(TranslateTransform.XProperty, moveXAnim);
            translate.BeginAnimation(TranslateTransform.YProperty, moveYAnim);
        }
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        HideWelcome();

        // Delta > 0 = scroll up, Delta < 0 = scroll down
        bool scrollUp = e.Delta > 0;

        // Base direction: up = -PI/2 (north), down = PI/2 (south)
        double baseAngle = scrollUp ? -Math.PI / 2 : Math.PI / 2;

        // Apply force to all objects with variance
        foreach (var obj in _floatingObjects)
        {
            // Random variance between -ScrollVariance and +ScrollVariance
            double variance = (_random.NextDouble() * 2 - 1) * ScrollVariance;
            double angle = baseAngle + variance;

            // Random force magnitude with some variance too
            double force = ScrollForce * (0.7 + _random.NextDouble() * 0.6);

            obj.VelocityX += Math.Cos(angle) * force;
            obj.VelocityY += Math.Sin(angle) * force;

            // Add some spin
            obj.RotationSpeed += (_random.NextDouble() - 0.5) * 3;
        }

        // Play sound
        _soundManager?.PlayRandomSound();

        // Create visual effect at mouse position
        var position = e.GetPosition(EffectsCanvas);
        CreateScrollVisual(position, scrollUp);

        Logger.Log($"Scroll {(scrollUp ? "up" : "down")}, applied force to {_floatingObjects.Count} objects");
    }

    private void CreateScrollVisual(Point position, bool scrollUp)
    {
        // Create arrows showing the direction
        var color = VisualEffects.GetRandomColor();

        for (int i = 0; i < 5; i++)
        {
            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(0, scrollUp ? 20 : 0),
                    new Point(10, scrollUp ? 0 : 20),
                    new Point(20, scrollUp ? 20 : 0)
                },
                Fill = new SolidColorBrush(color),
                Opacity = 0.8
            };

            var x = position.X - 10 + (_random.NextDouble() - 0.5) * 100;
            var y = position.Y - 10;

            Canvas.SetLeft(arrow, x);
            Canvas.SetTop(arrow, y);
            EffectsCanvas.Children.Add(arrow);

            // Animate moving in scroll direction and fading
            var translate = new TranslateTransform();
            arrow.RenderTransform = translate;

            var moveYAnim = new DoubleAnimation(0, scrollUp ? -100 : 100, TimeSpan.FromMilliseconds(500));
            var fadeAnim = new DoubleAnimation(0.8, 0, TimeSpan.FromMilliseconds(500));

            fadeAnim.Completed += (s, ev) => EffectsCanvas.Children.Remove(arrow);

            translate.BeginAnimation(TranslateTransform.YProperty, moveYAnim);
            arrow.BeginAnimation(OpacityProperty, fadeAnim);
        }
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
            Mass = size,
            Rotation = 0,
            RotationSpeed = (_random.NextDouble() - 0.5) * 4,
            CreatedAt = DateTime.Now
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

                // Add spin from the push
                obj.RotationSpeed += (_random.NextDouble() - 0.5) * 2;
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
        // Apply vortex effect if active
        ApplyVortexEffect();

        // Handle collisions between objects
        HandleCollisions();

        // Update positions
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

    private void ApplyVortexEffect()
    {
        if (_vortexCenter == null) return;

        var elapsed = (DateTime.Now - _vortexStartTime).TotalSeconds;
        if (elapsed > VortexDuration)
        {
            _vortexCenter = null;
            return;
        }

        // Vortex strength fades over time
        var strengthMultiplier = 1 - (elapsed / VortexDuration);
        var center = _vortexCenter.Value;

        foreach (var obj in _floatingObjects)
        {
            var dx = obj.X - center.X;
            var dy = obj.Y - center.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < VortexRadius && distance > 10)
            {
                // Normalize direction to center
                var nx = dx / distance;
                var ny = dy / distance;

                // Tangential direction (perpendicular to radial - creates spin)
                var tx = -ny;
                var ty = nx;

                // Force decreases with distance from center
                var distanceFactor = 1 - (distance / VortexRadius);
                var force = VortexStrength * distanceFactor * strengthMultiplier;

                // Apply tangential force (spinning) and slight inward pull
                var tangentialForce = force * 0.8;
                var inwardForce = force * 0.3;

                obj.VelocityX += tx * tangentialForce - nx * inwardForce;
                obj.VelocityY += ty * tangentialForce - ny * inwardForce;

                // Add spin to the objects themselves
                obj.RotationSpeed += force * 0.5 * (distance < VortexRadius / 2 ? 1 : -1);
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
        var count = _floatingObjects.Count;

        if (count > HardCap)
        {
            // Above hard cap: remove oldest objects to get back to hard cap
            var toRemove = count - HardCap;
            Logger.Log($"Hard cap cleanup: removing {toRemove} oldest objects");

            for (int i = 0; i < toRemove && _floatingObjects.Count > 0; i++)
            {
                var obj = _floatingObjects[0]; // Oldest is first
                FloatingCanvas.Children.Remove(obj.Element);
                _floatingObjects.RemoveAt(0);
            }
        }
        else if (count > SoftCap)
        {
            // Between soft and hard cap: only remove slow-moving objects
            var slowObjects = _floatingObjects
                .Where(o => GetSpeed(o) < SlowSpeedThreshold)
                .OrderBy(o => GetSpeed(o))
                .ToList();

            // Remove up to (count - SoftCap) slow objects, but don't go below SoftCap
            var canRemove = Math.Min(slowObjects.Count, count - SoftCap);

            if (canRemove > 0)
            {
                Logger.Log($"Soft cap cleanup: removing {canRemove} slow objects");

                for (int i = 0; i < canRemove; i++)
                {
                    var obj = slowObjects[i];
                    FloatingCanvas.Children.Remove(obj.Element);
                    _floatingObjects.Remove(obj);
                }
            }
        }

        // Clean up leftover effect elements
        while (EffectsCanvas.Children.Count > 100)
        {
            EffectsCanvas.Children.RemoveAt(0);
        }
    }

    private static double GetSpeed(FloatingObject obj)
    {
        return Math.Sqrt(obj.VelocityX * obj.VelocityX + obj.VelocityY * obj.VelocityY);
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
        public DateTime CreatedAt { get; set; }
    }
}
