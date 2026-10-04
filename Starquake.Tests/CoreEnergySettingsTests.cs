// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Display;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The arithmetic behind the core room's energy: which elements a beam joins, where those
///     elements are, and how far the funnel has turned. The funnel itself can only be judged by
///     looking at it.
/// </summary>
public class CoreEnergySettingsTests
{
    /// <summary>
    ///     A beam from an element to itself has no length and would draw as a blot sitting on it.
    /// </summary>
    [Fact]
    public void BeamEndsAt_OverALongRun_NeverJoinsAnElementToItself()
    {
        // Act / Assert
        for (int frame = 0; frame < 2000; frame++)
        {
            for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
            {
                (int from, int to) = CoreEnergySettings.BeamEndsAt(frame, beam);

                Assert.NotEqual(from, to);
            }
        }
    }

    [Fact]
    public void BeamEndsAt_OverALongRun_StaysWithinTheElementsThereAre()
    {
        // Act / Assert
        for (int frame = 0; frame < 2000; frame++)
        {
            for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
            {
                (int from, int to) = CoreEnergySettings.BeamEndsAt(frame, beam);

                Assert.InRange(from, 0, CoreEnergySettings.ElementCount - 1);
                Assert.InRange(to, 0, CoreEnergySettings.ElementCount - 1);
            }
        }
    }

    /// <summary>
    ///     Every element has to take its turn, or the beams would crawl over the same corner of the
    ///     core and leave the rest of it as dead as it was before.
    /// </summary>
    [Fact]
    public void BeamEndsAt_OverALongRun_ReachesEveryElement()
    {
        // Act
        HashSet<int> reached = [];

        for (int frame = 0; frame < 600; frame++)
        {
            for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
            {
                (int from, int to) = CoreEnergySettings.BeamEndsAt(frame, beam);

                reached.Add(from);
                reached.Add(to);
            }
        }

        // Assert
        Assert.Equal(CoreEnergySettings.ElementCount, reached.Count);
    }

    /// <summary>
    ///     A beam holds between the same two for a few frames and then jumps. Changing every frame
    ///     would read as a flicker rather than as electricity crawling about.
    /// </summary>
    [Fact]
    public void BeamEndsAt_WithinOneHold_StaysWhereItIs()
    {
        // Act
        (int from, int to) = CoreEnergySettings.BeamEndsAt(0, 0);

        // Assert
        for (int frame = 1; frame < CoreEnergySettings.BeamHoldFrames; frame++)
        {
            Assert.Equal((from, to), CoreEnergySettings.BeamEndsAt(frame, 0));
        }

        Assert.NotEqual((from, to),
            CoreEnergySettings.BeamEndsAt(CoreEnergySettings.BeamHoldFrames, 0));
    }

    /// <summary>
    ///     The three beams must not all join the same pair, or they would draw on top of one
    ///     another and read as one.
    /// </summary>
    [Fact]
    public void BeamEndsAt_AcrossTheBeams_JoinsDifferentPairs()
    {
        // Act
        HashSet<(int, int)> pairs = [];

        for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
        {
            pairs.Add(CoreEnergySettings.BeamEndsAt(0, beam));
        }

        // Assert
        Assert.Equal(CoreEnergySettings.BeamCount, pairs.Count);
    }

    [Fact]
    public void ElementCentre_TheFirstElement_IsHalfASpriteInFromTheCorner()
    {
        // Act
        Point centre = CoreEnergySettings.ElementCentre(new Point(416, 192), 0);

        // Assert
        Assert.Equal(new Point(448, 224), centre);
    }

    /// <summary>
    ///     The nine sit on a three by three grid, so the last of them is two sprites along and two
    ///     down from the first.
    /// </summary>
    [Fact]
    public void ElementCentre_TheLastElement_IsAcrossAndDownFromTheFirst()
    {
        // Act
        Point first = CoreEnergySettings.ElementCentre(new Point(416, 192), 0);
        Point last = CoreEnergySettings.ElementCentre(
            new Point(416, 192), CoreEnergySettings.ElementCount - 1);

        // Assert
        Assert.Equal(first.X + 128, last.X);
        Assert.Equal(first.Y + 128, last.Y);
    }

    [Fact]
    public void TurnedBy_OverSuccessiveFrames_KeepsTurningTheSameWay()
    {
        // Act / Assert
        for (int frame = 1; frame < 200; frame++)
        {
            Assert.True(
                CoreEnergySettings.TurnedBy(frame) > CoreEnergySettings.TurnedBy(frame - 1),
                $"the funnel stopped turning at frame {frame}");
        }
    }

    /// <summary>
    ///     The particles are thrown from a point going round the middle, not from the middle, which
    ///     is what makes them spiral instead of spraying evenly outward.
    /// </summary>
    [Fact]
    public void EmitterAt_OverSuccessiveFrames_GoesRoundTheMiddleRatherThanSittingOnIt()
    {
        // Act
        List<Point> emitters = [];

        for (int frame = 0; frame < 60; frame++)
        {
            emitters.Add(CoreEnergySettings.EmitterAt(CoreEnergySettings.TurnedBy(frame), 512, 288));
        }

        // Assert - it moves, and it keeps its distance rather than wandering off or spiralling in
        Assert.True(emitters.Distinct().Count() > 30, "the emitter barely moved");

        foreach (Point emitter in emitters)
        {
            double outFrom = Math.Sqrt(
                Math.Pow(emitter.X - 512, 2) + Math.Pow(emitter.Y - 288, 2));

            Assert.InRange(outFrom, 40, 48);
        }
    }

