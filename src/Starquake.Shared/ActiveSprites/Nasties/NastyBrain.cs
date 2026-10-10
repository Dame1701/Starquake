// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     How a nasty decides where to go, and the going. Fourteen behaviours - bouncing, scuttling,
///     chasing, the spiky nasty's lunge - and the frame of mind that picks between them.
/// </summary>
/// <remarks>
///     A nasty keeps one frame of mind for a few hundred frames at a time: whether it is chasing Blob
///     or wandering, which of the six movement types it uses, which way it set off and how fast.
///     <see cref="Step" /> is one frame of that: reconsider, then move.
///     <para>
///         Needs the collision map and a <see cref="Random" /> but no graphics, so getting stuck is
///         reproducible in a test. The counter that notices it and the release that fixes it are both
///         here, and both exist because it happens.
///     </para>
/// </remarks>
internal class NastyBrain
{
    /// <summary>
    ///     How long a nasty may sit at the same position while chasing before it gives up. A quarter
    ///     of a second without moving means something is in the way it cannot get round.
    /// </summary>
    private const int StuckFrames = 15;

    /// <summary>
    ///     How long a nasty keeps one frame of mind before picking another. Chasing lasts longer than
    ///     wandering, so the two ranges differ.
    /// </summary>
    private const int WanderingShortest = 200;

    /// <inheritdoc cref="WanderingShortest" />
    private const int WanderingLongest = 500;

    /// <inheritdoc cref="WanderingShortest" />
    private const int ChasingShortest = 400;

    /// <inheritdoc cref="WanderingShortest" />
    private const int ChasingLongest = 700;

    /// <summary>
    ///     A newly born nasty crawls at one pixel a frame for this long before picking up speed, so
    ///     it can be seen coming.
    /// </summary>
    private const int FirstMovementDuration = 90;

    /// <inheritdoc cref="FirstMovementDuration" />
    private const int FirstMovementSpeed = 1;

    /// <summary>
    ///     The core guardian's fixed speed. Alone among the nasties, it never picks another.
    /// </summary>
    private const int CoreGuardianSpeed = 8;

    /// <summary>
    ///     How fast a nasty moves. A wandering one ambles through a random walk and strides at
    ///     anything else; a chasing one does the opposite, picking its way on a random walk and
    ///     charging on the direct movements. Each pair is the range a speed is drawn from.
    /// </summary>
    private const int WanderingAmbleSpeed = 2;

    /// <inheritdoc cref="WanderingAmbleSpeed" />
    private const int WanderingAmbleSpeedLimit = 4;

    /// <inheritdoc cref="WanderingAmbleSpeed" />
    private const int WanderingStrideSpeed = 6;

    /// <inheritdoc cref="WanderingAmbleSpeed" />
    private const int WanderingStrideSpeedLimit = 8;

    /// <inheritdoc cref="WanderingAmbleSpeed" />
    private const int ChasingPickingSpeed = 4;

    /// <inheritdoc cref="WanderingAmbleSpeed" />
    private const int ChasingPickingSpeedLimit = 6;

    /// <inheritdoc cref="WanderingAmbleSpeed" />
    private const int ChasingChargeSpeed = 8;

    /// <inheritdoc cref="WanderingAmbleSpeed" />
    private const int ChasingChargeSpeedLimit = 10;

    /// <summary>
    ///     How far a nasty set to bounce in steps travels before choosing again. Shorter than the
    ///     other runs, which is what makes it step rather than bounce.
    /// </summary>
    private const int ShortestSteppedRun = 8;

    /// <inheritdoc cref="ShortestSteppedRun" />
    private const int LongestSteppedRun = 16;

    /// <summary>
    ///     How far a nasty travels before choosing a new direction. The ranges differ by movement
    ///     type: a bouncing nasty commits to longer runs than a wandering one.
    /// </summary>
    private const int ShortestRun = 8;

    /// <inheritdoc cref="ShortestRun" />
    private const int ShortestChasingRun = 10;

