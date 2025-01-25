using System;
using System.Collections.Generic;
using System.Text;

namespace StarquakeXNA
{
    public enum enSoundFade
    {
        None,
        Slow,
        Medium,
        Fast,
    }

    public enum enElementState
    {
        Missing,
        Replacing,
        Replaced,
    }

    public enum enParticleStatus
    {
        NotStarted,
        Triggered,
        HasStarted,
        Finished,
    }

    public enum enGameComponentStatus
    {
        NotStarted,
        StartingGraphicsLoaded,
        Starting,
        ContentLoaded,
        Started,
        StartGame,
    }

    public enum enScreenEffectType
    {
        None,
        AddictiveFade,
        LeftTunnelEntry,
        RightTunnelEntry,
    }

    public enum enCollectables
    {
        None = 255,
        AccessCard = 28,
        Key = 29,
        CoreElement1 = 0,
        CoreElement2 = 1,
        CoreElement3 = 2,
        CoreElement4 = 3,
        CoreElement5 = 4,
        CoreElement6 = 5,
        CoreElement7 = 6,
        CoreElement8 = 7,
        CoreElement9 = 8,
    }

    public enum enEffectSprites
    {
        Door = 0,
        BreakableBridge = 1,
    }

    public enum enHasFired
    {
        No,
        FromShuttlePad,
        NotFromShutlePad,
    }

    public enum enTileType
    {
        ElectricityNode = 394,
        ElectricityGate = 406,
    }

    public enum enDirection
    {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        DownLeft,
        UpRight,
        DownRight,
        None,
    }

    public enum enNastyType
    {
        CurleyWurley,
        Cushion,
        Scorpian,
        FriedEgg,
        Spiky,
        Snail,
        Gonzo,
        Ghost,
        Shellfish,
        Helmet,
        DragonFly,
        Helicopter,
        Hippo,
        Centipede,
        Bird,
        CoreGuardian,
        Conveyer,
    }

    public enum enNastyState
    {
        Disabled,
        PreConception,
        Conception,
        Birth,
        Normal,
        Shot,
        Dead,
    }

    public enum enBlobState
    {
        Normal,
        Overloading,
        Dead,
        Dying,
        Gameover,
    }

    public enum enActionEvent
    {
        Teleported,
    }

    public enum enNastyInterationType
    {
        HitBlob,
        BlobShot,
    }

    public enum enActionTiles
    {
        ShuttlePlatform = 395,
        RemovedShuttlePlatform = 398,
        ElectricityNode = 394,
        ShuttleTag1 = 401,
        TunnelEntranceRight = 402,
        TunnelEntranceLeft = 403,
        Spike1 = 404,
        Spike2 = 405,
        ElectricityGate = 406,
        TunnelLiftTile = 408,
        Spike3 = 416,
        ShuttleTag2 = 418,
        ShuttleTag3 = 417,
        BlankFiller = 390,
        ConveyerNasty = 391,
        BreakableBridges = 410,
        PreferredCoreLocation = 392,
    }

    public enum enGameScreen
    {
        None,
        Teleporter,
        Cheops,
        AccessBooth,
        PlanetCore,
        MainGame,
        Options,
        ComputerReport,
        TestScreen,
        DefineKeys,
        EndSequence,
        HiScores,
        GameOver,
        GameComplete,
    }

    public enum enPowerUpType // More to go in here i.e. the exact amount of the Powerup
    {
        ExtraLife = 7,
        ExtraPlatforms,
        ExtraAmmo,
        ExtraLowestItem,
        ExtraEnergy,
        
    }

    public enum enBlobAction
    {
        LaidPlatform,
        FiredLazer,
        GotPowerUp,
    }

    public enum enSoundEffectInstanceType
    {
        Single,
        Multi
    }

    public enum enIntroSequenceStage
    {
        None,
        InitialPause,
        ShipFalling,
        ShipExploading,
        PreFadePause,
        CaptionFadeUp,
        CaptionFadeDown,
        EndPause,
        Finished
    }
}
