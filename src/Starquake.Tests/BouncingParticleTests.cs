// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using ProjectMercury;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Particles;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     How Blob's six parts are thrown when he comes apart. They are the points of a star, thrown
///     evenly around him rather than all upward.
/// </summary>
public class BouncingParticleTests
{
    private const int Points = 6;

    private const int Speed = 24;

    /// <summary>
    ///     How far off a perfect star the rounding puts things. A velocity is whole pixels a frame,
    ///     so each axis can be half a pixel out, which at this speed is about 0.7 of a pixel of
    ///     length and 0.03 of a radian of direction. The star is as even as integers allow, and the
    ///     tests ask for no more than that.
    /// </summary>
    private const double Rounding = 0.08;

    [Fact]
    public void StarburstVelocity_AcrossThePoints_ThrowsThemEvenlyAroundTheCircle()
    {
        // Act
        List<double> angles = [];

        for (int point = 0; point < Points; point++)
        {
            Point thrown = BouncingParticle.StarburstVelocity(point, Points, Speed, 0);
            angles.Add(Math.Atan2(thrown.Y, thrown.X));
        }

        // Assert - each the same step round from the last, allowing for the wrap
        for (int point = 1; point < Points; point++)
        {
            double step = angles[point] - angles[point - 1];

            if (step < 0)
            {
                step += 2 * Math.PI;
            }

            Assert.InRange(step, 2 * Math.PI / Points - Rounding, 2 * Math.PI / Points + Rounding);
        }
    }

    /// <summary>
    ///     What makes it a burst rather than a fountain. Some parts go up and some go down.
    /// </summary>
    [Fact]
    public void StarburstVelocity_AcrossThePoints_ThrowsSomeUpAndSomeDown()
    {
        // Act
        List<Point> thrown = [];

        for (int point = 0; point < Points; point++)
        {
            thrown.Add(BouncingParticle.StarburstVelocity(point, Points, Speed, 0));
        }

        // Assert - y counts downward, so negative is up
        Assert.Contains(thrown, part => part.Y < 0);
        Assert.Contains(thrown, part => part.Y > 0);
        Assert.Contains(thrown, part => part.X < 0);
        Assert.Contains(thrown, part => part.X > 0);
    }

    /// <summary>Every part is thrown as hard as every other. The star has no short points.</summary>
    [Fact]
    public void StarburstVelocity_AcrossThePoints_ThrowsThemAllAtTheSameSpeed()
    {
        // Act / Assert
        for (int point = 0; point < Points; point++)
        {
            Point thrown = BouncingParticle.StarburstVelocity(point, Points, Speed, 0.7);

            Assert.InRange(
                Math.Sqrt(thrown.X * thrown.X + thrown.Y * thrown.Y), Speed - 1, Speed + 1);
        }
    }

    /// <summary>
    ///     Turning the whole star turns every point of it by the same amount, so the parts stay
    ///     evenly spread however the burst is oriented.
    /// </summary>
    [Fact]
    public void StarburstVelocity_WithTheStarTurned_KeepsThePointsEvenlySpread()
    {
        // Act
        List<double> angles = [];

        for (int point = 0; point < Points; point++)
        {
            Point thrown = BouncingParticle.StarburstVelocity(point, Points, Speed, 1.9);
            angles.Add(Math.Atan2(thrown.Y, thrown.X));
        }

        // Assert
        for (int point = 1; point < Points; point++)
        {
            double step = angles[point] - angles[point - 1];

            if (step < 0)
            {
                step += 2 * Math.PI;
            }

            Assert.InRange(step, 2 * Math.PI / Points - Rounding, 2 * Math.PI / Points + Rounding);
        }
    }

    /// <summary>
    ///     The sideways speed is the one thing nothing afterwards ever changes - no gravity acts
    ///     along it and nothing damps it - so a part thrown with none of it has none for the whole
    ///     of its life: straight up, straight back down the same line, and bouncing on that spot
    ///     until its time runs out. The ship crash is where that showed, because it throws the
    ///     parts without drawing them and all that is seen is the fire each one trails.
    /// </summary>
    /// <remarks>
    ///     Every turn of the star rather than a handful: a point lands near enough to the vertical
    ///     to round away about one crash in twenty, so a test of three spins would have passed on
    ///     the code this was written against.
    /// </remarks>
    [Fact]
    public void StarburstVelocity_HoweverTheStarIsTurned_ThrowsNoPartStraightUpOrStraightDown()
    {
        // Act / Assert - a tenth of a degree at a time, all the way round
        for (int tenths = 0; tenths < 3600; tenths++)
        {
            double spin = tenths * Math.PI / 1800;

            for (int point = 0; point < Points; point++)
            {
                Assert.NotEqual(0, BouncingParticle.StarburstVelocity(point, Points, Speed, spin).X);
            }
        }
    }

