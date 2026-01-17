/* ================================================================
CONTESTO DEL PROGETTO
================================================================
# PROJECT CONTEXT FILE: Space Inc. (Unity 6.3)

**ISTRUZIONI PER L'AI:**
Questo file è la **Fonte di Verità** per lo sviluppo di "Space Inc.". 
Usalo per comprendere l'architettura attuale, le dipendenze e lo stato di avanzamento prima di proporre nuovo codice.

---

## 1. 🎯 Obiettivo del Progetto
Sviluppare **"Space Inc."**, un videogioco Mobile (Android/iOS) di tipo **Idle/Clicker** ambientato nello spazio.

* **Core Loop:**
    1.  Il giocatore produce **Energy** tramite **Emitters**.
    2.  L'energia è limitata da **Logistica** (flusso/sec) e **Storage** (capacità max).
    3.  L'energia si spende per upgrade e **Ricerche**.
    4.  **Prestigio Soft (Quantum):** Accumulo `ScientificNodes` per bonus permanenti.
    5.  **Prestigio Hard (Viaggio Planetario):** Accumulo energia per "Launch Prep", viaggio verso nuovi pianeti (Scene diverse), reset delle strutture ma mantenimento dei nodi.

---

## 2. 🛠 Tech Stack & Strumenti
* **Engine:** Unity 6.3 (C#).
* **Matematica:** `BreakInfinity.cs` (`BigDouble` obbligatorio per valute/produzione).
* **UI:** TextMeshPro (TMP), `UITheme` (ScriptableObject per skinning dinamico).
* **Visual:** Particle System (Shuriken) ottimizzato (`SetParticles` buffer), Shader Graph.
* **Salvataggio:** JSON serializzato + Encoding Base64 (`SaveManager.cs`).
* **Scene Management:** Multi-scena (ogni pianeta è una scena, gestito da `PlanetManager`).

---

## 3. 🏗 Architettura Attuale (Codebase Analysis)

### Core Managers
* **`GameManager` (Singleton, Persistent):**
    * Cuore economico. Calcola `EffectiveIncomePerSec` (Min tra Produzione e Logistica).
    * Gestisce il **Button Ramp-Up** (Stati: Idle, RampingUp, HoldingMax, Cooldown).
    * Gestisce il calcolo **Offline Progress** (`HandleOfflineProgress`) al caricamento.
    * Gestisce reset parziali (Planet Change) e totali (Hard/Quantum).
    * Evento chiave: `OnEconomyUpdated`.

* **`PlanetManager` (Singleton, Persistent):**
    * Gestisce la lista `PlanetData` (ScriptableObjects).
    * Logica di **Launch Preparation**: Consuma energia nel tempo per riempire la barra di lancio.
    * Gestisce il cambio scena e il timer di viaggio (`isTraveling`).
    * Fix critico implementato: "Locked Launch Requirement" (il costo si blocca all'inizio della preparazione).

* **`ResearchManager` (Scene-Specific):**
    * Gestisce il Tech Tree. Carica dati da `ResearchDefinition` (SO).
    * Supporta costi manuali (primi livelli) e curve esponenziali/lineari (livelli infiniti).
    * Reset dei livelli al cambio pianeta.

* **`DailyGiftManager`:**
    * Gestisce premi giornalieri (28 giorni ciclici definiti in `DailyRewardSO`).
    * Logica temporale basata su `DateTime` e "Logical Day" (reset alle 03:00 AM).
    * Integra notifiche visive.

* **`NotificationManager` & `RewardNotificationManager`:**
    * Sistema centralizzato per popup e banner laterali (`NotificationButtonUI`).
    * `RewardNotificationManager`: Genera premi "Energy Bonus" periodici basati sulla produzione attuale.

### Visual & Rendering
* **`PlanetPopulationVisuals`:** * Popola la superficie del pianeta con luci (Particle System) basate sul numero di `Emitters`.
    * Usa un buffer di particelle per performance.
    * Gestisce animazione "Flash" alla nascita e persistenza posizioni luci nel salvataggio.
* **`UIPopupEffect`:** Gestisce animazioni di apertura/chiusura finestre (Tweening procedurale su Scala e Alpha).
* **`PlanetOrbitCamera`:** Camera orbitale con `UIBlocker` per fermare l'input quando si usa la UI.

---

## 4. 📝 Regole Matematiche & Formule

### A. Economia
* **Income Effettivo:** `Min(RawProduction, LogisticsCap)`
* **Raw Production:** `(EmitterCount * BaseEmission) * Multipliers`
* **Planet Value:** Usato per sbloccare il viaggio. `StableProduction * EmitterCap * BalanceFactor`.

### B. Formule di Costo (ResearchItem.cs)
Supporta un sistema ibrido stile "Egg Inc":
1.  **Manual List:** I primi livelli hanno costi fissi definiti a mano.
2.  **Automatic Curve:** Dopo la lista manuale, usa `Base * (Factor ^ LevelsBeyondManual)`.

### C. Offline Calculation
* Max Tempo Offline: `7200s (2h)` base + Bonus Ricerca.
* Capacità Accumulo: Se il giocatore sta via più del tempo massimo, guadagna solo fino al cap ("Batteries Died").

---

## 5. 📂 Dati & Salvataggio (SaveData.cs)
Struttura JSON complessa che include:
* **Economia:** Energy, LifetimeEarnings, EmitterCount, LogisticsLevel.
* **Progressione:** `ScientificNodes` (Prestigio), `CurrentPlanetIndex`.
* **Stato Viaggio:** `isPreparingForLaunch`, `launchPreparationProgress`, `lockedLaunchRequirement`.
* **Visual:** `cityLightPositions` (Lista stringhe coordinate luci pianeta).
* **Daily:** Timestamp ultimo claim, indice giorno corrente.

---

## 6. 📅 Stato dello Sviluppo & Roadmap

### ✅ Implementato (Completato)
* [x] **Economy Loop:** Produzione, Logistica, Storage, Ricerche.
* [x] **Energy Button:** Meccanica Ramp-Up e Cooldown visualizzata in UI.
* [x] **Planet System:** Preparazione lancio, barra progresso, viaggio tra scene, caricamento dati per pianeta.
* [x] **Offline System:** Popup riassuntivo con calcolo guadagni e warning batteria scarica.
* [x] **Daily Rewards:** Sistema 28 giorni con persistenza.
* [x] **Notification System:** Manager generico per eventi e rewards.
* [x] **Visuals:** Luci procedurali sul pianeta salvate tra le sessioni.
* [x] **UI Themes:** Sistema di skinning (`ThemedUIElement`).

### 🚧 Da Implementare / Migliorare (TODO Priority)
1.  **Content Expansion:**
    * Creare scene Unity specifiche per i pianeti successivi (Marte, Giove, ecc.) configurate in `PlanetData`.
    * Espandere il database delle Ricerche (attualmente limitato).
2.  **Audio:** Manca completamente il comparto audio (SFX click, Music, Ambient, Notification sounds).
3.  **Monetizzazione (Placeholder):** I bottoni "Ads" e "Premium Currency" sono presenti nel codice ma logica vuota (`Debug.Log`).
4.  **Tutorial:** Un sistema guidato per il primo avvio (attualmente il giocatore inizia senza guida).

## 7. Procedura Operativa per Gemini

1.  Verifica sempre se la modifica richiesta impatta `SaveData` (necessita aggiornamento versioning o reset).
2.  Se si aggiungono variabili a `GameManager`, controllare se vanno resettate in `PerformPlanetChangeReset` o `PerformQuantumReset`.
3.  Usa `BigDouble` per qualsiasi valore economico.
4.  Mantieni la distinzione tra Manager Persistenti (`DontDestroyOnLoad`) e Manager di Scena (`ResearchManager`, `UIManager`).
5.  Prendi decisioni e scrivi codice come se fossi un Programmatore Senior esperto di Unity, ma spiega passo passo le procedure da seguire perchè ti rivolgi ad una persona che non è un programmatore. Possibilmente dai una spiegazione di cosa stiamo facendo e perchè l’obiettivo secondario è l’apprendimento del processo di sviluppo
6.  Prima di proporre nuove funzionalità, verifica che non esistano già implementazioni parziali o placeholder nel codice esistente.
7.  Documenta sempre le modifiche significative nel contesto del progetto, aggiornando questo file se necessario
8.  Quando scrivi codice, segui le "Coding Style Rules" elencate nella sezione 8 di questo documento per garantire coerenza e leggibilità.
9.  Prima di finalizzare una modifica, esegui test approfonditi per assicurarti che non ci siano regressioni o bug introdotti.
10. Comunica chiaramente ogni modifica significativa al team, includendo dettagli tecnici e motivazioni dietro le decisioni prese.
11. Mantieni sempre una copia di backup del progetto prima di apportare modifiche sostanziali al codice o alla struttura dei dati.
12. Non usare Instantiate in Update.

 ... (fine della sezione Procedura Operativa)

================================================================
## 8. ⚡ API CHEAT SHEET (Riferimento Rapido)
Usa questi riferimenti esatti per evitare errori di compilazione.
================================================================

### GAME MANAGER (Singleton: GameManager.Instance)
- Valute: `CurrentEnergy` (BigDouble), `ScientificNodes` (BigDouble).
- Stats: `EffectiveIncomePerSec` (Prod reale), `EmitterCount` (int).
- Metodi: `AddEnergy(amount)`, `TrySpend(amount)`, `SaveGame()`.

### NOTIFICATION MANAGER (Singleton: NotificationManager.Instance)
- Spawn: `SpawnNotification(new NotificationData(title, desc, icon, action))`
- Icone: `giftIcon`, `moneyIcon`.

### PLANET MANAGER (Singleton: PlanetManager.Instance)
- Dati: `GetCurrentPlanetData()` -> restituisce `PlanetData` (SO).
- Stato: `isPreparingForLaunch` (bool), `isTraveling` (bool).
- Metodi: `StartLaunchPreparation()`.

### UTILS & MATH
- BigDouble: Usare `BigDouble` per tutto.
  - Esempio formattazione: `MyValue.ToString("F2")` o `FormatNumber(MyValue)`.
  - Math: `BigDouble.Pow()`, `BigDouble.Log10()`. NO `Mathf.Pow` su BigDouble.

### CODING STYLE RULES
1. Serializzazione: `[SerializeField] private` invece di `public`.
2. Loop: Evitare `foreach` in Update, usare `for`.
3. Stringhe: Usare `const string` per chiavi JSON o PlayerPrefs.
4. Header: Usare sempre `[Header("...")]` per raggruppare variabili inspector.
*/