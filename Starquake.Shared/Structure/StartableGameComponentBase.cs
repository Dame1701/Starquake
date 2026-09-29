// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Content;
using StarquakeXNA.Engines;

namespace StarquakeXNA.Structure;

public class StartableGameComponentBase
{
    protected ContentManager ContentManagerInstance;
    protected GameEngine GameEngineInstance;

    public StartableGameComponentBase()
    {
        Status = GameEngineStatus.NotStarted;
    }

    public StartableGameComponentBase(GameEngine gameEngineInstance)
    {
        GameEngineInstance = gameEngineInstance;
        ContentManagerInstance = gameEngineInstance.ContentMgr;
        Status = GameEngineStatus.NotStarted;
    }

    public GameEngineStatus Status { get; protected set; }

    public string StatusMessage { get; protected set; }

    public int PercentComplete { get; protected set; }
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