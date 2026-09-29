// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace StarquakeXNA.Structure;

public enum Collectables
{
    None = 255,
    AccessCard = 28,
    Key = 29
}

public enum TileType
{
    ElectricityNode = 394,
    ElectricityGate = 406
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    DownLeft,
    UpRight,
    DownRight,
    None
}

public enum NastyType
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
    Conveyer
}

public enum NastyState
{
    Disabled,
    PreConception,
    Conception,
    Birth,
    Normal,
    Shot,
    Dead
}

public enum BlobState
{
    Normal,
    Overloading,
    Dead,
    Dying,
    Gameover
}

public enum ActionEvent
{
    Teleported
}

public enum NastyInterationType
{
    HitBlob,
    BlobShot
}

public enum ActionTiles
{
    ShuttlePlatform = 395,
    RemovedShuttlePlatform = 398,
    ElectricityNode = 394,
    ShuttleTag1 = 401,
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
    PreferredCoreLocation = 392
}

public enum GameScreenType
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
    HiScores,
    GameOver,
    GameComplete
}

public enum BlobAction
{
    LaidPlatform,
    FiredLazer,
    GotPowerUp
}

public enum SoundEffectInstanceType
{
    Single,
    Multi
}

public enum IntroSequenceStage
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