    /// <summary>
    ///     And it is thrown the way it was aimed. Rounding a cosine of nearly nothing up to one
    ///     whole pixel must not throw a part that was aimed leftwards off to the right.
    /// </summary>
    [Theory]
    [InlineData(0.5 * Math.PI - 0.001, 1)]
    [InlineData(0.5 * Math.PI + 0.001, -1)]
    public void StarburstVelocity_AimedWithinADegreeOfTheVertical_KeepsTheSideItWasAimedAt(
        double spin, int sideways)
        => Assert.Equal(sideways, BouncingParticle.StarburstVelocity(0, Points, Speed, spin).X);

    /// <summary>
    ///     A star of one point would divide by nought working out the step between them.
    /// </summary>
    [Fact]
    public void StarburstVelocity_WithNoPointsAtAll_IsStillThrownSomewhere()
    {
        // Act
        Point thrown = BouncingParticle.StarburstVelocity(0, 0, Speed, 0);

        // Assert
        Assert.InRange(Math.Sqrt(thrown.X * thrown.X + thrown.Y * thrown.Y), Speed - 1, Speed + 1);
    }

    /// <summary>
    ///     Where a thrown part is allowed to be. Everything above is the arithmetic of the throw;
    ///     these walk whole flights a frame at a time, because the one death that showed this up had
    ///     the parts and their fire sitting inside a tile in the corner of the screen, clanging,
    ///     until the death ran out.
    /// </summary>
    /// <remarks>
    ///     A part is only ever moved by a distance the scenery or the screen's edge has agreed to.
    ///     The edge used to be checked after the step rather than before it, and the part put back
    ///     from beyond it afterwards, which both let a part out of the screen and wrote a position
    ///     nothing had been asked about. A part overlapping a solid cell is blocked in every
    ///     direction at once, so one landing in the floor or the wall along an edge stayed there.
    /// </remarks>
    public class WhereTheThrownPartsGo
    {
        /// <summary>Long enough for a part to cross the screen and come back several times.</summary>
        private const int Frames = 300;

        private const int PartSize = TileMetrics.CollisionCellWidth;

        private const int Across = TileMetrics.PlayAreaWidth;

        private const int Down = TileMetrics.PlayAreaHeight;

        /// <summary>
        ///     Thrown all the way round rather than at a handful of angles, because which way a part
        ///     goes is drawn fresh every death and it only takes one of them to end up somewhere it
        ///     cannot get out of.
        /// </summary>
        private static IEnumerable<double> EveryWayRound()
        {
            for (int degrees = 0; degrees < 360; degrees += 5)
            {
                yield return degrees * Math.PI / 180;
            }
        }

        [Fact]
        public void Update_ThrownEveryWayRoundAnOpenScreen_NeverLeavesThePlayfield()
        {
            // Arrange - nothing but the edges to stop it, so the edges are all that is being tested
            var surroundings = new Surroundings();

            // Act / Assert
            foreach (double spin in EveryWayRound())
            {
                BouncingParticle part = Thrown(surroundings, spin);

                for (int frame = 0; frame < Frames; frame++)
                {
                    part.Update(new GameTime());

                    Assert.InRange(part.XPos, 0, Across - PartSize);
                    Assert.InRange(part.YPos, 0, Down - PartSize);
                }
            }
        }

        [Fact]
        public void Update_ThrownEveryWayRoundAFloorWithAGapInIt_NeverComesToRestInsideTheScenery()
        {
            // Arrange - a floor along the bottom with a screen exit in it, and a wall up the right
            // hand side with another, which is the way a part gets past an edge at all
            var surroundings = new Surroundings
            {
                Solid =
                [
                    new Rectangle(0, Down - PartSize, 640, PartSize),
                    new Rectangle(800, Down - PartSize, Across - 800, PartSize),
                    new Rectangle(Across - PartSize, 0, PartSize, 288),
                    new Rectangle(Across - PartSize, 384, PartSize, Down - 384)
                ]
            };

            // Act / Assert
            foreach (double spin in EveryWayRound())
            {
                BouncingParticle part = Thrown(surroundings, spin);

                for (int frame = 0; frame < Frames; frame++)
                {
                    part.Update(new GameTime());

                    Assert.False(
                        surroundings.Hits(new Rectangle(part.XPos, part.YPos, PartSize, PartSize)),
                        $"Thrown at {spin:F2}, the part was inside scenery at {part.XPos},{part.YPos}");
                }
            }
        }

