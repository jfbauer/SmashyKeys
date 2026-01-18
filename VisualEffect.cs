using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SmashyKeys;

/// <summary>
/// Creates colorful visual effects for key presses and mouse clicks.
/// </summary>
public static class VisualEffects
{
    private static readonly Random _random = new();

    private static readonly Color[] _colors =
    {
        Colors.Red, Colors.Orange, Colors.Yellow, Colors.Lime, Colors.Cyan,
        Colors.DodgerBlue, Colors.BlueViolet, Colors.Magenta, Colors.HotPink,
        Colors.Gold, Colors.Coral, Colors.SpringGreen, Colors.Turquoise,
        Colors.Orchid, Colors.Tomato, Colors.LawnGreen, Colors.DeepSkyBlue
    };

    private static readonly string[] _letters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".Select(c => c.ToString()).ToArray();

    public static Color GetRandomColor() => _colors[_random.Next(_colors.Length)];

    /// <summary>
    /// Creates a shape that appears, grows, and fades out.
    /// </summary>
    public static void CreateKeyPressEffect(Canvas canvas, int? keyCode = null)
    {
        var color = GetRandomColor();
        var x = _random.NextDouble() * canvas.ActualWidth;
        var y = _random.NextDouble() * canvas.ActualHeight;
        var size = _random.Next(80, 200);

        // Randomly choose effect type
        var effectType = _random.Next(5);

        FrameworkElement element = effectType switch
        {
            0 => CreateCircle(color, size),
            1 => CreateStar(color, size),
            2 => CreateSquare(color, size),
            3 => CreateHeart(color, size),
            _ => CreateLetter(color, size, keyCode)
        };

        Canvas.SetLeft(element, x - size / 2);
        Canvas.SetTop(element, y - size / 2);
        canvas.Children.Add(element);

        AnimateElement(element, canvas, size);
    }

    /// <summary>
    /// Creates an effect at a specific position (for mouse clicks).
    /// </summary>
    public static void CreateClickEffect(Canvas canvas, Point position)
    {
        var color = GetRandomColor();

        // Create expanding ring effect
        for (int i = 0; i < 3; i++)
        {
            var delay = i * 100;
            var ring = new Ellipse
            {
                Width = 20,
                Height = 20,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 4,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(ring, position.X - 10);
            Canvas.SetTop(ring, position.Y - 10);
            canvas.Children.Add(ring);

            AnimateRing(ring, canvas, delay);
        }

        // Also create a burst of small shapes
        for (int i = 0; i < 8; i++)
        {
            var angle = i * 45 * Math.PI / 180;
            var particleColor = GetRandomColor();
            var particle = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(particleColor)
            };

            Canvas.SetLeft(particle, position.X - 10);
            Canvas.SetTop(particle, position.Y - 10);
            canvas.Children.Add(particle);

            AnimateParticle(particle, canvas, angle);
        }
    }

