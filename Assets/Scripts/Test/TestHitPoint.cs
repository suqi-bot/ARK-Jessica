using UnityEngine;
using UnityEngine.Pool;

public class TestHitPoint : MonoBehaviour
{
    [Header("参数")]
    public Transform hitPoint; // 生成点
    public GameObject item; // 生成物体
    public int hitCount; // 生成数量
    private float fireTime = .25f;
    private bool isEnter;

    private ObjectPool<GameObject> pool;

    // Start is called before the first frame update
   

    void Start()
    {
        BasePool.Instance.SetItem(item, hitPoint);
        pool = BasePool.Instance.GetPool(item.name);
    }

    // Update is called once per frame
    private float time;
    private int count = 0;
    void Update()
    {
        time -= Time.deltaTime;
        if (isEnter)
        {
            if (time <= 0)
            {
                if (count > hitCount)
                {
                    isEnter = false;
                    count = 0;
                }
                
                time = fireTime;
                count++;
            }
            
        }
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isEnter = true;
            BasePool.Instance.SetItem(item, hitPoint);
            pool.Get();
        }
       
    }
   
}
