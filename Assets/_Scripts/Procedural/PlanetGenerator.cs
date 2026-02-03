using UnityEngine;

[ExecuteAlways]
public class PlanetGenerator : MonoBehaviour
{
    [Header("Materials (Must be assigned)")]
    public Material baseSurfaceMaterial;    // Shader Graph Lit (Surface)
    public Material baseAtmosphereMaterial; // Shader Graph Unlit/Transparent (Atmo)
    public Material baseCloudMaterial;      // Shader Graph Lit/Transparent (Clouds) <--- NUOVO

    [Header("Debug / Editor")]
    [Tooltip("Trascina qui un visual data per testare in Editor senza avviare il gioco.")]
    public PlanetVisualData editorPreviewData;

    // Riferimenti agli oggetti generati
    private GameObject _surfaceObj;
    private GameObject _atmosphereObj;
    private GameObject _cloudsObj; // <--- NUOVO
    
    private PlanetVisualData _currentData;

    private void Start()
    {
        // Se siamo in Play Mode, cerchiamo i dati ufficiali dal Manager
        if (Application.isPlaying)
        {
            if (PlanetManager.Instance != null)
            {
                var planetData = PlanetManager.Instance.GetCurrentPlanetData();
                if (planetData != null && planetData.visualData != null)
                {
                    Generate(planetData.visualData);
                }
            }
        }
        else
        {
            // In Editor Mode, usiamo i dati di preview
            if (editorPreviewData != null) Generate(editorPreviewData);
        }
    }

    // Aggiornamento automatico in Editor quando cambi i valori
    private void OnValidate()
    {
        if (!Application.isPlaying && editorPreviewData != null)
        {
            // Delay per evitare errori durante la compilazione o il trascinamento
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => 
            {
                if (this != null) Generate(editorPreviewData);
            };
            #endif
        }
    }

    [ContextMenu("Force Regenerate")]
    public void Regenerate()
    {
        if (editorPreviewData != null) Generate(editorPreviewData);
    }

    private void Generate(PlanetVisualData data)
    {
        _currentData = data;

        // 1. PULIZIA
        // Rimuoviamo solo i figli creati da questo script (che iniziano con "Proc_")
        var children = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Proc_")) children.Add(child.gameObject);
        }
        foreach (var child in children)
        {
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        // 2. CREAZIONE MESH (Singola mesh condivisa per Surface e Atmo)
        Mesh planetMesh = IcosphereCreator.Create(data.resolution);

        // 3. LAYER SUPERFICIE
        _surfaceObj = CreateLayer("Proc_Surface", data.radius, planetMesh, baseSurfaceMaterial);
        ApplySurfaceProps(_surfaceObj.GetComponent<MeshRenderer>(), data);

        // 4. LAYER ATMOSFERA
        if (data.hasAtmosphere)
        {
            float atmoRadius = data.radius * data.atmosphereHeight;
            _atmosphereObj = CreateLayer("Proc_Atmosphere", atmoRadius, planetMesh, baseAtmosphereMaterial);
            ApplyAtmosphereProps(_atmosphereObj.GetComponent<MeshRenderer>(), data);
        }

        // 5. LAYER NUVOLE (NUOVO)
        if (data.hasClouds && baseCloudMaterial != null)
        {
            // Le nuvole usano la stessa sfera, leggermente più grande della superficie ma sotto l'atmosfera
            float cloudRadius = data.radius * data.cloudHeight;
            _cloudsObj = CreateLayer("Proc_Clouds", cloudRadius, planetMesh, baseCloudMaterial);
            ApplyCloudProps(_cloudsObj.GetComponent<MeshRenderer>(), data);

            // Aggiungiamo il componente Rotator per farle girare indipendentemente
            var cloudRotator = _cloudsObj.AddComponent<Rotator>();
            // Configurazione rotazione (Solo asse Y per simulare venti)
            // Nota: Assicurati che il tuo script Rotator supporti Vector3. 
            // Se supporta solo float, adatta questa riga o usa cloudRotator.rotationSpeed = data.cloudRotationSpeed;
            cloudRotator.rotationSpeed = new Vector3(0, data.cloudRotationSpeed, 0); 
        }

        // 6. INTEGRAZIONE LUCI CITTÀ (Sistema esistente)
        var popVisuals = GetComponent<PlanetPopulationVisuals>();
        if (popVisuals != null)
        {
            popVisuals.surfaceRadius = data.radius;
            if (Application.isPlaying) popVisuals.RefreshLights();
        }
    }

    private GameObject CreateLayer(string name, float scale, Mesh mesh, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        obj.transform.localScale = Vector3.one * scale;
        
        // DontSaveInEditor evita che Unity salvi la mesh generata nel file .unity, mantenendolo leggero
        obj.hideFlags = HideFlags.DontSaveInEditor; 

        var mf = obj.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        return obj;
    }

    // --- APPLICAZIONE PROPRIETÀ MATERIALI (MaterialPropertyBlock) ---

    private void ApplySurfaceProps(MeshRenderer r, PlanetVisualData d)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        if (d.albedoMap) mpb.SetTexture("_BaseMap", d.albedoMap);
        if (d.normalMap) mpb.SetTexture("_BumpMap", d.normalMap);
        // Emission rimossa come da richiesta precedente, ma il codice rimane sicuro
        if (d.emissionMap) 
        {
            mpb.SetTexture("_EmissionMap", d.emissionMap);
            mpb.SetColor("_EmissionColor", Color.white);
        }
        mpb.SetColor("_BaseColor", d.tintColor);
        r.SetPropertyBlock(mpb);
    }

    private void ApplyAtmosphereProps(MeshRenderer r, PlanetVisualData d)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", d.atmosphereColor);
        mpb.SetFloat("_FresnelPower", d.fresnelPower);
        r.SetPropertyBlock(mpb);
    }

    private void ApplyCloudProps(MeshRenderer r, PlanetVisualData d)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        if (d.cloudTexture) mpb.SetTexture("_BaseMap", d.cloudTexture);
        mpb.SetColor("_BaseColor", d.cloudColor);
        r.SetPropertyBlock(mpb);
    }
}