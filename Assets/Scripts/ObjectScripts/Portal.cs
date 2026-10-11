using Unity.VisualScripting;
using UnityEngine;
    

public class Portal : MonoBehaviour
{
    public PortalTransform portal;
    [SerializeField] private DescendingCeiling descendingCeiling;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"Collision! Transform to {portal}");
            GameManager.Instance.GoThroughPortal(portal);
            if (portal == PortalTransform.SHIP && descendingCeiling != null)
                descendingCeiling.BeginDescent();
        }
    }
}
 