    private static Ellipse CreateCircle(Color color, int size)
    {
        return new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color)
        };
    }

    private static Rectangle CreateSquare(Color color, int size)
    {
        return new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color),
            RadiusX = 10,
            RadiusY = 10
        };
    }

    private static Polygon CreateStar(Color color, int size)
    {
        var points = new PointCollection();
        var outerRadius = size / 2.0;
        var innerRadius = size / 4.0;
        var center = size / 2.0;

        for (int i = 0; i < 10; i++)
        {
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            var angle = i * 36 - 90;
            var rad = angle * Math.PI / 180;
            points.Add(new Point(
                center + radius * Math.Cos(rad),
                center + radius * Math.Sin(rad)
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

    private static Path CreateHeart(Color color, int size)
    {
        var geometry = Geometry.Parse(
            "M 0.5,0.2 " +
            "C 0.5,0.1 0.4,0 0.25,0 " +
            "C 0,0 0,0.3 0,0.3 " +
            "C 0,0.5 0.2,0.7 0.5,0.95 " +
            "C 0.8,0.7 1,0.5 1,0.3 " +
            "C 1,0.3 1,0 0.75,0 " +
            "C 0.6,0 0.5,0.1 0.5,0.2 Z"
        );

        return new Path
        {
            Data = geometry,
            Fill = new SolidColorBrush(color),
            Width = size,
            Height = size,
            Stretch = Stretch.Fill
        };
    }

    private static TextBlock CreateLetter(Color color, int size, int? keyCode)
    {
        string text;
        if (keyCode.HasValue && keyCode >= 0x41 && keyCode <= 0x5A) // A-Z
        {
            text = ((char)keyCode.Value).ToString();
        }
        else if (keyCode.HasValue && keyCode >= 0x30 && keyCode <= 0x39) // 0-9
        {
            text = ((char)keyCode.Value).ToString();
        }
        else
        {
            text = _letters[_random.Next(_letters.Length)];
        }

        return new TextBlock
        {
            Text = text,
            FontSize = size * 0.8,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(color),
            Width = size,
            Height = size,
            TextAlignment = TextAlignment.Center
        };
    }

    private static void AnimateElement(FrameworkElement element, Canvas canvas, int size)
    {
        var duration = TimeSpan.FromMilliseconds(_random.Next(800, 1500));
        var storyboard = new Storyboard();

        // Scale up animation
        var scaleTransform = new ScaleTransform(0.1, 0.1, size / 2.0, size / 2.0);
        element.RenderTransform = scaleTransform;

        var scaleXAnim = new DoubleAnimation(0.1, 1.2, duration) { EasingFunction = new ElasticEase() };
        var scaleYAnim = new DoubleAnimation(0.1, 1.2, duration) { EasingFunction = new ElasticEase() };

        Storyboard.SetTarget(scaleXAnim, element);
        Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("RenderTransform.ScaleX"));
        Storyboard.SetTarget(scaleYAnim, element);
        Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("RenderTransform.ScaleY"));

        // Fade out animation
        var fadeAnim = new DoubleAnimation(1, 0, duration);
        Storyboard.SetTarget(fadeAnim, element);
        Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));

        // Rotation animation
        var rotateTransform = new RotateTransform(0, size / 2.0, size / 2.0);
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(scaleTransform);
        transformGroup.Children.Add(rotateTransform);
        element.RenderTransform = transformGroup;

        var rotateAnim = new DoubleAnimation(0, _random.Next(-180, 180), duration);
        Storyboard.SetTarget(rotateAnim, element);
        Storyboard.SetTargetProperty(rotateAnim, new PropertyPath("RenderTransform.Children[1].Angle"));

        storyboard.Children.Add(scaleXAnim);
        storyboard.Children.Add(scaleYAnim);
        storyboard.Children.Add(fadeAnim);
        storyboard.Children.Add(rotateAnim);

        storyboard.Completed += (s, e) => canvas.Children.Remove(element);
        storyboard.Begin();
    }

    private static void AnimateRing(Ellipse ring, Canvas canvas, int delayMs)
    {
        var duration = TimeSpan.FromMilliseconds(600);
        var storyboard = new Storyboard { BeginTime = TimeSpan.FromMilliseconds(delayMs) };

        // Expand
        var widthAnim = new DoubleAnimation(20, 150, duration);
        var heightAnim = new DoubleAnimation(20, 150, duration);
        var leftAnim = new DoubleAnimation(0, -65, duration);
        var topAnim = new DoubleAnimation(0, -65, duration);
        var fadeAnim = new DoubleAnimation(1, 0, duration);

        Storyboard.SetTarget(widthAnim, ring);
        Storyboard.SetTargetProperty(widthAnim, new PropertyPath("Width"));
        Storyboard.SetTarget(heightAnim, ring);
        Storyboard.SetTargetProperty(heightAnim, new PropertyPath("Height"));
        Storyboard.SetTarget(fadeAnim, ring);
        Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));

        // For position, we need to use TranslateTransform
        var translate = new TranslateTransform();
        ring.RenderTransform = translate;

        var moveXAnim = new DoubleAnimation(0, -65, duration);
        var moveYAnim = new DoubleAnimation(0, -65, duration);
        Storyboard.SetTarget(moveXAnim, ring);
        Storyboard.SetTargetProperty(moveXAnim, new PropertyPath("RenderTransform.X"));
        Storyboard.SetTarget(moveYAnim, ring);
        Storyboard.SetTargetProperty(moveYAnim, new PropertyPath("RenderTransform.Y"));

        storyboard.Children.Add(widthAnim);
        storyboard.Children.Add(heightAnim);
        storyboard.Children.Add(fadeAnim);
        storyboard.Children.Add(moveXAnim);
        storyboard.Children.Add(moveYAnim);

        storyboard.Completed += (s, e) => canvas.Children.Remove(ring);
        storyboard.Begin();
    }

    private static void AnimateParticle(Ellipse particle, Canvas canvas, double angle)
    {
        var duration = TimeSpan.FromMilliseconds(500);
        var distance = 100;

        var translate = new TranslateTransform();
        particle.RenderTransform = translate;

        var storyboard = new Storyboard();

        var moveXAnim = new DoubleAnimation(0, Math.Cos(angle) * distance, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var moveYAnim = new DoubleAnimation(0, Math.Sin(angle) * distance, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeAnim = new DoubleAnimation(1, 0, duration);
        var scaleAnim = new DoubleAnimation(1, 0.2, duration);

        Storyboard.SetTarget(moveXAnim, particle);
        Storyboard.SetTargetProperty(moveXAnim, new PropertyPath("RenderTransform.X"));
        Storyboard.SetTarget(moveYAnim, particle);
        Storyboard.SetTargetProperty(moveYAnim, new PropertyPath("RenderTransform.Y"));
        Storyboard.SetTarget(fadeAnim, particle);
        Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));

        storyboard.Children.Add(moveXAnim);
        storyboard.Children.Add(moveYAnim);
        storyboard.Children.Add(fadeAnim);

        storyboard.Completed += (s, e) => canvas.Children.Remove(particle);
        storyboard.Begin();
    }
}
