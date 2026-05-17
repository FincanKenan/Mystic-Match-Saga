using UnityEngine;

[ExecuteAlways]
public class LayoutGridGuide : MonoBehaviour
{
    [Header("Grid Durumu")]
    [SerializeField] private bool showGrid = true;

    [Header("Grid Boyutu")]
    [Min(1)][SerializeField] private int columns = 12;
    [Min(1)][SerializeField] private int rows = 8;

    [Tooltip("Kapalýysa manuel cell size kullanýlýr.")]
    [SerializeField] private bool useSpriteSizeAsCellSize = true;

    [Min(0.05f)]
    [SerializeField] private float manualCellSize = 0.5f;

    [Header("Tile Referansý")]
    [SerializeField] private Sprite referenceTileSprite;
    [SerializeField] private float tileScale = 1f;

    [Tooltip("Ufak boþluk veya overlap düzeltmesi.")]
    [SerializeField] private float cellPadding = 0f;

    [Header("Grid Görünümü")]
    [SerializeField]
    private Color gridColor =
        new Color(0f, 1f, 1f, 0.35f);

    [SerializeField]
    private Color centerLineColor =
        new Color(1f, 1f, 0f, 0.55f);

    [Header("Gizmo")]
    [SerializeField] private bool drawOnlyWhenSelected = false;

    private void OnDrawGizmos()
    {
        if (drawOnlyWhenSelected)
            return;

        DrawGrid();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawOnlyWhenSelected)
            return;

        DrawGrid();
    }

    private void DrawGrid()
    {
        if (!showGrid)
            return;

        float cellSize = GetCellSize();

        if (columns <= 0 || rows <= 0 || cellSize <= 0f)
            return;

        Vector3 origin = transform.position;

        float width = columns * cellSize;
        float height = rows * cellSize;

        Vector3 bottomLeft =
            origin - new Vector3(width * 0.5f, height * 0.5f, 0f);

        Gizmos.color = gridColor;

        for (int x = 0; x <= columns; x++)
        {
            float px = bottomLeft.x + x * cellSize;

            Vector3 start =
                new Vector3(px, bottomLeft.y, origin.z);

            Vector3 end =
                new Vector3(px, bottomLeft.y + height, origin.z);

            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= rows; y++)
        {
            float py = bottomLeft.y + y * cellSize;

            Vector3 start =
                new Vector3(bottomLeft.x, py, origin.z);

            Vector3 end =
                new Vector3(bottomLeft.x + width, py, origin.z);

            Gizmos.DrawLine(start, end);
        }

        Gizmos.color = centerLineColor;

        Gizmos.DrawLine(
            new Vector3(origin.x, bottomLeft.y, origin.z),
            new Vector3(origin.x, bottomLeft.y + height, origin.z));

        Gizmos.DrawLine(
            new Vector3(bottomLeft.x, origin.y, origin.z),
            new Vector3(bottomLeft.x + width, origin.y, origin.z));
    }

    private float GetCellSize()
    {
        if (!useSpriteSizeAsCellSize)
            return manualCellSize;

        if (referenceTileSprite == null)
            return manualCellSize;

        float spriteWidth =
            referenceTileSprite.bounds.size.x * tileScale;

        return Mathf.Max(0.05f, spriteWidth + cellPadding);
    }
}