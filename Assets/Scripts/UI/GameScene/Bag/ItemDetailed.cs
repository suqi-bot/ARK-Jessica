using System.Collections.Generic;
[System.Serializable]
public class ItemDetailed
{
    public List<Item> info;   
}


[System.Serializable]
public class Item
{
    public int id;
    public string name;
    public string icon;
    public int type;
    public int price;
    public string tips;
}
