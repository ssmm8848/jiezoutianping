using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public enum GameState
{
    Ready,
    Playing,
    Paused,
    Result,
    Failed
}

public class GameManager : MonoBehaviour
{
    public GameState currentState = GameState.Ready;

    [Header("Audio")]
    public AudioSource musicSource;

    [Header("UI Panels")]
    public GameObject readyPanel;
    public GameObject resultPanel;
    public GameObject pausePanel;
    public GameObject pauseButton;

    [Header("Chart")]
    public ChartData chartData;
    public ScoreManager scoreManager;

    [Header("Scene")]
    public string songSelectSceneName = "SongSelectScene";
    public string mainMenuSceneName = "MainMenuScene";
    public bool autoStartFromSongSelect = true;

    [Header("Resume Countdown")]
    public ResumeCountdownUI resumeCountdownUI;
    public bool useResumeCountdown = true;

    [Header("Start Countdown")]
    public bool useStartCountdown = true;

    [Header("Pause")]
    public KeyCode pauseKey = KeyCode.Escape;

    private bool hasStarted = false;
    private bool isResumeCountingDown = false;
    private bool isStartCountingDown = false;
    private float musicLeadInDuration;
    private float musicLeadInElapsed;
    private float musicSourceVolume = 1f;
    private AudioClip gameplayMusicClip;
    private bool isSilentLeadIn;
    private bool chartLeadInPrepared;
    private TMP_Text resultTitleText;
    private TMP_Text resultScoreText;
    private TMP_Text resultDetailsText;
    private GravityHUD gravityHUD;
    private GravityTutorialUI gravityTutorialUI;

    private void Start()
    {
        currentState = GameState.Ready;
        InitializeGameplayBackdrop();
        ApplyGameplayHudStyle();

        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        bool shouldAutoStart = ShouldAutoStart();

        if (readyPanel != null)
        {
            readyPanel.SetActive(!shouldAutoStart);
        }

        if (pauseButton != null)
        {
            Button button = pauseButton.GetComponent<Button>();
            RhythmUITheme.StyleButton(button, RhythmUITheme.PanelRaised, RhythmUITheme.Text);
            TMP_Text label = pauseButton.GetComponentInChildren<TMP_Text>(true);
            RhythmUITheme.StyleText(label, 18f, RhythmUITheme.Text, true);
            pauseButton.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        EnsureResultPanelLayout();
        InitializeGravityHUD();
        InitializeGravityTutorialUI();

        if (SelectedSongData.Instance != null &&
            SelectedSongData.Instance.selectedSong != null &&
            SelectedSongData.Instance.selectedSong.audioClip != null &&
            musicSource != null)
        {
            musicSource.clip = SelectedSongData.Instance.selectedSong.audioClip;
        }


        if (musicSource != null)
        {
            musicSource.Stop();
            gameplayMusicClip = musicSource.clip;
            musicSourceVolume = musicSource.volume;
        }

        if (shouldAutoStart)
        {
            StartCoroutine(AutoStartNextFrame());
        }
    }

    private void Update()
    {
        if (gravityTutorialUI != null && gravityTutorialUI.IsVisible)
        {
            if (Input.GetKeyDown(pauseKey))
            {
                gravityTutorialUI.CloseTutorial();
            }
            return;
        }

        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }

        if (currentState != GameState.Playing)
        {
            return;
        }

        if (isSilentLeadIn)
        {
            musicLeadInElapsed = Mathf.Min(musicLeadInDuration, musicLeadInElapsed + Time.deltaTime);
            if (musicLeadInElapsed >= musicLeadInDuration)
            {
                StartAudibleMusic();
            }
            return;
        }

        if (musicSource != null && hasStarted && !musicSource.isPlaying)
        {
            EndGame();
        }
    }

    private bool ShouldAutoStart()
    {
        return autoStartFromSongSelect &&
               SelectedSongData.Instance != null &&
               SelectedSongData.Instance.selectedChart != null;
    }

