// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Content;
using Starquake.Engines;

namespace Starquake.Structure;

public class StartableGameComponentBase
{
    protected readonly ContentManager ContentManagerInstance;
    protected readonly GameEngine GameEngineInstance;

    protected StartableGameComponentBase()
    {
        Status = GameEngineStatus.NotStarted;
    }

    protected StartableGameComponentBase(GameEngine gameEngineInstance)
    {
        GameEngineInstance = gameEngineInstance;
        ContentManagerInstance = gameEngineInstance.ContentMgr;
        Status = GameEngineStatus.NotStarted;
    }

    public GameEngineStatus Status { get; protected set; }

    public string StatusMessage { get; protected set; }

    protected int PercentComplete { get; set; }
    public event RaiseEngineComponentStatusChanged OnGameComponentStatusChanged;

    protected void UpdateStatus(string statusMessage, int percentComplete, GameEngineStatus status)
    {
        StatusMessage = statusMessage;
        PercentComplete = percentComplete;
        Status = status;

        if (OnGameComponentStatusChanged != null)
        {
            OnGameComponentStatusChanged(statusMessage, percentComplete, status);
        }
    }
}