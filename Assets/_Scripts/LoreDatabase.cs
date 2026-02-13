using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ShipLogDB", menuName = "Aetheris/Lore Database")]
public class LoreDatabase : ScriptableObject
{
    [Header("Filosofia Cosmica (Random)")]
    [TextArea] public List<string> philosophicalLogs = new List<string>()
    {
        "Solo gli occhi aperti possono scoprire che l'universo è il libro della più alta Verità.",
        "Se fossimo soli l'immensità sarebbe davvero uno spreco.",
        "Guardare lontano significa guardare nel passato.",
        "Tutto è perfetto nell'universo, anche il tuo desiderio di migliorarlo.",
        "La materia è solo energia che ha deciso di rallentare.",
        "L'universo è fatto di storie, non di atomi.",
    };

    [Header("Tecnobabble / Analisi (Random)")]
    [TextArea] public List<string> techLogs = new List<string>()
    {
        "Analisi spettrografica completata: Tracce di carbonio rilevate.",
        "Ricalibrazione sensori a lungo raggio...",
        "Intercettata onda gravitazionale da una collisione di buchi neri.",
        "Temperatura esterna: -270.45 gradi Celsius.",
        "Sistemi di supporto vitale: Stabili. Umore equipaggio: Malinconico."
    };

    [Header("Eventi Specifici (Chiamati da codice)")]
    public string quantumResetLog = "Inizializzazione protocollo Big Crunch. Riavvolgimento temporale...";
    public string travelLog = "Motori a curvatura attivi. La relatività è in effetto.";
    public string adFinishedLog = "Segnale commerciale terminato. Il silenzio è tornato.";

    public string GetRandomLog()
    {
        // 50% filosofia, 50% tech
        if (Random.value > 0.5f && philosophicalLogs.Count > 0)
            return "MEMO: " + philosophicalLogs[Random.Range(0, philosophicalLogs.Count)];
        
        if (techLogs.Count > 0)
            return "SYS: " + techLogs[Random.Range(0, techLogs.Count)];

        return "...";
    }
}