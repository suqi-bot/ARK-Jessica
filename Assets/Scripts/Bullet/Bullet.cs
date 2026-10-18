using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float Speed; // 子弹速度
    [SerializeField] protected float hit; // 子弹伤害
    public bool isHit; // 是否已经击中目标
    public GameObject owner; // 子弹的拥有者
    public int ShootModel = 0; // 0:直线射击，1:曲线射击
    private bool invincible; // 是否无敌
    private bool isDoage; // 是否闪避
    [Header("曲线射击")]
    public Vector3 startPos;
    public Vector3 midPos;
    public Vector3 targetPos;
    private float time;
    void OnEnable()
    {
        hit = owner.GetComponent<CharacterBaseClass>().atk;
        Debug.Log("hit:" + hit);
        ResetBullet();
        Invoke("DestoryBullet", 5);
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime * Speed * 0.1f;
        if (ShootModel == 0)
        {
            this.transform.Translate(Speed * Vector2.right * Time.deltaTime);
        }
        else if (ShootModel == 1)
        {
            this.transform.position = CalculateBezierPoint(time, startPos, midPos, targetPos);
            if (time >= 1)
                DestoryBullet();
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log("owner"+ owner.tag);
        Debug.Log("collision" + collision.tag);
        if (collision.tag == owner.tag || isHit || collision.CompareTag("Bullet"))
        {
            return;
        }

        // ---- Glass: let the panel handle crack / shatter, then kill the bullet ----
        var glass = collision.GetComponentInParent<ARK.Glass.GlassPanel>();
        if (glass != null)
        {
            if (!isHit)
            {
                isHit = true;
                Vector2 dir = transform.right;
                glass.Hit(collision.ClosestPoint(transform.position), dir, hit);
                DestoryBullet();
            }
            return;
        }

        if (collision.CompareTag("Wall") || collision.CompareTag("Obstacle") || collision.CompareTag("Ground") || collision.CompareTag("Door"))
        {
            DestoryBullet();
        }
 
        invincible = collision.gameObject?.GetComponent<CharacterBaseClass>() == null ? true : !collision.gameObject.GetComponent<CharacterBaseClass>().isInvincible;
        isDoage = collision.gameObject?.GetComponent<CharacterBaseClass>() == null ? true : !collision.gameObject.GetComponent<CharacterBaseClass>().isDodge;
        CharacterBaseClass other = collision.gameObject?.GetComponent<CharacterBaseClass>();
        if ( collision.CompareTag("Player") && invincible && isDoage  || collision.CompareTag("Enemy") &&  invincible && isDoage)
        {
            if(!isHit)
                DestoryBullet();
            isHit = true;
            other = collision.gameObject?.GetComponent<CharacterBaseClass>();
            other.Injure(hit);
        }
        
    }

    private void DestoryBullet()
    {
        //Destroy(this.gameObject);
        CancelInvoke();
        BasePool.Instance.GetPool(this.gameObject.name).Release(this.gameObject);
        
    }
    
    public void ResetBullet()
    {
        isHit = false;
    }

    public Vector3 CalculateBezierPoint(float t, Vector3 start, Vector3 mid, Vector3 end)
    {
        return (1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * end;
    }
}
