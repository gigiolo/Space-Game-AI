using UnityEngine;
using System.Collections.Generic;

public class SpaceportHub : MonoBehaviour
{
    [Header("Slot di Lancio/Atterraggio")]
    [Tooltip("Trascina qui i GameObject vuoti posizionati al centro dei tuoi esagoni.")]
    public List<Transform> launchPads = new List<Transform>();

    // Metodo che il DroneManager userà per farsi dare uno slot a caso
    public Transform GetRandomPad()
    {
        if (launchPads != null && launchPads.Count > 0)
        {
            // Sceglie uno slot casuale dalla lista
            int randomIndex = Random.Range(0, launchPads.Count);
            return launchPads[randomIndex];
        }
        
        // Se hai dimenticato di assegnare i pad, restituisce il centro della struttura come backup
        return this.transform; 
    }
}