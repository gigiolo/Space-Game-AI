using UnityEngine;

// Gestisce la rotazione continua di un oggetto (es. Skybox, Pianeta decorativo, Anelli)
public class Rotator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Velocità di rotazione per ogni asse in gradi al secondo.\nEsempio: Y=0.5 per una rotazione orizzontale molto lenta.")]
    public Vector3 rotationSpeed = new Vector3(0f, 1f, 0f);

    [Tooltip("Definisce se ruotare rispetto ai propri assi (Self) o al mondo (World). Per una Skybox sferica, 'Self' è solitamente corretto.")]
    public Space rotationSpace = Space.Self;

    void Update()
    {
        // Ruotiamo usando deltaTime per garantire fluidità indipendente dagli FPS
        transform.Rotate(rotationSpeed * Time.deltaTime, rotationSpace);
    }
}