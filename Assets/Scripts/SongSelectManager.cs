using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SongSelectManager : MonoBehaviour
{
    [Header("Songs")]
    public List<SongConfig> songConfigs = new List<SongConfig>();

    [Header("Scene")]
    public string gameSceneName = "RhythmTestScene";
    public string vertical2DSceneName = "Rhythm2DScene";

    [Header("Song Preview")]
    public AudioSource previewAudioSource;
    [Range(0f, 1f)]
    public float previewVolume = 0.75f;
    [Min(0.01f)]
    public float previewFadeInDuration = 0.3f;
    [Min(0.01f)]
    public float previewFadeOutDuration = 0.35f;

    [Header("Song List")]
    public SongItemUI songItemPrefab;
    public Transform songListContent;

    [Header("Selected Song Detail UI")]
    public SongDetailUI songDetailUI;
    public Image selectedCoverImage;
    public TMP_Text selectedSongTitleText;
    public TMP_Text selectedArtistText;
    public TMP_Text selectedBpmText;
    public TMP_Text selectedDifficultyText;
    public TMP_Text selectedLevelText;
    public TMP_Text selectedNoteCountText;

    [Header("Top UI")]
    public TMP_Text songCountText;

    [Header("Bottom Difficulty UI")]
    public TMP_Text difficultyNameText;
    public TMP_Text difficultyLevelText;

    private readonly string[] difficultyNames =
    {
        "Easy",
        "Normal",
        "Hard"
    };

    private readonly List<SongItemUI> spawnedSongItems = new List<SongItemUI>();

    private int selectedSongIndex = 0;
    private int selectedDifficultyIndex = 1;

    private TextAsset selectedChart;
    private ChartJsonData selectedChartData;
    private Coroutine previewCoroutine;

    private void Awake()
    {
        EnsurePreviewAudioSource();
    }

    private void Start()
    {
        RemoveDuplicateSongs();
        BuildSongList();
        CreateVertical2DStartButton();
        ApplySongSelectVisualStyle();

        if (songConfigs.Count > 0)
        {
            SelectSong(0);
        }
        else
        {
            Debug.LogWarning("SongSelectManager 没有绑定任何 SongConfig");
            UpdateSongCountText();
        }
    }

    private void RemoveDuplicateSongs()
    {
        if (songConfigs == null)
        {
            songConfigs = new List<SongConfig>();
            return;
        }

        HashSet<SongConfig> uniqueSongs = new HashSet<SongConfig>();
        int originalCount = songConfigs.Count;
        songConfigs.RemoveAll(song => song == null || !uniqueSongs.Add(song));

        if (songConfigs.Count < originalCount)
        {
            Debug.LogWarning(
                $"SongSelectManager 移除了 {originalCount - songConfigs.Count} 个重复或无效曲目引用"
            );
        }
    }

    private void BuildSongList()
    {
        ClearSongList();

        for (int i = 0; i < songConfigs.Count; i++)
        {
            SongConfig songConfig = songConfigs[i];

            SongItemUI item = Instantiate(
                songItemPrefab,
                songListContent
            );

            item.gameObject.SetActive(true);

            ChartJsonData chartData = LoadChartData(
                GetChartByDifficultyIndex(songConfig, selectedDifficultyIndex)
            );

            item.Initialize(
                this,
                i,
                songConfig,
                chartData,
                difficultyNames[selectedDifficultyIndex]
            );

            item.SetSelected(false);

            spawnedSongItems.Add(item);
        }

        UpdateSongCountText();
    }

    private void ClearSongList()
    {
        for (int i = 0; i < spawnedSongItems.Count; i++)
        {
            if (spawnedSongItems[i] != null)
            {
                Destroy(spawnedSongItems[i].gameObject);
            }
        }

        spawnedSongItems.Clear();

        if (songListContent == null)
        {
            return;
        }

        for (int i = songListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(songListContent.GetChild(i).gameObject);
        }
    }

    public void SelectSong(int songIndex)
    {
        if (songIndex < 0 || songIndex >= songConfigs.Count)
        {
            return;
        }

        selectedSongIndex = songIndex;

        LoadSelectedChart();
        UpdateAllSongItems();
        UpdateSelectedSongDetailUI();
        UpdateDifficultyUI();
        PlaySongPreview(GetSelectedSong());
    }

    public void PreviousDifficulty()
    {
        selectedDifficultyIndex--;

        if (selectedDifficultyIndex < 0)
        {
            selectedDifficultyIndex = difficultyNames.Length - 1;
        }

        LoadSelectedChart();
        UpdateAllSongItems();
        UpdateSelectedSongDetailUI();
        UpdateDifficultyUI();
    }

    public void NextDifficulty()
    {
        selectedDifficultyIndex++;

        if (selectedDifficultyIndex >= difficultyNames.Length)
        {
            selectedDifficultyIndex = 0;
        }

        LoadSelectedChart();
        UpdateAllSongItems();
        UpdateSelectedSongDetailUI();
        UpdateDifficultyUI();
    }

    private void LoadSelectedChart()
    {
        SongConfig selectedSong = GetSelectedSong();

        selectedChart = GetChartByDifficultyIndex(
            selectedSong,
            selectedDifficultyIndex
        );

        selectedChartData = LoadChartData(selectedChart);
    }

    private SongConfig GetSelectedSong()
    {
        if (songConfigs == null || songConfigs.Count == 0)
        {
            return null;
        }

        if (selectedSongIndex < 0 || selectedSongIndex >= songConfigs.Count)
        {
            return null;
        }

        return songConfigs[selectedSongIndex];
    }

    private TextAsset GetChartByDifficultyIndex(
        SongConfig songConfig,
        int difficultyIndex)
    {
        if (songConfig == null)
        {
            return null;
        }

        switch (difficultyIndex)
        {
            case 0:
                return songConfig.easyChart;

            case 1:
                return songConfig.normalChart;

            case 2:
                return songConfig.hardChart;

            default:
                return null;
        }
    }

    private ChartJsonData LoadChartData(TextAsset chart)
    {
        if (chart == null)
        {
            return null;
        }

        ChartJsonData chartData =
            JsonUtility.FromJson<ChartJsonData>(chart.text);

        return chartData;
    }

    private void UpdateAllSongItems()
    {
        for (int i = 0; i < spawnedSongItems.Count; i++)
        {
            SongConfig songConfig = songConfigs[i];

            ChartJsonData chartData = LoadChartData(
                GetChartByDifficultyIndex(songConfig, selectedDifficultyIndex)
            );

            spawnedSongItems[i].UpdateDisplay(
                songConfig,
                chartData,
                difficultyNames[selectedDifficultyIndex]
            );

            spawnedSongItems[i].SetSelected(i == selectedSongIndex);
        }
    }

    private void UpdateSelectedSongDetailUI()
    {

        SongConfig selectedSong = GetSelectedSong();

        if (songDetailUI != null)
        {
            songDetailUI.UpdateDetail(
                selectedSong,
                selectedChartData,
                difficultyNames[selectedDifficultyIndex]
            );

            return;
        }

        if (selectedSong == null)
        {
            return;
        }

        if (selectedCoverImage != null)
        {
            if (selectedSong.coverSprite != null)
            {
                selectedCoverImage.sprite = selectedSong.coverSprite;
                selectedCoverImage.enabled = true;
            }
            else
            {
                selectedCoverImage.enabled = false;
            }
        }

        if (selectedSongTitleText != null)
        {
            selectedSongTitleText.text = selectedSong.title;
        }

        if (selectedArtistText != null)
        {
            selectedArtistText.text = $"Artist: {selectedSong.artist}";
        }

        if (selectedChartData == null)
        {
            if (selectedBpmText != null)
            {
                selectedBpmText.text = "BPM -";
            }

            if (selectedDifficultyText != null)
            {
                selectedDifficultyText.text =
                    $"{difficultyNames[selectedDifficultyIndex]} 无谱面";
            }

            if (selectedLevelText != null)
            {
                selectedLevelText.text = "Lv.-";
            }

            if (selectedNoteCountText != null)
            {
                selectedNoteCountText.text = "Notes -";
            }

            return;
        }

        if (selectedBpmText != null)
        {
            selectedBpmText.text = $"BPM {selectedChartData.bpm}";
        }

        if (selectedDifficultyText != null)
        {
            selectedDifficultyText.text = selectedChartData.difficulty;
        }

        if (selectedLevelText != null)
        {
            selectedLevelText.text = $"Lv.{selectedChartData.level}";
        }

        if (selectedNoteCountText != null)
        {
            int noteCount = selectedChartData.notes != null
                ? selectedChartData.notes.Count
                : 0;

            selectedNoteCountText.text = $"Notes {noteCount}";
        }
    }

    private void UpdateDifficultyUI()
    {
        if (difficultyNameText != null)
        {
            difficultyNameText.text = difficultyNames[selectedDifficultyIndex];
        }

        if (difficultyLevelText != null)
        {
            if (selectedChartData != null)
            {
                difficultyLevelText.text = $"Lv.{selectedChartData.level}";
            }
            else
            {
                difficultyLevelText.text = "无谱面";
            }
        }
    }

    private void UpdateSongCountText()
    {
        if (songCountText != null)
        {
            songCountText.text = $"{songConfigs.Count} 首曲目";
        }
    }

    private void ApplySongSelectVisualStyle()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform background = RhythmUITheme.FindDeep(canvas.transform, "Background");
            RhythmUITheme.ApplyBackdrop(
                background != null ? background.GetComponent<Image>() : null,
                "GameplayPanorama",
                new Color32(112, 139, 173, 255)
            );

            StylePanelByName(canvas.transform, "TopBar", new Color32(6, 15, 31, 235));
            StylePanelByName(canvas.transform, "BottomBar", new Color32(8, 18, 34, 238));
            StylePanelByName(canvas.transform, "SongListPanel", new Color32(9, 20, 38, 232));
            StylePanelByName(canvas.transform, "SongDetailPanel", new Color32(12, 25, 44, 242));
            StylePanelByName(canvas.transform, "ListHeader", new Color32(20, 37, 59, 248));
            StylePanelByName(canvas.transform, "DifficultyDisplay", new Color32(18, 34, 53, 250));
            StylePanelByName(canvas.transform, "SongScrollView", new Color32(9, 18, 34, 236));

            Transform viewport = RhythmUITheme.FindDeep(canvas.transform, "Viewport");
            RhythmUITheme.StylePanel(
                viewport != null ? viewport.GetComponent<Image>() : null,
                new Color32(8, 17, 31, 236),
                false
            );

            StyleSceneButton(canvas.transform, "BackButton", RhythmUITheme.PanelRaised, RhythmUITheme.Text);
            StyleSceneButton(canvas.transform, "SortButton", RhythmUITheme.PanelRaised, RhythmUITheme.Text);
            StyleSceneButton(canvas.transform, "PreviousDifficultyButton", RhythmUITheme.PanelRaised, RhythmUITheme.Text);
            StyleSceneButton(canvas.transform, "NextDifficultyButton", RhythmUITheme.PanelRaised, RhythmUITheme.Text);

            StyleTextByName(canvas.transform, "PageTitleText", 34f, RhythmUITheme.Text, true);
            StyleTextByName(canvas.transform, "SongCountText", 18f, RhythmUITheme.MutedText);
            StyleTextByName(canvas.transform, "ListTitleText", 21f, RhythmUITheme.Text, true);
            StyleTextByName(canvas.transform, "DifficultyNameText", 24f, RhythmUITheme.Text, true);
            StyleTextByName(canvas.transform, "DifficultyLevelText", 18f, RhythmUITheme.Gold);
        }

        RhythmUITheme.StyleText(selectedSongTitleText, 32f, RhythmUITheme.Text, true);
        RhythmUITheme.StyleText(selectedArtistText, 20f, RhythmUITheme.MutedText);
        RhythmUITheme.StyleText(selectedBpmText, 19f, new Color32(194, 219, 236, 255));
        RhythmUITheme.StyleText(selectedDifficultyText, 18f, RhythmUITheme.Primary, true);
        RhythmUITheme.StyleText(selectedLevelText, 24f, RhythmUITheme.Gold, true);
        RhythmUITheme.StyleText(selectedNoteCountText, 16f, RhythmUITheme.MutedText);
    }

    private void StylePanelByName(Transform root, string objectName, Color color)
    {
        Transform target = RhythmUITheme.FindDeep(root, objectName);
        RhythmUITheme.StylePanel(target != null ? target.GetComponent<Image>() : null, color);
    }

    private void StyleSceneButton(Transform root, string objectName, Color color, Color labelColor)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == objectName)
            {
                RhythmUITheme.StyleButton(buttons[i], color, labelColor);
            }
        }
    }

    private void StyleTextByName(Transform root, string objectName, float size, Color color, bool bold = false)
    {
        Transform target = RhythmUITheme.FindDeep(root, objectName);
        RhythmUITheme.StyleText(target != null ? target.GetComponent<TMP_Text>() : null, size, color, bold);
    }

    public void BackToMainMenu()
    {
        StopSongPreview();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }

    public void StartGame()
    {
        StartSelectedGame(gameSceneName);
    }

    public void StartGame2D()
    {
        StartSelectedGame(vertical2DSceneName);
    }

    private void StartSelectedGame(string targetSceneName)
    {
        SongConfig selectedSong = GetSelectedSong();

        if (selectedSong == null)
        {
            Debug.LogError("无法开始游戏：没有选择歌曲");
            return;
        }

        if (selectedChart == null)
        {
            Debug.LogError("无法开始游戏：当前难度没有谱面");
            return;
        }

        StopSongPreview();

        if (SelectedSongData.Instance == null)
        {
            GameObject dataObject = new GameObject("SelectedSongData");
            dataObject.AddComponent<SelectedSongData>();
        }

        SelectedSongData.Instance.SetSelection(
            selectedSong,
            selectedChart,
            difficultyNames[selectedDifficultyIndex]
        );

        SceneManager.LoadScene(targetSceneName);
    }

    private void CreateVertical2DStartButton()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        Button standardStartButton = null;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button candidate = buttons[i];
            for (int callIndex = 0; callIndex < candidate.onClick.GetPersistentEventCount(); callIndex++)
            {
                if (candidate.onClick.GetPersistentTarget(callIndex) == this &&
                    candidate.onClick.GetPersistentMethodName(callIndex) == "StartGame")
                {
                    standardStartButton = candidate;
                    break;
                }
            }

            if (standardStartButton != null)
            {
                break;
            }
        }

        if (standardStartButton == null || standardStartButton.transform.parent == null)
        {
            Debug.LogWarning("未找到选曲页的 3D 开始按钮，无法创建 2D 模式入口");
            return;
        }

        RectTransform standardRect = standardStartButton.GetComponent<RectTransform>();
        Image standardImage = standardStartButton.GetComponent<Image>();
        GameObject buttonObject = new GameObject(
            "StartVertical2DButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.layer = standardStartButton.gameObject.layer;
        buttonObject.transform.SetParent(standardStartButton.transform.parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = standardRect.anchorMin;
        buttonRect.anchorMax = standardRect.anchorMax;
        buttonRect.pivot = standardRect.pivot;
        buttonRect.sizeDelta = new Vector2(230f, standardRect.sizeDelta.y);
        buttonRect.anchoredPosition = standardRect.anchoredPosition +
            new Vector2(-standardRect.sizeDelta.x - 14f, 0f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        if (standardImage != null)
        {
            buttonImage.sprite = standardImage.sprite;
            buttonImage.type = standardImage.type;
        }
        buttonImage.color = new Color32(32, 167, 190, 255);
        buttonImage.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        RhythmUITheme.StyleButton(button, RhythmUITheme.Primary, new Color32(4, 26, 39, 255));
        button.onClick.AddListener(StartGame2D);

        RhythmUITheme.StyleButton(standardStartButton, RhythmUITheme.PanelRaised, RhythmUITheme.Text);

        TMP_Text standardLabel = standardStartButton.GetComponentInChildren<TMP_Text>(true);
        if (standardLabel != null)
        {
            standardLabel.text = "3D 斜轨";
        }

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        labelObject.layer = buttonObject.layer;
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        if (standardLabel != null)
        {
            label.font = standardLabel.font;
        }
        else
        {
            label.font = TMP_Settings.defaultFontAsset;
        }
        label.text = "垂直 2D";
        label.fontSize = 24f;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 24f;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(5, 28, 43, 255);
        label.raycastTarget = false;
    }

    private void EnsurePreviewAudioSource()
    {
        if (previewAudioSource == null)
        {
            GameObject previewObject = new GameObject("Song Preview Audio");
            previewObject.transform.SetParent(transform, false);
            previewAudioSource = previewObject.AddComponent<AudioSource>();
        }

        previewAudioSource.playOnAwake = false;
        previewAudioSource.loop = false;
        previewAudioSource.spatialBlend = 0f;
        previewAudioSource.volume = previewVolume;
    }

    private void PlaySongPreview(SongConfig song)
    {
        StopSongPreview();

        if (song == null || song.audioClip == null || previewAudioSource == null)
        {
            return;
        }

        AudioClip clip = song.audioClip;
        float startTime = Mathf.Clamp(song.previewStartTime, 0f, Mathf.Max(0f, clip.length - 0.1f));
        float previewLength = Mathf.Min(Mathf.Max(0.1f, song.previewDuration), clip.length - startTime);

        previewAudioSource.clip = clip;
        previewAudioSource.volume = 0f;
        previewAudioSource.timeSamples = Mathf.Clamp(
            Mathf.RoundToInt(startTime * clip.frequency),
            0,
            Mathf.Max(0, clip.samples - 1)
        );
        previewAudioSource.Play();

        previewCoroutine = StartCoroutine(FadeAndStopPreview(previewLength));
    }

    private IEnumerator FadeAndStopPreview(float previewLength)
    {
        float fadeInDuration = Mathf.Min(previewFadeInDuration, previewLength * 0.4f);
        float fadeOutDuration = Mathf.Min(previewFadeOutDuration, previewLength * 0.4f);
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            previewAudioSource.volume = previewVolume * Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        previewAudioSource.volume = previewVolume;

        float holdDuration = Mathf.Max(0f, previewLength - fadeInDuration - fadeOutDuration);
        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            previewAudioSource.volume = previewVolume * (1f - Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        previewAudioSource.Stop();
        previewAudioSource.volume = previewVolume;
        previewCoroutine = null;
    }

    private void StopSongPreview()
    {
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }

        if (previewAudioSource != null)
        {
            previewAudioSource.Stop();
            previewAudioSource.clip = null;
            previewAudioSource.volume = previewVolume;
        }
    }

    private void OnDestroy()
    {
        StopSongPreview();
    }
}
