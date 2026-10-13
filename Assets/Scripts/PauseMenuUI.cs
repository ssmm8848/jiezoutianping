using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public NoteSpawner noteSpawner;
    public JudgeSystem judgeSystem;

    [Header("Note Speed")]
    public Slider noteTravelTimeSlider;
    public TMP_Text noteTravelTimeValueText;

    [Header("Input Offset")]
    public Slider inputOffsetSlider;
    public TMP_Text inputOffsetValueText;

    private void Start()
    {
        SetupSliders();
        RefreshAllTexts();
        ApplyPauseMenuStyle();
    }

    private void ApplyPauseMenuStyle()
    {
        Image overlay = GetComponent<Image>();
        if (overlay != null)
        {
            overlay.color = new Color32(3, 8, 19, 205);
            overlay.raycastTarget = true;
        }

        Transform window = transform.Find("PauseWindow");
        if (window == null)
        {
            window = transform;
        }

        RectTransform windowRect = window as RectTransform;
        if (windowRect != null && window != transform)
        {
            windowRect.sizeDelta = new Vector2(800f, 620f);
        }

        Image windowImage = window.GetComponent<Image>();
        if (windowImage != null)
        {
            RhythmUITheme.StylePanel(windowImage, RhythmUITheme.Panel);
            windowImage.raycastTarget = true;
        }

        Outline windowOutline = window.GetComponent<Outline>();
        if (windowOutline == null && windowImage != null)
        {
            windowOutline = window.gameObject.AddComponent<Outline>();
        }
        if (windowOutline != null)
        {
            windowOutline.effectColor = new Color32(0, 203, 224, 180);
            windowOutline.effectDistance = new Vector2(2f, -2f);
        }

        TMP_Text title = FindPauseText("PauseTitleText");
        if (title != null)
        {
            title.text = "暂停";
            title.color = new Color32(235, 246, 255, 255);
            title.fontSize = 38f;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;
        }

        TMP_Text settingsTitle = FindPauseText("SettingsTitleText");
        if (settingsTitle != null)
        {
            settingsTitle.text = "设置";
            settingsTitle.color = new Color32(194, 222, 238, 255);
            settingsTitle.fontSize = 25f;
            settingsTitle.alignment = TextAlignmentOptions.Center;
            settingsTitle.raycastTarget = false;
        }

        ConfigurePauseButton("ResumeButton", new Vector2(-132f, 170f), new Color32(12, 178, 204, 255), new Color32(5, 19, 30, 255));
        ConfigurePauseButton("RetryButton", new Vector2(132f, 170f), new Color32(28, 48, 73, 255), Color.white);
        ConfigurePauseButton("BackToSongSelectButton", new Vector2(-132f, 95f), new Color32(28, 48, 73, 255), Color.white);
        ConfigurePauseButton("BackToMainMenuButton", new Vector2(132f, 95f), new Color32(75, 39, 57, 255), Color.white);

        PositionPauseElement(FindPauseText("SettingsTitleText"), new Vector2(0f, 20f));
        PositionPauseElement(FindPauseText("NoteTravelTimeLabel"), new Vector2(-230f, -25f));
        PositionPauseElement(noteTravelTimeValueText, new Vector2(90f, -58f));
        PositionPauseElement(FindPauseText("InputOffsetLabel"), new Vector2(-230f, -115f));
        PositionPauseElement(inputOffsetValueText, new Vector2(90f, -160f));
        PositionPauseElement(noteTravelTimeSlider != null ? noteTravelTimeSlider.GetComponent<RectTransform>() : null, new Vector2(90f, -25f));
        PositionPauseElement(inputOffsetSlider != null ? inputOffsetSlider.GetComponent<RectTransform>() : null, new Vector2(90f, -115f));
        EnsureTutorialButton(window);

        StyleSlider(noteTravelTimeSlider);
        StyleSlider(inputOffsetSlider);
        StyleLabel(FindPauseText("NoteTravelTimeLabel"), 18f, new Color32(193, 219, 235, 255));
        StyleLabel(FindPauseText("InputOffsetLabel"), 18f, new Color32(193, 219, 235, 255));
        StyleLabel(noteTravelTimeValueText, 16f, new Color32(139, 184, 208, 255));
        StyleLabel(inputOffsetValueText, 16f, new Color32(139, 184, 208, 255));
    }

    private TMP_Text FindPauseText(string objectName)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private void PositionPauseElement(Component element, Vector2 position)
    {
        if (element == null)
        {
            return;
        }

        RectTransform rect = element.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
    }

    private void EnsureTutorialButton(Transform window)
    {
        if (window == null || window.Find("GravityTutorialButton") != null)
        {
            return;
        }

        GameObject buttonObject = new GameObject(
            "GravityTutorialButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = window.gameObject.layer;
        buttonObject.transform.SetParent(window, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -230f);
        rect.sizeDelta = new Vector2(220f, 42f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(23, 75, 88, 255);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = Color.Lerp(image.color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(image.color, Color.black, 0.18f);
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            if (gameManager != null)
            {
                gameManager.ReplayGravityTutorial();
            }
        });

        TMP_FontAsset font = FindPauseText("PauseTitleText") != null
            ? FindPauseText("PauseTitleText").font
            : null;
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.layer = window.gameObject.layer;
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        if (font != null)
        {
            label.font = font;
        }
        label.text = "重看玩法说明";
        label.fontSize = 16f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(226, 245, 250, 255);
        label.raycastTarget = false;
    }

    private void ConfigurePauseButton(string objectName, Vector2 position, Color backgroundColor, Color labelColor)
    {
        Transform buttonTransform = transform.Find("PauseWindow/" + objectName);
        if (buttonTransform == null)
        {
            return;
        }

        RectTransform rect = buttonTransform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 58f);
        }

        Button button = buttonTransform.GetComponent<Button>();
        Image buttonImage = buttonTransform.GetComponent<Image>();
        if (buttonImage != null && button == null)
        {
            buttonImage.sprite = RhythmUITheme.RoundedSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = backgroundColor;
            buttonImage.raycastTarget = true;
        }

        if (button != null)
        {
            RhythmUITheme.StyleButton(button, backgroundColor, labelColor);
        }

        TMP_Text label = buttonTransform.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = labelColor;
            label.fontSize = 19f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 20f;
            label.raycastTarget = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }

    private void StyleSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        Image track = slider.GetComponent<Image>();
        if (track != null)
        {
            track.color = new Color32(22, 38, 58, 255);
            track.sprite = RhythmUITheme.RoundedSprite;
            track.type = Image.Type.Sliced;
        }

        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.color = new Color32(0, 196, 218, 255);
            fill.sprite = RhythmUITheme.RoundedSprite;
            fill.type = Image.Type.Sliced;
        }

        Image handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null)
        {
            handle.color = new Color32(224, 248, 255, 255);
            handle.sprite = RhythmUITheme.RoundedSprite;
            handle.type = Image.Type.Sliced;
        }
    }

    private void StyleLabel(TMP_Text label, float fontSize, Color color)
    {
        if (label == null)
        {
            return;
        }

        label.fontSize = fontSize;
        label.color = color;
        label.raycastTarget = false;
    }

    private void SetupSliders()
    {
        if (noteTravelTimeSlider != null)
        {
            noteTravelTimeSlider.minValue = 4f;
            noteTravelTimeSlider.maxValue = 24f;

            if (noteSpawner != null)
            {
                noteTravelTimeSlider.value = noteSpawner.GetNoteSpeed();
            }
            else
            {
                noteTravelTimeSlider.value = 8f;
            }

            noteTravelTimeSlider.onValueChanged.RemoveAllListeners();
            noteTravelTimeSlider.onValueChanged.AddListener(OnNoteTravelTimeChanged);
        }

        if (inputOffsetSlider != null)
        {
            inputOffsetSlider.minValue = -200f;
            inputOffsetSlider.maxValue = 200f;

            if (judgeSystem != null)
            {
                inputOffsetSlider.value = judgeSystem.GetInputOffsetMilliseconds();
            }

            inputOffsetSlider.onValueChanged.RemoveAllListeners();
            inputOffsetSlider.onValueChanged.AddListener(OnInputOffsetChanged);
        }
    }

    public void OnNoteTravelTimeChanged(float value)
    {
        if (noteSpawner != null)
        {
            noteSpawner.SetNoteSpeed(value, true);
        }

        UpdateNoteTravelTimeText(value);
    }

    public void OnInputOffsetChanged(float milliseconds)
    {
        if (judgeSystem != null)
        {
            judgeSystem.SetInputOffsetMilliseconds(milliseconds);
        }

        UpdateInputOffsetText(milliseconds);
    }

    public void ResumeGame()
    {
        if (gameManager != null)
        {
            gameManager.ResumeGame();
        }
    }

    public void RetryGame()
    {
        if (gameManager != null)
        {
            gameManager.RetryGame();
        }
    }

    public void BackToSongSelect()
    {
        if (gameManager != null)
        {
            gameManager.ReturnToSongSelect();
        }
    }

    private void RefreshAllTexts()
    {
        if (noteSpawner != null)
        {
            UpdateNoteTravelTimeText(noteSpawner.GetNoteSpeed());
        }

        if (judgeSystem != null)
        {
            UpdateInputOffsetText(judgeSystem.GetInputOffsetMilliseconds());
        }
    }

    private void UpdateNoteTravelTimeText(float value)
    {
        if (noteTravelTimeValueText != null)
        {
            noteTravelTimeValueText.text = $"流速：{value:F1}（越大越快）";
        }
    }

    public void BackToMainMenu()
    {
        if (gameManager != null)
        {
            gameManager.ReturnToMainMenu();
        }
    }

    private void UpdateInputOffsetText(float milliseconds)
    {
        if (inputOffsetValueText != null)
        {
            inputOffsetValueText.text = $"按键延迟：{milliseconds:F0} ms";
        }
    }
}
