// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Content;

namespace StarquakeXNA.Structure;

public class StartableGameComponentBase
{
    protected ContentManager contentManagerInstance;
    protected GameEngine gameEngineInstance;

    protected int percentComplete;

    protected GameEngineStatus status;

    protected string statusMessage;

    public StartableGameComponentBase()
    {
        status = GameEngineStatus.NotStarted;
    }

    public StartableGameComponentBase(GameEngine GameEngineInstance)
    {
        gameEngineInstance = GameEngineInstance;
        contentManagerInstance = GameEngineInstance.ContentMgr;
        status = GameEngineStatus.NotStarted;
    }

    public GameEngineStatus Status => status;

    public string StatusMessage => statusMessage;

    public int PercentComplete => percentComplete;
    public event RaiseEngineComponentStatusChanged OnGameComponentStatusChanged;

    protected void UpdateStatus(string StatusMessage, int PercentComplete, GameEngineStatus Status)
    {
        statusMessage = StatusMessage;
        percentComplete = PercentComplete;
        status = Status;

        if (OnGameComponentStatusChanged != null)
        {
            OnGameComponentStatusChanged(StatusMessage, PercentComplete, Status);
        }
    }
}