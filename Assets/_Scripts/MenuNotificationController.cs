using UnityEngine;
using BreakInfinity;
using System.Collections;

public class MenuNotificationController : MonoBehaviour
{
    [Header("Riferimenti Bottoni")]
    [Tooltip("Trascina qui l'oggetto con lo script AttentionPulseEffect del bottone Ricerca.")]
    public AttentionPulseEffect researchButtonEffect;

    [Tooltip("Trascina qui l'oggetto con lo script AttentionPulseEffect del bottone Navi Spaziali.")]
    public AttentionPulseEffect spaceshipButtonEffect;

    [Tooltip("Trascina qui l'oggetto con lo script AttentionPulseEffect del bottone Planet Status (in alto a sinistra).")]
    public AttentionPulseEffect planetButtonEffect; // <--- NUOVO

    [Header("Riferimenti UI per Stop")]
    [Tooltip("Trascina qui il pannello del PlanetStatusPopup. Se è aperto, smettiamo di pulsare.")]
    public GameObject planetPopupPanel; // <--- NUOVO

    [Header("Configurazione Controllo")]
    [Tooltip("Ogni quanti secondi controllare se ci sono notifiche (per performance).")]
    public float checkInterval = 1.0f;

    private void Start()
    {
        // Avvia il loop di controllo
        StartCoroutine(CheckRoutine());
    }

    private IEnumerator CheckRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            CheckResearchAvailability();
            CheckSpaceshipAvailability();
            CheckPlanetAvailability(); // <--- NUOVO
            yield return wait;
        }
    }

    // --- LOGICA RICERCA ---
    private void CheckResearchAvailability()
    {
        if (researchButtonEffect == null || ResearchManager.Instance == null || GameManager.Instance == null) return;

        if (ResearchManager.Instance.menuPanel != null && ResearchManager.Instance.menuPanel.activeSelf)
        {
            researchButtonEffect.SetActive(false);
            return;
        }

        bool canAffordAny = false;
        BigDouble currentEnergy = GameManager.Instance.CurrentEnergy;

        foreach (var item in ResearchManager.Instance.allResearches)
        {
            if (!item.IsMaxed() && currentEnergy >= item.GetCost())
            {
                canAffordAny = true;
                break; 
            }
        }

        researchButtonEffect.SetActive(canAffordAny);
    }

    // --- LOGICA NAVI ---
    private void CheckSpaceshipAvailability()
    {
        if (spaceshipButtonEffect == null || SpaceshipManager.Instance == null || GameManager.Instance == null) return;

        if (SpaceshipManager.Instance.menuPanel != null && SpaceshipManager.Instance.menuPanel.activeSelf)
        {
            spaceshipButtonEffect.SetActive(false);
            return;
        }

        bool canAffordAny = false;
        BigDouble currentEnergy = GameManager.Instance.CurrentEnergy;
        BigDouble currentIridium = GameManager.Instance.PureIridium;

        foreach (var ship in SpaceshipManager.Instance.fleet)
        {
            if (ship.IsMaxed()) continue;

            BigDouble cost = ship.GetCost();
            
            if (ship.info.currencyType == SpaceshipCurrency.Energy)
            {
                if (currentEnergy >= cost)
                {
                    canAffordAny = true;
                    break;
                }
            }
            else // Pure Iridium
            {
                if (currentIridium >= cost)
                {
                    canAffordAny = true;
                    break;
                }
            }
        }

        spaceshipButtonEffect.SetActive(canAffordAny);
    }

    // --- NUOVA LOGICA PIANETA ---
    private void CheckPlanetAvailability()
    {
        // Se non abbiamo assegnato l'effetto, usciamo
        if (planetButtonEffect == null || PlanetManager.Instance == null) return;

        // 1. Se il popup del pianeta è già aperto, spegni l'effetto (non serve più attirare l'attenzione)
        if (planetPopupPanel != null && planetPopupPanel.activeSelf)
        {
            planetButtonEffect.SetActive(false);
            return;
        }

        // 2. Se stiamo già preparando o viaggiando, non pulsare (l'azione è già in corso)
        if (PlanetManager.Instance.isPreparingForLaunch || PlanetManager.Instance.isTraveling)
        {
            planetButtonEffect.SetActive(false);
            return;
        }

        // 3. Controlla se abbiamo raggiunto il valore target
        PlanetData currentData = PlanetManager.Instance.GetCurrentPlanetData();
        if (currentData == null) return;

        BigDouble currentValue = PlanetManager.Instance.CalculatePlanetValue();
        BigDouble requiredValue = currentData.requiredPlanetValue;

        // Attiva l'effetto SOLO se abbiamo superato la soglia richiesta
        bool isReadyToLaunch = currentValue >= requiredValue;

        planetButtonEffect.SetActive(isReadyToLaunch);
    }
}