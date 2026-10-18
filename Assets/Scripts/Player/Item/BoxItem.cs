using UnityEngine;

public class BoxItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer itemImg;//物品图像
    [SerializeField] private int id; //物品id
    [SerializeField] private int num; //增加数量
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            GameDataMgr.Instance.AddPlayerItemData(id, num);
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 添加物品
    /// </summary>
    public void SetItem(Sprite img, int id,int num)
    {
        this.itemImg.sprite = img;
        this.id = id;
        this.num = num;
    }
}
