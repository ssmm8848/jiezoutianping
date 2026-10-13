using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongDetailUI : MonoBehaviour
{
    [Header("Root")]
    public RectTransform detailRoot;
    public CanvasGroup canvasGroup;

    [Header("Cover")]
    public Image coverImage;
    public Image coverFrameImage;

    [Header("Texts")]
    public TMP_Text songTitleText;
    public TMP_Text artistText;
    public TMP_Text bpmText;
    public TMP_Text difficultyText;
    public TMP_Text levelText;
    public TMP_Text noteCountText;

    [Header("Decoration")]
    public Image gravityStatusBar;

    [Header("Colors RGB")]
    public Color panelColor = new Color32(22, 30, 46, 230);
    public Color coverPlaceholderColor = new Color32(48, 58, 78, 255);
    public Color titleColor = new Color32(240, 248, 255, 255);
    public Color subTextColor = new Color32(165, 180, 205, 255);
    public Color infoTextColor = new Color32(210, 225, 240, 255);

    public Color easyColor = new Color32(64, 191, 89, 255);
    public Color normalColor = new Color32(64, 140, 255, 255);
    public Color hardColor = new Color32(255, 89, 46, 255);
    public Color noChartColor = new Color32(130, 135, 145, 255);

    public Color highlightColor = new Color32(0, 220, 255, 255);

    [Header("Animation")]
    public float animationDuration = 0.18f;
    public float startScale = 0.965f;
    public float targetScale = 1.0f;

    private float animationTimer = 0f;
    private bool isAnimating = false;

    private void Awake()
    {
        if (detailRoot == null)
        {
            detailRoot = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        ApplyStaticColors();
    }

    private void Update()
    {
        UpdateAnimation();
    }

    public void UpdateDetail(
        SongConfig songConfig,
        ChartJsonData chartData,
        string selectedDifficultyName)
    {
        ApplySongInfo(songConfig);
        ApplyChartInfo(chartData, selectedDifficultyName);
        PlayRefreshAnimation();
    }

    private void ApplySongInfo(SongConfig songConfig)
    {
        if (songConfig == null)
        {
            if (songTitleText != null)
            {
                songTitleText.text = "Unknown Song";
            }

            if (artistText != null)
            {
                artistText.text = "Artist: Unknown";
            }

            SetCover(null);
            return;
        }

        if (songTitleText != null)
        {
            songTitleText.text = songConfig.title;
            songTitleText.color = titleColor;
        }

        if (artistText != null)
        {
            artistText.text = $"Artist: {songConfig.artist}";
            artistText.color = subTextColor;
        }

        SetCover(songConfig.coverSprite);
    }

    private void ApplyChartInfo(
        ChartJsonData chartData,
        string selectedDifficultyName)
    {
        if (chartData == null)
        {
            if (bpmText != null)
            {
                bpmText.text = "BPM -";
                bpmText.color = noChartColor;
            }

            if (difficultyText != null)
            {
                difficultyText.text = $"{selectedDifficultyName} 无谱面";
                difficultyText.color = noChartColor;
            }

            if (levelText != null)
            {
                levelText.text = "Lv.-";
                levelText.color = noChartColor;
            }

            if (noteCountText != null)
            {
                noteCountText.text = "Notes -";
                noteCountText.color = noChartColor;
            }

            if (gravityStatusBar != null)
            {
                gravityStatusBar.color = noChartColor;
            }

            return;
        }

        Color difficultyColor = GetDifficultyColor(chartData.difficulty);

        if (bpmText != null)
        {
            bpmText.text = $"BPM {chartData.bpm}";
            bpmText.color = infoTextColor;
        }

        if (difficultyText != null)
        {
            difficultyText.text = chartData.difficulty;
            difficultyText.color = difficultyColor;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{chartData.level}";
            levelText.color = difficultyColor;
        }

        if (noteCountText != null)
        {
            int count = chartData.notes != null ? chartData.notes.Count : 0;
            noteCountText.text = $"Notes {count}";
            noteCountText.color = infoTextColor;
        }

        if (gravityStatusBar != null)
        {
            gravityStatusBar.color = difficultyColor;
        }
    }

    private void SetCover(Sprite coverSprite)
    {
        if (coverImage == null)
        {
            return;
        }

        if (coverSprite != null)
        {
            coverImage.sprite = coverSprite;
            coverImage.color = Color.white;
            coverImage.enabled = true;
        }
        else
        {
            coverImage.sprite = null;
            coverImage.color = coverPlaceholderColor;
            coverImage.enabled = true;
        }
    }

    private void ApplyStaticColors()
    {
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            RhythmUITheme.StylePanel(rootImage, panelColor);
        }

        if (coverFrameImage != null)
        {
            coverFrameImage.sprite = RhythmUITheme.RoundedSprite;
            coverFrameImage.type = Image.Type.Sliced;
            coverFrameImage.color = new Color32(27, 42, 62, 255);
        }

        RhythmUITheme.StyleText(songTitleText, 30f, titleColor, true);
        RhythmUITheme.StyleText(artistText, 19f, subTextColor);
        RhythmUITheme.StyleText(bpmText, 18f, infoTextColor);
        RhythmUITheme.StyleText(difficultyText, 17f, infoTextColor, true);
        RhythmUITheme.StyleText(levelText, 24f, infoTextColor, true);
        RhythmUITheme.StyleText(noteCountText, 16f, infoTextColor);

        if (songTitleText != null)
        {
            songTitleText.color = titleColor;
        }

        if (artistText != null)
        {
            artistText.color = subTextColor;
        }

        if (bpmText != null)
        {
            bpmText.color = infoTextColor;
        }

        if (noteCountText != null)
        {
            noteCountText.color = infoTextColor;
        }

        if (gravityStatusBar != null)
        {
            gravityStatusBar.color = highlightColor;
        }
    }

    private void PlayRefreshAnimation()
    {
        animationTimer = 0f;
        isAnimating = true;

        if (detailRoot != null)
        {
            detailRoot.localScale = Vector3.one * startScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.65f;
        }
    }

    private void UpdateAnimation()
    {
        if (!isAnimating)
        {
            return;
        }

        animationTimer += Time.deltaTime;

        float progress = animationTimer / animationDuration;
        progress = Mathf.Clamp01(progress);

        float eased = 1f - Mathf.Pow(1f - progress, 3f);

        if (detailRoot != null)
        {
            float scale = Mathf.Lerp(startScale, targetScale, eased);
            detailRoot.localScale = Vector3.one * scale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(0.65f, 1f, eased);
        }

        if (progress >= 1f)
        {
            isAnimating = false;

            if (detailRoot != null)
            {
                detailRoot.localScale = Vector3.one * targetScale;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
    }

    private Color GetDifficultyColor(string difficultyName)
    {
        switch (difficultyName)
        {
            case "Easy":
                return easyColor;

            case "Hard":
                return hardColor;

            case "Normal":
                return normalColor;

            default:
                return noChartColor;
        }
    }
}
