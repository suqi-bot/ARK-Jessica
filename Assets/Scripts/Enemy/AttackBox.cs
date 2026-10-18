using UnityEngine;

public class AttackBox : MonoBehaviour
{
    public Collider2D[] targetColliders;

    [SerializeField] private float targetCheckRadius;
    [SerializeField] private LayerMask target;


    public Collider2D[] GetDetectedColliders()
    {
        targetColliders = Physics2D.OverlapCircleAll(this.transform.position, targetCheckRadius , target);
        return targetColliders;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(this.transform.position, targetCheckRadius);
    }
}
