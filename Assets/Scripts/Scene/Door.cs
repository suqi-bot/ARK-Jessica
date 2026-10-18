using UnityEngine;

public class Door : MonoBehaviour
{
    // 门的状态
    private bool isOpen;
    // 开始角度
    Quaternion startRotation;
    // 目标角度
    Quaternion targetRotation;
    // 获取门的碰撞器
    public Collider2D coll;
    // 门移动的时间
    public float doorMoveSpeed;
    public float kickMoveSpeed;
    private float tempTime;
    // Update is called once per frame

    void Start()
    {
        startRotation = transform.rotation;
       

    }

    void Update()
    {
        
        if (isOpen)
        {
            tempTime += Time.deltaTime * doorMoveSpeed;

            this.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, tempTime );
            if(tempTime >= 1)
            {
                tempTime = 0;
                isOpen = false;
            }
        }
    }

    public void KickDoor(Transform kicker)
    {
        doorMoveSpeed = kickMoveSpeed;
        OpenDoor(kicker);
    }

    public void OpenDoor(Transform person)
    {
        isOpen = true;
        targetRotation = this.transform.position.x - person.transform.position.x > 0 ? 
            Quaternion.AngleAxis(-90, Vector3.up) : Quaternion.AngleAxis(90, Vector3.up);
        coll.enabled = false;
    }
}
