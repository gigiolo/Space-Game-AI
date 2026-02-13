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

    [Header("Configurazione Controllo")]
    [Tooltip("Ogni quanti secondi controllare se ci sono upgrade acquistabili (per performance).")]
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
            yield return wait;
        }
    }

    private void CheckResearchAvailability()
    {
        if (researchButtonEffect == null || ResearchManager.Instance == null || GameManager.Instance == null) return;

        bool canAffordAny = false;
        BigDouble currentEnergy = GameManager.Instance.CurrentEnergy;

        // Itera su tutte le ricerche disponibili
        foreach (var item in ResearchManager.Instance.allResearches)
        {
            // Se non è maxata E abbiamo abbastanza energia
            if (!item.IsMaxed() && currentEnergy >= item.GetCost())
            {
                canAffordAny = true;
                break; // Ne basta una per accendere la notifica
            }
        }

        researchButtonEffect.SetActive(canAffordAny);
    }

    private void CheckSpaceshipAvailability()
    {
        if (spaceshipButtonEffect == null || SpaceshipManager.Instance == null || GameManager.Instance == null) return;

        bool canAffordAny = false;
        BigDouble currentEnergy = GameManager.Instance.CurrentEnergy;
        BigDouble currentIridium = GameManager.Instance.PureIridium;

        foreach (var ship in SpaceshipManager.Instance.fleet)
        {
            if (ship.IsMaxed()) continue;

            BigDouble cost = ship.GetCost();
            
            // Controlla in base al tipo di valuta della nave
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
}