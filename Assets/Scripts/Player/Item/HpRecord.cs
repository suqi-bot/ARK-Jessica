using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回复类型道具
/// </summary>
public class HpRecord : MonoBehaviour, IUsed
{
    [SerializeField] private float record;
    [SerializeField] private int id;

    public bool Used(CharacterBaseClass character)
    {
        if (character.GetHealthIsFull())
        {
            UIManager.Instance.HidePanel<InfoPanel>(false);
            UIManager.Instance.ShowPanel<InfoPanel>();
            InfoPanel info = UIManager.Instance.GetPanel<InfoPanel>();
            info.SetInfoText("血量已满无法使用");
            return false;
        }
        character.Injure(-record);
        List<ItemData> temp = GameDataMgr.Instance.GetItemInfoList(id);
        foreach (var item in temp)
        {
            if (item.itemId == id)
            {
                item.itemCount--;
                return true;
            }
        }
        return false;
    }

}
