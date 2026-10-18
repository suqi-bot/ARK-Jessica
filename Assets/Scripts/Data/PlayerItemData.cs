using System.Collections;
using System.Collections.Generic;

public enum Item_Type
{
    Weapon, // 武器
    Item, // 物品
    Projectile, // 投掷物
    Equip // 装备
};

/// <summary>
/// 角色拥有的道具数据
/// </summary>
public class PlayerItemData
{
    public int money;
    public List<ItemData> items = new List<ItemData>();// 物品列表
    public List<ItemData> weapens = new List<ItemData>();// 武器列表
    public List<ItemData> equips = new List<ItemData>();// 装备列表
}

/// <summary>
/// 简易物品数据
/// </summary>
public class ItemData
{
    public int itemId;
    public int itemCount;
}