    /// <inheritdoc cref="ShortestRun" />
    private const int LongestRun = 40;

    /// <inheritdoc cref="ShortestRun" />
    private const int ShortestBouncingRun = 40;

    /// <inheritdoc cref="ShortestRun" />
    private const int LongestBouncingRun = 80;

    /// <summary>
    ///     The shortest runs of all, which makes a nasty on
    ///     <see cref="MoveType.RandomMovement" /> scuttle rather than stride.
    /// </summary>
    private const int ShortestScuttle = 1;

    /// <inheritdoc cref="ShortestScuttle" />
    private const int LongestScuttle = 4;

    /// <summary>
    ///     Added to the run a spiky nasty sets itself so that it goes a little past Blob. Without
    ///     it they get stuck on the scenery.
    /// </summary>
    private const int SpikeyOvershoot = 10;

    private readonly ScreenCollisionMap _collisions;
    private readonly Random _randomNumberGenerator;

    /// <summary>
    ///     State a nasty starts fresh with after a screen swap, which is why it lives outside
    ///     <see cref="NastyMovementState" />. Keeping the two apart is what makes the distinction
    ///     deliberate rather than an accident of which fields get copied.
    /// </summary>
    private bool _firstMovement;

    /// <inheritdoc cref="_firstMovement" />
    private int _nastyStuckCounter;

    /// <inheritdoc cref="_firstMovement" />
    private int _prevXpos;

    /// <inheritdoc cref="_firstMovement" />
    private int _prevYpos;

    /// <inheritdoc cref="_firstMovement" />
    private bool _spikeyPause;

    /// <summary>
    ///     Everything a nasty carries across a screen swap. One struct, so copying it copies all of
    ///     it and nothing added later gets silently dropped.
    /// </summary>
    private NastyMovementState _state;

    public NastyBrain(ScreenCollisionMap collisions, Random randomNumberGenerator, int moveSpeed)
    {
        _collisions = collisions;
        _randomNumberGenerator = randomNumberGenerator;
        _state.MoveSpeed = moveSpeed;

        // Not travelling anywhere yet, which every movement method reads as "choose a direction".
        // Left at the enum's default of Up, the core guardian would never choose at all.
        _state.Direction = Direction.None;
    }

