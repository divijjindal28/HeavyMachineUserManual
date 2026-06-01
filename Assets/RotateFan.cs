using UnityEngine;

public class RotateFan : MonoBehaviour
{
    [SerializeField] private GameObject fanBlade; // The blade to rotate
    [SerializeField] private Transform pivotPoint; // Empty GameObject
    [SerializeField] private float rotationSpeed = 100f;

    private void Update()
    {
        if (pivotPoint == null) return;

        // Rotate around the pivot's local up axis
        fanBlade.transform.RotateAround(
            pivotPoint.position,
            pivotPoint.forward,
            rotationSpeed * Time.deltaTime
        );
    }
}