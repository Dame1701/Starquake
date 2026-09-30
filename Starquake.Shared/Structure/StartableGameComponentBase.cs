// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

public class StartableGameComponentBase
{
    protected StartableGameComponentBase()
    {
        Status = GameEngineStatus.NotStarted;
    }

    /// <summary>
    ///     How far through starting up this component is. Written by whichever thread is doing the
    ///     work and read by the one drawing the loading screen, so both are backed by volatile
    ///     fields - the reader polls these every frame and needs to see a change promptly rather
    ///     than whenever the CPU happens to publish it.
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
        set => _percentComplete = value;
    }
    public event RaiseEngineComponentStatusChanged? OnGameComponentStatusChanged;

    private volatile int _percentComplete;

    private volatile GameEngineStatus _status;

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
