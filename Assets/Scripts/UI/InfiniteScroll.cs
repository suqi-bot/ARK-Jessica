using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteScroll : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private float spacing = 2; //间隙
    [SerializeField] private int bufferSize = 2;//缓冲数

    private List<TaskItem> itemPool = new List<TaskItem>();//对象池
    private List<TaskDataItem> dataList = new List<TaskDataItem>();//全部数据列表
    private float itemHeight;//项高度
    private int visibleItemCount;//可见项数
    private int totalItemCount;//总数据量
    private int currentStarIndex;//当前起始索引

    


    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <param name="data"></param>
    public void InitData(List<TaskDataItem> data)
    {
        itemHeight = contentRect.GetComponent<RectTransform>().sizeDelta.y;
        float tempCount = GetComponent<RectTransform>().rect.height;
        visibleItemCount = Mathf.CeilToInt(tempCount / itemHeight) + bufferSize * 2;
        dataList = data;
        totalItemCount = data.Count;
        InitPool();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, itemHeight * totalItemCount + spacing * (totalItemCount - 1));
        currentStarIndex = 0;
        RefreshVisibleItems();
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitPool()
    {
        foreach (var item in itemPool)
        {
            Destroy(item.gameObject);
        }

        for (int i = 0; i < visibleItemCount; i++)
        {
            TaskItem nowItem = Instantiate(Resources.Load<TaskItem>("Item/TaskItem"),contentRect);
            nowItem.gameObject.SetActive(false);
            itemPool.Add(nowItem);
        }
    }
    /// <summary>
    /// 刷新可见列表
    /// </summary>
    public void RefreshVisibleItems()
    {
        for (int i = 0; i < visibleItemCount; i++)
        {
            int index = currentStarIndex + i;
            TaskItem item = itemPool[i];
            if(index < 0 || index >= totalItemCount)
            {
                item.gameObject.SetActive(false);
                continue;
            }

            item.SetTaskItem(dataList[index]);
            float posY = - (itemHeight + spacing) * index + contentRect.GetComponent<RectTransform>().rect.height - 50;
            item.rectTransform.anchoredPosition = new Vector2 (item.rectTransform.anchoredPosition.x, posY);
            item.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 滚动位置
    /// </summary>
    /// <param name="pos"></param>
    private void OnScroll(Vector2 pos)
    {
        // 计算当前滚动位置
        float scrollY = contentRect.anchoredPosition.y;
       
        // 计算当前起始索引
        int newStartIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Abs(scrollY) / (itemHeight + spacing)) - bufferSize, 0, totalItemCount - visibleItemCount);
       
        if(newStartIndex != currentStarIndex)
        {
            currentStarIndex = newStartIndex;
            RefreshVisibleItems();
        }

        
    }
}
