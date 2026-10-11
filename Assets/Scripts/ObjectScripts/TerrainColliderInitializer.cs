using UnityEngine;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(CompositeCollider2D))]
public sealed class TerrainColliderInitializer : MonoBehaviour
{
    private void Start() => Rebuild();

    public void Rebuild()
    {
        var composite = GetComponent<CompositeCollider2D>();
        var sources = GetComponentsInChildren<Collider2D>(true);
        composite.generationType = CompositeCollider2D.GenerationType.Manual;

        // Re-register sources after all child colliders have initialized.
        // Cached composite paths can be empty after loading or instantiating a scene.
        foreach (var source in sources)
        {
            if (source == composite || source.attachedRigidbody != composite.attachedRigidbody ||
                source.compositeOperation != Collider2D.CompositeOperation.Merge)
                continue;

            source.compositeOperation = Collider2D.CompositeOperation.None;
            source.compositeOperation = Collider2D.CompositeOperation.Merge;
        }

        composite.GenerateGeometry();
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
    }
}
