using UnityEngine;

public class IndicatorsManager : MonoBehaviour
{
    [Header("Indicatori")]
    [Tooltip("Indicatore per il sito di lancio (giallo/arancio).")]
    public UniversalIndicator launchSiteIndicator;
    
    [Tooltip("Indicatore per la nave in arrivo (rosso/allarme).")]
    public UniversalIndicator spaceshipIndicator;

    [Header("Configurazione Timer")]
    [Tooltip("Per quanti secondi mostrare l'indicatore della nave.")]
    public float spaceshipIndicatorDuration = 4.0f; // <--- NUOVO PARAMETRO

    // Cache per evitare ricerche continue e gestire il timer
    private Transform _currentSpaceship;
    private float _shipIndicatorTimer = 0f;
    
    // Per il Launch Site usiamo un "Dummy"
    private GameObject _launchSiteDummy;

    private void Update()
    {
        HandleLaunchSite();
        HandleSpaceship();
    }

    private void HandleLaunchSite()
    {
        if (launchSiteIndicator == null) return;

        // Fix Priority: Nascondi subito se il lancio è avvenuto
        if (PlanetManager.Instance != null && PlanetManager.Instance.isTraveling)
        {
            launchSiteIndicator.Hide();
            return;
        }

        var visualScript = FindFirstObjectByType<LaunchSiteVisuals>();

        // Check Validità
        if (visualScript == null || visualScript.GetCurrentWorldPosition() == Vector3.zero)
        {
            launchSiteIndicator.Hide();
            return;
        }

        // Dummy Target
        if (_launchSiteDummy == null)
        {
            _launchSiteDummy = new GameObject("LaunchSite_Target_Helper");
            _launchSiteDummy.transform.SetParent(transform); 
        }

        // Update Posizione
        _launchSiteDummy.transform.position = visualScript.GetCurrentWorldPosition();

        // Attiva
        launchSiteIndicator.Show(_launchSiteDummy.transform, "LAUNCH SITE");
    }

    private void HandleSpaceship()
    {
        if (spaceshipIndicator == null) return;

        // --- FASE A: Cerca una nuova nave se non ne stiamo tracciando una ---
        if (_currentSpaceship == null)
        {
            // 1. Cerca Nave in Atterraggio
            var landingShip = FindFirstObjectByType<SpaceshipLanding>();
            if (landingShip != null)
            {
                StartTrackingShip(landingShip.transform, "COLONY SHIP");
                return;
            }

            // 2. Cerca Nave in Partenza
            var flightShip = FindFirstObjectByType<SpaceshipFlight>();
            if (flightShip != null)
            {
                StartTrackingShip(flightShip.transform, "FLEET DEPARTURE");
                return;
            }
        }
        // --- FASE B: Gestisci la nave corrente ---
        else
        {
            // Se la nave è stata distrutta (null check di Unity), resetta tutto
            if (_currentSpaceship == null)
            {
                spaceshipIndicator.Hide();
                _currentSpaceship = null; // Reset per permettere di cercarne una nuova in futuro
                return;
            }

            // La nave esiste ancora: Aggiorna il Timer
            _shipIndicatorTimer += Time.deltaTime;

            // Se il tempo è scaduto, nascondi l'indicatore
            if (_shipIndicatorTimer >= spaceshipIndicatorDuration)
            {
                spaceshipIndicator.Hide();
                // NOTA IMPORTANTE: NON resettiamo _currentSpaceship a null qui!
                // Se lo facessimo, il codice tornerebbe alla Fase A, ritroverebbe la stessa nave
                // (che esiste ancora) e farebbe ripartire il timer da zero.
                // Mantenendo il riferimento, "sappiamo" che questa nave l'abbiamo già gestita.
            }
        }
    }

    private void StartTrackingShip(Transform shipTransform, string label)
    {
        _currentSpaceship = shipTransform;
        _shipIndicatorTimer = 0f; // Azzera il timer
        spaceshipIndicator.Show(_currentSpaceship, label);
    }
}