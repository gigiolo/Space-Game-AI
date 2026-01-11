using UnityEngine;
using System;

// Questa classe definisce "cos'è" una notifica
[Serializable]
public class NotificationData
{
    public string title;            // Es: "Daily Gift"
    public string description;      // Es: "Here is 50 Energy!"
    public Sprite icon;             // L'icona da mostrare
    public Action onClaimAction;    // La funzione da eseguire quando prendi il premio (codice flessibile)
    public bool isAds;              // Se è true, magari mostri l'icona "Video"

    // Costruttore per creare notifiche velocemente
    public NotificationData(string t, string d, Sprite i, Action action, bool ads = false)
    {
        title = t;
        description = d;
        icon = i;
        onClaimAction = action;
        isAds = ads;
    }
}