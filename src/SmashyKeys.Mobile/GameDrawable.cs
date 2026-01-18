namespace SmashyKeys.Mobile;

public class GameDrawable : IDrawable
{
    // Physics constants (matching desktop)
    private const double Friction = 0.995;
    private const double CollisionElasticity = 0.8;
    private const double SpawnForce = 12.0;
    private const double SpawnForceRadius = 200.0;
    private const int ObjectsPerTouch = 5;
    
    // Object limits
    private const int SoftCap = 100;
    private const int HardCap = 200;
    private const double SlowSpeedThreshold = 0.5;
    
    // Vortex settings
    private const double VortexDuration = 2.0;
    private const double VortexRadius = 300.0;
    private const double VortexStrength = 15.0;
    
    private readonly List<PhysicsObject> _objects = new();
    private readonly List<VisualEffect> _effects = new();
    private readonly List<TouchTrail> _trails = new();
    private readonly Random _random = new();
    
    // Active vortex
    private PointF? _vortexCenter;
    private double _vortexTimeRemaining;
    
    // Vibrant colors
    private readonly Color[] _colors = new[]
    {
        Color.FromRgb(255, 107, 107),  // Red
        Color.FromRgb(255, 159, 67),   // Orange
        Color.FromRgb(254, 202, 87),   // Yellow
        Color.FromRgb(29, 209, 161),   // Teal
        Color.FromRgb(84, 160, 255),   // Blue
        Color.FromRgb(156, 136, 255),  // Purple
        Color.FromRgb(255, 107, 181),  // Pink
        Color.FromRgb(16, 172, 132),   // Green
    };

    private float _width = 800;
    private float _height = 600;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        _width = dirtyRect.Width;
        _height = dirtyRect.Height;
        
        // Clear background
        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(dirtyRect);
        
        // Draw vortex effect
        if (_vortexCenter.HasValue && _vortexTimeRemaining > 0)
        {
            DrawVortex(canvas);
        }
        
        // Draw trails
        foreach (var trail in _trails.ToList())
        {
            canvas.FillColor = trail.Color.WithAlpha((float)trail.Alpha);
            canvas.FillCircle(trail.X, trail.Y, trail.Size);
        }
        
        // Draw physics objects
        foreach (var obj in _objects.ToList())
        {
            DrawObject(canvas, obj);
        }
        
