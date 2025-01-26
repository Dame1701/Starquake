namespace SharedLibs;

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