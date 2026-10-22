// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace ProjectMercury.Controllers;

/// <summary>
///     A single event on the timeline.
/// </summary>
public struct TimelineEvent
{
    /// <summary>
    ///     The time offset of the event in whole and fractional seconds.
    /// </summary>
    public float TimeOffset;

    /// <summary>
    ///     The name of the emitter to trigger at the specified time offset.
    /// </summary>
    public string EmitterName;

    /// <summary>
    ///     The position of the trigger.
    /// </summary>
    public Vector2 TriggerPosition;
}

/// <summary>
///     Defines a simple controller which triggers emitters based on a timeline.
/// </summary>
public sealed class TimelineController : Controller
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="TimelineController" /> class.
    /// </summary>
    public TimelineController()
    {
        Timeline = new List<TimelineEvent>();
        EventQueue = new List<TimelineEvent>();
    }
    /// <summary>
    ///     Counts the total seconds.
    /// </summary>
    public float TotalSeconds { get; private set; }

    /// <summary>
    ///     Gets the timeline for a single trigger.
    /// </summary>
    public List<TimelineEvent> Timeline { get; }

    /// <summary>
    ///     Gets or sets the queue of timeline events which are due to be processed.
    /// </summary>
    private List<TimelineEvent> EventQueue { get; }

    /// <summary>
    ///     Called by the particle effect when it is triggered.
    /// </summary>
    /// <param name="position">The desired position of the trigger.</param>
    /// <remarks>
    ///     This method should not be called directly, the ParticleEffect class
    ///     will defer control to this method when the controller is assigned.
    /// </remarks>
    internal protected override void Trigger(ref Vector2 position)
    {
        foreach (TimelineEvent timelineEvent in Timeline)
        {
            EventQueue.Add(new TimelineEvent
            {
                EmitterName = timelineEvent.EmitterName,
                TimeOffset = timelineEvent.TimeOffset + TotalSeconds,
                TriggerPosition = position
            });
        }
    }

    /// <summary>
    ///     Called by the particle effect when it is updated.
    /// </summary>
    /// <param name="deltaSeconds">Elapsed time in whole and fractional seconds.</param>
    /// <remarks>
    ///     This method should not be called directly, the ParticleEffect class
    ///     will defer control to this method when the controller is assigned.
    /// </remarks>
    internal protected override void Update(float deltaSeconds)
    {
        TotalSeconds += deltaSeconds;

        if (EventQueue.Count > 0)
        {
            for (int i = EventQueue.Count - 1; i >= 0; i--)
            {
                TimelineEvent timelineEvent = EventQueue[i];

                if (timelineEvent.TimeOffset <= TotalSeconds)
                {
                    ParticleEffect[timelineEvent.EmitterName].Trigger(ref timelineEvent.TriggerPosition);

                    EventQueue.RemoveAt(i);
                }
            }
        }

        base.Update(deltaSeconds);
    }
}