        // Draw visual effects
        foreach (var effect in _effects.ToList())
        {
            DrawEffect(canvas, effect);
        }
    }

    private void DrawObject(ICanvas canvas, PhysicsObject obj)
    {
        canvas.FillColor = obj.Color;
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        
        switch (obj.Shape)
        {
            case ShapeType.Circle:
                canvas.FillCircle(obj.X, obj.Y, obj.Size);
                canvas.DrawCircle(obj.X, obj.Y, obj.Size);
                break;
                
            case ShapeType.Square:
                canvas.FillRectangle(obj.X - obj.Size, obj.Y - obj.Size, obj.Size * 2, obj.Size * 2);
                canvas.DrawRectangle(obj.X - obj.Size, obj.Y - obj.Size, obj.Size * 2, obj.Size * 2);
                break;
                
            case ShapeType.Star:
                DrawStar(canvas, obj.X, obj.Y, obj.Size);
                break;
                
            case ShapeType.Heart:
                DrawHeart(canvas, obj.X, obj.Y, obj.Size);
                break;
                
            case ShapeType.Triangle:
                DrawTriangle(canvas, obj.X, obj.Y, obj.Size);
                break;
        }
    }

    private void DrawStar(ICanvas canvas, float cx, float cy, float size)
    {
        var path = new PathF();
        int points = 5;
        double outerRadius = size;
        double innerRadius = size * 0.4;
        
        for (int i = 0; i < points * 2; i++)
        {
            double radius = i % 2 == 0 ? outerRadius : innerRadius;
            double angle = Math.PI / 2 + i * Math.PI / points;
            float x = cx + (float)(radius * Math.Cos(angle));
            float y = cy - (float)(radius * Math.Sin(angle));
            
            if (i == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }
        path.Close();
        
        canvas.FillPath(path);
        canvas.DrawPath(path);
    }

    private void DrawHeart(ICanvas canvas, float cx, float cy, float size)
    {
        var path = new PathF();
        float scale = size / 10;
        
        path.MoveTo(cx, cy + 8 * scale);
        path.CurveTo(cx, cy + 6 * scale, cx - 5 * scale, cy, cx - 10 * scale, cy);
        path.CurveTo(cx - 15 * scale, cy, cx - 15 * scale, cy - 7.5f * scale, cx - 15 * scale, cy - 7.5f * scale);
        path.CurveTo(cx - 15 * scale, cy - 11 * scale, cx - 12 * scale, cy - 15.4f * scale, cx, cy - 15.4f * scale);
        path.CurveTo(cx + 12 * scale, cy - 15.4f * scale, cx + 15 * scale, cy - 11 * scale, cx + 15 * scale, cy - 7.5f * scale);
        path.CurveTo(cx + 15 * scale, cy - 7.5f * scale, cx + 15 * scale, cy, cx + 10 * scale, cy);
        path.CurveTo(cx + 5 * scale, cy, cx, cy + 6 * scale, cx, cy + 8 * scale);
        path.Close();
        
        canvas.FillPath(path);
        canvas.DrawPath(path);
    }

    private void DrawTriangle(ICanvas canvas, float cx, float cy, float size)
    {
        var path = new PathF();
        path.MoveTo(cx, cy - size);
        path.LineTo(cx - size, cy + size * 0.7f);
        path.LineTo(cx + size, cy + size * 0.7f);
        path.Close();
        
        canvas.FillPath(path);
        canvas.DrawPath(path);
    }

    private void DrawVortex(ICanvas canvas)
    {
        if (!_vortexCenter.HasValue) return;
        
        float alpha = (float)(_vortexTimeRemaining / VortexDuration) * 0.3f;
        
        // Draw spinning rings
        for (int i = 0; i < 5; i++)
        {
            float radius = (float)(VortexRadius * (1 - i * 0.15));
            canvas.StrokeColor = _colors[i % _colors.Length].WithAlpha(alpha);
            canvas.StrokeSize = 3;
            canvas.DrawCircle(_vortexCenter.Value.X, _vortexCenter.Value.Y, radius);
        }
    }

    private void DrawEffect(ICanvas canvas, VisualEffect effect)
    {
        canvas.FillColor = effect.Color.WithAlpha((float)effect.Alpha);
        canvas.StrokeColor = Colors.White.WithAlpha((float)effect.Alpha * 0.5f);
        canvas.StrokeSize = 2;
        
        switch (effect.Type)
        {
            case EffectType.ExpandingRing:
                canvas.StrokeColor = effect.Color.WithAlpha((float)effect.Alpha);
                canvas.StrokeSize = 4;
                canvas.DrawCircle(effect.X, effect.Y, effect.Size);
                break;
                
            case EffectType.Particle:
                canvas.FillCircle(effect.X, effect.Y, effect.Size);
                break;
                
            case EffectType.Burst:
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    float px = effect.X + (float)(effect.Size * Math.Cos(angle));
                    float py = effect.Y + (float)(effect.Size * Math.Sin(angle));
                    canvas.FillCircle(px, py, 5);
                }
                break;
        }
    }

    public void Update()
    {
        // Update vortex
        if (_vortexTimeRemaining > 0)
        {
            _vortexTimeRemaining -= 0.016;
            if (_vortexTimeRemaining <= 0)
            {
                _vortexCenter = null;
            }
        }
        
        // Update physics objects
        foreach (var obj in _objects.ToList())
        {
            // Apply vortex force
            if (_vortexCenter.HasValue && _vortexTimeRemaining > 0)
            {
                ApplyVortexForce(obj);
            }
            
            // Apply friction
            obj.VelocityX *= Friction;
            obj.VelocityY *= Friction;
            
            // Update position
            obj.X += (float)obj.VelocityX;
            obj.Y += (float)obj.VelocityY;
            
            // Bounce off walls
            if (obj.X - obj.Size < 0)
            {
                obj.X = obj.Size;
                obj.VelocityX = -obj.VelocityX * CollisionElasticity;
            }
            else if (obj.X + obj.Size > _width)
            {
                obj.X = _width - obj.Size;
                obj.VelocityX = -obj.VelocityX * CollisionElasticity;
            }
            
            if (obj.Y - obj.Size < 0)
            {
                obj.Y = obj.Size;
                obj.VelocityY = -obj.VelocityY * CollisionElasticity;
            }
            else if (obj.Y + obj.Size > _height)
            {
                obj.Y = _height - obj.Size;
                obj.VelocityY = -obj.VelocityY * CollisionElasticity;
            }
        }
        
        // Handle collisions between objects
        HandleCollisions();
        
        // Enforce object limits
        EnforceObjectLimits();
        
        // Update trails
        foreach (var trail in _trails.ToList())
        {
            trail.Alpha -= 0.02;
            if (trail.Alpha <= 0)
            {
                _trails.Remove(trail);
            }
        }
        
        // Update effects
        foreach (var effect in _effects.ToList())
        {
            effect.Update();
            if (effect.Alpha <= 0)
            {
                _effects.Remove(effect);
            }
        }
    }

    private void ApplyVortexForce(PhysicsObject obj)
    {
        if (!_vortexCenter.HasValue) return;
        
        double dx = _vortexCenter.Value.X - obj.X;
        double dy = _vortexCenter.Value.Y - obj.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        
        if (distance < VortexRadius && distance > 1)
        {
            double force = VortexStrength * (1 - distance / VortexRadius);
            
            // Tangential (spinning) + radial (inward) force
            double angle = Math.Atan2(dy, dx);
            double tangentAngle = angle + Math.PI / 2;
            
            obj.VelocityX += force * Math.Cos(tangentAngle) * 0.1;
            obj.VelocityY += force * Math.Sin(tangentAngle) * 0.1;
            obj.VelocityX += dx / distance * force * 0.03;
            obj.VelocityY += dy / distance * force * 0.03;
        }
    }

    private void HandleCollisions()
    {
        for (int i = 0; i < _objects.Count; i++)
        {
            for (int j = i + 1; j < _objects.Count; j++)
            {
                var a = _objects[i];
                var b = _objects[j];
                
                double dx = b.X - a.X;
                double dy = b.Y - a.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double minDist = a.Size + b.Size;
                
                if (distance < minDist && distance > 0.1)
                {
                    // Elastic collision
                    double nx = dx / distance;
                    double ny = dy / distance;
                    
                    double dvx = a.VelocityX - b.VelocityX;
                    double dvy = a.VelocityY - b.VelocityY;
                    double dvn = dvx * nx + dvy * ny;
                    
                    if (dvn > 0)
                    {
                        double massA = a.Size * a.Size;
                        double massB = b.Size * b.Size;
                        double impulse = 2 * dvn / (massA + massB) * CollisionElasticity;
                        
                        a.VelocityX -= impulse * massB * nx;
                        a.VelocityY -= impulse * massB * ny;
                        b.VelocityX += impulse * massA * nx;
                        b.VelocityY += impulse * massA * ny;
                    }
                    
                    // Separate overlapping objects
                    double overlap = minDist - distance;
                    a.X -= (float)(overlap * nx * 0.5);
                    a.Y -= (float)(overlap * ny * 0.5);
                    b.X += (float)(overlap * nx * 0.5);
                    b.Y += (float)(overlap * ny * 0.5);
                }
            }
        }
    }

    private void EnforceObjectLimits()
    {
        // Hard cap: remove oldest
        while (_objects.Count > HardCap)
        {
            _objects.RemoveAt(0);
        }
        
        // Soft cap: remove slow objects
        if (_objects.Count > SoftCap)
        {
            var slowObjects = _objects
                .Where(o => Math.Sqrt(o.VelocityX * o.VelocityX + o.VelocityY * o.VelocityY) < SlowSpeedThreshold)
                .Take(_objects.Count - SoftCap)
                .ToList();
            
            foreach (var obj in slowObjects)
            {
                _objects.Remove(obj);
            }
        }
    }

    public void OnTouch(float x, float y, TouchType touchType)
    {
        switch (touchType)
        {
            case TouchType.Tap:
            case TouchType.Press:
                CreateTouchBurst(x, y);
                break;
                
            case TouchType.Move:
            case TouchType.Drag:
                AddTrail(x, y);
                break;
                
            case TouchType.Release:
                // Optional: could add release effect
                break;
        }
    }

    public void CreateVortex(float x, float y)
    {
        _vortexCenter = new PointF(x, y);
        _vortexTimeRemaining = VortexDuration;
    }

    private void CreateTouchBurst(float x, float y)
    {
        // Create multiple objects
        for (int i = 0; i < ObjectsPerTouch; i++)
        {
            float objX = x + (float)(_random.NextDouble() * 100 - 50);
            float objY = y + (float)(_random.NextDouble() * 100 - 50);
            
            var obj = new PhysicsObject
            {
                X = objX,
                Y = objY,
                Size = 15 + (float)_random.NextDouble() * 25,
                Color = _colors[_random.Next(_colors.Length)],
                Shape = (ShapeType)_random.Next(5),
                VelocityX = (_random.NextDouble() - 0.5) * 10,
                VelocityY = (_random.NextDouble() - 0.5) * 10
            };
            
            _objects.Add(obj);
        }
        
        // Push nearby objects away
        foreach (var obj in _objects)
        {
            double dx = obj.X - x;
            double dy = obj.Y - y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance < SpawnForceRadius && distance > 1)
            {
                double force = SpawnForce * (1 - distance / SpawnForceRadius);
                obj.VelocityX += dx / distance * force;
                obj.VelocityY += dy / distance * force;
            }
        }
        
        // Add visual effects
        _effects.Add(new VisualEffect
        {
            X = x,
            Y = y,
            Size = 10,
            Color = _colors[_random.Next(_colors.Length)],
            Type = EffectType.ExpandingRing
        });
        
        _effects.Add(new VisualEffect
        {
            X = x,
            Y = y,
            Size = 20,
            Color = _colors[_random.Next(_colors.Length)],
            Type = EffectType.Burst
        });
    }

    private void AddTrail(float x, float y)
    {
        _trails.Add(new TouchTrail
        {
            X = x,
            Y = y,
            Size = 8 + (float)_random.NextDouble() * 8,
            Color = _colors[_random.Next(_colors.Length)],
            Alpha = 0.8
        });
    }
}

public class PhysicsObject
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Size { get; set; }
    public Color Color { get; set; } = Colors.White;
    public ShapeType Shape { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
}

public class VisualEffect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Size { get; set; }
    public Color Color { get; set; } = Colors.White;
    public EffectType Type { get; set; }
    public double Alpha { get; set; } = 1.0;
    
    public void Update()
    {
        switch (Type)
        {
            case EffectType.ExpandingRing:
                Size += 8;
                Alpha -= 0.03;
                break;
            case EffectType.Particle:
                Alpha -= 0.05;
                break;
            case EffectType.Burst:
                Size += 5;
                Alpha -= 0.04;
                break;
        }
    }
}

public class TouchTrail
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Size { get; set; }
    public Color Color { get; set; } = Colors.White;
    public double Alpha { get; set; } = 1.0;
}

public enum ShapeType
{
    Circle,
    Square,
    Star,
    Heart,
    Triangle
}

public enum EffectType
{
    ExpandingRing,
    Particle,
    Burst
}
