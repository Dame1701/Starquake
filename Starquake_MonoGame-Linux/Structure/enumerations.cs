namespace StarquakeXNA;





public enum enCollectables
{
    None = 255,
    AccessCard = 28,
    Key = 29
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
    Conveyer
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
    HiScores,
    GameOver,
    GameComplete,
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