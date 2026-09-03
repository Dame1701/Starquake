using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Structure;

public class StartableGameComponentBase
{
    public event RaiseEngineComponentStatusChanged OnGameComponentStatusChanged;
    protected GameEngine gameEngineInstance;
    protected ContentManager contentManagerInstance;

    protected GameEngineStatus status;

    public GameEngineStatus Status
    {
        get { return status; }
    }

    protected string statusMessage;

    public string StatusMessage
    {
        get { return statusMessage; }
    }

    protected int percentComplete;

    public int PercentComplete
    {
        get { return percentComplete; }
    }

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