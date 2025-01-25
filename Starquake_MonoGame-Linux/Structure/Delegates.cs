using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarquakeXNA
{
    public delegate void RaiseEngineComponentStatusChanged(string Message, int PercentComplete, enGameComponentStatus Status);
    public delegate void RaiseInteractionWithBlob(enNastyInterationType InteractionType, enNastyType NastyType);
    public delegate void RaiseEffectComplete();
}