    /// <summary>
    ///     The particles have to go the same way round as the funnel over them, or the core reads
    ///     as two things spinning against one another rather than one body of energy turning.
    /// </summary>
    [Fact]
    public void EmitterAt_TurnsTheSameWayAsTheFunnel_AndALittleFaster()
    {
        // Act - where the emitter sits, as an angle, over a few frames
        List<double> angles = [];

        for (int frame = 0; frame < 8; frame++)
        {
            Point emitter = CoreEnergySettings.EmitterAt(CoreEnergySettings.TurnedBy(frame), 512, 288);
            angles.Add(Math.Atan2(emitter.Y - 288, emitter.X - 512));
        }

        // The funnel's arms hold where angle * Arms + Turned is constant, so the angle they sit at
        // falls as the turn climbs. The emitter's has to fall too, and faster.
        double funnelFellBy =
            (CoreEnergySettings.TurnedBy(1) - CoreEnergySettings.TurnedBy(0)) / CoreEnergySettings.Arms;

        double emitterFellBy = angles[0] - angles[1];

        // Assert
        Assert.True(emitterFellBy > 0, "the emitter turned against the funnel");
        Assert.True(emitterFellBy > funnelFellBy, "the emitter did not outrun the funnel");
    }

    /// <summary>
    ///     While a slot is being filled every beam has to end on it - that is the core gathering
    ///     on the part going in, and it is the whole of what the targeting is for.
    /// </summary>
    [Fact]
    public void BeamEndsAt_WhileASlotIsBeingFilled_EveryBeamArrivesAtIt()
    {
        // Act / Assert
        for (int frame = 0; frame < 300; frame++)
        {
            for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
            {
                (int from, int to) = CoreEnergySettings.BeamEndsAt(frame, beam, 4);

                Assert.Equal(4, to);
                Assert.NotEqual(4, from);
            }
        }
    }

    /// <summary>
    ///     The beam that was already starting at the slot is turned around rather than dropped, or
    ///     the three would sometimes collapse to two.
    /// </summary>
    [Fact]
    public void BeamEndsAt_WhenABeamAlreadyStartedAtTheSlot_KeepsItsOtherEnd()
    {
        // Arrange - find a frame and beam that starts at the slot of its own accord
        for (int frame = 0; frame < 500; frame++)
        {
            for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
            {
                (int wouldStartAt, int wouldEndAt) = CoreEnergySettings.BeamEndsAt(frame, beam);

                if (wouldStartAt != 2)
                {
                    continue;
                }

                // Act
                (int from, int to) = CoreEnergySettings.BeamEndsAt(frame, beam, 2);

                // Assert - turned about, so the end it had is the end it keeps
                Assert.Equal(wouldEndAt, from);
                Assert.Equal(2, to);

                return;
            }
        }

        Assert.Fail("no beam ever started at that slot, so the turnaround was never exercised");
    }

    [Fact]
    public void BeamEndsAt_WithNoSlotBeingFilled_IsLeftToWanderAsBefore()
    {
        // Act / Assert
        for (int frame = 0; frame < 200; frame++)
        {
            for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
            {
                Assert.Equal(
                    CoreEnergySettings.BeamEndsAt(frame, beam),
                    CoreEnergySettings.BeamEndsAt(frame, beam, null));
            }
        }
    }

    /// <summary>
    ///     The core leans into a replacement and settles out of it rather than snapping either
    ///     way, which is what a ramp buys over a flag.
    /// </summary>
    [Fact]
    public void WoundUpBy_AcrossTheWind_RunsFromRestToFullBearing()
    {
        // Assert
        Assert.Equal(0f, CoreEnergySettings.WoundUpBy(0));
        Assert.Equal(1f, CoreEnergySettings.WoundUpBy(CoreEnergySettings.WindFrames));
        Assert.InRange(CoreEnergySettings.WoundUpBy(CoreEnergySettings.WindFrames / 2), 0.4f, 0.6f);
    }

    /// <summary>
    ///     A wind that ran past one would push the funnel brighter than it was ever meant to go,
    ///     and a replacement that took longer than the wind-up is the ordinary case.
    /// </summary>
    [Theory]
    [InlineData(-5)]
    [InlineData(10000)]
    public void WoundUpBy_OutsideTheWind_IsHeldAtRestOrFullBearing(int frames)
        => Assert.InRange(CoreEnergySettings.WoundUpBy(frames), 0f, 1f);

    /// <summary>
    ///     The funnel has to be gone before the corners of the rectangle it is drawn over, or it
    ///     would show as a hard square edge against the room.
    /// </summary>
    [Fact]
    public void OuterEdge_IsInsideTheRectangleItIsDrawnOver()
        => Assert.InRange(CoreEnergySettings.OuterEdge, CoreEnergySettings.ThroatEdge + 0.01f, 0.5f);
}
