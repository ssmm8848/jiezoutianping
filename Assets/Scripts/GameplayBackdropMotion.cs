using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GameplayBackdropMotion : MonoBehaviour
{
    private const int RibbonPointCount = 48;
    private const float BaseDepth = 27f;

    private sealed class Ribbon
    {
        public LineRenderer glow;
        public LineRenderer core;
        public Vector3[] points;
        public float side;
        public float column;
        public float phase;
        public float speed;
        public float alpha;
        public Color color;
    }

    private readonly List<Ribbon> ribbons = new List<Ribbon>();
    private Camera targetCamera;
    private Material lineMaterial;
    private float audioPulse;
    private float beatPulse;
    private bool isInitialized;

    public void Initialize(string stageName)
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        targetCamera = GetComponent<Camera>();

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Particles/Standard Unlit");
        }

        if (targetCamera == null || lineShader == null)
        {
            return;
        }

        lineMaterial = new Material(lineShader)
        {
            name = "Gravity Beat Flowing Light Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] palette = GetPalette(stageName);
        for (int i = 0; i < 6; i++)
        {
            CreateRibbon(i, palette[i % palette.Length]);
        }
    }

    public void SetAudioPulse(float waveformPulse, float rhythmPulse)
    {
        audioPulse = Mathf.Clamp01(waveformPulse);
        beatPulse = Mathf.Clamp01(rhythmPulse);
    }

    private void Update()
    {
        if (targetCamera == null || ribbons.Count == 0)
        {
            return;
        }

        float halfHeight = BaseDepth * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * targetCamera.aspect;
        float time = Time.unscaledTime;
        float pulseScale = 0.7f + audioPulse * 0.35f + beatPulse * 0.35f;

        for (int ribbonIndex = 0; ribbonIndex < ribbons.Count; ribbonIndex++)
        {
            Ribbon ribbon = ribbons[ribbonIndex];
            float flow = Mathf.Repeat(time * ribbon.speed + ribbon.phase, 1f);
            float centerY = Mathf.Lerp(-halfHeight * 0.68f, halfHeight * 0.68f, flow);

            for (int pointIndex = 0; pointIndex < RibbonPointCount; pointIndex++)
            {
                float progress = pointIndex / (float)(RibbonPointCount - 1);
                float waveA = Mathf.Sin(progress * 6.2f + time * 0.55f + ribbon.phase);
                float waveB = Mathf.Sin(progress * 3.4f - time * 0.31f + ribbon.phase * 1.7f);
                float x = ribbon.side * halfWidth * ribbon.column +
                          waveA * halfWidth * 0.035f + waveB * halfWidth * 0.018f;
                float y = centerY + (progress - 0.5f) * halfHeight * 0.3f +
                          waveB * halfHeight * 0.075f;
                ribbon.points[pointIndex] = new Vector3(x, y, 0f);
            }

            ribbon.glow.SetPositions(ribbon.points);
            ribbon.core.SetPositions(ribbon.points);

            float alpha = Mathf.Clamp01(ribbon.alpha * pulseScale);
            ribbon.glow.startColor = WithAlpha(ribbon.color, alpha * 0.45f);
            ribbon.glow.endColor = WithAlpha(ribbon.color, alpha * 0.45f);
            ribbon.core.startColor = WithAlpha(ribbon.color, alpha);
            ribbon.core.endColor = WithAlpha(ribbon.color, alpha);
        }
    }

    private void CreateRibbon(int index, Color color)
    {
        float side = index % 2 == 0 ? -1f : 1f;
        float column = 0.74f + (index / 2) * 0.08f;
        float phase = index * 0.83f;
        float depth = BaseDepth + (index % 3) * 2.5f;
        float speed = 0.07f + (index % 3) * 0.025f;
        float alpha = 0.24f - (index % 3) * 0.035f;
        Vector3[] points = new Vector3[RibbonPointCount];

        LineRenderer glow = CreateLineRenderer("Flow Ribbon Glow " + index, depth, 0.16f);
        LineRenderer core = CreateLineRenderer("Flow Ribbon Core " + index, depth - 0.025f, 0.045f);

        ribbons.Add(new Ribbon
        {
            glow = glow,
            core = core,
            points = points,
            side = side,
            column = column,
            phase = phase,
            speed = speed,
            alpha = alpha,
            color = color
        });
    }

    private LineRenderer CreateLineRenderer(string objectName, float depth, float width)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.layer = gameObject.layer;
        lineObject.transform.SetParent(transform, false);
        lineObject.transform.localPosition = new Vector3(0f, 0f, depth);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = RibbonPointCount;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = lineMaterial;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        return line;
    }

    private Color[] GetPalette(string stageName)
    {
        if (stageName.Contains("Solar"))
        {
            return new[]
            {
                new Color(1f, 0.38f, 0.12f),
                new Color(1f, 0.19f, 0.48f)
            };
        }

        if (stageName.Contains("Violet"))
        {
            return new[]
            {
                new Color(0.68f, 0.3f, 1f),
                new Color(0.14f, 0.82f, 1f)
            };
        }

        return new[]
        {
            new Color(0.12f, 0.86f, 1f),
            new Color(0.56f, 0.38f, 1f)
        };
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
