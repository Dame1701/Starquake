// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

internal class StartableGameComponentBase
{

    private volatile int _percentComplete;

    private volatile GameEngineStatus _status;
    protected StartableGameComponentBase()
    {
        Status = GameEngineStatus.NotStarted;
    }

    /// <summary>
    ///     How far through starting up this component is. Written by whichever thread is doing the
    ///     work and read by the one drawing the loading screen, so both are backed by volatile
    ///     fields: the reader polls every frame and must see a change promptly rather than whenever
    ///     the CPU happens to publish it.
    /// </summary>
    public GameEngineStatus Status
    {
        get => _status;
        protected set => _status = value;
    }

    /// <inheritdoc cref="Status" />
    protected int PercentComplete
    {
        get => _percentComplete;
        private set => _percentComplete = value;
    }

    public event RaiseEngineComponentStatusChanged? OnGameComponentStatusChanged;

    protected void UpdateStatus(string statusMessage, int percentComplete, GameEngineStatus status)
    {
        PercentComplete = percentComplete;
        Status = status;

        if (OnGameComponentStatusChanged != null)
        {
            OnGameComponentStatusChanged(statusMessage, percentComplete, status);
        }
    }
}
