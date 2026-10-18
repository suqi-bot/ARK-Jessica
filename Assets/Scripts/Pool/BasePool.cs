using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class BasePool : MonoBehaviour
{
    private static BasePool instance;
    public static BasePool Instance => instance;

    // 获取生成的位置角度信息
    private Transform itemTransform;
    // 对象预制体
    private GameObject item;
    // 多对象池
    private Dictionary<string, ObjectPool<GameObject>> objectPools = new Dictionary<string, ObjectPool<GameObject>>();
    // 当前对象池
    //private ObjectPool<GameObject> pool;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        //pool = new ObjectPool<GameObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestory, true, 5, 10);

    }
    /// <summary>
    /// 创建新对象池
    /// </summary>
    /// <param name="item"></param>
    /// <param name="itemTransform"></param>
    public void SetItem(GameObject item, Transform itemTransform)
    {
        this.item = item;
        this.itemTransform = itemTransform;
        if (!objectPools.ContainsKey(item.name))
            objectPools.Add(item.name, new ObjectPool<GameObject>(CreateFunc, ActionOnGet, 
                ActionOnRelease, ActionOnDestory, true, 5, 10));
    }

    /// <summary>
    /// 对象池放置位置
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private Transform SaveItem(GameObject item)
    {
        if (!transform.Find(item.name + "Pool"))
        {
            GameObject childObj = new GameObject(item.name + "Pool");
            childObj.transform.SetParent(transform);
            return childObj.transform;
        }
        else
        {
            return transform.Find(item.name + "Pool");
        }


    }

    GameObject CreateFunc()
    {
        if (itemTransform == null || item == null)
        {
            throw new ArgumentNullException("没有初始化Transform信息或初始化预制体");
        }
        var obj = Instantiate(item, SaveItem(item));
        obj.name = item.name;
        return obj;
    }

    void ActionOnGet(GameObject obj)
    {
        obj.transform.position = itemTransform.position;
        obj.transform.rotation = itemTransform.rotation;
        obj.SetActive(true);
    }

    void ActionOnRelease(GameObject obj)
    {
        obj.SetActive(false);
    }

    void ActionOnDestory(GameObject obj)
    {
        Destroy(obj);
    }

    /// <summary>
    /// 获取当前名称的对象池
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ObjectPool<GameObject> GetPool(string name)
    {
        if (objectPools.ContainsKey(name))
        {
            return objectPools[name];
        }
        else
        {
            throw new ArgumentException("没有这个对象池" + name);
        }

    }

}
