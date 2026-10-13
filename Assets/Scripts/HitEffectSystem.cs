using System.Collections.Generic;
using UnityEngine;

public class HitEffectSystem : MonoBehaviour
{
    [Header("Effect Materials")]
    public Material normalEffectMaterial;
    public Material heavyEffectMaterial;
    public Material lightEffectMaterial;
    public Material stabilizeEffectMaterial;
    public Material missEffectMaterial;
    public Material calibrationEffectMaterial;

    [Header("Effect Settings")]
    public float effectLifeTime = 0.34f;
    public float baseScale = 0.5f;

    private static Mesh ringMesh;
    private static Material particleMaterial;

    public void PlayHitEffect(Vector3 position, WeightType weightType)
    {
        Material material = GetMaterialByWeightType(weightType);
        float scale = GetScaleByWeightType(weightType);
        CreateEnergyBurst(position, material, scale, GetPaletteColor(weightType));
    }

    public void PlayMissEffect(Vector3 position)
    {
        CreateEnergyBurst(position, missEffectMaterial, baseScale * 0.8f, new Color(1f, 0.18f, 0.28f));
    }

    public void PlayCalibrationEffect(Vector3 position)
    {
        CreateEnergyBurst(position, calibrationEffectMaterial, baseScale * 1.5f, new Color(0.25f, 1f, 0.9f));
    }

    public void PlayCounterweightEffect(Vector3 position)
    {
        CreateEnergyBurst(position, stabilizeEffectMaterial, baseScale * 1.8f, new Color(0.34f, 1f, 0.72f));
    }

    private Material GetMaterialByWeightType(WeightType weightType)
    {
        switch (weightType)
        {
            case WeightType.Heavy:
                return heavyEffectMaterial;
            case WeightType.Light:
                return lightEffectMaterial;
            case WeightType.Stabilize:
                return stabilizeEffectMaterial;
            default:
                return normalEffectMaterial;
        }
    }

    private float GetScaleByWeightType(WeightType weightType)
    {
        switch (weightType)
        {
            case WeightType.Heavy:
                return baseScale * 1.3f;
            case WeightType.Light:
                return baseScale * 0.82f;
            case WeightType.Stabilize:
                return baseScale * 1.15f;
            default:
                return baseScale;
        }
    }

    private void CreateEnergyBurst(Vector3 position, Material material, float scale, Color fallbackColor)
    {
        Color effectColor = GetMaterialColor(material, fallbackColor);
        GameObject ringObject = new GameObject("Gravity Hit Pulse");
        ringObject.transform.position = position;
        if (Camera.main != null)
        {
            ringObject.transform.rotation = Camera.main.transform.rotation;
        }

        MeshFilter filter = ringObject.AddComponent<MeshFilter>();
        filter.sharedMesh = GetRingMesh();
        MeshRenderer renderer = ringObject.AddComponent<MeshRenderer>();
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Vector3 startScale = Vector3.one * Mathf.Max(0.1f, scale * 0.34f);
        Vector3 endScale = Vector3.one * Mathf.Max(0.7f, scale * 2.8f);
        ringObject.transform.localScale = startScale;
        HitEffectVisual visual = ringObject.AddComponent<HitEffectVisual>();
        visual.Initialize(renderer, effectColor, effectLifeTime, startScale, endScale);
        Destroy(ringObject, effectLifeTime + 0.08f);

        CreateSparkBurst(position, effectColor, scale);
    }

    private void CreateSparkBurst(Vector3 position, Color color, float scale)
    {
        GameObject burstObject = new GameObject("Gravity Hit Sparks", typeof(ParticleSystem));
        burstObject.transform.position = position;
        ParticleSystem particles = burstObject.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.42f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f * scale, 2.6f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f * scale, 0.085f * scale);
        main.startColor = color;
        main.gravityModifier = 0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 28;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.045f * scale;
        shape.randomDirectionAmount = 1f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 0.25f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.25f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader != null)
            {
                particleMaterial = new Material(shader)
                {
                    name = "Gravity Beat Spark Material",
                    hideFlags = HideFlags.DontSave
                };
                if (particleMaterial.HasProperty("_Color"))
                {
                    particleMaterial.SetColor("_Color", Color.white);
                }
            }
        }
        if (particleMaterial != null)
        {
            particleRenderer.sharedMaterial = particleMaterial;
        }

        particles.Emit(18);
        particles.Play();
        Destroy(burstObject, 0.8f);
    }

    private static Color GetMaterialColor(Material material, Color fallback)
    {
        return material != null && material.HasProperty("_Color")
            ? material.GetColor("_Color")
            : fallback;
    }

    private static Color GetPaletteColor(WeightType weightType)
    {
        switch (weightType)
        {
            case WeightType.Heavy:
                return new Color(1f, 0.24f, 0.12f);
            case WeightType.Light:
                return new Color(0.2f, 0.95f, 1f);
            case WeightType.Stabilize:
                return new Color(0.27f, 1f, 0.62f);
            default:
                return new Color(0.34f, 0.42f, 1f);
        }
    }

    private static Mesh GetRingMesh()
    {
        if (ringMesh != null)
        {
            return ringMesh;
        }

        const int segments = 40;
        const int tubeSegments = 8;
        const float majorRadius = 0.72f;
        const float tubeRadius = 0.07f;
        List<Vector3> vertices = new List<Vector3>((segments + 1) * (tubeSegments + 1));
        List<int> triangles = new List<int>(segments * tubeSegments * 6);

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);
            for (int j = 0; j <= tubeSegments; j++)
            {
                float tubeAngle = Mathf.PI * 2f * j / tubeSegments;
                float ringRadius = majorRadius + Mathf.Cos(tubeAngle) * tubeRadius;
                vertices.Add(new Vector3(cosAngle * ringRadius, sinAngle * ringRadius, Mathf.Sin(tubeAngle) * tubeRadius));
            }
        }

        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < tubeSegments; j++)
            {
                int current = i * (tubeSegments + 1) + j;
                int next = current + tubeSegments + 1;
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);
                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        ringMesh = new Mesh
        {
            name = "Gravity Beat Energy Ring",
            hideFlags = HideFlags.DontSave
        };
        ringMesh.SetVertices(vertices);
        ringMesh.SetTriangles(triangles, 0);
        ringMesh.RecalculateNormals();
        ringMesh.RecalculateBounds();
        return ringMesh;
    }
}

public class HitEffectVisual : MonoBehaviour
{
    private float lifeTime = 0.34f;
    private float timer;
    private Vector3 startScale;
    private Vector3 endScale;
    private Color effectColor;
    private Renderer effectRenderer;
    private MaterialPropertyBlock propertyBlock;

    public void Initialize(Renderer targetRenderer, Color color, float duration, Vector3 initialScale, Vector3 finalScale)
    {
        effectRenderer = targetRenderer;
        effectColor = color;
        lifeTime = Mathf.Max(0.05f, duration);
        startScale = initialScale;
        endScale = finalScale;
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / lifeTime);
        transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, progress));
        transform.Rotate(0f, 0f, 130f * Time.deltaTime, Space.Self);

        if (effectRenderer != null)
        {
            effectRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", effectColor * (1.9f * (1f - progress)));
            effectRenderer.SetPropertyBlock(propertyBlock);
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
