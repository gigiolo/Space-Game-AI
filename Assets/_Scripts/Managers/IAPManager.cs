// --- File: _Scripts\Managers\IAPManager.cs ---
using UnityEngine;
using UnityEngine.Purchasing;
using System;

public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance { get; private set; }

    [Header("Configurazione")]
    [Tooltip("Mostra log dettagliati nella console per il debug.")]
    [SerializeField] private bool debugMode = true;

    // --- ID DEI PRODOTTI ---
    // Questi dovranno essere IDENTICI a quelli che creeremo su Google Play / App Store in futuro.
    private const string PRODUCT_IRIDIUM_50 = "com.tuonome.spaceinc.iridium50";
    private const string PRODUCT_IRIDIUM_200 = "com.tuonome.spaceinc.iridium200";
    private const string PRODUCT_IRIDIUM_500 = "com.tuonome.spaceinc.iridium500";

    // Variabili interne di Unity IAP
    private IStoreController _storeController;
    private IExtensionProvider _storeExtensionProvider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Sopravvive ai cambi di scena come gli altri Manager
    }

    private void Start()
    {
        // Inizializza il sistema di acquisti all'avvio del gioco
        if (_storeController == null)
        {
            InitializePurchasing();
        }
    }

    private void InitializePurchasing()
    {
        if (IsInitialized()) return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Registriamo i nostri 3 pacchetti come beni "Consumabili" (puoi comprarli infinite volte)
        builder.AddProduct(PRODUCT_IRIDIUM_50, ProductType.Consumable);
        builder.AddProduct(PRODUCT_IRIDIUM_200, ProductType.Consumable);
        builder.AddProduct(PRODUCT_IRIDIUM_500, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    private bool IsInitialized()
    {
        return _storeController != null && _storeExtensionProvider != null;
    }

    // --- METODI PUBBLICI PER I BOTTONI DELLA UI ---

    public void BuyIridium50() => BuyProduct(PRODUCT_IRIDIUM_50);
    public void BuyIridium200() => BuyProduct(PRODUCT_IRIDIUM_200);
    public void BuyIridium500() => BuyProduct(PRODUCT_IRIDIUM_500);

    private void BuyProduct(string productId)
    {
        if (IsInitialized())
        {
            Product product = _storeController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                if (debugMode) Debug.Log($"[IAPManager] Avvio acquisto per: {product.definition.id}");
                _storeController.InitiatePurchase(product);
            }
            else
            {
                if (debugMode) Debug.LogWarning("[IAPManager] Prodotto non trovato o non disponibile.");
            }
        }
        else
        {
            if (debugMode) Debug.LogWarning("[IAPManager] Acquisti non inizializzati.");
        }
    }

    // --- CALLBACK DA UNITY IAP (IStoreListener) ---

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        if (debugMode) Debug.Log("[IAPManager] Unity IAP Inizializzato con successo.");
        _storeController = controller;
        _storeExtensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        if (debugMode) Debug.LogError($"[IAPManager] Inizializzazione fallita. Errore: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        if (debugMode) Debug.LogError($"[IAPManager] Inizializzazione fallita. Errore: {error} - Msg: {message}");
    }

    // QUESTO È IL PUNTO CRITICO: Cosa succede quando l'acquisto va a buon fine
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;
        
        if (string.Equals(id, PRODUCT_IRIDIUM_50, StringComparison.Ordinal))
        {
            GiveReward(50);
        }
        else if (string.Equals(id, PRODUCT_IRIDIUM_200, StringComparison.Ordinal))
        {
            GiveReward(200);
        }
        else if (string.Equals(id, PRODUCT_IRIDIUM_500, StringComparison.Ordinal))
        {
            GiveReward(500);
        }
        else
        {
            if (debugMode) Debug.LogWarning($"[IAPManager] Prodotto sconosciuto acquistato: {id}");
        }

        // Restituisce "Complete" per dire allo store (Google/Apple) che abbiamo consegnato la merce
        return PurchaseProcessingResult.Complete;
    }

    private void GiveReward(int amount)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPureIridium(amount);
            if (debugMode) Debug.Log($"[IAPManager] Transazione completata! Aggiunti {amount} Iridio Puro.");
            
            // Opzionale: Mostra un messaggio nel terminale se esiste
            if (ShipTerminalController.Instance != null)
            {
                ShipTerminalController.Instance.ShowSystemMessage($"ACQUISTO COMPLETATO. +{amount} IRIDIO PURO ESTRATTO.");
            }
        }
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        if (debugMode) Debug.LogError($"[IAPManager] Acquisto fallito per {product.definition.id}. Motivo: {failureReason}");
    }
}