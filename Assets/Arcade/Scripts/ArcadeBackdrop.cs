using UnityEngine;
using UnityEngine.UI;

namespace Arcade
{
    /// <summary>Lightweight vector background: no downloaded artwork or extra package.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcadeBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var r = rectTransform.rect;
            Quad(mesh, r.xMin, r.yMin, r.width, r.height, new Color32(10, 16, 29, 255), new Color32(19, 29, 48, 255));
            Disc(mesh, new Vector2(r.xMax - 50, r.yMax - 25), 410, new Color32(61, 192, 192, 7));
            Disc(mesh, new Vector2(r.xMin + 50, r.yMin + 80), 300, new Color32(140, 117, 245, 7));
            for (float x = r.xMin; x < r.xMax; x += 48)
                Quad(mesh, x, r.yMin, .6f, r.height, new Color32(112, 155, 190, 9), new Color32(112, 155, 190, 9));
            for (float y = r.yMin; y < r.yMax; y += 48)
                Quad(mesh, r.xMin, y, r.width, .6f, new Color32(112, 155, 190, 9), new Color32(112, 155, 190, 9));
        }

        private static void Quad(VertexHelper mesh, float x, float y, float w, float h, Color bottom, Color top)
        {
            int i = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, y), bottom, Vector2.zero);
            mesh.AddVert(new Vector3(x, y + h), top, Vector2.zero);
            mesh.AddVert(new Vector3(x + w, y + h), top, Vector2.zero);
            mesh.AddVert(new Vector3(x + w, y), bottom, Vector2.zero);
            mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
        }

        private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color color)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            const int steps = 72;
            for (int i = 0; i <= steps; i++) {
                float angle = i * Mathf.PI * 2f / steps;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
            }
        }
    }
}
