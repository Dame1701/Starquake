using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using System.Threading;

namespace StarquakeXNA
{
    public class StartableGameComponentBase
    {
        public event RaiseEngineComponentStatusChanged OnGameComponentStatusChanged;
        protected GameEngine gameEngineInstance;
        protected ContentManager contentManagerInstance;

        protected enGameComponentStatus status;
        public enGameComponentStatus Status
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
            status = enGameComponentStatus.NotStarted;
        }

        public StartableGameComponentBase(GameEngine GameEngineInstance)
        {
            gameEngineInstance = GameEngineInstance;
            contentManagerInstance = GameEngineInstance.ContentMgr;
            status = enGameComponentStatus.NotStarted;
        }

        protected void UpdateStatus(string StatusMessage, int PercentComplete, enGameComponentStatus Status)
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
}
