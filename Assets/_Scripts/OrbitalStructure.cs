using UnityEngine;

public class OrbitalStructure : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Trascina qui il pianeta. Se lo lasci vuoto, userà il centro della scena (0,0,0).")]
    public Transform planetTarget;

    [Header("Impostazioni Orbita")]
    [Tooltip("Quanto è distante l'orbita dal centro del pianeta?")]
    public float orbitDistance = 5.0f;
    
    [Tooltip("Velocità di rivoluzione (Gradi al secondo). Valori negativi per girare al contrario.")]
    public float orbitSpeed = 15.0f;

    [Tooltip("L'inclinazione dell'orbita. \n(0, 1, 0) = Equatore.\n(1, 0, 0) = Poli.\n(1, 1, 0) = Diagonale.")]
    public Vector3 orbitAxis = Vector3.up;

    [Header("Orientamento")]
    [Tooltip("Se VERO, l'oggetto guarderà costantemente il centro del pianeta.")]
    public bool alwaysFacePlanet = true;

    void Update()
    {
        // 1. Definisci il centro (se non hai assegnato il pianeta, usa il centro dell'universo)
        Vector3 center = planetTarget != null ? planetTarget.position : Vector3.zero;

        // 2. Fai ruotare l'oggetto attorno all'asse scelto
        transform.RotateAround(center, orbitAxis, orbitSpeed * Time.deltaTime);

        // 3. Correzione della distanza (evita che l'oggetto "scivoli" via nel tempo per errori di calcolo)
        Vector3 directionFromCenter = (transform.position - center).normalized;
        transform.position = center + (directionFromCenter * orbitDistance);

        // 4. Orientamento verso il pianeta
        if (alwaysFacePlanet)
        {
            // L'asse Z (avanti) dell'oggetto punterà verso il centro del pianeta.
            // Usiamo il vettore 'directionFromCenter' ma invertito, perché vogliamo guardare VERSO il centro, non VIA dal centro.
            transform.rotation = Quaternion.LookRotation(-directionFromCenter, Vector3.up);
        }
    }
}