        /// <summary>
        ///     The scenery and the edge used to be able to turn the same part around twice in one
        ///     frame, which is no turn at all: it carried on into whatever had just stopped it, and
        ///     clanged twice for it.
        /// </summary>
        [Fact]
        public void Update_StoppedByTheSceneryAndTheEdgeAtOnce_TurnsAroundOnceOnEachAxis()
        {
            // Arrange - the floor and the wall are the edges themselves, so a part reaching either
            // is stopped by both at once
            var surroundings = new Surroundings
            {
                Solid =
                [
                    new Rectangle(0, Down - PartSize, Across, PartSize),
                    new Rectangle(Across - PartSize, 0, PartSize, Down)
                ]
            };

            // Act / Assert
            foreach (double spin in EveryWayRound())
            {
                BouncingParticle part = Thrown(surroundings, spin);

                for (int frame = 0; frame < Frames; frame++)
                {
                    int clangsBefore = surroundings.Clangs;

                    part.Update(new GameTime());

                    Assert.InRange(surroundings.Clangs - clangsBefore, 0, 2);
                }
            }
        }

        /// <summary>
        ///     Being stopped on one axis says nothing about the other, so a part turned around by the
        ///     floor still takes its sideways step that frame.
        /// </summary>
        [Fact]
        public void Update_StoppedByTheFloor_KeepsItsSidewaysStepThatFrame()
        {
            // Arrange - a floor across the bottom, and a part thrown almost straight down into it so
            // it stays well away from the side edges and nothing but the floor ever stops it
            var surroundings = new Surroundings
            {
                Solid = [new Rectangle(0, Down - 2 * PartSize, Across, 2 * PartSize)]
            };

            BouncingParticle part = ThrownFrom(surroundings, Across / 2, PartSize, 0.5 * Math.PI - 0.05);

            int timesStopped = 0;

            // Act / Assert - a part never comes to rest, it bounces for as long as it lives, so this
            // watches the frames the floor stops it on rather than waiting for it to settle
            for (int frame = 0; frame < Frames; frame++)
            {
                int wasAt = part.YPos;
                int startedFrom = part.XPos;

                part.Update(new GameTime());

                if (part.YPos != wasAt)
                {
                    continue;
                }

                timesStopped++;

                Assert.NotEqual(startedFrom, part.XPos);
            }

            Assert.True(timesStopped > 0, "The part never met the floor at all");
        }

        /// <summary>
        ///     One part thrown at a chosen angle, rather than the six points of the star, so a test
        ///     can aim it. The life is longer than any of these tests run for, because a part that
        ///     has finished is no longer stepped at all and would hide anything it had got into.
        /// </summary>
        private static BouncingParticle Thrown(Surroundings surroundings, double spin)
            => ThrownFrom(surroundings, Across / 2, Down / 2, spin);

        /// <inheritdoc cref="Thrown" />
        private static BouncingParticle ThrownFrom(
            Surroundings surroundings, int xPos, int yPos, double spin)
        {
            var part = new BouncingParticle(surroundings, new Random(1), 0)
            {
                Fireball = new SilentEffect()
            };

            part.Trigger(xPos, yPos, 0, 1, spin, Frames * 2, Frames * 2 + 1);

            return part;
        }

        /// <summary>
        ///     A screen of solid rectangles, measured a pixel at a time. The game solves the whole
        ///     step at once off a baked grid; walking it here is slower and obviously right.
        /// </summary>
        private sealed class Surroundings : IFragmentSurroundings
        {
            public List<Rectangle> Solid { get; init; } = [];

            public int Clangs { get; private set; }

            public int ScreenWidth => Across;

            public int ScreenHeight => Down;

            public int DistanceClear(
                Direction direction, int x, int y, int width, int height, int distance)
            {
                (int alongX, int alongY) = direction switch
                {
                    Direction.Up => (0, -1),
                    Direction.Down => (0, 1),
                    Direction.Left => (-1, 0),
                    _ => (1, 0)
                };

                for (int step = 1; step <= distance; step++)
                {
                    if (Hits(new Rectangle(
                            x + alongX * step, y + alongY * step, width, height)))
                    {
                        return step - 1;
                    }
                }

                return distance;
            }

            public void ThrowSparks(int x, int y)
            {
            }

            public void Clang() => Clangs++;

            public bool Hits(Rectangle box)
                => Solid.Exists(solid => solid.Intersects(box));
        }

        /// <summary>The fire a part trails, which has nothing to say about where the part goes.</summary>
        private sealed class SilentEffect : IStandardParticleEffect
        {
            public ParticleEffect Effect => new();

            public bool EffectActive => false;

            public event RaiseEffectComplete? ParticleEffectComplete
            {
                add { }
                remove { }
            }

            public void Start()
            {
            }

            public void Update(int xPos, int yPos, GameTime gameTime)
            {
            }

            public void Stop()
            {
            }

            public void Reset()
            {
            }

            public void Draw()
            {
            }

            public void Draw(int screenFade)
            {
            }
        }
    }
}
