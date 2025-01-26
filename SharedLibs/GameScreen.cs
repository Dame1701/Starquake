using MessagePack;

namespace SharedLibs;

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
                    screenTiles[x, y] = new ScreenTile(-1);
                }
            }

            pickUps = new List<Item>();
            accessCode = null;
            screenVisited = false;
            cheopsAccessCode = null;
        }
    }