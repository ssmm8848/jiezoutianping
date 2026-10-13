using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Rhythm2DGameManager : MonoBehaviour
{
    private const float PerfectWindow = 0.05f;
    private const float GoodWindow = 0.10f;
    private const float BadWindow = 0.15f;
    private const string NoteSpeedKey = "GB_NoteSpeed";
    private const string InputOffsetKey = "GB_InputOffsetMs";
    private const float MinNoteSpeed = 4f;
    private const float MaxNoteSpeed = 24f;
    private static Sprite uiSprite;
    private static Sprite circleSprite;

    private static readonly KeyCode[] LaneKeys =
    {
        KeyCode.S,
        KeyCode.D,
        KeyCode.J,
        KeyCode.K
    };

    private sealed class NoteView
    {
        public NoteData data;
        public RectTransform rect;
        public float spawnTime;
        public float travelDuration;
    }

    private sealed class Sparkle
    {
        public RectTransform rect;
        public Image image;
        public Vector2 home;
        public float phase;
        public float speed;
        public float size;
    }

    private readonly List<NoteData> chartNotes = new List<NoteData>();
    private readonly List<NoteView> activeNotes = new List<NoteView>();
    private readonly List<Sparkle> backgroundSparkles = new List<Sparkle>();

    private Canvas gameplayCanvas;
    private RectTransform canvasRect;
    private RectTransform trackRect;
    private AnimatedBackdropRawImage animatedBackdrop;
    private Image judgeLineImage;
    private TMP_Text scoreText;
    private TMP_Text comboText;
    private TMP_Text judgementText;
    private TMP_Text songInfoText;
    private TMP_Text leadInText;
    private GameObject pausePanel;
    private GameObject resultPanel;
    private TMP_Text resultTitleText;
    private TMP_Text resultDetailsText;
    private TMP_Text pauseNoteSpeedLabel;
    private Slider pauseNoteSpeedSlider;

    private AudioSource musicSource;
    private ScoreManager scoreManager;
    private SongConfig selectedSong;
    private ChartJsonData chartData;

    private float trackWidth;
    private float trackHeight;
    private float spawnY;
    private float judgeY;
    private float laneWidth;
    private float fallDuration;
    private float noteSpeed;
    private float leadInDuration;
    private float leadInElapsed;
    private float currentSongTime;
    private float inputOffsetSeconds;
    private float judgementTimer;
    private float lineFlashTimer;
    private float backgroundElapsed;
    private Color lineBaseColor = new Color32(112, 230, 255, 255);
    private Color lineFlashColor = new Color32(255, 224, 113, 255);
    private int nextNoteIndex;
    private float lastNoteTime;
    private bool musicStarted;
    private bool isPaused;
    private bool isFinished;

    private void Awake()
    {
        EnsureMainCamera();
        EnsureEventSystem();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 1f;
    }

    private void Start()
    {
        BuildUserInterface();
        LoadSelectedChart();
        SetupScoreManager();
        SetupSelectedSong();

        if (chartData == null || selectedSong == null || selectedSong.audioClip == null)
        {
            ShowLoadError();
            return;
        }

        BeginRun();
    }

    private void Update()
    {
        if (isFinished)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        if (isPaused)
        {
            return;
        }

        if (chartData == null || selectedSong == null)
        {
            UpdateBackgroundSparkles();
            return;
        }

        UpdateSongClock();
        SpawnDueNotes();
        UpdateNotes();
        HandleLaneInput();
        UpdateJudgementFeedback();
        UpdateBackgroundSparkles();
        UpdateRunCompletion();
    }

    private void EnsureMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        if (mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color32(4, 12, 31, 255);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );
    }

    private void BuildUserInterface()
    {
        GameObject canvasObject = new GameObject(
            "Vertical 2D Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.layer = 5;
        gameplayCanvas = canvasObject.GetComponent<Canvas>();
        gameplayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameplayCanvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();
        CreateBackground(canvasRect);
        Canvas.ForceUpdateCanvases();

        trackWidth = Mathf.Min(610f, canvasRect.rect.width * 0.54f);
        trackHeight = Mathf.Min(930f, canvasRect.rect.height * 0.92f);
        laneWidth = trackWidth / LaneKeys.Length;
        spawnY = trackHeight * 0.5f - 125f;
        judgeY = -trackHeight * 0.5f + trackHeight * 0.22f;

        CreateBackgroundSparkles();
        CreateTrack();
        CreateHud();
        CreatePausePanel();
        CreateResultPanel();
    }

    private void CreateBackground(RectTransform parent)
    {
        GameObject backgroundObject = CreateUiObject("Stage Background", parent, typeof(AnimatedBackdropRawImage));
        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        Stretch(rect);

        animatedBackdrop = backgroundObject.GetComponent<AnimatedBackdropRawImage>();
        RawImage background = animatedBackdrop;
        background.texture = Resources.Load<Texture2D>("Vertical2DStage");
        background.color = Color.white;
        background.raycastTarget = false;

        GameObject shadeObject = CreateUiObject("Stage Shade", parent, typeof(Image));
        RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
        Stretch(shadeRect);
        Image shade = shadeObject.GetComponent<Image>();
        shade.color = new Color32(2, 9, 25, 42);
        shade.raycastTarget = false;
    }

    private void CreateTrack()
    {
        trackRect = CreateRect("Vertical Track", canvasRect);
        SetCenteredRect(trackRect, new Vector2(trackWidth, trackHeight), Vector2.zero);

        Image trackImage = trackRect.gameObject.AddComponent<Image>();
        trackImage.sprite = GetUiSprite();
        trackImage.type = Image.Type.Sliced;
        trackImage.color = new Color32(5, 13, 29, 235);
        trackImage.raycastTarget = false;

        Outline trackOutline = trackRect.gameObject.AddComponent<Outline>();
        trackOutline.effectColor = new Color32(255, 204, 113, 210);
        trackOutline.effectDistance = new Vector2(3f, -3f);
        trackOutline.useGraphicAlpha = true;

        for (int lane = 0; lane < LaneKeys.Length; lane++)
        {
            RectTransform laneRect = CreateRect("Lane " + (lane + 1), trackRect);
            laneRect.anchorMin = new Vector2(lane / (float)LaneKeys.Length, 0f);
            laneRect.anchorMax = new Vector2((lane + 1) / (float)LaneKeys.Length, 1f);
            laneRect.offsetMin = Vector2.zero;
            laneRect.offsetMax = Vector2.zero;

            Image laneImage = laneRect.gameObject.AddComponent<Image>();
            laneImage.sprite = GetUiSprite();
            laneImage.type = Image.Type.Sliced;
            laneImage.color = lane % 2 == 0
                ? new Color32(34, 77, 106, 52)
                : new Color32(78, 74, 119, 48);
            laneImage.raycastTarget = false;

            if (lane > 0)
            {
                RectTransform separator = CreateRect("Lane Separator " + lane, trackRect);
                separator.anchorMin = new Vector2(lane / (float)LaneKeys.Length, 0f);
                separator.anchorMax = new Vector2(lane / (float)LaneKeys.Length, 1f);
                separator.sizeDelta = new Vector2(2f, 0f);
                separator.anchoredPosition = Vector2.zero;
                Image separatorImage = separator.gameObject.AddComponent<Image>();
                separatorImage.color = new Color32(134, 204, 231, 76);
                separatorImage.raycastTarget = false;
            }
        }

        RectTransform topTrim = CreateRect("Top Track Trim", trackRect);
        SetCenteredRect(topTrim, new Vector2(trackWidth - 18f, 3f), new Vector2(0f, trackHeight * 0.5f - 8f));
        Image topTrimImage = topTrim.gameObject.AddComponent<Image>();
        topTrimImage.color = new Color32(255, 219, 130, 230);
        topTrimImage.raycastTarget = false;

        RectTransform judgeRect = CreateRect("Judgement Line", trackRect);
        SetCenteredRect(judgeRect, new Vector2(trackWidth - 14f, 12f), new Vector2(0f, judgeY));
        judgeLineImage = judgeRect.gameObject.AddComponent<Image>();
        judgeLineImage.sprite = GetUiSprite();
        judgeLineImage.type = Image.Type.Sliced;
        judgeLineImage.color = lineBaseColor;
        judgeLineImage.raycastTarget = false;

        Outline judgeOutline = judgeRect.gameObject.AddComponent<Outline>();
        judgeOutline.effectColor = new Color32(76, 223, 255, 180);
        judgeOutline.effectDistance = new Vector2(0f, -5f);

        for (int lane = 0; lane < LaneKeys.Length; lane++)
        {
            float x = -trackWidth * 0.5f + laneWidth * (lane + 0.5f);
            TMP_Text keyLabel = CreateText("Lane Key " + lane, trackRect, new Vector2(x, -trackHeight * 0.5f + 30f), new Vector2(laneWidth, 34f), 22f);
            keyLabel.text = LaneKeys[lane].ToString();
            keyLabel.fontStyle = FontStyles.Bold;
            keyLabel.color = new Color32(224, 242, 255, 230);

            RectTransform laneMarker = CreateRect("Lane Marker " + lane, trackRect);
            SetCenteredRect(laneMarker, new Vector2(9f, 9f), new Vector2(x, -trackHeight * 0.5f + 58f));
            Image marker = laneMarker.gameObject.AddComponent<Image>();
            marker.sprite = GetCircleSprite();
            marker.color = GetLaneColor(lane);
            marker.raycastTarget = false;
        }
    }

    private void CreateHud()
    {
        songInfoText = CreateText("Song Info", canvasRect, Vector2.zero, new Vector2(500f, 70f), 26f);
        songInfoText.rectTransform.anchorMin = new Vector2(0f, 1f);
        songInfoText.rectTransform.anchorMax = new Vector2(0f, 1f);
        songInfoText.rectTransform.pivot = new Vector2(0f, 1f);
        songInfoText.rectTransform.anchoredPosition = new Vector2(42f, -42f);
        songInfoText.alignment = TextAlignmentOptions.TopLeft;
        RhythmUITheme.StyleText(songInfoText, 23f, RhythmUITheme.Text, true);

        scoreText = CreateText("Score", trackRect, new Vector2(-trackWidth * 0.5f + 22f, trackHeight * 0.5f - 45f), new Vector2(210f, 38f), 21f);
        scoreText.rectTransform.pivot = new Vector2(0f, 0.5f);
        scoreText.alignment = TextAlignmentOptions.Left;
        scoreText.text = "Score: 0";
        RhythmUITheme.StyleText(scoreText, 23f, RhythmUITheme.Text, true);

        comboText = CreateText("Combo", trackRect, new Vector2(trackWidth * 0.5f - 22f, trackHeight * 0.5f - 45f), new Vector2(210f, 38f), 21f);
        comboText.rectTransform.pivot = new Vector2(1f, 0.5f);
        comboText.alignment = TextAlignmentOptions.Right;
        comboText.text = "";
        RhythmUITheme.StyleText(comboText, 23f, RhythmUITheme.Gold, true);

        judgementText = CreateText("Judgement", trackRect, new Vector2(0f, judgeY + 64f), new Vector2(trackWidth - 80f, 58f), 36f);
        judgementText.fontStyle = FontStyles.Bold;
        judgementText.alignment = TextAlignmentOptions.Center;
        judgementText.text = "";
        judgementText.color = Color.white;

        leadInText = CreateText("Lead In", trackRect, new Vector2(0f, trackHeight * 0.5f - 93f), new Vector2(150f, 42f), 32f);
        leadInText.fontStyle = FontStyles.Bold;
        leadInText.alignment = TextAlignmentOptions.Center;
        RhythmUITheme.StyleText(leadInText, 36f, RhythmUITheme.Gold, true);
        leadInText.text = "";

        Button pauseButton = CreateButton("Pause Button", canvasRect, new Vector2(-42f, -44f), new Vector2(104f, 48f), "暂停", new Color32(9, 35, 57, 220), 20f, PauseGame);
        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = new Vector2(1f, 1f);
        pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(1f, 1f);
    }

    private void CreateBackgroundSparkles()
    {
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        for (int i = 0; i < 28; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float horizontalRange = UnityEngine.Random.Range(width * 0.23f, width * 0.47f);
            float x = side * horizontalRange;
            float y = UnityEngine.Random.Range(-height * 0.43f, height * 0.43f);
            float size = UnityEngine.Random.Range(4f, 11f);

            RectTransform rect = CreateRect("Stage Sparkle " + i, canvasRect);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(x, y);
            rect.localRotation = Quaternion.Euler(0f, 0f, i % 3 == 0 ? 45f : 0f);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = i % 3 == 0
                ? new Color32(255, 226, 132, 150)
                : new Color32(162, 225, 255, 140);
            image.raycastTarget = false;

            backgroundSparkles.Add(new Sparkle
            {
                rect = rect,
                image = image,
                home = new Vector2(x, y),
                phase = i * 0.71f,
                speed = 0.45f + (i % 5) * 0.17f,
                size = size
            });
        }
    }

    private void CreatePausePanel()
    {
        pausePanel = CreateOverlay("Pause Overlay", out RectTransform cardRect);
        SetCenteredRect(cardRect, new Vector2(680f, 640f), Vector2.zero);
        SetPanelImage(cardRect.gameObject, new Color32(6, 18, 39, 248), new Color32(76, 203, 237, 220));

        TMP_Text title = CreateText("Pause Title", cardRect, new Vector2(0f, 267f), new Vector2(560f, 64f), 42f);
        title.text = "暂停演奏";
        title.alignment = TextAlignmentOptions.Center;
        RhythmUITheme.StyleText(title, 40f, RhythmUITheme.Text, true);

        pauseNoteSpeedLabel = CreateText("Pause Note Speed Label", cardRect, new Vector2(0f, 204f), new Vector2(500f, 34f), 20f);
        pauseNoteSpeedLabel.alignment = TextAlignmentOptions.Center;
        pauseNoteSpeedLabel.color = new Color32(196, 224, 242, 255);
        pauseNoteSpeedSlider = CreatePauseNoteSpeedSlider(cardRect);
        pauseNoteSpeedSlider.onValueChanged.AddListener(OnPauseNoteSpeedChanged);

        CreateButton("Resume Button", cardRect, new Vector2(0f, 95f), new Vector2(390f, 62f), "继续", new Color32(17, 147, 174, 255), 25f, ResumeGame);
        CreateButton("Retry Button", cardRect, new Vector2(0f, 14f), new Vector2(390f, 62f), "重新开始", new Color32(35, 69, 105, 255), 25f, RetryGame);
        CreateButton("Pause Select Button", cardRect, new Vector2(0f, -67f), new Vector2(390f, 62f), "返回选曲", new Color32(85, 52, 83, 255), 25f, ReturnToSongSelect);
        CreateButton("Pause Menu Button", cardRect, new Vector2(0f, -148f), new Vector2(390f, 62f), "返回主菜单", new Color32(84, 41, 51, 255), 25f, ReturnToMainMenu);

        pausePanel.SetActive(false);
    }

    private Slider CreatePauseNoteSpeedSlider(Transform parent)
    {
        GameObject sliderObject = CreateUiObject("Pause Note Speed Slider", parent, typeof(Image), typeof(Slider));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        SetCenteredRect(sliderRect, new Vector2(440f, 28f), new Vector2(0f, 165f));

        Image trackImage = sliderObject.GetComponent<Image>();
        trackImage.color = new Color32(31, 47, 70, 255);
        trackImage.sprite = RhythmUITheme.RoundedSprite;
        trackImage.type = Image.Type.Sliced;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = MinNoteSpeed;
        slider.maxValue = MaxNoteSpeed;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform fillArea = CreateRect("Fill Area", sliderRect);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(12f, -4f);
        fillArea.offsetMax = new Vector2(-12f, 4f);

        RectTransform fillRect = CreateRect("Fill", fillArea);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.sprite = RhythmUITheme.RoundedSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = new Color32(0, 205, 228, 255);

        RectTransform handleArea = CreateRect("Handle Slide Area", sliderRect);
        handleArea.anchorMin = new Vector2(0f, 0.5f);
        handleArea.anchorMax = new Vector2(1f, 0.5f);
        handleArea.offsetMin = new Vector2(12f, 0f);
        handleArea.offsetMax = new Vector2(-12f, 0f);

        RectTransform handleRect = CreateRect("Handle", handleArea);
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.sizeDelta = new Vector2(22f, 30f);
        Image handleImage = handleRect.gameObject.AddComponent<Image>();
        handleImage.sprite = RhythmUITheme.RoundedSprite;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color32(236, 249, 255, 255);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.SetValueWithoutNotify(noteSpeed);
        UpdatePauseNoteSpeedLabel(noteSpeed);
        return slider;
    }

    private void OnPauseNoteSpeedChanged(float value)
    {
        noteSpeed = Mathf.Clamp(value, MinNoteSpeed, MaxNoteSpeed);
        float chartTravelTime = chartData != null && chartData.spawnAheadTime > 0f
            ? chartData.spawnAheadTime
            : 2.5f;
        fallDuration = Mathf.Clamp(chartTravelTime * 8f / noteSpeed, 0.65f, 4f);
        if (!musicStarted)
        {
            float firstNoteTime = chartNotes.Count > 0 ? chartNotes[0].time : 0f;
            leadInDuration = Mathf.Max(0f, fallDuration - firstNoteTime);
            currentSongTime = leadInElapsed - leadInDuration;
        }
        PlayerPrefs.SetFloat(NoteSpeedKey, noteSpeed);
        PlayerPrefs.Save();
        UpdatePauseNoteSpeedLabel(noteSpeed);
    }

    private void UpdatePauseNoteSpeedLabel(float value)
    {
        if (pauseNoteSpeedLabel != null)
        {
            pauseNoteSpeedLabel.text = $"音符流速  {value:F1}  ·  越大越快";
        }
    }

    private void CreateResultPanel()
    {
        resultPanel = CreateOverlay("Result Overlay", out RectTransform cardRect);
        SetCenteredRect(cardRect, new Vector2(780f, 600f), Vector2.zero);
        SetPanelImage(cardRect.gameObject, new Color32(6, 18, 39, 250), new Color32(255, 209, 119, 220));

        resultTitleText = CreateText("Result Title", cardRect, new Vector2(0f, 228f), new Vector2(680f, 70f), 48f);
        resultTitleText.alignment = TextAlignmentOptions.Center;
        RhythmUITheme.StyleText(resultTitleText, 46f, RhythmUITheme.Gold, true);

        resultDetailsText = CreateText("Result Details", cardRect, new Vector2(0f, 70f), new Vector2(690f, 250f), 25f);
        resultDetailsText.alignment = TextAlignmentOptions.Center;
        RhythmUITheme.StyleText(resultDetailsText, 24f, RhythmUITheme.Text);

        CreateButton("Result Retry Button", cardRect, new Vector2(-165f, -225f), new Vector2(250f, 68f), "再玩一次", new Color32(17, 147, 174, 255), 24f, RetryGame);
        CreateButton("Result Select Button", cardRect, new Vector2(165f, -225f), new Vector2(250f, 68f), "返回选曲", new Color32(35, 69, 105, 255), 24f, ReturnToSongSelect);

        resultPanel.SetActive(false);
    }

    private GameObject CreateOverlay(string objectName, out RectTransform cardRect)
    {
        GameObject overlay = CreateUiObject(objectName, canvasRect, typeof(Image));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect);
        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color32(1, 6, 17, 205);
        overlayImage.raycastTarget = true;

        cardRect = CreateRect(objectName + " Card", overlayRect);
        SetCenteredRect(cardRect, new Vector2(650f, 520f), Vector2.zero);
        return overlay;
    }

    private void LoadSelectedChart()
    {
        if (SelectedSongData.Instance == null || SelectedSongData.Instance.selectedChart == null)
        {
            return;
        }

        chartData = JsonUtility.FromJson<ChartJsonData>(SelectedSongData.Instance.selectedChart.text);
        if (chartData == null)
        {
            return;
        }

        if (chartData.notes == null)
        {
            chartData.notes = new List<NoteData>();
        }

        chartData.notes.RemoveAll(note => note == null || note.lane < 0 || note.lane >= LaneKeys.Length);
        chartData.notes.Sort((first, second) => first.time.CompareTo(second.time));
        chartNotes.AddRange(chartData.notes);
        lastNoteTime = chartNotes.Count > 0 ? chartNotes[chartNotes.Count - 1].time : 0f;
    }

    private void SetupSelectedSong()
    {
        if (SelectedSongData.Instance == null)
        {
            return;
        }

        selectedSong = SelectedSongData.Instance.selectedSong;
        if (selectedSong != null && selectedSong.audioClip != null)
        {
            musicSource.clip = selectedSong.audioClip;
        }

        if (songInfoText != null)
        {
            string title = selectedSong != null ? selectedSong.title : "未知曲目";
            string difficulty = SelectedSongData.Instance.selectedDifficulty;
            int level = chartData != null ? chartData.level : 0;
            songInfoText.text = $"{title}   ·   {difficulty} Lv.{level}\n垂直 2D 轨道";
        }
    }

    private void SetupScoreManager()
    {
        scoreManager = GetComponent<ScoreManager>();
        if (scoreManager == null)
        {
            scoreManager = gameObject.AddComponent<ScoreManager>();
        }

        scoreManager.scoreText = scoreText;
        scoreManager.comboText = comboText;
        scoreManager.ResetScore();
    }

    private void BeginRun()
    {
        noteSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(NoteSpeedKey, 8f), MinNoteSpeed, MaxNoteSpeed);
        float chartTravelTime = chartData.spawnAheadTime > 0f ? chartData.spawnAheadTime : 2.5f;
        fallDuration = Mathf.Clamp(chartTravelTime * 8f / noteSpeed, 0.65f, 4f);
        if (pauseNoteSpeedSlider != null)
        {
            pauseNoteSpeedSlider.SetValueWithoutNotify(noteSpeed);
            UpdatePauseNoteSpeedLabel(noteSpeed);
        }
        inputOffsetSeconds = PlayerPrefs.GetFloat(InputOffsetKey, 0f) / 1000f;

        float firstNoteTime = chartNotes.Count > 0 ? chartNotes[0].time : 0f;
        leadInDuration = Mathf.Max(0f, fallDuration - firstNoteTime);
        leadInElapsed = 0f;
        currentSongTime = -leadInDuration;
        musicStarted = false;
        nextNoteIndex = 0;
        isPaused = false;
        isFinished = false;

        if (leadInDuration <= 0f)
        {
            StartMusic();
        }
    }

    private void UpdateSongClock()
    {
        if (!musicStarted)
        {
            leadInElapsed += Time.unscaledDeltaTime;
            currentSongTime = leadInElapsed - leadInDuration;

            if (leadInText != null)
            {
                float remaining = Mathf.Max(0f, leadInDuration - leadInElapsed);
                leadInText.text = remaining > 0.75f ? Mathf.CeilToInt(remaining).ToString() : "GO";
                leadInText.enabled = leadInDuration > 0.25f;
            }

            if (leadInElapsed >= leadInDuration)
            {
                StartMusic();
                currentSongTime = 0f;
            }

            return;
        }

        if (leadInText != null)
        {
            leadInText.enabled = false;
        }

        currentSongTime = musicSource != null ? musicSource.time : currentSongTime + Time.unscaledDeltaTime;
        if (musicSource != null && !musicSource.isPlaying && musicSource.clip != null &&
            musicSource.time >= musicSource.clip.length - 0.08f)
        {
            currentSongTime = Mathf.Max(currentSongTime, lastNoteTime + BadWindow + 0.05f);
        }
    }

    private void StartMusic()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            return;
        }

        musicStarted = true;
        musicSource.time = 0f;
        musicSource.Play();
    }

    private void SpawnDueNotes()
    {
        while (nextNoteIndex < chartNotes.Count &&
               currentSongTime >= chartNotes[nextNoteIndex].time - fallDuration)
        {
            SpawnNote(chartNotes[nextNoteIndex]);
            nextNoteIndex++;
        }
    }

    private void SpawnNote(NoteData data)
    {
        int lane = data.lane;
        float noteHeight = GetNoteHeight(data.GetWeightType());
        float noteWidth = laneWidth * 0.78f;
        float centerX = -trackWidth * 0.5f + laneWidth * (lane + 0.5f);

        RectTransform noteRect = CreateRect("Note " + lane + " @ " + data.time.ToString("F2"), trackRect);
        SetCenteredRect(noteRect, new Vector2(noteWidth, noteHeight), new Vector2(centerX, spawnY));

        Image noteImage = noteRect.gameObject.AddComponent<Image>();
        noteImage.sprite = GetUiSprite();
        noteImage.type = Image.Type.Sliced;
        noteImage.color = GetWeightColor(data.GetWeightType());
        noteImage.raycastTarget = false;

        Outline outline = noteRect.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.Lerp(noteImage.color, Color.white, 0.28f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        RectTransform glossRect = CreateRect("Note Gloss", noteRect);
        glossRect.anchorMin = new Vector2(0.08f, 0.52f);
        glossRect.anchorMax = new Vector2(0.92f, 0.78f);
        glossRect.offsetMin = Vector2.zero;
        glossRect.offsetMax = Vector2.zero;
        Image glossImage = glossRect.gameObject.AddComponent<Image>();
        glossImage.sprite = GetUiSprite();
        glossImage.type = Image.Type.Sliced;
        glossImage.color = new Color(1f, 1f, 1f, 0.30f);
        glossImage.raycastTarget = false;

        activeNotes.Add(new NoteView
        {
            data = data,
            rect = noteRect,
            spawnTime = data.time - fallDuration,
            travelDuration = fallDuration
        });
    }

    private void UpdateNotes()
    {
        float judgementTime = currentSongTime + inputOffsetSeconds;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteView note = activeNotes[i];
            float progress = Mathf.Clamp01((currentSongTime - note.spawnTime) / note.travelDuration);
            Vector2 position = note.rect.anchoredPosition;
            position.y = Mathf.Lerp(spawnY, judgeY, progress);
            note.rect.anchoredPosition = position;

            float proximity = 1f - Mathf.Clamp01(Mathf.Abs(judgementTime - note.data.time) / BadWindow);
            float scale = 1f + proximity * 0.07f;
            note.rect.localScale = new Vector3(scale, scale, 1f);

            if (judgementTime > note.data.time + BadWindow)
            {
                ApplyJudgement(JudgeResult.Miss);
                RemoveNoteAt(i);
            }
        }
    }

    private void HandleLaneInput()
    {
        for (int lane = 0; lane < LaneKeys.Length; lane++)
        {
            if (Input.GetKeyDown(LaneKeys[lane]))
            {
                TryHitLane(lane);
            }
        }
    }

    private void TryHitLane(int lane)
    {
        float judgementTime = currentSongTime + inputOffsetSeconds;
        int nearestIndex = -1;
        float nearestDifference = float.MaxValue;

        for (int i = 0; i < activeNotes.Count; i++)
        {
            NoteView note = activeNotes[i];
            if (note.data.lane != lane)
            {
                continue;
            }

            float difference = Mathf.Abs(judgementTime - note.data.time);
            if (difference <= BadWindow && difference < nearestDifference)
            {
                nearestDifference = difference;
                nearestIndex = i;
            }
        }

        if (nearestIndex < 0)
        {
            return;
        }

        NoteData hitNote = activeNotes[nearestIndex].data;
        JudgeResult result = nearestDifference <= PerfectWindow
            ? JudgeResult.Perfect
            : nearestDifference <= GoodWindow
                ? JudgeResult.Good
                : JudgeResult.Bad;

        ApplyJudgement(result);
        lineFlashTimer = 0.12f;
        RemoveNoteAt(nearestIndex);
    }

    private void ApplyJudgement(JudgeResult result)
    {
        if (scoreManager != null)
        {
            scoreManager.ApplyJudgeResult(result);
        }

        if (judgementText == null)
        {
            return;
        }

        judgementText.text = result.ToString().ToUpperInvariant();
        judgementText.color = result == JudgeResult.Perfect
            ? new Color32(111, 237, 255, 255)
            : result == JudgeResult.Good
                ? new Color32(155, 242, 198, 255)
                : result == JudgeResult.Bad
                    ? new Color32(255, 198, 119, 255)
                    : new Color32(255, 103, 130, 255);

        judgementText.rectTransform.localScale = Vector3.one * 1.08f;
        judgementTimer = 0.34f;
    }

    private void UpdateJudgementFeedback()
    {
        if (judgementTimer > 0f)
        {
            judgementTimer -= Time.unscaledDeltaTime;
            if (judgementText != null)
            {
                float alpha = Mathf.Clamp01(judgementTimer / 0.34f);
                Color color = judgementText.color;
                color.a = alpha;
                judgementText.color = color;
                judgementText.rectTransform.localScale = Vector3.Lerp(
                    judgementText.rectTransform.localScale,
                    Vector3.one,
                    Time.unscaledDeltaTime * 9f
                );
            }
        }
        else if (judgementText != null)
        {
            judgementText.text = "";
        }

        if (judgeLineImage != null)
        {
            if (lineFlashTimer > 0f)
            {
                lineFlashTimer -= Time.unscaledDeltaTime;
                judgeLineImage.color = Color.Lerp(lineBaseColor, lineFlashColor, lineFlashTimer / 0.12f);
            }
            else
            {
                judgeLineImage.color = lineBaseColor;
            }
        }
    }

    private void UpdateBackgroundSparkles()
    {
        backgroundElapsed += Time.unscaledDeltaTime;
        float time = backgroundElapsed;
        if (animatedBackdrop != null)
        {
            animatedBackdrop.SetAnimationTime(time);
        }

        for (int i = 0; i < backgroundSparkles.Count; i++)
        {
            Sparkle sparkle = backgroundSparkles[i];
            float wave = Mathf.Sin(time * sparkle.speed + sparkle.phase);
            sparkle.rect.anchoredPosition = sparkle.home + new Vector2(
                Mathf.Cos(time * sparkle.speed * 0.63f + sparkle.phase) * 4f,
                wave * 12f
            );

            float brightness = 0.22f + (wave + 1f) * 0.15f;
            Color color = sparkle.image.color;
            color.a = brightness;
            sparkle.image.color = color;
            float scale = 0.82f + (wave + 1f) * 0.18f;
            sparkle.rect.localScale = Vector3.one * scale;
        }
    }

    private void UpdateRunCompletion()
    {
        if (nextNoteIndex >= chartNotes.Count && activeNotes.Count == 0 &&
            currentSongTime > lastNoteTime + BadWindow + 0.25f)
        {
            FinishRun();
        }
    }

    private void FinishRun()
    {
        if (isFinished)
        {
            return;
        }

        isFinished = true;
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        if (scoreManager != null)
        {
            string songId = selectedSong != null ? selectedSong.songId : chartData.songId;
            string difficulty = SelectedSongData.Instance != null
                ? SelectedSongData.Instance.selectedDifficulty
                : chartData.difficulty;
            scoreManager.FinalizeRun(songId + "_Vertical2D", difficulty);
        }

        UpdateResultPanel();
        resultPanel.SetActive(true);
    }

    private void UpdateResultPanel()
    {
        if (scoreManager == null)
        {
            resultTitleText.text = "演奏结束";
            resultDetailsText.text = "本局数据暂不可用";
            return;
        }

        string grade = scoreManager.GetGrade();
        resultTitleText.text = grade + " 评级";
        resultTitleText.color = grade == "S"
            ? new Color32(255, 224, 126, 255)
            : grade == "A"
                ? new Color32(99, 230, 203, 255)
                : new Color32(172, 205, 255, 255);

        resultDetailsText.text =
            $"{selectedSong.title} · {SelectedSongData.Instance.selectedDifficulty}\n" +
            $"得分  {scoreManager.score:N0}     准确率  {scoreManager.GetAccuracy():F1}%\n\n" +
            $"最高连击  {scoreManager.maxCombo}\n" +
            $"PERFECT {scoreManager.perfectCount}    GOOD {scoreManager.goodCount}    " +
            $"BAD {scoreManager.badCount}    MISS {scoreManager.missCount}\n\n" +
            $"本曲最佳  {scoreManager.bestScore:N0} / 连击 {scoreManager.bestCombo}";
    }

    private void PauseGame()
    {
        if (isPaused || isFinished)
        {
            return;
        }

        isPaused = true;
        if (musicSource != null && musicStarted)
        {
            musicSource.Pause();
        }

        if (pauseNoteSpeedSlider != null)
        {
            pauseNoteSpeedSlider.SetValueWithoutNotify(noteSpeed);
            UpdatePauseNoteSpeedLabel(noteSpeed);
        }
        pausePanel.SetActive(true);
    }

    private void ResumeGame()
    {
        if (!isPaused || isFinished)
        {
            return;
        }

        isPaused = false;
        pausePanel.SetActive(false);
        if (musicSource != null && musicStarted)
        {
            musicSource.UnPause();
        }
    }

    private void RetryGame()
    {
        StopMusic();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToSongSelect()
    {
        StopMusic();
        SceneManager.LoadScene("SongSelectScene");
    }

    private void ReturnToMainMenu()
    {
        StopMusic();
        SceneManager.LoadScene("MainMenuScene");
    }

    private void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private void RemoveNoteAt(int index)
    {
        NoteView note = activeNotes[index];
        activeNotes.RemoveAt(index);
        if (note.rect != null)
        {
            Destroy(note.rect.gameObject);
        }
    }

    private void ShowLoadError()
    {
        if (songInfoText != null)
        {
            songInfoText.text = "无法读取所选歌曲或谱面";
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = "谱面载入失败";
            resultTitleText.color = new Color32(255, 130, 145, 255);
        }

        if (resultDetailsText != null)
        {
            resultDetailsText.text = "请返回选曲页重新选择歌曲和难度。";
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        isFinished = true;
    }

    private TMP_Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = CreateUiObject(objectName, parent, typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(14f, fontSize - 6f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        string label,
        Color color,
        float fontSize,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = GetUiSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        RhythmUITheme.StyleButton(button, color, Color.white);
        button.onClick.AddListener(onClick);

        TMP_Text buttonText = CreateText("Label", buttonObject.transform, Vector2.zero, size - new Vector2(18f, 8f), fontSize);
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = new Vector2(8f, 4f);
        buttonText.rectTransform.offsetMax = new Vector2(-8f, -4f);
        buttonText.text = label;
        buttonText.color = Color.white;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.raycastTarget = false;
        return button;
    }

    private void SetPanelImage(GameObject panelObject, Color color, Color outlineColor)
    {
        Image image = panelObject.GetComponent<Image>();
        if (image == null)
        {
            image = panelObject.AddComponent<Image>();
        }
        RhythmUITheme.StylePanel(image, color);
        image.raycastTarget = true;

        Outline outline = panelObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panelObject.AddComponent<Outline>();
        }
        outline.effectColor = Color.Lerp(outlineColor, RhythmUITheme.Primary, 0.28f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private static GameObject CreateUiObject(string objectName, Transform parent, params System.Type[] components)
    {
        List<System.Type> componentTypes = new List<System.Type> { typeof(RectTransform), typeof(CanvasRenderer) };
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != typeof(RectTransform) && components[i] != typeof(CanvasRenderer))
            {
                componentTypes.Add(components[i]);
            }
        }

        GameObject uiObject = new GameObject(objectName, componentTypes.ToArray());
        uiObject.layer = 5;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        return CreateUiObject(objectName, parent).GetComponent<RectTransform>();
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite GetUiSprite()
    {
        if (uiSprite != null)
        {
            return uiSprite;
        }

        uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (uiSprite != null)
        {
            return uiSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Vertical Rhythm UI Fallback",
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        uiSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
        uiSprite.hideFlags = HideFlags.HideAndDontSave;
        return uiSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Vertical Rhythm Sparkle Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(radius + 1f - distance));
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        circleSprite.name = "Vertical Rhythm Sparkle";
        circleSprite.hideFlags = HideFlags.HideAndDontSave;
        return circleSprite;
    }

    private static Color GetLaneColor(int lane)
    {
        Color[] colors =
        {
            new Color32(83, 216, 255, 255),
            new Color32(255, 181, 102, 255),
            new Color32(190, 153, 255, 255),
            new Color32(106, 236, 195, 255)
        };
        return colors[lane % colors.Length];
    }

    private static Color GetWeightColor(WeightType weightType)
    {
        switch (weightType)
        {
            case WeightType.Heavy:
                return new Color32(255, 87, 130, 255);
            case WeightType.Light:
                return new Color32(255, 209, 91, 255);
            case WeightType.Stabilize:
                return new Color32(77, 234, 193, 255);
            default:
                return new Color32(62, 203, 255, 255);
        }
    }

    private static float GetNoteHeight(WeightType weightType)
    {
        switch (weightType)
        {
            case WeightType.Heavy: return 38f;
            case WeightType.Light: return 24f;
            case WeightType.Stabilize: return 31f;
            default: return 30f;
        }
    }
}
