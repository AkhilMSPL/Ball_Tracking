using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITrajectoryRendrer : Graphic
{
    public List<Vector2> points = new List<Vector2>();
    public float thickness = 10f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points.Count < 2) return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;

            UIVertex v0 = UIVertex.simpleVert;
            v0.color = color;
            v0.position = start - normal;

            UIVertex v1 = UIVertex.simpleVert;
            v1.color = color;
            v1.position = start + normal;

            UIVertex v2 = UIVertex.simpleVert;
            v2.color = color;
            v2.position = end + normal;

            UIVertex v3 = UIVertex.simpleVert;
            v3.color = color;
            v3.position = end - normal;

            int index = vh.currentVertCount;

            vh.AddVert(v0);
            vh.AddVert(v1);
            vh.AddVert(v2);
            vh.AddVert(v3);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }

    public void Draw(List<Vector2> newPoints)
    {
        points = newPoints;
        SetVerticesDirty();
    }

    public void Clear()
    {
        points.Clear();
        SetVerticesDirty(); // Clears the mesh
    }
}
