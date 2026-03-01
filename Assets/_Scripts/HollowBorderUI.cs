// --- File: _Scripts\HollowBorderUI.cs ---
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("UI/Hollow Border")]
public class HollowBorderUI : MaskableGraphic
{
    [Header("Impostazioni Bordo")]
    [Tooltip("Lo spessore del bordo in pixel")]
    public float thickness = 5f;

    // Questo metodo viene chiamato da Unity ogni volta che disegna l'UI.
    // Invece di disegnare un quadrato pieno, disegniamo 4 rettangoli per fare una cornice.
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = GetPixelAdjustedRect();
        Color32 c32 = color; // Usa il colore impostato nel componente

        // 1. Rettangolo SINISTRO
        AddQuad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + thickness, r.yMax), c32);
        
        // 2. Rettangolo DESTRO
        AddQuad(vh, new Vector2(r.xMax - thickness, r.yMin), new Vector2(r.xMax, r.yMax), c32);
        
        // 3. Rettangolo SUPERIORE (tra il sinistro e il destro)
        AddQuad(vh, new Vector2(r.xMin + thickness, r.yMax - thickness), new Vector2(r.xMax - thickness, r.yMax), c32);
        
        // 4. Rettangolo INFERIORE (tra il sinistro e il destro)
        AddQuad(vh, new Vector2(r.xMin + thickness, r.yMin), new Vector2(r.xMax - thickness, r.yMin + thickness), c32);
    }

    // Metodo di supporto per disegnare un rettangolo dati due punti (min e max)
    private void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 c)
    {
        int startIndex = vh.currentVertCount;

        // Aggiungiamo i 4 angoli del rettangolo
        vh.AddVert(new Vector3(min.x, min.y), c, Vector2.zero);
        vh.AddVert(new Vector3(min.x, max.y), c, Vector2.up);
        vh.AddVert(new Vector3(max.x, max.y), c, Vector2.one);
        vh.AddVert(new Vector3(max.x, min.y), c, Vector2.right);

        // Creiamo i due triangoli che formano il rettangolo
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    // Questo fa in modo che se cambi lo spessore nell'Editor, si aggiorni subito in tempo reale
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        thickness = Mathf.Max(0, thickness); // Evita spessori negativi
        SetVerticesDirty(); // Forza Unity a ridisegnare la mesh
    }
#endif
}
