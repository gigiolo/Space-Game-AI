// --- File: _Scripts\GameBalanceManifesto.cs ---

using UnityEngine;

/// <summary>
/// DOCUMENTO UFFICIALE DI BILANCIAMENTO DEL GIOCO
/// Queste regole governano la matematica, la progressione e il feeling dell'intera economia.
/// Qualsiasi nuova ricerca, pianeta o meccanica deve sottostare a questi principi.
/// </summary>
public static class GameBalanceManifesto
{
    /*
    ====================================================================================================
    IL MANIFESTO DEL BILANCIAMENTO
    ====================================================================================================

    1. REGOLE DELL'ENGAGEMENT E DEI CLICK
    ----------------------------------------------------------------------------------------------------
    - Regola del Numero dei Click (Micro-Ricompense): A parità di incremento finale, va sempre preferita 
      una ricerca Additive con molti livelli rispetto a una ricerca Multiplier con un solo livello. 
      Questo garantisce che il giocatore abbia sempre qualcosa da cliccare e non viva lunghi "tempi morti".
      
    - Regola del 10% (Rarità dei Moltiplicatori): Le ricerche con effetto esponenziale puro (Multiplier) 
      non devono mai superare il 10% del totale delle ricerche, sia per la Logistica che per la Global 
      Production. I veri moltiplicatori devono essere traguardi rari; il grosso della progressione è 
      affidato alla somma delle ricerche additive.


    2. REGOLE DELLA CURVA DEI COSTI (PROGRESSIONE)
    ----------------------------------------------------------------------------------------------------
    - Regola del Salto di Tier (9-10x): Il costo base del primo livello della prima ricerca del Tier N+1 
      deve sempre essere tra le 9 e le 10 volte superiore al costo totale cumulativo dell'ultimo livello 
      della ricerca più costosa del Tier N. Forza la necessità di viaggiare o fare prestigio.
      
    - Regola del "Muro Morbido": Il parametro Fattore di ogni ricerca deve essere calibrato affinché 
      gli ultimi livelli diventino proibitivi, spingendo il giocatore a passare al Tier successivo.


    3. REGOLE DEL DUALISMO PRODUZIONE / LOGISTICA
    ----------------------------------------------------------------------------------------------------
    - Regola dell'Equilibrio Dinamico (80%-120%): Le ricerche di Produzione e Logistica dello stesso Tier 
      devono essere bilanciate in modo che, durante la fase attiva su un pianeta, il Logistics_Cap oscilli 
      costantemente tra l'80% e il 120% del Raw_Income.
      
    - Corollario di Compensazione: Poiché la Produzione cresce attivamente anche tramite i nuovi Emitter, 
      i Moltiplicatori/Bonus delle ricerche Logistiche devono essere leggermente superiori a quelli di 
      Produzione per riassorbire l'aumento passivo.
      
    - Regola della Tensione Breve (Max 10 Minuti): Se il divario esce dai parametri (la logistica scende 
      sotto l'80% o sale sopra il 120% del Raw Income), questa situazione deve risolversi in tempi brevi, 
      con una durata massima di 10 minuti di gioco attivo. Il collo di bottiglia deve essere una sfida 
      dinamica, non un blocco frustrante.
      
    - Regola del Tappo Iniziale (Minuto Zero): All'inizio di ogni nuovo pianeta, il limite logistico 
      base deve costringere il giocatore a scontrarsi con il primo collo di bottiglia entro i primissimi 
      minuti di gioco.


    4. REGOLE DELL'ESPANSIONE FISICA (GLI EMITTER)
    ----------------------------------------------------------------------------------------------------
    - Regola della Crescita Additiva: Il limite massimo degli Emitter (EmitterMaxCap) deve crescere 
      esclusivamente in modo additivo (es. +2, +10, +50) per evitare overflow catastrofici del sistema.
      
    - Regola dell'Auto-Costruzione (Pacing): La velocità di generazione passiva degli Emitter deve 
      saturare il limite massimo in un tempo che va dai 15 ai 30 minuti di gioco attivo 
      (o il doppio in offline).


    5. REGOLE DEL VIAGGIO INTERPLANETARIO (SOFT PRESTIGE)
    ----------------------------------------------------------------------------------------------------
    - Regola della Distanza Planetaria (Fattore x3): Il requisito di valore (Planet_Req) per sbloccare 
      il pianeta N+1 deve essere calcolato affinché un giocatore privo di Nodi Quantistici e senza l'uso 
      del Bottone Energia impieghi esattamente il triplo del tempo rispetto al pianeta precedente 
      (Partendo da 30 minuti per il P2, 90 min per il P3, 270 min per il P4). 
      
    - Corollario del Prestigio: Questa regola definisce la distanza "base" dell'universo. Dal Pianeta 
      4-5 in poi, il tempo base supererà le 10+ ore, costringendo fisicamente il giocatore a usare 
      il Reset Quantistico. I Nodi accumulati abbatteranno drasticamente questi tempi nelle run successive, 
      trasformando muri di 40 ore in attese di pochi minuti.
      
    - Regola della Gravità Universale: Il moltiplicatore di produzione del pianeta deve sempre moltiplicare 
      anche il Logistics_Cap base, altrimenti la logistica non riuscirà mai a supportare l'economia 
      interplanetaria.


    6. REGOLE DEL RESET QUANTISTICO (HARD PRESTIGE)
    ----------------------------------------------------------------------------------------------------
    - Regola del Primo Nodo: La formula deve concedere il primo Nodo Quantistico (valore 1.00) solo ed 
      esclusivamente a partire dal 3° o 4° Pianeta.
      
    - Regola dei Rendimenti Decrescenti Severi: Rimanere fermi su un pianeta nel late-game non deve 
      essere premiante. Per raddoppiare i Nodi, il giocatore deve viaggiare o resettare, non "farmare" 
      a vuoto sul muro di fine Tier.

    7. ALTRE REGOLE
    - Nessuna ricerca che aumenta la durata dei guadagni offline (questo sarà un contenuto a pagamento)

    ====================================================================================================
    */
}