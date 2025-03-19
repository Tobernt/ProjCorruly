using UnityEngine;

public class SpinObject : MonoBehaviour
{
    [Header("Spin Settings")]
    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 90f; // degrees per second

    private void Update()
    {
        transform.Rotate(spinAxis.normalized * spinSpeed * Time.deltaTime);
    }
}
