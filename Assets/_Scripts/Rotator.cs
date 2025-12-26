using UnityEngine;

// Questo script gestisce la rotazione puramente visiva (Zen movement)
public class Rotator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Velocità di rotazione in gradi al secondo")]
    public float rotationSpeed = 5f;
    
    [Tooltip("Asse di rotazione")]
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        // Usiamo deltaTime per un movimento fluido indipendentemente dagli FPS
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}