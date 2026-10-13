using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    [Header("Note Data")]
    public float hitTime;
    public int lane;
    public WeightType weightType;

    [Header("Movement")]
    public Vector3 spawnPosition;
    public Transform judgePoint;
    public float spawnAheadTime = 2f;

    [Header("Runtime")]
    public bool hasBeenHit;
    public bool hasMissed;

    [Header("Miss")]
    public float missWindow = 0.15f;

    [Header("Gravity Path")]
    [Tooltip("Visual curve strength for notes traveling through the current gravity field.")]
    public float gravityBendStrength = 0.75f;

    [Header("Visual")]
    public Renderer noteRenderer;
    public Material normalMaterial;
    public Material heavyMaterial;
    public Material lightMaterial;
    public Material stabilizeMaterial;

    private static readonly Dictionary<WeightType, Mesh> CrystalMeshes = new Dictionary<WeightType, Mesh>();
    private static readonly Dictionary<int, Material> AccentMaterials = new Dictionary<int, Material>();
    private static Material fallbackMaterial;
    private static Material trailMaterial;

    private GameManager gameManager;
    private JudgeSystem judgeSystem;
    private float spinSpeed;

    public void SetJudgeSystem(JudgeSystem newJudgeSystem)
    {
        judgeSystem = newJudgeSystem;
    }

    public void Initialize(float newHitTime, int newLane, WeightType newWeightType)
    {
        hitTime = newHitTime;
        lane = newLane;
        weightType = newWeightType;
        spawnPosition = transform.position;
        ApplyVisual();
    }

    public void SetMovementData(Vector3 newSpawnPosition, Transform newJudgePoint, float newSpawnAheadTime)
    {
        spawnPosition = newSpawnPosition;
        judgePoint = newJudgePoint;
        spawnAheadTime = newSpawnAheadTime;
    }

    public void SetGameManager(GameManager newGameManager)
    {
        gameManager = newGameManager;
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.IsPlaying() || hasBeenHit || hasMissed)
        {
            return;
        }

        transform.Rotate(0f, spinSpeed * Time.deltaTime, spinSpeed * 0.36f * Time.deltaTime, Space.Self);
        UpdateMovement();
        CheckAutoMiss();
    }

    private void CheckAutoMiss()
    {
        float currentMusicTime = judgeSystem != null
            ? judgeSystem.GetAdjustedMusicTime()
            : gameManager.GetMusicTime();

        if (currentMusicTime <= hitTime + missWindow)
        {
            return;
        }

        hasMissed = true;
        if (judgeSystem != null)
        {
            judgeSystem.ApplyAutoMiss(this);
        }
        else
        {
            Debug.Log($"Auto Miss: lane={lane}, time={hitTime}");
            Destroy(gameObject);
        }
    }

    private void UpdateMovement()
    {
        if (judgePoint == null)
        {
            return;
        }

        float currentMusicTime = gameManager.GetMusicTime();
        float timeToHit = hitTime - currentMusicTime;
        float progress = 1f - timeToHit / Mathf.Max(0.01f, spawnAheadTime);
        progress = Mathf.Clamp01(progress);
        Vector3 position = Vector3.Lerp(spawnPosition, judgePoint.position, progress);
        float gravityOffset = judgeSystem != null ? judgeSystem.GetGravityOffset() : 0f;
        float gravityCurve = gravityOffset * gravityBendStrength * Mathf.Sin(progress * Mathf.PI);
        transform.position = position + Vector3.up * gravityCurve;
    }

    private void ApplyVisual()
    {
        if (noteRenderer == null)
        {
            noteRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (noteRenderer == null)
        {
            Debug.LogWarning("Note 缺少 MeshRenderer，无法设置棱晶外观");
            return;
        }

        Material baseMaterial = GetMaterialByWeightType(weightType);
        if (baseMaterial == null)
        {
            baseMaterial = GetFallbackMaterial();
        }

        MeshFilter meshFilter = noteRenderer.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = GetCrystalMesh(weightType);
        }

        noteRenderer.sharedMaterials = new[] { baseMaterial, GetAccentMaterial(baseMaterial) };

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            ConfigureTrail(trail, baseMaterial);
        }

        switch (weightType)
        {
            case WeightType.Heavy:
                transform.localScale = Vector3.one * 1.35f;
                spinSpeed = 24f;
                break;
            case WeightType.Light:
                transform.localScale = Vector3.one * 0.68f;
                spinSpeed = 72f;
                break;
            case WeightType.Stabilize:
                transform.localScale = Vector3.one;
                spinSpeed = 18f;
                break;
            default:
                transform.localScale = Vector3.one * 0.92f;
                spinSpeed = 42f;
                break;
        }
    }

    private Material GetMaterialByWeightType(WeightType type)
    {
        switch (type)
        {
            case WeightType.Heavy:
                return heavyMaterial;
            case WeightType.Light:
                return lightMaterial;
            case WeightType.Stabilize:
                return stabilizeMaterial;
            default:
                return normalMaterial;
        }
    }

    private static Material GetAccentMaterial(Material source)
    {
        int key = source.GetInstanceID();
        Material accent;
        if (AccentMaterials.TryGetValue(key, out accent) && accent != null)
        {
            return accent;
        }

        accent = new Material(source)
        {
            name = source.name + " Facet Highlight",
            hideFlags = HideFlags.DontSave
        };

        if (accent.HasProperty("_Color"))
        {
            Color color = accent.GetColor("_Color");
            accent.SetColor("_Color", new Color(color.r * 1.35f, color.g * 1.35f, color.b * 1.35f, color.a));
        }
        if (accent.HasProperty("_EmissionColor"))
        {
            accent.EnableKeyword("_EMISSION");
            accent.SetColor("_EmissionColor", accent.GetColor("_EmissionColor") * 1.6f);
        }
        if (accent.HasProperty("_Glossiness"))
        {
            accent.SetFloat("_Glossiness", 0.86f);
        }
        if (accent.HasProperty("_Metallic"))
        {
            accent.SetFloat("_Metallic", 0.28f);
        }

        AccentMaterials[key] = accent;
        return accent;
    }

    private static Material GetFallbackMaterial()
    {
        if (fallbackMaterial != null)
        {
            return fallbackMaterial;
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            return null;
        }

        fallbackMaterial = new Material(shader)
        {
            name = "Gravity Beat Fallback Note",
            hideFlags = HideFlags.DontSave
        };
        if (fallbackMaterial.HasProperty("_Color"))
        {
            fallbackMaterial.SetColor("_Color", new Color(0.1f, 0.85f, 1f, 1f));
        }
        if (fallbackMaterial.HasProperty("_EmissionColor"))
        {
            fallbackMaterial.EnableKeyword("_EMISSION");
            fallbackMaterial.SetColor("_EmissionColor", new Color(0.08f, 1.2f, 1.7f, 1f));
        }
        return fallbackMaterial;
    }

    private static Mesh GetCrystalMesh(WeightType type)
    {
        Mesh mesh;
        if (CrystalMeshes.TryGetValue(type, out mesh) && mesh != null)
        {
            return mesh;
        }

        int sides = type == WeightType.Light ? 6 : 8;
        float radius = type == WeightType.Heavy ? 0.46f : type == WeightType.Light ? 0.24f : 0.35f;
        float halfHeight = type == WeightType.Heavy ? 0.46f : type == WeightType.Light ? 0.39f : type == WeightType.Stabilize ? 0.4f : 0.35f;
        float middleRadius = type == WeightType.Heavy
            ? radius * 1.08f
            : type == WeightType.Stabilize
                ? radius * 1.3f
                : radius * 0.78f;
        mesh = BuildCrystalMesh(type, sides, radius, halfHeight, middleRadius);
        CrystalMeshes[type] = mesh;
        return mesh;
    }

    private static Mesh BuildCrystalMesh(WeightType type, int sides, float radius, float halfHeight, float middleRadius)
    {
        List<Vector3> vertices = new List<Vector3>(sides * 30);
        List<int> mainTriangles = new List<int>(sides * 24);
        List<int> accentTriangles = new List<int>(sides * 24);
        Vector3 top = new Vector3(0f, halfHeight, 0f);
        Vector3 bottom = new Vector3(0f, -halfHeight, 0f);
        Vector3[] upperRing = new Vector3[sides];
        Vector3[] middleRing = new Vector3[sides];
        Vector3[] lowerRing = new Vector3[sides];
        float upperY = halfHeight * 0.24f;

        for (int i = 0; i < sides; i++)
        {
            float angle = (Mathf.PI * 2f * i / sides) + Mathf.PI / sides;
            float lowerAngle = angle + Mathf.PI / sides;
            upperRing[i] = new Vector3(Mathf.Cos(angle) * radius, upperY, Mathf.Sin(angle) * radius);
            middleRing[i] = new Vector3(Mathf.Cos(angle) * middleRadius, 0f, Mathf.Sin(angle) * middleRadius);
            lowerRing[i] = new Vector3(Mathf.Cos(lowerAngle) * radius * 0.92f, -upperY, Mathf.Sin(lowerAngle) * radius * 0.92f);
        }

        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int material = i % 2;
            AddTriangle(vertices, mainTriangles, accentTriangles, top, upperRing[i], upperRing[next], material);
            AddTriangle(vertices, mainTriangles, accentTriangles, upperRing[i], middleRing[i], middleRing[next], material);
            AddTriangle(vertices, mainTriangles, accentTriangles, upperRing[i], middleRing[next], upperRing[next], 1 - material);
            AddTriangle(vertices, mainTriangles, accentTriangles, middleRing[i], lowerRing[i], lowerRing[next], 1 - material);
            AddTriangle(vertices, mainTriangles, accentTriangles, middleRing[i], lowerRing[next], middleRing[next], material);
            AddTriangle(vertices, mainTriangles, accentTriangles, bottom, lowerRing[next], lowerRing[i], material);
        }

        Mesh mesh = new Mesh
        {
            name = "Gravity Beat " + type + " Crystal",
            hideFlags = HideFlags.DontSave
        };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(mainTriangles, 0, true);
        mesh.SetTriangles(accentTriangles, 1, true);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddTriangle(
        List<Vector3> vertices,
        List<int> mainTriangles,
        List<int> accentTriangles,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        int materialIndex)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);
        Vector3 center = (a + b + c) / 3f;
        if (Vector3.Dot(normal, center) < 0f)
        {
            Vector3 swap = b;
            b = c;
            c = swap;
        }

        List<int> target = materialIndex == 0 ? mainTriangles : accentTriangles;
        int first = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        target.Add(first);
        target.Add(first + 1);
        target.Add(first + 2);
    }

    private static void ConfigureTrail(TrailRenderer trail, Material noteMaterial)
    {
        Color noteColor = noteMaterial.HasProperty("_Color")
            ? noteMaterial.GetColor("_Color")
            : Color.cyan;
        Color glowColor = Color.Lerp(noteColor, Color.white, 0.48f);
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(noteColor, 0f),
                new GradientColorKey(glowColor, 0.5f),
                new GradientColorKey(noteColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.12f, 0f),
                new GradientAlphaKey(0.58f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });

        trail.colorGradient = gradient;
        trail.time = 0.18f;
        trail.widthMultiplier = 0.16f;
        trail.minVertexDistance = 0.055f;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        if (trailMaterial == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader != null)
            {
                trailMaterial = new Material(shader)
                {
                    name = "Gravity Beat Note Trail",
                    hideFlags = HideFlags.DontSave
                };
                if (trailMaterial.HasProperty("_Color"))
                {
                    trailMaterial.SetColor("_Color", Color.white);
                }
            }
        }
        if (trailMaterial != null)
        {
            trail.sharedMaterial = trailMaterial;
        }
    }
}
