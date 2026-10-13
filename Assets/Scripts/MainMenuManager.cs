using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private const string MasterVolumeKey = "GB_MasterVolume";
    private const string NoteSpeedKey = "GB_NoteSpeed";
    private const string InputOffsetKey = "GB_InputOffsetMs";
    private const float MinNoteSpeed = 4f;
    private const float MaxNoteSpeed = 24f;
    private const float MinInputOffset = -200f;
    private const float MaxInputOffset = 200f;

    [Header("Scenes")]
    public string songSelectSceneName = "SongSelectScene";

    [Header("UI")]
    public GameObject settingsPanel;

    private Slider masterVolumeSlider;
    private Slider noteSpeedSlider;
    private Slider inputOffsetSlider;
    private TMP_Text masterVolumeLabel;
    private TMP_Text noteSpeedLabel;
    private TMP_Text inputOffsetLabel;
    private TMP_FontAsset settingsFont;
    private bool settingsControlsInitialized;

    private void Start()
    {
        AudioListener.volume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(MasterVolumeKey, AudioListener.volume)
        );

        ApplyMainMenuStyle();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            EnsureSettingsControls();
        }
    }

    private void Update()
    {
        if (settingsPanel != null && settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
        }
    }

    private void InitializeSettingsControls()
    {
        Transform settingsWindow = settingsPanel.transform.Find("SettingsWindow");
        if (settingsWindow == null)
        {
            settingsWindow = settingsPanel.transform;
        }

        RectTransform windowRect = settingsWindow as RectTransform;
        if (windowRect != null)
        {
            windowRect.sizeDelta = new Vector2(680f, 520f);
        }

        TMP_Text[] existingTexts = settingsPanel.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text tipText = null;
        for (int i = 0; i < existingTexts.Length; i++)
        {
            if (settingsFont == null && existingTexts[i].font != null)
            {
                settingsFont = existingTexts[i].font;
            }

            if (existingTexts[i].gameObject.name == "SettingsTipText")
            {
                tipText = existingTexts[i];
            }
        }

        if (tipText != null)
        {
            tipText.text = "音符流速越大，音符移动越快；此设置会保存。\n演奏中也可按 Esc 暂停后调整流速和判定偏移。";
            tipText.alignment = TextAlignmentOptions.Center;
            RhythmUITheme.StyleText(tipText, 13f, RhythmUITheme.MutedText);
            tipText.rectTransform.anchoredPosition = new Vector2(0f, -150f);
            tipText.rectTransform.sizeDelta = new Vector2(580f, 40f);
        }

        masterVolumeLabel = CreateSettingsLabel(
            "MasterVolumeLabel",
            settingsWindow,
            "主音量",
            new Vector2(0f, 125f),
            new Vector2(500f, 34f),
            19f
        );

        masterVolumeSlider = CreateSettingsSlider(settingsWindow, "MasterVolumeSlider", 92f, 0f, 1f);
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        UpdateMasterVolumeLabel(AudioListener.volume);

        noteSpeedLabel = CreateSettingsLabel(
            "NoteSpeedLabel",
            settingsWindow,
            "音符流速",
            new Vector2(0f, 34f),
            new Vector2(500f, 34f),
            19f);
        float savedNoteSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(NoteSpeedKey, 8f), MinNoteSpeed, MaxNoteSpeed);
        noteSpeedSlider = CreateSettingsSlider(settingsWindow, "NoteSpeedSlider", 0f, MinNoteSpeed, MaxNoteSpeed);
        noteSpeedSlider.onValueChanged.AddListener(OnNoteSpeedChanged);
        noteSpeedSlider.SetValueWithoutNotify(savedNoteSpeed);
        UpdateNoteSpeedLabel(savedNoteSpeed);

        inputOffsetLabel = CreateSettingsLabel(
            "InputOffsetLabel",
            settingsWindow,
            "判定偏移",
            new Vector2(0f, -57f),
            new Vector2(500f, 34f),
            19f);
        float savedInputOffset = Mathf.Clamp(PlayerPrefs.GetFloat(InputOffsetKey, 0f), MinInputOffset, MaxInputOffset);
        inputOffsetSlider = CreateSettingsSlider(settingsWindow, "InputOffsetSlider", -90f, MinInputOffset, MaxInputOffset);
        inputOffsetSlider.onValueChanged.AddListener(OnInputOffsetChanged);
        inputOffsetSlider.SetValueWithoutNotify(savedInputOffset);
        UpdateInputOffsetLabel(savedInputOffset);

        TMP_Text title = FindMenuText("SettingsTitleText");
        RhythmUITheme.StyleText(title, 30f, RhythmUITheme.Text, true);
        if (title != null)
        {
            title.alignment = TextAlignmentOptions.Center;
        }

        Transform closeTransform = FindMenuObject("CloseSettingsButton");
        if (closeTransform != null)
        {
            Button closeButton = closeTransform.GetComponent<Button>();
            RhythmUITheme.StyleButton(closeButton, RhythmUITheme.PanelRaised, RhythmUITheme.Text);
            RectTransform closeRect = closeTransform as RectTransform;
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.5f, 0.5f);
                closeRect.anchorMax = new Vector2(0.5f, 0.5f);
                closeRect.pivot = new Vector2(0.5f, 0.5f);
                closeRect.anchoredPosition = new Vector2(0f, -218f);
                closeRect.sizeDelta = new Vector2(220f, 52f);
            }
        }
    }

    private void ApplyMainMenuStyle()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform background = RhythmUITheme.FindDeep(canvas.transform, "Background");
        RhythmUITheme.ApplyBackdrop(
            background != null ? background.GetComponent<Image>() : null,
            "GameplayPanorama",
            new Color32(150, 166, 192, 255)
        );

        Transform glow = RhythmUITheme.FindDeep(canvas.transform, "GlowCircle");
        Image glowImage = glow != null ? glow.GetComponent<Image>() : null;
        if (glowImage != null)
        {
            glowImage.color = new Color32(12, 173, 197, 34);
        }

        StyleMenuButton(canvas.transform, "StartButton", RhythmUITheme.Primary, new Color32(4, 26, 39, 255));
        StyleMenuButton(canvas.transform, "SettingsButton", RhythmUITheme.PanelRaised, RhythmUITheme.Text);
        StyleMenuButton(canvas.transform, "QuitButton", new Color32(82, 40, 55, 255), RhythmUITheme.Text);

        TMP_Text title = FindMenuText("TitleText");
        RhythmUITheme.StyleText(title, 70f, RhythmUITheme.Text, true);
        if (title != null)
        {
            title.characterSpacing = 2.5f;
        }

        TMP_Text subtitle = FindMenuText("SubtitleText");
        RhythmUITheme.StyleText(subtitle, 24f, new Color32(176, 205, 226, 255));
        RhythmUITheme.StyleText(FindMenuText("VersionText"), 14f, RhythmUITheme.MutedText);

        if (settingsPanel != null)
        {
            Image overlay = settingsPanel.GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color32(1, 5, 15, 205);
                overlay.raycastTarget = true;
            }

            Transform settingsWindow = RhythmUITheme.FindDeep(settingsPanel.transform, "SettingsWindow");
            Image windowImage = settingsWindow != null ? settingsWindow.GetComponent<Image>() : null;
            RhythmUITheme.StylePanel(windowImage, RhythmUITheme.Panel);
        }
    }

    private void StyleMenuButton(Transform root, string objectName, Color color, Color labelColor)
    {
        Transform buttonTransform = RhythmUITheme.FindDeep(root, objectName);
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        RhythmUITheme.StyleButton(button, color, labelColor);

        if (button != null)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            RhythmUITheme.StyleText(label, 29f, labelColor, true);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.Center;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 330f), 74f);
            }
        }
    }

    private Transform FindMenuObject(string objectName)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        return canvas != null ? RhythmUITheme.FindDeep(canvas.transform, objectName) : null;
    }

    private TMP_Text FindMenuText(string objectName)
    {
        Transform target = FindMenuObject(objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private TMP_Text CreateSettingsLabel(
        string objectName,
        Transform parent,
        string labelText,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        GameObject labelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        labelObject.layer = parent.gameObject.layer;
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = labelObject.GetComponent<TMP_Text>();
        if (settingsFont != null)
        {
            text.font = settingsFont;
        }
        text.text = labelText;
        RhythmUITheme.StyleText(text, fontSize, RhythmUITheme.Text, true);
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private Slider CreateSettingsSlider(Transform parent, string objectName, float yPosition, float minValue, float maxValue)
    {
        GameObject sliderObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Slider)
        );
        sliderObject.layer = parent.gameObject.layer;
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0f, yPosition);
        sliderRect.sizeDelta = new Vector2(440f, 28f);

        Image trackImage = sliderObject.GetComponent<Image>();
        trackImage.color = new Color32(31, 47, 70, 255);
        trackImage.sprite = RhythmUITheme.RoundedSprite;
        trackImage.type = Image.Type.Sliced;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.targetGraphic = trackImage;

        RectTransform fillArea = CreateRectTransform("Fill Area", sliderRect);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(12f, -4f);
        fillArea.offsetMax = new Vector2(-12f, 4f);

        GameObject fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        fillObject.layer = parent.gameObject.layer;
        fillObject.transform.SetParent(fillArea, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color32(0, 205, 228, 255);
        fillImage.sprite = RhythmUITheme.RoundedSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        slider.fillRect = fillRect;

        RectTransform handleArea = CreateRectTransform("Handle Slide Area", sliderRect);
        handleArea.anchorMin = new Vector2(0f, 0.5f);
        handleArea.anchorMax = new Vector2(1f, 0.5f);
        handleArea.offsetMin = new Vector2(12f, 0f);
        handleArea.offsetMax = new Vector2(-12f, 0f);

        GameObject handleObject = new GameObject(
            "Handle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        handleObject.layer = parent.gameObject.layer;
        handleObject.transform.SetParent(handleArea, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.sizeDelta = new Vector2(22f, 30f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color32(236, 249, 255, 255);
        handleImage.sprite = RhythmUITheme.RoundedSprite;

        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        return slider;
    }

    private RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
        UpdateMasterVolumeLabel(AudioListener.volume);
    }

    private void OnNoteSpeedChanged(float value)
    {
        value = Mathf.Clamp(value, MinNoteSpeed, MaxNoteSpeed);
        PlayerPrefs.SetFloat(NoteSpeedKey, value);
        PlayerPrefs.Save();
        UpdateNoteSpeedLabel(value);
    }

    private void OnInputOffsetChanged(float value)
    {
        value = Mathf.Clamp(value, MinInputOffset, MaxInputOffset);
        PlayerPrefs.SetFloat(InputOffsetKey, value);
        PlayerPrefs.Save();
        UpdateInputOffsetLabel(value);
    }

    private void UpdateMasterVolumeLabel(float value)
    {
        if (masterVolumeLabel != null)
        {
            masterVolumeLabel.text = $"主音量  {Mathf.RoundToInt(value * 100f)}%";
        }
    }

    private void UpdateNoteSpeedLabel(float value)
    {
        if (noteSpeedLabel != null)
        {
            noteSpeedLabel.text = $"音符流速  {value:F1}  ·  越大越快";
        }
    }

    private void UpdateInputOffsetLabel(float value)
    {
        if (inputOffsetLabel != null)
        {
            string sign = value > 0f ? "+" : "";
            inputOffsetLabel.text = $"判定偏移  {sign}{value:F0} ms";
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(songSelectSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            EnsureSettingsControls();
            RefreshSettingsValues();
            settingsPanel.SetActive(true);
        }
    }

    private void EnsureSettingsControls()
    {
        if (settingsPanel == null || settingsControlsInitialized)
        {
            return;
        }

        InitializeSettingsControls();
        settingsControlsInitialized = true;
    }

    private void RefreshSettingsValues()
    {
        if (masterVolumeSlider != null)
        {
            float volume = Mathf.Clamp01(AudioListener.volume);
            masterVolumeSlider.SetValueWithoutNotify(volume);
            UpdateMasterVolumeLabel(volume);
        }

        if (noteSpeedSlider != null)
        {
            float speed = Mathf.Clamp(PlayerPrefs.GetFloat(NoteSpeedKey, 8f), MinNoteSpeed, MaxNoteSpeed);
            noteSpeedSlider.SetValueWithoutNotify(speed);
            UpdateNoteSpeedLabel(speed);
        }

        if (inputOffsetSlider != null)
        {
            float offset = Mathf.Clamp(PlayerPrefs.GetFloat(InputOffsetKey, 0f), MinInputOffset, MaxInputOffset);
            inputOffsetSlider.SetValueWithoutNotify(offset);
            UpdateInputOffsetLabel(offset);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
