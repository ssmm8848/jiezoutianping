using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class GameplayBackdrop : MonoBehaviour
{
    private const int TextureWidth = 512;
    private const int TextureHeight = 256;
    private const float PanoramaRotationSpeed = 0.42f;

    private static int nextStageIndex;

    private Camera targetCamera;
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;
    private Material originalSkybox;
    private AmbientMode originalAmbientMode;
    private Color originalAmbientLight;
    private float originalAmbientIntensity;
    private bool originalFog;
    private FogMode originalFogMode;
    private Color originalFogColor;
    private float originalFogStartDistance;
    private float originalFogEndDistance;

    private Material runtimeSkybox;
    private Texture2D runtimePanorama;
    private Texture2D panoramaTexture;
    private AudioSource musicSource;
    private GameManager gameManager;
    private GameplayBackdropMotion motionLayer;
    private readonly float[] audioSamples = new float[64];
    private float smoothedPulse;
    private float smoothedBeatPulse;
    private bool hasApplied;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        gameManager = FindObjectOfType<GameManager>();
        musicSource = gameManager != null ? gameManager.musicSource : null;
        SaveOriginalSettings();
        ApplyAtmosphere();
    }

    private void Update()
    {
        if (runtimeSkybox == null)
        {
            return;
        }

        if (runtimeSkybox.HasProperty("_Rotation"))
        {
            runtimeSkybox.SetFloat("_Rotation", (Time.time * PanoramaRotationSpeed) % 360f);
        }

        if (!runtimeSkybox.HasProperty("_Exposure"))
        {
            return;
        }

        float pulse = 0f;
        float beatPulse = 0f;
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.GetOutputData(audioSamples, 0);
            float energy = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                energy += audioSamples[i] * audioSamples[i];
            }

            pulse = Mathf.Clamp01(Mathf.Sqrt(energy / audioSamples.Length) * 7f);

            float bpm = gameManager != null && gameManager.chartData != null
                ? gameManager.chartData.bpm
                : 0f;
            if (bpm > 0f)
            {
                float beatPhase = Mathf.Repeat(musicSource.time * bpm / 60f, 1f);
                beatPulse = Mathf.Exp(-beatPhase * 18f);
            }
        }

        smoothedPulse = Mathf.Lerp(smoothedPulse, pulse, 1f - Mathf.Exp(-Time.deltaTime * 8f));
        smoothedBeatPulse = Mathf.Lerp(
            smoothedBeatPulse,
            beatPulse,
            1f - Mathf.Exp(-Time.deltaTime * 18f)
        );
        runtimeSkybox.SetFloat("_Exposure", 0.82f + smoothedPulse * 0.14f + smoothedBeatPulse * 0.08f);
        if (motionLayer != null)
        {
            motionLayer.SetAudioPulse(smoothedPulse, smoothedBeatPulse);
        }
    }

    private void SaveOriginalSettings()
    {
        originalClearFlags = targetCamera.clearFlags;
        originalBackgroundColor = targetCamera.backgroundColor;
        originalSkybox = RenderSettings.skybox;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientLight = RenderSettings.ambientLight;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalFog = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogColor = RenderSettings.fogColor;
        originalFogStartDistance = RenderSettings.fogStartDistance;
        originalFogEndDistance = RenderSettings.fogEndDistance;
    }

    private void ApplyAtmosphere()
    {
        Color deepNavy = new Color32(4, 9, 22, 255);
        targetCamera.backgroundColor = deepNavy;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(28, 45, 75, 255);
        RenderSettings.ambientIntensity = 0.8f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color32(7, 15, 33, 255);
        RenderSettings.fogStartDistance = 17f;
        RenderSettings.fogEndDistance = 75f;

        Shader panoramicShader = Shader.Find("Skybox/Panoramic");
        if (panoramicShader == null)
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            hasApplied = true;
            return;
        }

        panoramaTexture = LoadNextStagePanorama();
        if (panoramaTexture == null)
        {
            panoramaTexture = Resources.Load<Texture2D>("GameplayPanorama");
        }

        if (panoramaTexture == null)
        {
            runtimePanorama = CreatePanoramaTexture();
            panoramaTexture = runtimePanorama;
        }

        runtimeSkybox = new Material(panoramicShader)
        {
            name = "Gravity Beat Gameplay Panorama",
            hideFlags = HideFlags.HideAndDontSave
        };
        runtimeSkybox.SetTexture("_MainTex", panoramaTexture);
        if (runtimeSkybox.HasProperty("_Tint"))
        {
            runtimeSkybox.SetColor("_Tint", Color.white);
        }
        if (runtimeSkybox.HasProperty("_Exposure"))
        {
            runtimeSkybox.SetFloat("_Exposure", 1f);
        }
        if (runtimeSkybox.HasProperty("_Rotation"))
        {
            runtimeSkybox.SetFloat("_Rotation", 0f);
        }
        if (runtimeSkybox.HasProperty("_Mapping"))
        {
            runtimeSkybox.SetFloat("_Mapping", 1f);
        }
        if (runtimeSkybox.HasProperty("_ImageType"))
        {
            runtimeSkybox.SetFloat("_ImageType", 0f);
        }
        if (runtimeSkybox.HasProperty("_Layout"))
        {
            runtimeSkybox.SetFloat("_Layout", 0f);
        }

        RenderSettings.skybox = runtimeSkybox;
        targetCamera.clearFlags = CameraClearFlags.Skybox;

        motionLayer = targetCamera.GetComponent<GameplayBackdropMotion>();
        if (motionLayer == null)
        {
            motionLayer = targetCamera.gameObject.AddComponent<GameplayBackdropMotion>();
        }
        motionLayer.Initialize(panoramaTexture.name);

        hasApplied = true;
    }

    private Texture2D LoadNextStagePanorama()
    {
        Texture2D[] stagePanoramas = Resources.LoadAll<Texture2D>("GameplayStages");
        if (stagePanoramas == null || stagePanoramas.Length == 0)
        {
            return null;
        }

        System.Array.Sort(
            stagePanoramas,
            (first, second) => string.CompareOrdinal(first.name, second.name)
        );

        int selectedIndex = nextStageIndex % stagePanoramas.Length;
        nextStageIndex = (selectedIndex + 1) % stagePanoramas.Length;
        return stagePanoramas[selectedIndex];
    }

    private Texture2D CreatePanoramaTexture()
    {
        Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
        {
            name = "Gravity Beat Procedural Starfield",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[TextureWidth * TextureHeight];
        Color topColor = new Color(0.004f, 0.008f, 0.023f, 1f);
        Color bottomColor = new Color(0.009f, 0.019f, 0.045f, 1f);

        for (int y = 0; y < TextureHeight; y++)
        {
            float v = y / (float)(TextureHeight - 1);
            for (int x = 0; x < TextureWidth; x++)
            {
                float u = x / (float)(TextureWidth - 1);
                float angle = u * Mathf.PI * 2f;
                float broadHaze = Gaussian(v, 0.49f, 0.24f);
                float noise = Mathf.PerlinNoise(
                    4f + Mathf.Cos(angle) * 2f,
                    4f + Mathf.Sin(angle) * 2f + v * 3f);
                float ribbonCenter = 0.53f + 0.055f * Mathf.Sin(u * Mathf.PI * 4f) +
                                     0.022f * Mathf.Sin(u * Mathf.PI * 12f + 0.6f);
                float ribbon = Mathf.Exp(-Mathf.Pow((v - ribbonCenter) * 26f, 2f));
                float ribbonDetail = 0.25f + 0.75f * Mathf.Pow(
                    0.5f + 0.5f * Mathf.Sin((u * 6f + v * 2f + noise) * Mathf.PI * 2f), 2f);
                float cyanNebula = broadHaze * (0.15f + 0.3f * noise);
                float violetNebula = Gaussian(WrappedDistance(u, 0.77f), 0f, 0.18f) *
                                     Gaussian(v, 0.62f, 0.17f) * (0.2f + 0.3f * noise);

                Color color = Color.Lerp(bottomColor, topColor, v);
                color += new Color(0.008f, 0.055f, 0.10f, 0f) * cyanNebula;
                color += new Color(0.065f, 0.018f, 0.10f, 0f) * violetNebula;
                color += new Color(0.005f, 0.12f, 0.18f, 0f) * ribbon * ribbonDetail;
                pixels[y * TextureWidth + x] = color;
            }
        }

        Random.State previousRandomState = Random.state;
        Random.InitState(18427);
        for (int i = 0; i < 680; i++)
        {
            int x = Random.Range(0, TextureWidth);
            int y = Random.Range(TextureHeight / 3, TextureHeight);
            float intensity = Random.Range(0.25f, 0.9f);
            Color star = Color.Lerp(new Color(0.35f, 0.65f, 1f, 0f), Color.white, Random.value) * intensity;
            AddStar(pixels, x, y, star);

            if (intensity > 0.78f)
            {
                AddStar(pixels, x + 1, y, star * 0.28f);
                AddStar(pixels, x, y + 1, star * 0.28f);
            }
        }
        Random.state = previousRandomState;

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static float Gaussian(float value, float center, float width)
    {
        float normalized = (value - center) / width;
        return Mathf.Exp(-normalized * normalized);
    }

    private static float WrappedDistance(float value, float center)
    {
        float distance = Mathf.Abs(value - center);
        return Mathf.Min(distance, 1f - distance);
    }

    private void AddStar(Color[] pixels, int x, int y, Color addition)
    {
        if (y < 0 || y >= TextureHeight)
        {
            return;
        }

        x = (x % TextureWidth + TextureWidth) % TextureWidth;

        int index = y * TextureWidth + x;
        Color color = pixels[index] + addition;
        color.r = Mathf.Min(color.r, 1f);
        color.g = Mathf.Min(color.g, 1f);
        color.b = Mathf.Min(color.b, 1f);
        pixels[index] = color;
    }

    private void OnDestroy()
    {
        if (hasApplied)
        {
            if (RenderSettings.skybox == runtimeSkybox)
            {
                RenderSettings.skybox = originalSkybox;
            }

            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientLight;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.fog = originalFog;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogStartDistance = originalFogStartDistance;
            RenderSettings.fogEndDistance = originalFogEndDistance;

            if (targetCamera != null)
            {
                targetCamera.clearFlags = originalClearFlags;
                targetCamera.backgroundColor = originalBackgroundColor;
            }
        }

        if (runtimeSkybox != null)
        {
            Destroy(runtimeSkybox);
        }
        if (runtimePanorama != null)
        {
            Destroy(runtimePanorama);
        }
    }
}