    /// <summary>
    ///     The kind of nasty this is thinking for. The core guardian has one frame of mind and never
    ///     changes it; a spiky nasty is always chasing.
    /// </summary>
    public NastyType NastyType { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    /// <summary>The size a nasty is tested against the collision map at.</summary>
    private static int NastyWidth => SpriteMetrics.SpriteWidth;

    /// <inheritdoc cref="NastyWidth" />
    private static int NastyHeight => SpriteMetrics.SpriteHeight;

    public Direction Direction
    {
        get => _state.Direction;
        set => _state.Direction = value;
    }

    public int MoveSpeed => _state.MoveSpeed;

    /// <summary>
    ///     The whole of what survives a screen swap, copied as one value by
    ///     <c>Nasty.CopyFromSource</c>.
    /// </summary>
    public NastyMovementState State
    {
        get => _state;
        set => _state = value;
    }

    /// <summary>
    ///     A nasty being born forgets what it was thinking and crawls through its first frame of
    ///     mind, so it can be seen coming.
    /// </summary>
    public void StartFirstMovement()
    {
        _state.MovementChangeCounter = 0;
        _state.MovementChangeCount = 0;
        _firstMovement = true;
    }

    /// <summary>Gives up the chase. The core guardian never chases, so becoming one stops it.</summary>
    public void StopChasing()
        => _state.AttractedToBlob = false;

    /// <summary>
    ///     Settles how this kind of nasty moves. The guardian bounces, a spiky one lunges, and the
    ///     rest start on one of the five types a wandering nasty can draw.
    /// </summary>
    public void BecomeA(NastyType nastyType)
    {
        NastyType = nastyType;

        _state.MovementType = nastyType switch
        {
            NastyType.CoreGuardian => MoveType.DiagonalBouncing,
            NastyType.Spiky => MoveType.SpikeyMovement,
            _ => (MoveType)_randomNumberGenerator.Next(0, Enum.GetValues<MoveType>().Length - 1)
        };
    }

    /// <summary>
    ///     How a nasty about to be conceived is set moving. A spiky one starts paused, which is the
    ///     half of its cycle that makes it beatable.
    /// </summary>
    public void PrepareForConception()
    {
        if (NastyType == NastyType.Spiky)
        {
            _state.MovementType = MoveType.SpikeyMovement;
            _spikeyPause = true;
        }
        else
        {
            _state.MovementType = MoveType.RandomMovement;
        }
    }

    /// <summary>
    ///     One frame of a nasty's life: give up chasing if it is stuck, move, then count down towards
    ///     the next change of mind.
    /// </summary>
    /// <remarks>
    ///     Moving before reconsidering is deliberate. The movement methods read what the last step
    ///     walked into, so reconsidering first would act on the previous frame's collision.
    /// </remarks>
    public void Step(int blobsXPosition, int blobsYPosition)
    {
        ReleaseAttractionIfStuck();
        MoveNasty(blobsXPosition, blobsYPosition);
        UpdateFrameOfMind();
    }

    /// <summary>
    ///     A nasty chasing Blob with something in the way cannot move, so it gives up the chase after
    ///     a while rather than grinding against the scenery forever.
    /// </summary>
    private void ReleaseAttractionIfStuck()
    {
        if (NastyType != NastyType.CoreGuardian)
        {
            if (_state.AttractedToBlob && NastyType != NastyType.Spiky)
            {
                if (_prevXpos == XPos &&
                    _prevYpos == YPos)
                {
                    _nastyStuckCounter++;
                }
                else
                {
                    _nastyStuckCounter = 0;
                }

                if (_nastyStuckCounter > StuckFrames)
                {
                    // Pinned against something for a while, so it gives up on Blob and picks its own
                    // direction rather than walking into the same wall.
                    _state.AttractedToBlob = false;
                    _nastyStuckCounter = 0;
                }

                _prevXpos = XPos;
                _prevYpos = YPos;
            }
        }
    }

    private void MoveNasty(int blobsXPosition, int blobsYPosition)
    {
        if (NastyType == NastyType.CoreGuardian)
        {
            MoveNastyOneStep();
            DiagonalBouncing();
        }
        else
        {
            if (_state.StepsMovedCounter < _state.StepsToMove)
            {
                bool moveOneStep = true;

                if (_state.MovementType == MoveType.SpikeyMovement && _spikeyPause)
                {
                    moveOneStep = false;
                    _state.StepsMovedCounter++;
                }

                if (moveOneStep)
                {
                    MoveNastyOneStep();
                }
            }
            else
            {
                Direction = Direction.None;
                _state.StepsMovedCounter = 0;

                if (_state.MovementType == MoveType.RandomMovement)
                {
                    _state.ConstantMovement = false;
                    RandomMovement(blobsXPosition, blobsYPosition);
                }
                else if (_state.MovementType == MoveType.RandomUpDownLeftRight)
                {
                    _state.ConstantMovement = false;
                    RandomUpDownLeftRight(blobsXPosition, blobsYPosition);
                }
                else if (_state.MovementType == MoveType.DiagonalBouncing)
                {
                    _state.ConstantMovement = true;
                    DiagonalBouncing();
                }
                else if (_state.MovementType == MoveType.DiagonalBouncingInSteps)
                {
                    _state.ConstantMovement = false;
                    DiagonalBouncingInSteps();
                }
                else if (_state.MovementType == MoveType.RandomMovementsAllDirections)
                {
                    _state.ConstantMovement = false;
                    RandomMovementsAllDirections(blobsXPosition, blobsYPosition);
                }
                else if (_state.MovementType == MoveType.SpikeyMovement)
                {
                    _spikeyPause = !_spikeyPause;
                    _state.ConstantMovement = false;
                    SpikeyMovement(blobsXPosition, blobsYPosition);
                }
            }
        }
    }

    /// <summary>
    ///     Picks the next frame of mind and how long to keep it: whether to chase Blob or wander, how
    ///     to move, which way to set off and how fast.
    /// </summary>
    private void UpdateFrameOfMind()
    {
        if (NastyType == NastyType.CoreGuardian)
        {
            // The guardian has one frame of mind and never changes it.
            _state.AttractedToBlob = false;
            _state.ConstantMovement = true;
            _state.MovementType = MoveType.DiagonalBouncing;
            _state.MoveSpeed = CoreGuardianSpeed;

            return;
        }

        if (_state.MovementChangeCounter < _state.MovementChangeCount)
        {
            _state.MovementChangeCounter++;

            return;
        }

        ChooseNewFrameOfMind();
    }

    private void ChooseNewFrameOfMind()
    {
        bool wandering = CoinFlip();

        _state.AttractedToBlob = !wandering;
        _state.MovementChangeCount = wandering
            ? _randomNumberGenerator.Next(WanderingShortest, WanderingLongest)
            : _randomNumberGenerator.Next(ChasingShortest, ChasingLongest);
        _state.MovementChangeCounter = 0;

        if (NastyType == NastyType.Spiky)
        {
            _state.AttractedToBlob = true; // Spiky nasties are always attracted to blob
            _state.MovementType = MoveType.SpikeyMovement;

            return;
        }

        _state.MovementType = _state.AttractedToBlob
            // The first two move types are the chasing ones, so a nasty after Blob draws only from
            // those.
            ? (MoveType)_randomNumberGenerator.Next(0, 2)
            : (MoveType)_randomNumberGenerator.Next(0, Enum.GetValues<MoveType>().Length - 1);

        SetStartingDirection();

        _state.MoveSpeed = ChooseMoveSpeed();

        if (_firstMovement)
        {
            // The first move after being born is always slow, so a nasty eases into motion instead
            // of appearing already at speed.
            _state.MoveSpeed = FirstMovementSpeed;
            _state.MovementChangeCount = FirstMovementDuration;
            _firstMovement = false;
        }
    }

    /// <summary>
    ///     Which way a nasty sets off, given how it has just decided to move. The bouncing types
    ///     start on a diagonal, the others on a straight direction, and anything else keeps going the
    ///     way it already was.
    /// </summary>
    private void SetStartingDirection()
    {
        switch (_state.MovementType)
        {
            case MoveType.DiagonalBouncing:
            {
                Direction = RandomDiagonal();

                break;
            }
            case MoveType.RandomMovement or MoveType.RandomUpDownLeftRight:
            {
                Direction = RandomStraightDirection();

                break;
            }
            case MoveType.DiagonalBouncingInSteps:
            {
                Direction = RandomDiagonal();
                _state.StepsToMove = _randomNumberGenerator.Next(ShortestSteppedRun, LongestSteppedRun);

                break;
            }
        }
    }

    /// <summary>
    ///     How fast a nasty moves, given what it has just decided to do. A chasing nasty is faster than
    ///     a wandering one at everything except the two random walks, where it is slower - it is picking
    ///     its way towards Blob rather than striding about.
    /// </summary>
    private int ChooseMoveSpeed()
    {
        bool randomWalk = _state.MovementType is MoveType.RandomMovement or MoveType.RandomMovementsAllDirections;

        if (!_state.AttractedToBlob)
        {
            return randomWalk
                ? _randomNumberGenerator.Next(WanderingAmbleSpeed, WanderingAmbleSpeedLimit)
                : _randomNumberGenerator.Next(WanderingStrideSpeed, WanderingStrideSpeedLimit);
        }

        return randomWalk || _state.MovementType == MoveType.SpikeyMovement
            ? _randomNumberGenerator.Next(ChasingPickingSpeed, ChasingPickingSpeedLimit)
            : _randomNumberGenerator.Next(ChasingChargeSpeed, ChasingChargeSpeedLimit);
    }

    private void DiagonalBouncingInSteps()
    {
        if (_state.LastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomDiagonal();
        }

        // Hitting nothing still turns it away vertically, which stops a stepping nasty running in a
        // straight line forever.
        Direction = Reflected(Direction, _state.LastCollisionDirection) ?? TurnedAwayVertically(Direction);
    }

    private void DiagonalBouncing()
    {
        if (Direction == Direction.None)
        {
            Direction = RandomDiagonal();
        }

        if (Reflected(Direction, _state.LastCollisionDirection) is { } reflected)
        {
            Direction = reflected;
        }

        _state.StepsToMove = _randomNumberGenerator.Next(ShortestRun, LongestRun);
    }

    /// <summary>
    ///     Which diagonal a nasty comes off on, given the one it was travelling on and what it just
    ///     hit, or null if what it hit does not turn it. Walking into the corner sends it straight
    ///     back the way it came; walking into the wall on one side flips only that side.
    /// </summary>
    private static Direction? Reflected(Direction travelling, Direction hit)
        => (travelling, hit) switch
        {
            (Direction.UpLeft, Direction.UpLeft) => Direction.DownRight,
            (Direction.UpLeft, Direction.Left) => Direction.UpRight,
            (Direction.UpLeft, Direction.Up) => Direction.DownLeft,
            (Direction.UpRight, Direction.UpRight) => Direction.DownLeft,
            (Direction.UpRight, Direction.Right) => Direction.UpLeft,
            (Direction.UpRight, Direction.Up) => Direction.DownRight,
            (Direction.DownLeft, Direction.DownLeft) => Direction.UpRight,
            (Direction.DownLeft, Direction.Left) => Direction.DownRight,
            (Direction.DownLeft, Direction.Down) => Direction.UpLeft,
            (Direction.DownRight, Direction.DownRight) => Direction.UpLeft,
            (Direction.DownRight, Direction.Right) => Direction.DownLeft,
            (Direction.DownRight, Direction.Down) => Direction.UpRight,
            _ => null
        };

    /// <summary>
    ///     The same diagonal with its up or down half flipped. Anything that is not a diagonal is
    ///     left alone.
    /// </summary>
    private static Direction TurnedAwayVertically(Direction direction)
        => direction switch
        {
            Direction.UpLeft => Direction.DownLeft,
            Direction.UpRight => Direction.DownRight,
            Direction.DownLeft => Direction.UpLeft,
            Direction.DownRight => Direction.UpRight,
            _ => direction
        };

    private void RandomUpDownLeftRight(int blobsXPosition, int blobsYPosition)
    {
        if (_state.LastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomStraightDirection();
        }

        BounceStraightBack();

        if (_state.AttractedToBlob)
        {
            HeadTowardsBlob(blobsXPosition, blobsYPosition);
        }

        _state.StepsToMove = _randomNumberGenerator.Next(ShortestRun, LongestRun);
    }

    /// <summary>
    ///     Like <see cref="RandomUpDownLeftRight" /> but in much shorter runs, and it forgets which
    ///     way it was going before chasing. That second part matters: with no direction carried in, a
    ///     nasty level with Blob heads straight at him rather than turning the move into a diagonal.
    /// </summary>
    private void RandomMovement(int blobsXPosition, int blobsYPosition)
    {
        if (_state.LastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomStraightDirection();
        }

        BounceStraightBack();

        if (_state.AttractedToBlob)
        {
            Direction = Direction.None;
            HeadTowardsBlob(blobsXPosition, blobsYPosition);
        }

        _state.StepsToMove = _randomNumberGenerator.Next(ShortestScuttle, LongestScuttle);
    }

    /// <summary>
    ///     Turns a nasty that has walked straight into something back the way it came, and leaves one
    ///     that has hit nothing or hit a corner alone.
    /// </summary>
    private void BounceStraightBack()
        => Direction = _state.LastCollisionDirection switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction
        };