    private IEnumerator AutoStartNextFrame()
    {
        yield return null;

        if (currentState == GameState.Ready)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (currentState != GameState.Ready || isStartCountingDown)
        {
            return;
        }

        if (gravityTutorialUI != null && !gravityTutorialUI.HasSeenTutorial)
        {
            gravityTutorialUI.Show(false, OnFirstTutorialClosed);
            return;
        }

        if (useStartCountdown && resumeCountdownUI != null)
        {
            isStartCountingDown = true;
            resumeCountdownUI.PlayCountdown(StartGameImmediately);
            return;
        }

        StartGameImmediately();
    }

    private void OnFirstTutorialClosed()
    {
        if (gravityTutorialUI != null)
        {
            gravityTutorialUI.MarkTutorialSeen();
        }

        StartGame();
    }

    public void ReplayGravityTutorial()
    {
        if (currentState != GameState.Paused || gravityTutorialUI == null)
        {
            return;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        gravityTutorialUI.Show(true, () =>
        {
            if (currentState == GameState.Paused && pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
        });
    }

    private void StartGameImmediately()
    {
        if (currentState != GameState.Ready)
        {
            return;
        }

        PrepareChartLeadIn();
        isStartCountingDown = false;

        currentState = GameState.Playing;
        hasStarted = true;

        if (readyPanel != null)
        {
            readyPanel.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }

        SetGravityHUDVisible(true);

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = gameplayMusicClip;
            musicSource.time = 0f;
            musicLeadInElapsed = 0f;

            if (musicLeadInDuration > 0f && gameplayMusicClip != null)
            {
                isSilentLeadIn = true;
                musicSource.volume = musicSourceVolume;
            }
            else
            {
                isSilentLeadIn = false;
                musicSource.volume = musicSourceVolume;
                musicSource.Play();
            }
        }

        Debug.Log("游戏开始");
    }

    private void PrepareChartLeadIn()
    {
        if (chartLeadInPrepared)
        {
            return;
        }

        NoteSpawner noteSpawner = FindObjectOfType<NoteSpawner>();
        ChartData activeChart = noteSpawner != null && noteSpawner.chartData != null
            ? noteSpawner.chartData
            : chartData;
        if (activeChart == null || activeChart.notes == null || activeChart.notes.Count == 0)
        {
            chartLeadInPrepared = true;
            return;
        }

        float firstNoteTime = float.MaxValue;
        for (int i = 0; i < activeChart.notes.Count; i++)
        {
            NoteData note = activeChart.notes[i];
            if (note != null && note.time < firstNoteTime)
            {
                firstNoteTime = note.time;
            }
        }

        if (firstNoteTime == float.MaxValue)
        {
            chartLeadInPrepared = true;
            return;
        }

        float travelTime = noteSpawner != null
            ? noteSpawner.GetSpawnAheadTime()
            : activeChart.spawnAheadTime;
        musicLeadInDuration = Mathf.Max(0f, travelTime - firstNoteTime);
        if (musicLeadInDuration > 0f)
        {
            for (int i = 0; i < activeChart.notes.Count; i++)
            {
                if (activeChart.notes[i] != null)
                {
                    activeChart.notes[i].time += musicLeadInDuration;
                }
            }
        }

        chartData = activeChart;
        chartLeadInPrepared = true;
    }

    public float GetMusicLeadInRemaining()
    {
        if (!isSilentLeadIn)
        {
            return 0f;
        }

        return Mathf.Max(0f, musicLeadInDuration - musicLeadInElapsed);
    }

    private void StartAudibleMusic()
    {
        isSilentLeadIn = false;
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = gameplayMusicClip;
        musicSource.time = 0f;
        musicSource.volume = musicSourceVolume;
        musicSource.Play();
    }

    private void InitializeGravityHUD()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("找不到 Canvas，无法创建重力玩法 HUD");
            return;
        }

