// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
#nullable enable
using Starquake.Engines;

namespace Starquake.Structure;

public class StartableGameComponentBase
{
    protected StartableGameComponentBase()
    {
        Status = GameEngineStatus.NotStarted;
    }

    public GameEngineStatus Status { get; protected set; }

    protected int PercentComplete { get; set; }
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