    /// <summary>
    ///     Points a nasty at Blob, diagonally when he is off in both axes, while refusing to turn
    ///     into whatever it just walked into.
    /// </summary>
    private void HeadTowardsBlob(int blobsXPosition, int blobsYPosition)
    {
        if (XPos < blobsXPosition)
        {
            if (_state.LastCollisionDirection != Direction.Right)
            {
                Direction = Direction.Right;
            }
        }
        else if (XPos > blobsXPosition)
        {
            if (_state.LastCollisionDirection != Direction.Left)
            {
                Direction = Direction.Left;
            }
        }

        if (YPos < blobsYPosition)
        {
            if (_state.LastCollisionDirection != Direction.Down)
            {
                Direction = Direction switch
                {
                    Direction.Left => Direction.DownLeft,
                    Direction.Right => Direction.DownRight,
                    _ => Direction.Down
                };
            }
        }
        else if (YPos > blobsYPosition)
        {
            if (_state.LastCollisionDirection != Direction.Up)
            {
                Direction = Direction switch
                {
                    Direction.Left => Direction.UpLeft,
                    Direction.Right => Direction.UpRight,
                    _ => Direction.Up
                };
            }
        }
    }

    private void RandomMovementsAllDirections(int blobsXPosition, int blobsYPosition)
    {
        if (_state.LastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomAnyDirection();
        }

        Direction[]? choices = _state.AttractedToBlob
            ? WaysTowardsBlob(blobsXPosition, blobsYPosition)
            : WaysAwayFrom(_state.LastCollisionDirection);

        if (choices is not null)
        {
            Direction = choices[_randomNumberGenerator.Next(0, choices.Length)];
        }

        _state.StepsToMove = _randomNumberGenerator.Next(ShortestChasingRun, LongestRun);
    }

