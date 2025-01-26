namespace SharedLibs;

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