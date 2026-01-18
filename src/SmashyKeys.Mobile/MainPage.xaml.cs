namespace SmashyKeys.Mobile;

public partial class MainPage : ContentPage
{
    private readonly GameDrawable _gameDrawable;
    private readonly SoundManager _soundManager;
    private IDispatcherTimer? _gameTimer;
    
    // Secret exit: 5 rapid taps in top-left corner
    private const int ExitTapsRequired = 5;
    private const double ExitTapTimeWindow = 2.0; // seconds
    private const double ExitTapCornerSize = 80;  // pixels from top-left
    private readonly List<DateTime> _exitTaps = new();
    
    public MainPage()
    {
        InitializeComponent();
        
        _soundManager = new SoundManager();
        _gameDrawable = new GameDrawable();
        
        GameView.Drawable = _gameDrawable;
        
        // Set up touch handlers
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnTapped;
        GameView.GestureRecognizers.Add(tapGesture);
        
        // Use pointer gesture for continuous touch tracking
        var pointerGesture = new PointerGestureRecognizer();
        pointerGesture.PointerPressed += OnPointerPressed;
        pointerGesture.PointerMoved += OnPointerMoved;
        pointerGesture.PointerReleased += OnPointerReleased;
        GameView.GestureRecognizers.Add(pointerGesture);
        
        // Pan gesture for multi-touch drag
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GameView.GestureRecognizers.Add(panGesture);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Start game loop
        _gameTimer = Dispatcher.CreateTimer();
        _gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        _gameTimer.Tick += OnGameTick;
        _gameTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _gameTimer?.Stop();
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        _gameDrawable.Update();
        GameView.Invalidate();
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        var position = e.GetPosition(GameView);
        if (position == null) return;
        
        var x = (float)position.Value.X;
        var y = (float)position.Value.Y;
        
        // Check for secret exit tap (top-left corner)
        if (x < ExitTapCornerSize && y < ExitTapCornerSize)
        {
            CheckSecretExit();
        }
        else
        {
            // Normal tap - create effects
            _gameDrawable.OnTouch(x, y, TouchType.Tap);
            _soundManager.PlayRandomSound();
        }
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(GameView);
        if (position == null) return;
        
        _gameDrawable.OnTouch((float)position.Value.X, (float)position.Value.Y, TouchType.Press);
        _soundManager.PlayRandomSound();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(GameView);
        if (position == null) return;
        
        _gameDrawable.OnTouch((float)position.Value.X, (float)position.Value.Y, TouchType.Move);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(GameView);
        if (position == null) return;
        
        _gameDrawable.OnTouch((float)position.Value.X, (float)position.Value.Y, TouchType.Release);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Running)
        {
            // Pan creates trail effect
            var centerX = (float)(GameView.Width / 2 + e.TotalX);
            var centerY = (float)(GameView.Height / 2 + e.TotalY);
            _gameDrawable.OnTouch(centerX, centerY, TouchType.Drag);
        }
    }

    private void CheckSecretExit()
    {
        var now = DateTime.Now;
        
        // Remove old taps outside the time window
        _exitTaps.RemoveAll(t => (now - t).TotalSeconds > ExitTapTimeWindow);
        
        // Add this tap
        _exitTaps.Add(now);
        
        // Check if we have enough taps
        if (_exitTaps.Count >= ExitTapsRequired)
        {
            // Secret exit triggered!
            _exitTaps.Clear();
            
#if ANDROID
            // Exit the app
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#endif
        }
    }

    // Override back button - do nothing (toddler-proof)
    protected override bool OnBackButtonPressed()
    {
        // Don't allow back button to exit
        return true;
    }
}

public enum TouchType
{
    Tap,
    Press,
    Move,
    Release,
    Drag
}