    /// <summary>
    ///     The three ways out of having walked into something from each of the eight directions, or
    ///     null when nothing was hit. Each one keeps whichever half of the direction was not blocked
    ///     and reverses the other, so the three are the straight reverse and the two diagonals
    ///     either side of it.
    /// </summary>
    private static Direction[]? WaysAwayFrom(Direction hit)
        => hit switch
        {
            Direction.Up => [Direction.Down, Direction.DownLeft, Direction.DownRight],
            Direction.Down => [Direction.Up, Direction.UpLeft, Direction.UpRight],
            Direction.Left => [Direction.Right, Direction.DownRight, Direction.UpRight],
            Direction.Right => [Direction.Left, Direction.DownLeft, Direction.UpLeft],
            Direction.UpLeft => [Direction.Down, Direction.DownLeft, Direction.DownRight],
            Direction.UpRight => [Direction.Down, Direction.DownRight, Direction.DownLeft],
            Direction.DownLeft => [Direction.Up, Direction.UpLeft, Direction.UpRight],
            Direction.DownRight => [Direction.Up, Direction.UpRight, Direction.UpLeft],
            _ => null
        };

    /// <summary>
    ///     The three ways towards Blob from whichever quadrant he is in: straight across, straight up
    ///     or down, and the diagonal between. The tests are deliberately inclusive on both sides, so
    ///     a nasty level with Blob in one axis still has a quadrant and this never answers "nowhere".
    /// </summary>
    private Direction[] WaysTowardsBlob(int blobsXPosition, int blobsYPosition)
    {
        if (XPos <= blobsXPosition && YPos <= blobsYPosition)
        {
            return [Direction.Right, Direction.Down, Direction.DownRight];
        }

        if (XPos <= blobsXPosition && YPos >= blobsYPosition)
        {
            return [Direction.Right, Direction.Up, Direction.UpRight];
        }

        if (XPos >= blobsXPosition && YPos >= blobsYPosition)
        {
            return [Direction.Left, Direction.Up, Direction.UpLeft];
        }

        return [Direction.Left, Direction.Down, Direction.DownLeft];
    }

