// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Starquake.ContentManagement;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Animations;

internal class BlobAccessBoothAnim
{
    private readonly Animation _animLeft;
    private readonly Animation _animRight;
    private readonly Animation _animTurn;
    private readonly int _endPoint;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;
    private readonly int _startPoint;

    private Direction _blobDirection;
    private int _blobFrame;
    private int _blobX;

    public BlobAccessBoothAnim(ResourceManager resources, GraphicsSet graphicsSet, int startPoint, int endPoint)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;
        _startPoint = startPoint;
        _endPoint = endPoint;

        _animLeft = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = 3,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = false
        };

        _animRight = new Animation
        {
            FirstFrame = 7,
            CurrentFrame = 7,
            LastFrame = 10,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = true
        };

        _animTurn = new Animation
        {
            FirstFrame = 4,
            CurrentFrame = 4,
            LastFrame = 10,
            Yoyo = false,
            Delay = 2,
            Repeat = false,
            AnimateForwards = false,
            Finished = false
        };
    }

    public void Reset(Direction startingDirection)
    {
        if (startingDirection == Direction.Right)
        {
            _blobX = _startPoint;
            _blobFrame = 10;
        }
        else if (startingDirection == Direction.Left)
        {
            _blobX = _endPoint;
            _blobFrame = 0;
        }

        _blobDirection = startingDirection;
    }

    public void Update()
    {
        if (_blobDirection == Direction.Right)
        {
            _blobX += 8;

            if (!_animTurn.Finished)
            {
                _animTurn.GetNextFrame();
            }
            else
            {
                _animRight.GetNextFrame();
            }

            if (_animTurn.Finished)
            {
                _blobFrame = _animRight.CurrentFrame;
            }
            else
            {
                _blobFrame = _animTurn.CurrentFrame;
            }
        }
        else if (_blobDirection == Direction.Left)
        {
            _blobX -= 8;

            if (!_animTurn.Finished)
            {
                _animTurn.GetNextFrame();
            }
            else
            {
                _animLeft.GetNextFrame();
            }

            if (_animTurn.Finished)
            {
                _blobFrame = _animLeft.CurrentFrame;
            }
            else
            {
                _blobFrame = _animTurn.CurrentFrame;
            }
        }

        if (_blobX > _endPoint)
        {
            _blobDirection = Direction.Left;

            _animTurn.FirstFrame = 0;
            _animTurn.CurrentFrame = 6;
            _animTurn.LastFrame = 6;
            _animTurn.AnimateForwards = false;
            _animTurn.Finished = false;
        }
        else if (_blobX < _startPoint)
        {
            _blobDirection = Direction.Right;

            _animTurn.FirstFrame = 4;
            _animTurn.CurrentFrame = 4;
            _animTurn.LastFrame = 10;
            _animTurn.AnimateForwards = true;
            _animTurn.Finished = false;
        }
    }

    public void Draw()
    {
        // Draw blob animation
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);
        blobSprites.Begin(BlendState.AlphaBlend);
        blobSprites.DrawSprite(_blobFrame, _blobX, 192);
        blobSprites.End();
    }
}