        JudgeLineController judgeLine = FindObjectOfType<JudgeLineController>();
        StabilitySystem stabilitySystem = FindObjectOfType<StabilitySystem>();

        GameObject hudObject = new GameObject("GravityHUD", typeof(RectTransform));
        hudObject.layer = canvas.gameObject.layer;
        hudObject.transform.SetParent(canvas.transform, false);
        hudObject.transform.SetAsFirstSibling();
        gravityHUD = hudObject.AddComponent<GravityHUD>();
        gravityHUD.Initialize(this, chartData, judgeLine, stabilitySystem);
        gravityHUD.SetVisible(false);
    }

    private void InitializeGravityTutorialUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("找不到 Canvas，无法创建重力教学界面");
            return;
        }

        gravityTutorialUI = GetComponent<GravityTutorialUI>();
        if (gravityTutorialUI == null)
        {
            gravityTutorialUI = gameObject.AddComponent<GravityTutorialUI>();
        }

        gravityTutorialUI.Initialize(canvas.transform);
    }

    private void InitializeGameplayBackdrop()
    {
        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            Debug.LogWarning("找不到 Main Camera，跳过游戏背景初始化");
            return;
        }

        if (gameplayCamera.GetComponent<GameplayBackdrop>() == null)
        {
            gameplayCamera.gameObject.AddComponent<GameplayBackdrop>();
        }
    }

    private void ApplyGameplayHudStyle()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == "ScoreText")
            {
                RhythmUITheme.StyleText(texts[i], 26f, RhythmUITheme.Text, true);
            }
            else if (texts[i].gameObject.name == "ComboText")
            {
                RhythmUITheme.StyleText(texts[i], 25f, RhythmUITheme.Gold, true);
            }
        }
    }

    private void SetGravityHUDVisible(bool visible)
    {
        if (gravityHUD != null)
        {
            gravityHUD.SetVisible(visible);
        }
    }

    private void EnsureResultPanelLayout()
    {
        if (resultPanel == null)
        {
            return;
        }

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(720f, 520f);
        }

        Image background = resultPanel.GetComponent<Image>();
        if (background == null)
        {
            background = resultPanel.AddComponent<Image>();
        }
        RhythmUITheme.StylePanel(background, RhythmUITheme.Panel);
        background.raycastTarget = true;

        Outline outline = resultPanel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = resultPanel.AddComponent<Outline>();
        }
        outline.effectColor = new Color32(48, 190, 213, 120);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        resultTitleText = FindResultText("ResultTitleText");
        resultScoreText = FindResultText("ResultScoreText");
        resultDetailsText = FindResultText("ResultMaxComboText");

        ConfigureResultText(resultTitleText, new Vector2(0f, 204f), new Vector2(640f, 66f), 46f, 34f, 50f);
        ConfigureResultText(resultScoreText, new Vector2(0f, 96f), new Vector2(620f, 126f), 28f, 22f, 32f);
        ConfigureResultText(resultDetailsText, new Vector2(0f, -35f), new Vector2(640f, 112f), 20f, 16f, 23f);
        RhythmUITheme.StyleText(resultTitleText, 42f, RhythmUITheme.Gold, true);
        RhythmUITheme.StyleText(resultScoreText, 29f, RhythmUITheme.Text, true);
        RhythmUITheme.StyleText(resultDetailsText, 19f, RhythmUITheme.MutedText);

        EnsureResultRetryButton();

        EnsureResultSongSelectButton();
    }

    private void EnsureResultRetryButton()
    {
        Transform retryTransform = resultPanel.transform.Find("RetryButton");
        if (retryTransform == null)
        {
            return;
        }

        TMP_Text retryLabel = retryTransform.GetComponent<TMP_Text>();
        Image retryImage = retryTransform.GetComponent<Image>();
        Button retryButton = retryTransform.GetComponent<Button>();

        if (retryImage == null && retryLabel != null)
        {
            int siblingIndex = retryTransform.GetSiblingIndex();
            retryTransform.gameObject.name = "RetryLabel";

            GameObject buttonObject = new GameObject(
                "RetryButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = resultPanel.layer;
            buttonObject.transform.SetParent(resultPanel.transform, false);
            buttonObject.transform.SetSiblingIndex(siblingIndex);

            retryTransform.SetParent(buttonObject.transform, false);
            retryTransform = buttonObject.transform;
            retryLabel = retryTransform.GetComponentInChildren<TMP_Text>(true);
            retryImage = buttonObject.GetComponent<Image>();
            retryButton = buttonObject.GetComponent<Button>();
        }
        else if (retryImage == null && retryTransform.GetComponent<Graphic>() == null)
        {
            retryImage = retryTransform.gameObject.AddComponent<Image>();
        }

        if (retryButton == null && retryImage != null)
        {
            retryButton = retryTransform.gameObject.AddComponent<Button>();
        }

        if (retryImage == null || retryButton == null)
        {
            Debug.LogWarning("重试按钮缺少可用的背景或 Button 组件，已跳过结算页重试按钮初始化");
            return;
        }

        RhythmUITheme.StyleButton(retryButton, RhythmUITheme.Primary, new Color32(4, 26, 39, 255));
        if (retryButton.onClick.GetPersistentEventCount() == 0)
        {
            retryButton.onClick.AddListener(RetryGame);
        }

        RectTransform retryRect = retryTransform.GetComponent<RectTransform>();
        ConfigureResultButton(retryRect, new Vector2(-132f, -205f), new Vector2(230f, 66f));

        if (retryLabel == null)
        {
            retryLabel = retryTransform.GetComponentInChildren<TMP_Text>(true);
        }
        if (retryLabel != null)
        {
            RectTransform labelRect = retryLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            retryLabel.text = "再玩一次";
            retryLabel.fontSize = 21f;
            retryLabel.alignment = TextAlignmentOptions.Center;
            retryLabel.color = new Color32(4, 26, 39, 255);
            retryLabel.raycastTarget = false;
        }
    }

    private TMP_Text FindResultText(string objectName)
    {
        TMP_Text[] texts = resultPanel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private void ConfigureResultText(
        TMP_Text text,
        Vector2 position,
        Vector2 size,
        float fontSize,
        float minFontSize,
        float maxFontSize)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontSizeMin = minFontSize;
        text.fontSizeMax = maxFontSize;
        text.enableAutoSizing = true;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
    }

    private void ConfigureResultButton(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void EnsureResultSongSelectButton()
    {
        Transform existing = resultPanel.transform.Find("ResultSongSelectButton");
        if (existing != null)
        {
            return;
        }

        GameObject buttonObject = new GameObject(
            "ResultSongSelectButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.layer = resultPanel.layer;
        buttonObject.transform.SetParent(resultPanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        ConfigureResultButton(buttonRect, new Vector2(132f, -205f), new Vector2(230f, 66f));

        Image buttonImage = buttonObject.GetComponent<Image>();
        Button button = buttonObject.GetComponent<Button>();
        RhythmUITheme.StyleButton(button, RhythmUITheme.PanelRaised, RhythmUITheme.Text);
        button.onClick.AddListener(ReturnToSongSelect);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        labelObject.layer = resultPanel.layer;
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        if (resultTitleText != null)
        {
            label.font = resultTitleText.font;
        }
        label.text = "返回选曲";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void UpdateResultDisplay(bool failed)
    {
        if (scoreManager == null)
        {
            if (resultTitleText != null) resultTitleText.text = failed ? "演奏失败" : "游戏结束";
            if (resultScoreText != null) resultScoreText.text = "本局数据暂不可用";
            if (resultDetailsText != null) resultDetailsText.text = "";
            return;
        }

        string songId = "unknown";
        string songTitle = "未知曲目";
        string difficulty = "未知难度";

        if (SelectedSongData.Instance != null)
        {
            if (SelectedSongData.Instance.selectedSong != null)
            {
                songId = SelectedSongData.Instance.selectedSong.songId;
                songTitle = SelectedSongData.Instance.selectedSong.title;
            }

            difficulty = SelectedSongData.Instance.selectedDifficulty;
        }
        else if (chartData != null)
        {
            songId = chartData.songId;
            songTitle = chartData.title;
            difficulty = chartData.difficulty;
        }

        scoreManager.FinalizeRun(songId, difficulty);

        string grade = scoreManager.GetGrade();
        if (resultTitleText != null)
        {
            resultTitleText.text = failed ? "演奏失败" : $"{grade} 评级";
            resultTitleText.color = failed
                ? new Color32(255, 111, 117, 255)
                : GetGradeColor(grade);
        }

        if (resultScoreText != null)
        {
            resultScoreText.text =
                $"{songTitle} · {difficulty}\n得分  {scoreManager.score:N0}    准确率  {scoreManager.GetAccuracy():F1}%";
        }

        if (resultDetailsText != null)
        {
            string recordLabel = scoreManager.isNewBestScore || scoreManager.isNewBestCombo
                ? " · 新纪录"
                : "";
            resultDetailsText.text =
                $"最高连击  {scoreManager.maxCombo}\n" +
                $"PERFECT {scoreManager.perfectCount}   GOOD {scoreManager.goodCount}   " +
                $"BAD {scoreManager.badCount}   MISS {scoreManager.missCount}\n" +
                $"重力配平 {scoreManager.counterweightCount} 次  ·  奖励 +{scoreManager.counterweightBonusScore:N0}\n" +
                $"本曲最佳  {scoreManager.bestScore:N0} / 连击 {scoreManager.bestCombo}{recordLabel}";
        }
    }

    private Color GetGradeColor(string grade)
    {
        switch (grade)
        {
            case "S": return new Color32(255, 220, 106, 255);
            case "A": return new Color32(88, 234, 204, 255);
            case "B": return new Color32(98, 184, 255, 255);
            case "C": return new Color32(191, 165, 255, 255);
            default: return new Color32(225, 235, 248, 255);
        }
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (currentState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = GameState.Paused;
        SetGravityHUDVisible(false);

        if (musicSource != null)
        {
            musicSource.Pause();
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Debug.Log("游戏暂停");
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
        {
            return;
        }

        if (isResumeCountingDown)
        {
            return;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (useResumeCountdown && resumeCountdownUI != null)
        {
            isResumeCountingDown = true;
            resumeCountdownUI.PlayCountdown(ResumeGameImmediately);
            return;
        }

        ResumeGameImmediately();
    }

    private void ResumeGameImmediately()
    {
        if (currentState != GameState.Paused)
        {
            return;
        }

        isResumeCountingDown = false;
        currentState = GameState.Playing;
        SetGravityHUDVisible(true);

        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }

        if (musicSource != null)
        {
            musicSource.UnPause();
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Debug.Log("继续游戏");
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToSongSelect()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        SceneManager.LoadScene(songSelectSceneName);
    }

    public void ReturnToMainMenu()
    {
        isResumeCountingDown = false;

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void EndGame()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = GameState.Result;
        SetGravityHUDVisible(false);

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        UpdateResultDisplay(false);

        Debug.Log("游戏结束");
    }

    public void FailGame()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = GameState.Failed;
        SetGravityHUDVisible(false);

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        UpdateResultDisplay(true);

        Debug.Log("游戏失败");
    }

    public float GetMusicTime()
    {
        if (isSilentLeadIn)
        {
            return musicLeadInElapsed;
        }

        return musicSource != null ? musicLeadInDuration + musicSource.time : 0f;
    }

    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }

    public bool IsPaused()
    {
        return currentState == GameState.Paused;
    }
}