    /// <summary>One of the four diagonals - the second half of <see cref="Direction" />.</summary>
    private Direction RandomDiagonal() => (Direction)_randomNumberGenerator.Next(4, 8);

    /// <summary>One of the four straight directions - the first half of <see cref="Direction" />.</summary>
    private Direction RandomStraightDirection() => (Direction)_randomNumberGenerator.Next(0, 4);

    /// <summary>Any of the eight, but never <see cref="Direction.None" />.</summary>
    private Direction RandomAnyDirection() => (Direction)_randomNumberGenerator.Next(0, 8);

    /// <summary>
    ///     A spiky nasty lunges at Blob along whichever axis he is further away on, sometimes cutting
    ///     the corner diagonally, and takes the other axis if it has just walked into something. Then
    ///     it rests, which is the other half of the <see cref="_spikeyPause" /> cycle and the reason
    ///     it is beatable.
    /// </summary>
    private void SpikeyMovement(int blobsXPosition, int blobsYPosition)
    {
        if (_spikeyPause)
        {
            Direction = Direction.None;
            _state.StepsToMove = _randomNumberGenerator.Next(ShortestBouncingRun, LongestBouncingRun);
            return;
        }

        Direction acrossToBlob = XPos <= blobsXPosition ? Direction.Right : Direction.Left;
        Direction alongToBlob = VerticalDirectionTowardsBlob(blobsXPosition, blobsYPosition);
        Direction cornerToBlob = Diagonal(acrossToBlob, alongToBlob);

        int distanceX = Math.Abs(blobsXPosition - XPos);
        int distanceY = Math.Abs(blobsYPosition - YPos);
        int stepsFromBlob = 0;

        if (distanceX > distanceY)
        {
            Direction = acrossToBlob;
            stepsFromBlob = distanceX;
        }
        else if (distanceY > distanceX)
        {
            Direction = alongToBlob;
            stepsFromBlob = distanceY;
        }

        // Half the time it cuts the corner rather than closing one axis at a time.
        if (CoinFlip())
        {
            if (Direction == acrossToBlob)
            {
                Direction = cornerToBlob;
                stepsFromBlob = distanceX;
            }
            else if (Direction == alongToBlob)
            {
                Direction = cornerToBlob;
                stepsFromBlob = distanceY;
            }
        }

        // It walked into something last step, so try a way it has not just been stopped on.
        if (_state.LastCollisionDirection == acrossToBlob)
        {
            Direction = alongToBlob;
            stepsFromBlob = distanceY;
        }
        else if (_state.LastCollisionDirection == alongToBlob)
        {
            Direction = acrossToBlob;
            stepsFromBlob = distanceX;
        }
        else if (_state.LastCollisionDirection == cornerToBlob)
        {
            if (CoinFlip())
            {
                Direction = acrossToBlob;
                stepsFromBlob = distanceX;
            }
            else
            {
                Direction = alongToBlob;
                stepsFromBlob = distanceY;
            }
        }

        _state.StepsToMove = _randomNumberGenerator.Next(
                                 (int)(stepsFromBlob / (double)_state.MoveSpeed / 2),
                                 (int)(stepsFromBlob / (double)_state.MoveSpeed)) +
                             SpikeyOvershoot;
    }

