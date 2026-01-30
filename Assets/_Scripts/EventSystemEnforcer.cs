using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemEnforcer : MonoBehaviour
{
    void Awake()
    {
        // Cerca tutti gli EventSystem nella scena
        EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (systems.Length > 1)
        {
            foreach (var sys in systems)
            {
                // Se trovo un EventSystem che NON è questo (cioè è quello della scena nuova), lo distruggo
                if (sys.gameObject != this.gameObject)
                {
                    // Controllo extra: se l'altro è figlio di Core_Systems non lo tocco (improbabile ma sicuro)
                    if (sys.transform.root != transform.root)
                    {
                        Destroy(sys.gameObject);
                        Debug.Log("EventSystem duplicato rimosso automaticamente.");
                    }
                }
            }
        }
    }

    // Eseguiamo il controllo anche ogni volta che carichi una nuova scena
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Al caricamento, rieseguiamo la pulizia
        Awake();
    }
}