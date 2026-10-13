using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime HUD for reading the judge-line field, planning upcoming weight notes,
/// and tracking the calibration barrier.
/// </summary>
public class GravityHUD : MonoBehaviour
{
    private const float RefreshInterval = 0.08f;
    private static readonly string[] LaneKeys = { "S", "D", "J", "K" };

    private GameManager gameManager;
    private ChartData chartData;
    private JudgeLineController judgeLineController;
    private StabilitySystem stabilitySystem;

    private TMP_FontAsset fontAsset;
    private TMP_Text stateText;
    private TMP_Text counterweightText;
    private TMP_Text offsetText;
    private TMP_Text energyText;
    private TMP_Text barrierText;
    private readonly TMP_Text[] forecastTexts = new TMP_Text[4];

    private RectTransform gaugeSafeZone;
    private RectTransform gaugeMarker;
    private RectTransform stabilityFill;
    private RectTransform energyFill;
    private RectTransform rootRect;
    private float refreshTimer;
    private int lastScreenWidth;
    private int lastScreenHeight;

    public void Initialize(
        GameManager owner,
        ChartData chart,
        JudgeLineController judgeLine,
        StabilitySystem stability)
    {
        gameManager = owner;
        chartData = chart;
        judgeLineController = judgeLine;
        stabilitySystem = stability;

        TMP_Text[] existingTexts = FindObjectsOfType<TMP_Text>();
        if (existingTexts.Length > 0)
        {
            fontAsset = existingTexts[0].font;
        }

        if (fontAsset == null && TMP_Settings.defaultFontAsset != null)
        {
            fontAsset = TMP_Settings.defaultFontAsset;
        }

        BuildUI();
        RefreshHUD();
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }
    }

    private void Update()
    {
        if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
        {
            UpdateResponsiveScale();
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = RefreshInterval;
        RefreshHUD();
    }

    private void BuildUI()
    {
        rootRect = transform as RectTransform;
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(0f, 170f);

        Image panel = CreateImage("Panel", rootRect, new Vector2(0f, -12f), new Vector2(760f, 152f));
        RhythmUITheme.StylePanel(panel, new Color32(7, 15, 31, 232));

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(42, 164, 196, 110);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);

        CreateText("Heading", panelRect, new Vector2(-270f, 53f), new Vector2(220f, 22f), 15f,
            TextAlignmentOptions.Left, "重力场  GRAVITY FIELD", new Color32(170, 222, 240, 255));
        counterweightText = CreateText("CounterweightStatus", panelRect, new Vector2(0f, 53f), new Vector2(270f, 22f), 12f,
            TextAlignmentOptions.Center, "重 H 下坠 · 轻 L 上浮 · 稳 Z 配平", new Color32(215, 190, 125, 255));
        stateText = CreateText("FieldState", panelRect, new Vector2(250f, 53f), new Vector2(180f, 22f), 15f,
            TextAlignmentOptions.Right, "稳定", new Color32(114, 231, 193, 255));

        CreateText("OffsetLabel", panelRect, new Vector2(-325f, 26f), new Vector2(100f, 22f), 13f,
            TextAlignmentOptions.Left, "判定线偏移", new Color32(195, 208, 224, 255));

        Image gauge = CreateImage("OffsetGauge", panelRect, new Vector2(-175f, 26f), new Vector2(180f, 14f));
        gauge.color = new Color32(23, 36, 54, 255);
        gauge.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        gauge.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        gaugeSafeZone = CreateImage("SafeZone", gauge.rectTransform, Vector2.zero, Vector2.zero).rectTransform;
        Image safeImage = gaugeSafeZone.GetComponent<Image>();
        safeImage.color = new Color32(26, 101, 91, 255);
        gaugeSafeZone.anchorMin = new Vector2(0.5f, 0.5f);
        gaugeSafeZone.anchorMax = new Vector2(0.5f, 0.5f);
        gaugeSafeZone.pivot = new Vector2(0.5f, 0.5f);
        gaugeMarker = CreateImage("OffsetMarker", gauge.rectTransform, Vector2.zero, new Vector2(3f, 20f)).rectTransform;
        gaugeMarker.anchorMin = new Vector2(0.5f, 0.5f);
        gaugeMarker.anchorMax = new Vector2(0.5f, 0.5f);
        gaugeMarker.pivot = new Vector2(0.5f, 0.5f);
        gaugeMarker.GetComponent<Image>().color = new Color32(246, 250, 255, 255);

        offsetText = CreateText("OffsetValue", panelRect, new Vector2(-37f, 26f), new Vector2(105f, 22f), 12f,
            TextAlignmentOptions.Left, "+0.00", new Color32(235, 241, 250, 255));

        CreateText("StabilityLabel", panelRect, new Vector2(88f, 26f), new Vector2(70f, 22f), 13f,
            TextAlignmentOptions.Left, "稳定度", new Color32(195, 208, 224, 255));
        Image stabilityBar = CreateImage("StabilityBar", panelRect, new Vector2(194f, 26f), new Vector2(130f, 12f));
        stabilityBar.color = new Color32(23, 36, 54, 255);
        stabilityFill = CreateFill("StabilityFill", stabilityBar.rectTransform, new Color32(68, 218, 172, 255));
        CreateText("StabilityKeyHint", panelRect, new Vector2(301f, 26f), new Vector2(62f, 20f), 11f,
            TextAlignmentOptions.Right, "100%", new Color32(177, 205, 219, 255));

        CreateText("CalibrationLabel", panelRect, new Vector2(-325f, -1f), new Vector2(100f, 22f), 13f,
            TextAlignmentOptions.Left, "校准能量", new Color32(195, 208, 224, 255));
        Image energyBar = CreateImage("CalibrationBar", panelRect, new Vector2(-175f, -1f), new Vector2(180f, 12f));
        energyBar.color = new Color32(23, 36, 54, 255);
        energyFill = CreateFill("CalibrationFill", energyBar.rectTransform, new Color32(78, 191, 255, 255));
        energyText = CreateText("CalibrationValue", panelRect, new Vector2(-37f, -1f), new Vector2(105f, 22f), 12f,
            TextAlignmentOptions.Left, "0 / 100", new Color32(187, 222, 243, 255));
        barrierText = CreateText("BarrierValue", panelRect, new Vector2(210f, -1f), new Vector2(240f, 22f), 12f,
            TextAlignmentOptions.Right, "SPACE 校准 · 抑制四拍重力", new Color32(106, 207, 242, 255));

        CreateText("ForecastLegend", panelRect, new Vector2(-5f, -29f), new Vector2(690f, 18f), 11f,
            TextAlignmentOptions.Left, "未来四拍  ·  H 重  /  L 轻  /  Z 稳  /  N 普通", new Color32(155, 181, 202, 255));

        float[] cardX = { -285f, -95f, 95f, 285f };
        for (int i = 0; i < forecastTexts.Length; i++)
        {
            Image card = CreateImage("ForecastBeat" + (i + 1), panelRect, new Vector2(cardX[i], -55f), new Vector2(178f, 31f));
            RhythmUITheme.StylePanel(card, new Color32(17, 31, 49, 255), false);
            forecastTexts[i] = CreateText("ForecastText" + (i + 1), card.rectTransform, Vector2.zero,
                new Vector2(170f, 29f), 12f, TextAlignmentOptions.Center, (i + 1) + "拍  —",
                new Color32(218, 230, 242, 255));
        }

        UpdateResponsiveScale();
    }

    private void UpdateResponsiveScale()
    {
        if (rootRect == null)
        {
            return;
        }

        float widthScale = Screen.width > 0 ? Screen.width / 1000f : 1f;
        float heightScale = Screen.height > 0 ? Screen.height / 720f : 1f;
        float scale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.85f, 1.25f);
        rootRect.localScale = Vector3.one * scale;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void RefreshHUD()
    {
        float maxOffset = judgeLineController != null ? Mathf.Max(0.1f, judgeLineController.maxOffset) : 1.5f;
        float offset = judgeLineController != null ? judgeLineController.offsetY : 0f;
        float warningOffset = stabilitySystem != null ? stabilitySystem.warningOffset : maxOffset * 0.53f;
        float dangerOffset = stabilitySystem != null ? stabilitySystem.dangerOffset : maxOffset * 0.8f;
        float absoluteOffset = Mathf.Abs(offset);

        Color stateColor;
        if (absoluteOffset >= dangerOffset)
        {
            stateText.text = "危险  ·  DANGER";
            stateColor = new Color32(255, 105, 112, 255);
        }
        else if (absoluteOffset >= warningOffset)
        {
            stateText.text = "偏移过大  ·  WARNING";
            stateColor = new Color32(255, 195, 94, 255);
        }
        else if (offset <= -0.08f)
        {
            stateText.text = "重力下坠  ·  DOWN";
            stateColor = new Color32(255, 166, 88, 255);
        }
        else if (offset >= 0.08f)
        {
            stateText.text = "反重力上浮  ·  UP";
            stateColor = new Color32(97, 180, 255, 255);
        }
        else
        {
            stateText.text = "稳定  ·  STABLE";
            stateColor = new Color32(114, 231, 193, 255);
        }

        stateText.color = stateColor;
        offsetText.text = (offset >= 0f ? "+" : "") + offset.ToString("F2") + " / " + maxOffset.ToString("F1");
        offsetText.color = stateColor;

        if (judgeLineController != null && judgeLineController.TryGetCounterweightTarget(out WeightType targetWeight, out float linkRemaining))
        {
            int remainingBeats = Mathf.CeilToInt(linkRemaining / (chartData != null && chartData.bpm > 0f ? 60f / chartData.bpm : 0.5f));
            string targetName = targetWeight == WeightType.Heavy ? "重" : "轻";
            counterweightText.text = "配平待触发：" + targetName + "或稳定音符（" + remainingBeats + "拍内）";
            counterweightText.color = new Color32(255, 211, 127, 255);
        }
        else
        {
            counterweightText.text = "重下坠 · 轻上浮 · 稳定音配平";
            counterweightText.color = new Color32(215, 190, 125, 255);
        }

        float gaugeWidth = 180f;
        gaugeSafeZone.sizeDelta = new Vector2(gaugeWidth * Mathf.Clamp01(warningOffset / maxOffset), 10f);
        gaugeMarker.anchoredPosition = new Vector2(Mathf.Clamp(offset / maxOffset, -1f, 1f) * gaugeWidth * 0.5f, 0f);
        gaugeMarker.GetComponent<Image>().color = stateColor;

        float stability = stabilitySystem != null ? stabilitySystem.GetStability() : 0f;
        float maxStability = stabilitySystem != null ? Mathf.Max(1f, stabilitySystem.maxStability) : 100f;
        float stabilityRatio = Mathf.Clamp01(stability / maxStability);
        SetFill(stabilityFill, stabilityRatio);
        stabilityFill.GetComponent<Image>().color = stabilityRatio < 0.3f
            ? new Color32(255, 104, 112, 255)
            : new Color32(68, 218, 172, 255);

        float energy = judgeLineController != null ? judgeLineController.calibrationEnergy : 0f;
        float maxEnergy = judgeLineController != null ? Mathf.Max(1f, judgeLineController.maxCalibrationEnergy) : 100f;
        float cost = judgeLineController != null ? judgeLineController.calibrationCost : 30f;
        SetFill(energyFill, Mathf.Clamp01(energy / maxEnergy));
        energyText.text = Mathf.FloorToInt(energy) + " / " + Mathf.FloorToInt(maxEnergy);
        energyText.color = energy >= cost
            ? new Color32(112, 219, 255, 255)
            : new Color32(155, 174, 194, 255);

        float bpm = chartData != null ? chartData.bpm : 0f;
        float beatDuration = bpm > 0f ? 60f / bpm : 0.5f;
        float lockRemaining = judgeLineController != null ? judgeLineController.GetGravityLockRemaining() : 0f;
        float musicLeadInRemaining = gameManager != null ? gameManager.GetMusicLeadInRemaining() : 0f;
        if (musicLeadInRemaining > 0f)
        {
            barrierText.text = "音乐即将开始  ·  " + musicLeadInRemaining.ToString("F1") + " 秒";
            barrierText.color = new Color32(112, 219, 255, 255);
        }
        else if (lockRemaining > 0f)
        {
            int remainingBeats = Mathf.CeilToInt(lockRemaining / beatDuration);
            barrierText.text = "重力屏障  ·  剩余 " + remainingBeats + " 拍";
            barrierText.color = new Color32(112, 219, 255, 255);
        }
        else
        {
            barrierText.text = energy >= cost ? "SPACE 校准  ·  抑制四拍重力" : "SPACE 校准  ·  能量不足";
            barrierText.color = energy >= cost
                ? new Color32(106, 207, 242, 255)
                : new Color32(146, 158, 176, 255);
        }

        RefreshForecast(beatDuration);
    }

    private void RefreshForecast(float beatDuration)
    {
        StringBuilder[] beatNotes =
        {
            new StringBuilder(), new StringBuilder(), new StringBuilder(), new StringBuilder()
        };
        int[] beatCounts = new int[4];

        if (chartData != null && chartData.notes != null && gameManager != null)
        {
            float currentTime = gameManager.GetMusicTime();
            for (int i = 0; i < chartData.notes.Count; i++)
            {
                NoteData note = chartData.notes[i];
                if (note == null)
                {
                    continue;
                }

                float timeUntilNote = note.time - currentTime;
                if (timeUntilNote < 0f)
                {
                    continue;
                }

                int beatIndex = Mathf.FloorToInt(timeUntilNote / beatDuration);
                if (beatIndex < 0 || beatIndex >= forecastTexts.Length)
                {
                    continue;
                }

                if (beatCounts[beatIndex] < 4)
                {
                    if (beatNotes[beatIndex].Length > 0)
                    {
                        beatNotes[beatIndex].Append("  ");
                    }

                    string laneKey = note.lane >= 0 && note.lane < LaneKeys.Length
                        ? LaneKeys[note.lane]
                        : "?";
                    beatNotes[beatIndex].Append(laneKey).Append(':').Append(GetWeightCode(note.GetWeightType()));
                }

                beatCounts[beatIndex]++;
            }
        }

        for (int i = 0; i < forecastTexts.Length; i++)
        {
            string notes = beatNotes[i].Length > 0 ? beatNotes[i].ToString() : "—";
            if (beatCounts[i] > 4)
            {
                notes += "  +" + (beatCounts[i] - 4);
            }

            forecastTexts[i].text = (i + 1) + "拍   " + notes;
        }
    }

    private static string GetWeightCode(WeightType weightType)
    {
        switch (weightType)
        {
            case WeightType.Heavy: return "H";
            case WeightType.Light: return "L";
            case WeightType.Stabilize: return "Z";
            default: return "N";
        }
    }

    private TMP_Text CreateText(
        string objectName,
        RectTransform parent,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment,
        string initialText,
        Color color)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }
        text.text = initialText;
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(9f, fontSize - 2f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.enableWordWrapping = false;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(string objectName, RectTransform parent, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateFill(string objectName, RectTransform parent, Color color)
    {
        Image fill = CreateImage(objectName, parent, Vector2.zero, Vector2.zero);
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        fill.color = color;
        return rect;
    }

    private static void SetFill(RectTransform fill, float ratio)
    {
        if (fill != null)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }
    }
}