    /// <summary>Which of up and down points at Blob.</summary>
    /// <remarks>
    ///     A nasty exactly level with Blob answers down when it is to his left and up when it is to
    ///     his right. That tie-break is an artefact of the original's ordering rather than a decision,
    ///     but changing it changes what the game does, so it is reproduced deliberately.
    /// </remarks>
    private Direction VerticalDirectionTowardsBlob(int blobsXPosition, int blobsYPosition)
    {
        if (XPos <= blobsXPosition)
        {
            return YPos <= blobsYPosition ? Direction.Down : Direction.Up;
        }

        return YPos < blobsYPosition ? Direction.Down : Direction.Up;
    }

    /// <summary>The diagonal made of one horizontal and one vertical direction.</summary>
    private static Direction Diagonal(Direction across, Direction along)
        => (across, along) switch
        {
            (Direction.Right, Direction.Down) => Direction.DownRight,
            (Direction.Right, _) => Direction.UpRight,
            (_, Direction.Down) => Direction.DownLeft,
            _ => Direction.UpLeft
        };

    /// <summary>A one-in-two chance, which is how most of a nasty's mind gets made up.</summary>
    private bool CoinFlip() => _randomNumberGenerator.Next(0, 2) == 0;

    /// <summary>
    ///     Moves a nasty one step the way it is facing, and records what it walked into so that the
    ///     movement methods know where to send it next.
    /// </summary>
    /// <remarks>
    ///     Every direction is a vertical component, a horizontal one, or both. A diagonal is checked
    ///     vertically first and then horizontally from where that step left it, which lets a nasty
    ///     pressed diagonally against a wall slide along it rather than stopping dead.
    /// </remarks>
    private void MoveNastyOneStep()
    {
        Direction vertical = VerticalComponent(Direction);
        Direction horizontal = HorizontalComponent(Direction);

        int amountToMoveY = vertical == Direction.None
            ? 0
            : DistanceClear(vertical, XPos, YPos);

        int amountToMoveX = horizontal == Direction.None
            ? 0
            : DistanceClear(horizontal, XPos, YPos + StepSign(vertical) * amountToMoveY);

        bool blockedVertically = vertical != Direction.None && amountToMoveY == 0;
        bool blockedHorizontally = horizontal != Direction.None && amountToMoveX == 0;

        if (blockedVertically || blockedHorizontally)
        {
            _state.StepsMovedCounter = _state.StepsToMove;
        }

        YPos += StepSign(vertical) * amountToMoveY;
        XPos += StepSign(horizontal) * amountToMoveX;

        // A diagonal stopped on one axis only reports that axis, so the movement methods can turn
        // along the wall instead of away from it. Stopped on both, it reports the diagonal.
        _state.LastCollisionDirection = (blockedVertically, blockedHorizontally) switch
        {
            (true, true) => Direction,
            (true, false) => vertical,
            (false, true) => horizontal,
            _ => Direction.None
        };

        if (!_state.ConstantMovement)
        {
            if (_state.LastCollisionDirection != Direction.None)
            {
                _state.StepsMovedCounter = _state.StepsToMove;
            }
            else
            {
                _state.StepsMovedCounter++;
            }
        }
    }

    private int DistanceClear(Direction direction, int x, int y)
        => _collisions.CheckCollision(direction, x, y, NastyWidth, NastyHeight, _state.MoveSpeed, false);

    /// <summary>Which of up and down a direction contains, if either.</summary>
    private static Direction VerticalComponent(Direction direction)
        => direction switch
        {
            Direction.Up or Direction.UpLeft or Direction.UpRight => Direction.Up,
            Direction.Down or Direction.DownLeft or Direction.DownRight => Direction.Down,
            _ => Direction.None
        };

    /// <summary>Which of left and right a direction contains, if either.</summary>
    private static Direction HorizontalComponent(Direction direction)
        => direction switch
        {
            Direction.Left or Direction.UpLeft or Direction.DownLeft => Direction.Left,
            Direction.Right or Direction.UpRight or Direction.DownRight => Direction.Right,
            _ => Direction.None
        };

    /// <summary>
    ///     Which way along its axis a straight direction adds, and nought for anything with no axis.
    /// </summary>
    private static int StepSign(Direction direction)
        => direction switch
        {
            Direction.Up or Direction.Left => -1,
            Direction.Down or Direction.Right => 1,
            _ => 0
        };
}
