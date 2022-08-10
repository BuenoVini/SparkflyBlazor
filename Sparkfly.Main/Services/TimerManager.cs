namespace Sparkfly.Main.Services;

public class TimerManager
{
    public delegate void TimeElapsedEventHandler(object source, EventArgs args);
    public event TimeElapsedEventHandler? TimeElapsed;
    //public event EventHandler? TimeElapsed;

    private Timer? _loopTimer;
    public bool HasStarted { get; private set; } = false;

    public void Start(int seconds)
    {
        if (_loopTimer is null)
        {
            _loopTimer = new Timer(_ => OnTimerElapsed(), null, TimeSpan.Zero, TimeSpan.FromSeconds(seconds));
            HasStarted = true;
        }
    }

    public void Stop()
    {
        _loopTimer?.Dispose();
        HasStarted = false;
    }

    protected virtual void OnTimerElapsed() => TimeElapsed?.Invoke(this, EventArgs.Empty);
    //{
    //    if (TimeElapsed is not null)
    //        TimeElapsed(this, EventArgs.Empty);
    //}
}
