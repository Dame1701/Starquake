using MessagePack;

namespace SharedLibs
{
    [Serializable]
    [MessagePackObject]
    public class GameScreen
    {
        private ScreenTile[,] screenTiles;
        
        [Key(0)]
        public ScreenTile[,] ScreenTiles
        {
            get { return screenTiles; }
            set
            {
                screenTiles = value;
            }
        }

        private List<CollisionTile> collisionMap;
        
        [Key(1)]
        public List<CollisionTile> CollisionMap
        {
            get { return collisionMap; }
            set { collisionMap = value; }
        }

        [NonSerialized]
        private bool keepNastiesTheSame;
        
        [IgnoreMember]
        public bool KeepNastiesTheSame
        {
            get { return keepNastiesTheSame; }
            set { keepNastiesTheSame = value; }
        }


        [NonSerialized]
        private Item powerUp;
        
        [IgnoreMember]
        public Item PowerUp
        {
            get { return powerUp; }
            set { powerUp = value; }
        }

        [NonSerialized]
        private Item cheopsPyramid;
        
        [IgnoreMember]
        public Item CheopsPyramid
        {
            get { return cheopsPyramid; }
            set { cheopsPyramid = value; }
        }

        [NonSerialized]
        private List<Item> pickUps;
        
        [IgnoreMember]
        public List<Item> PickUps
        {
            get { return pickUps; }
            set { pickUps = value; }
        }

        [NonSerialized]
        private string accessCode;
        
        [IgnoreMember]
        public string AccessCode
        {
            get { return accessCode; }
            set { accessCode = value; }
        }

        [NonSerialized]
        private string cheopsAccessCode;
        
        [IgnoreMember]
        public string CheopsAccessCode
        {
            get { return cheopsAccessCode; }
            set { cheopsAccessCode = value; }
        }
        

        [NonSerialized]
        private bool screenVisited;
        
        [IgnoreMember]
        public bool ScreenVisited
        {
            get { return screenVisited; }
            set { screenVisited = value; }
        }

        public GameScreen()
        {
            pickUps = new List<Item>();
            accessCode = null;
            cheopsAccessCode = null;
            screenVisited = false;
        }

        public GameScreen(int TilesAcross, int TilesDown)
        {
            screenTiles = new ScreenTile[TilesAcross, TilesDown];

            for (int y = 0; y < TilesDown; y++)
            {
                for (int x = 0; x < TilesAcross; x++)
                {
                    screenTiles[x, y] = new ScreenTile(-1, 0);
                }
            }

            pickUps = new List<Item>();
            accessCode = null;
            screenVisited = false;
            cheopsAccessCode = null;
        }
    }

    public class ScreenTileCollisionMap
    {
        private int collisionAreaWidth;
        public int CollisionAreaWidth
        {
            get { return collisionAreaWidth; }
            set { collisionAreaWidth = value; }
        }

        private int collisionAreaHeight;
        public int CollisionAreaHeight
        {
            get { return collisionAreaHeight; }
            set { collisionAreaHeight = value; }
        }

        private int collisionAreasAcross;
        public int CollisionAreasAcross
        {
            get { return collisionAreasAcross; }
            set { collisionAreasAcross = value; }
        }

        private int collisionAreasDown;
        public int CollisionAreasDown
        {
            get { return collisionAreasDown; }
            set { collisionAreasDown = value; }
        }

        private int[,] collisionMap;
        public int[,] CollisionMap
        {
            get { return collisionMap; }
            set { collisionMap = value; }
        }

        private int[,] actionCollisionMap;
        public int[,] ActionCollisionMap
        {
            get { return actionCollisionMap; }
            set { actionCollisionMap = value; }
        }

        public ScreenTileCollisionMap(int[,] collisionMap, int[,] actionCollisionMap)
        {
            CollisionMap = collisionMap;
            ActionCollisionMap = actionCollisionMap;
        }
    }

    public enum enCollTileType
    {
        None = 0,
        EveryoneHits = 1,
        NotBlob = 2,
        NotStuckNasty = 3,
    }

    public enum enCollisionObjectType
    {
        MapTile = 999,
        CoreElement = 1000,
        JumpPlatform = 1001,
        GameObject = 1002,
        KeyAccessDoor = 1003,
    }

    public enum enFadeDirection
    {
        FadeUp,
        FadeDown,
    }

    [MessagePackObject]
    [Serializable]
    public class CollisionTile
    {
        private int tileImage;
        
        [Key(0)]
        public int TileImage
        {
            get { return tileImage; }
            set { tileImage = value; }
        }

        private int xPos;
        
        [Key(1)]
        public int XPos
        {
            get { return xPos; }
            set { xPos = value; }
        }

        private int yPos;
        
        [Key(2)]
        public int YPos
        {
            get { return yPos; }
            set { yPos = value; }
        }

        private enCollTileType colType;
        
        [Key(3)]
        public enCollTileType ColType
        {
            get { return colType; }
            set { colType = value; }
        }

        public CollisionTile(int TileImage, int XPos, int YPos, enCollTileType CollType)
        {
            tileImage = TileImage;
            xPos = XPos;
            yPos = YPos;
            colType = CollType;
        }

        public override string ToString()
        {
            return "TileImage: " + tileImage.ToString() + ", XPos: " + xPos.ToString() + ", YPos: " + yPos.ToString() + ", ColType: " + colType.ToString();
        }
    }

    [MessagePackObject]
    [Serializable]
    public class ScreenTile
    {
        private int tileImage;
        
        [Key(0)]
        public int TileImage
        {
            get { return tileImage; }
            set { tileImage = value; }
        }

        public ScreenTile()
        {
        }

        public ScreenTile(int tileImage, int tileType)
        {
            TileImage = tileImage;
        }
    }

    public class Item
    {
        int xLocation;
        public int XLocation
        {
            get { return xLocation; }
            set { xLocation = value; }
        }

        int yLocation;
        public int YLocation
        {
            get { return yLocation; }
            set { yLocation = value; }
        }

        int type;
        public int Type
        {
            get { return type; }
            set { type = value; }
        }

        public Item(int xLocation, int yLocation, int type)
        {
            XLocation = xLocation;
            YLocation = yLocation;
            Type = type;
        }

    }

}
