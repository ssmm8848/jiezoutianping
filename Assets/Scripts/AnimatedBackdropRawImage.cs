using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds gentle water and cloud motion to a single painted backdrop by warping
/// a subdivided UI mesh. The image remains one texture, so no extra art layers
/// or per-song background assets are required.
/// </summary>
public sealed class AnimatedBackdropRawImage : RawImage
{
    private const int HorizontalSegments = 44;
    private const int VerticalSegments = 26;

    private float animationTime;

    public void SetAnimationTime(float time)
    {
        if (Mathf.Approximately(animationTime, time))
        {
            return;
        }

        animationTime = time;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        if (texture == null)
        {
            base.OnPopulateMesh(vertexHelper);
            return;
        }

        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        int rowSize = HorizontalSegments + 1;
        float width = rect.width;
        float height = rect.height;
        Color vertexColor = color;

        for (int y = 0; y <= VerticalSegments; y++)
        {
            float v = y / (float)VerticalSegments;
            for (int x = 0; x <= HorizontalSegments; x++)
            {
                float u = x / (float)HorizontalSegments;
                float edgeMask = Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI);
                float waterMask = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0.48f, v));
                float cloudDistance = (v - 0.57f) / 0.16f;
                float cloudMask = Mathf.Exp(-cloudDistance * cloudDistance);

                float waterWave = Mathf.Sin(u * 18f + animationTime * 1.25f + v * 3.5f) * 4.2f;
                waterWave += Mathf.Sin(u * 31f - animationTime * 0.82f) * 1.6f;
                float cloudSway = Mathf.Sin(v * 10f + animationTime * 0.18f + u * 2f) * 8f;
                float slowParallax = Mathf.Sin(v * 2.4f + animationTime * 0.12f) * 2.2f;

                float offsetX = (cloudSway * cloudMask + slowParallax) * edgeMask;
                float offsetY = (waterWave * waterMask + Mathf.Sin(u * 5f + animationTime * 0.2f) * cloudMask * 2f) * edgeMask;

                UIVertex vertex = UIVertex.simpleVert;
                vertex.position = new Vector3(
                    rect.xMin + u * width + offsetX,
                    rect.yMin + v * height + offsetY,
                    0f
                );
                vertex.color = vertexColor;
                vertex.uv0 = new Vector2(
                    Mathf.Lerp(uvRect.xMin, uvRect.xMax, u),
                    Mathf.Lerp(uvRect.yMin, uvRect.yMax, v)
                );
                vertexHelper.AddVert(vertex);
            }
        }

        for (int y = 0; y < VerticalSegments; y++)
        {
            for (int x = 0; x < HorizontalSegments; x++)
            {
                int bottomLeft = y * rowSize + x;
                int topLeft = bottomLeft + rowSize;
                vertexHelper.AddTriangle(bottomLeft, topLeft, topLeft + 1);
                vertexHelper.AddTriangle(bottomLeft, topLeft + 1, bottomLeft + 1);
            }
        }
    }
}
