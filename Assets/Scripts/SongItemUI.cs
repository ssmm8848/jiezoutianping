using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SongItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    public Button button;
    public GameObject selectedIndicator;

    public Image miniCoverImage;
    public TMP_Text songNameText;
    public TMP_Text artistText;
    public TMP_Text bpmText;

    public Image difficultyBadgeImage;
    public TMP_Text difficultyText;

    [Header("Background")]
    public Image backgroundImage;

    [Header("Colors RGB")]
    public Color normalColor = new Color32(16, 28, 47, 242);
    public Color hoverColor = new Color32(25, 44, 66, 248);
    public Color selectedColor = new Color32(17, 65, 82, 252);
    public Color pressedColor = new Color32(12, 52, 68, 255);

    public Color easyColor = new Color32(64, 191, 89, 255);
    public Color normalDifficultyColor = new Color32(64, 140, 255, 255);
    public Color hardColor = new Color32(255, 89, 46, 255);

    public Color titleColor = new Color32(240, 248, 255, 255);
    public Color subTextColor = new Color32(165, 180, 205, 255);
    public Color selectedIndicatorColor = new Color32(0, 220, 255, 255);

    [Header("Animation")]
    public float colorLerpSpeed = 12f;
    public float scaleLerpSpeed = 14f;
    public float normalScale = 1.0f;
    public float hoverScale = 1.018f;
    public float selectedScale = 1.025f;
    public float pressedScale = 0.985f;

    private RectTransform rectTransform;
    private int songIndex;
    private SongSelectManager songSelectManager;

    private bool isSelected = false;
    private bool isHovering = false;
    private bool isPressing = false;

    private Color targetColor;
    private float targetScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = RhythmUITheme.RoundedSprite;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.raycastTarget = true;
        }

        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.targetGraphic = backgroundImage;
        }

        if (difficultyBadgeImage != null)
        {
            difficultyBadgeImage.sprite = RhythmUITheme.RoundedSprite;
            difficultyBadgeImage.type = Image.Type.Sliced;
        }

        RhythmUITheme.StyleText(songNameText, 22f, titleColor, true);
        RhythmUITheme.StyleText(artistText, 16f, subTextColor);
        RhythmUITheme.StyleText(bpmText, 16f, subTextColor);
        RhythmUITheme.StyleText(difficultyText, 16f, Color.white, true);

        targetColor = normalColor;
        targetScale = normalScale;
    }

    private void Update()
    {
        UpdateAnimation();
    }

    public void Initialize(
        SongSelectManager manager,
        int index,
        SongConfig songConfig,
        ChartJsonData chartData,
        string difficultyName)
    {
        songSelectManager = manager;
        songIndex = index;

        UpdateDisplay(songConfig, chartData, difficultyName);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        RefreshTargetVisual();
        ApplyImmediateVisual();
    }

    public void UpdateDisplay(
        SongConfig songConfig,
        ChartJsonData chartData,
        string difficultyName)
    {
        if (songNameText != null)
        {
            songNameText.text = songConfig != null ? songConfig.title : "Unknown Song";
            songNameText.color = titleColor;
        }

        if (artistText != null)
        {
            artistText.text = songConfig != null ? songConfig.artist : "Unknown Artist";
            artistText.color = subTextColor;
        }

        if (miniCoverImage != null)
        {
            if (songConfig != null && songConfig.coverSprite != null)
            {
                miniCoverImage.sprite = songConfig.coverSprite;
                miniCoverImage.enabled = true;
                miniCoverImage.color = Color.white;
            }
            else
            {
                miniCoverImage.enabled = true;
                miniCoverImage.sprite = null;
                miniCoverImage.color = new Color32(48, 58, 78, 255);
            }
        }

        if (chartData != null)
        {
            if (bpmText != null)
            {
                bpmText.text = $"BPM {chartData.bpm}";
                bpmText.color = subTextColor;
            }

            if (difficultyText != null)
            {
                difficultyText.text = $"{chartData.difficulty} Lv.{chartData.level}";
                difficultyText.color = Color.white;
            }

            if (difficultyBadgeImage != null)
            {
                difficultyBadgeImage.color = GetDifficultyColor(chartData.difficulty);
            }
        }
        else
        {
            if (bpmText != null)
            {
                bpmText.text = "BPM -";
                bpmText.color = subTextColor;
            }

            if (difficultyText != null)
            {
                difficultyText.text = $"{difficultyName} 无谱面";
                difficultyText.color = Color.white;
            }

            if (difficultyBadgeImage != null)
            {
                difficultyBadgeImage.color = new Color32(95, 100, 110, 255);
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(selected);
        }

        Image indicatorImage = selectedIndicator != null
            ? selectedIndicator.GetComponent<Image>()
            : null;

        if (indicatorImage != null)
        {
            indicatorImage.color = selectedIndicatorColor;
        }

        RefreshTargetVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        RefreshTargetVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        isPressing = false;
        RefreshTargetVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        RefreshTargetVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
        RefreshTargetVisual();
    }

    private void OnClick()
    {
        if (songSelectManager != null)
        {
            songSelectManager.SelectSong(songIndex);
        }
    }

    private void RefreshTargetVisual()
    {
        if (isPressing)
        {
            targetColor = pressedColor;
            targetScale = pressedScale;
            return;
        }

        if (isSelected)
        {
            targetColor = selectedColor;
            targetScale = selectedScale;
            return;
        }

        if (isHovering)
        {
            targetColor = hoverColor;
            targetScale = hoverScale;
            return;
        }

        targetColor = normalColor;
        targetScale = normalScale;
    }

    private void UpdateAnimation()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(
                backgroundImage.color,
                targetColor,
                Time.deltaTime * colorLerpSpeed
            );
        }

        if (rectTransform != null)
        {
            Vector3 target = Vector3.one * targetScale;

            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                target,
                Time.deltaTime * scaleLerpSpeed
            );
        }
    }

    private void ApplyImmediateVisual()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * targetScale;
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
            default:
                return normalDifficultyColor;
        }
    }
}
