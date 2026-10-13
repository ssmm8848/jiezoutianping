using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GravityTutorialUI : MonoBehaviour
{
    private const string SeenKey = "GB_GravityTutorialSeen";

    private static readonly string[] PageTitles =
    {
        "操作与判定",
        "重音与轻音改变重力方向",
        "亲手演示重力变化",
        "配平与稳定",
        "校准与失败"
    };

    private static readonly string[] PageBodies =
    {
        "用 S / D / J / K 击打四条轨道上的音符。\n按键时机越接近判定线，评价越高。\n重、轻、稳定是音符类型标记，不是额外按键。",
        "重音（橙色）会把判定线向下推；轻音（绿色）会把它向上拉。\n偏移会带动后续音符轨迹弯曲，请观察判定线颜色和 HUD 方向。",
        "先点重音或轻音，再点相反重量配平，或点稳定音回正。\n观察指针回中；完成一次示范后即可继续。",
        "在 4 拍内以良好或完美接住相反重量（重后轻，或轻后重），可触发配平、回正并获得奖励。\n稳定音（黄色）会削弱当前偏移，也能参与配平。",
        "完美或良好击中会积累校准能量。能量达到 30 后按 Space，可快速回正并短暂锁住重力。\n偏移进入危险区会持续消耗稳定度；稳定度归零则演奏失败。"
    };

    private GameObject overlayRoot;
    private TMP_Text pageTitleText;
    private TMP_Text bodyText;
    private RectTransform bodyRect;
    private TMP_Text progressText;
    private TMP_Text practiceStateText;
    private TMP_Text primaryButtonText;
    private GameObject practicePanel;
    private RectTransform practiceMarker;
    private Button primaryButton;
    private Button skipButton;
    private Action onClosed;
    private int pageIndex;
    private bool replayMode;
    private bool practiceComplete;
    private float demoOffset;
    private WeightType lastDemoType;
    private bool hasLastDemoType;

    public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;
    public bool HasSeenTutorial => PlayerPrefs.GetInt(SeenKey, 0) != 0;

    public void Initialize(Transform canvasRoot)
    {
        if (overlayRoot != null || canvasRoot == null)
        {
            return;
        }

        TMP_FontAsset font = FindFontAsset();
        overlayRoot = new GameObject("GravityTutorialOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayRoot.layer = canvasRoot.gameObject.layer;
        overlayRoot.transform.SetParent(canvasRoot, false);
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlayRoot.GetComponent<Image>();
        overlayImage.color = new Color32(2, 8, 18, 232);
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panel.layer = canvasRoot.gameObject.layer;
        panel.transform.SetParent(overlayRoot.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800f, 520f);
        Image panelImage = panel.GetComponent<Image>();
        RhythmUITheme.StylePanel(panelImage, RhythmUITheme.Panel);
        panelImage.raycastTarget = true;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color32(48, 190, 213, 130);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CreateText("TutorialTitle", panelRect, font, "重力场 · 快速教学", new Vector2(0f, 211f), new Vector2(700f, 48f), 30f,
            new Color32(236, 247, 255, 255), TextAlignmentOptions.Center, false);
        pageTitleText = CreateText("TutorialPageTitle", panelRect, font, "", new Vector2(0f, 143f), new Vector2(700f, 42f), 23f,
            new Color32(95, 221, 226, 255), TextAlignmentOptions.Center, false);
        bodyText = CreateText("TutorialBody", panelRect, font, "", new Vector2(0f, 20f), new Vector2(690f, 205f), 20f,
            new Color32(214, 229, 241, 255), TextAlignmentOptions.Center, true);
        bodyRect = bodyText.rectTransform;
        progressText = CreateText("TutorialProgress", panelRect, font, "", new Vector2(0f, -151f), new Vector2(320f, 25f), 14f,
            new Color32(143, 171, 194, 255), TextAlignmentOptions.Center, false);

        practicePanel = new GameObject("GravityPractice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        practicePanel.layer = panelRect.gameObject.layer;
        practicePanel.transform.SetParent(panelRect, false);
        RectTransform practiceRect = practicePanel.GetComponent<RectTransform>();
        practiceRect.anchorMin = new Vector2(0.5f, 0.5f);
        practiceRect.anchorMax = new Vector2(0.5f, 0.5f);
        practiceRect.pivot = new Vector2(0.5f, 0.5f);
        practiceRect.anchoredPosition = new Vector2(0f, -35f);
        practiceRect.sizeDelta = new Vector2(680f, 142f);
        Image practiceBackground = practicePanel.GetComponent<Image>();
        RhythmUITheme.StylePanel(practiceBackground, new Color32(7, 17, 31, 255), false);
        practiceBackground.raycastTarget = false;
        practiceStateText = CreateText("GravityPracticeState", practiceRect, font, "重力稳定 · 偏移 0.00",
            new Vector2(0f, 42f), new Vector2(620f, 28f), 16f, new Color32(155, 221, 231, 255),
            TextAlignmentOptions.Center, false);

        Image meterTrack = CreateImage("GravityPracticeTrack", practiceRect, Vector2.zero, new Vector2(520f, 12f),
            new Color32(30, 48, 66, 255));
        CreateImage("GravityPracticeCenter", meterTrack.rectTransform, Vector2.zero, new Vector2(2f, 24f),
            new Color32(178, 204, 218, 255));
        Image marker = CreateImage("GravityPracticeMarker", meterTrack.rectTransform, Vector2.zero, new Vector2(14f, 26f),
            new Color32(95, 221, 226, 255));
        practiceMarker = marker.rectTransform;

        CreateButton("PracticeHeavyButton", "重音 · 下坠", new Vector2(-215f, -44f), font,
            new Color32(111, 58, 39, 255), new Color32(255, 190, 129, 255), practiceRect, new Vector2(150f, 38f))
            .onClick.AddListener(() => ApplyDemoWeight(WeightType.Heavy));
        CreateButton("PracticeLightButton", "轻音 · 上浮", new Vector2(0f, -44f), font,
            new Color32(30, 82, 71, 255), new Color32(137, 237, 194, 255), practiceRect, new Vector2(150f, 38f))
            .onClick.AddListener(() => ApplyDemoWeight(WeightType.Light));
        CreateButton("PracticeStabilizeButton", "稳定 · 回正", new Vector2(215f, -44f), font,
            new Color32(98, 82, 34, 255), new Color32(255, 226, 133, 255), practiceRect, new Vector2(150f, 38f))
            .onClick.AddListener(() => ApplyDemoWeight(WeightType.Stabilize));
        practicePanel.SetActive(false);

        skipButton = CreateButton("SkipTutorialButton", "跳过教学", new Vector2(-170f, -215f), font,
            new Color32(31, 51, 73, 255), Color.white, panelRect, new Vector2(220f, 52f));
        skipButton.onClick.AddListener(Finish);

        primaryButton = CreateButton("ContinueTutorialButton", "下一步", new Vector2(170f, -215f), font,
            new Color32(12, 178, 204, 255), new Color32(5, 19, 30, 255), panelRect, new Vector2(220f, 52f));
        primaryButton.onClick.AddListener(Advance);
        primaryButtonText = primaryButton.GetComponentInChildren<TMP_Text>(true);

        overlayRoot.SetActive(false);
    }

    public void Show(bool isReplay, Action closeCallback)
    {
        if (overlayRoot == null)
        {
            if (closeCallback != null)
            {
                closeCallback();
            }
            return;
        }

        replayMode = isReplay;
        onClosed = closeCallback;
        pageIndex = 0;
        demoOffset = 0f;
        practiceComplete = false;
        hasLastDemoType = false;
        practiceStateText.text = "重力稳定 · 偏移 0.00";
        practiceStateText.color = new Color32(155, 221, 231, 255);
        practiceMarker.anchoredPosition = Vector2.zero;
        practiceMarker.GetComponent<Image>().color = new Color32(95, 221, 226, 255);
        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();
        RefreshPage();
    }

    public void MarkTutorialSeen()
    {
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
    }

    public void CloseTutorial()
    {
        Finish();
    }

    private void Advance()
    {
        if (pageIndex == 2 && !practiceComplete)
        {
            return;
        }

        if (pageIndex < PageTitles.Length - 1)
        {
            pageIndex++;
            RefreshPage();
            return;
        }

        Finish();
    }

    private void Finish()
    {
        if (!IsVisible)
        {
            return;
        }

        overlayRoot.SetActive(false);
        Action callback = onClosed;
        onClosed = null;
        if (callback != null)
        {
            callback();
        }
    }

    private void RefreshPage()
    {
        pageTitleText.text = (pageIndex + 1).ToString("00") + "  /  " + PageTitles[pageIndex];
        bodyText.text = PageBodies[pageIndex];
        bool isPracticePage = pageIndex == 2;
        bodyRect.anchoredPosition = new Vector2(0f, isPracticePage ? 98f : 20f);
        bodyRect.sizeDelta = new Vector2(690f, isPracticePage ? 64f : 205f);
        practicePanel.SetActive(isPracticePage);
        progressText.text = (pageIndex + 1) + " / " + PageTitles.Length + "    ·    " + (replayMode ? "玩法说明" : "首次游玩引导");
        primaryButtonText.text = pageIndex == PageTitles.Length - 1
            ? (replayMode ? "返回暂停" : "开始演奏")
            : pageIndex == 2 && !practiceComplete
                ? "先试一次重力"
                : "下一步";
        primaryButton.interactable = pageIndex != 2 || practiceComplete;
        skipButton.GetComponentInChildren<TMP_Text>(true).text = replayMode ? "关闭说明" : "跳过教学";
    }

    private void ApplyDemoWeight(WeightType type)
    {
        float previousOffset = demoOffset;
        bool reversePair = hasLastDemoType &&
            ((lastDemoType == WeightType.Heavy && type == WeightType.Light) ||
             (lastDemoType == WeightType.Light && type == WeightType.Heavy));
        bool stabilizePair = hasLastDemoType &&
            (lastDemoType == WeightType.Heavy || lastDemoType == WeightType.Light) &&
            type == WeightType.Stabilize;

        if (reversePair || stabilizePair)
        {
            demoOffset *= 0.22f;
            practiceStateText.text = reversePair
                ? "反向重量配平：偏移快速回正 · " + demoOffset.ToString("+0.00;-0.00;0.00")
                : "稳定音配平：偏移快速回正 · " + demoOffset.ToString("+0.00;-0.00;0.00");
            practiceStateText.color = new Color32(115, 239, 195, 255);
        }
        else
        {
            switch (type)
            {
                case WeightType.Heavy:
                    demoOffset = Mathf.Clamp(demoOffset - 0.42f, -1f, 1f);
                    practiceStateText.text = "重音：重力向下 · 偏移 " + demoOffset.ToString("+0.00;-0.00;0.00");
                    practiceStateText.color = new Color32(255, 190, 129, 255);
                    break;
                case WeightType.Light:
                    demoOffset = Mathf.Clamp(demoOffset + 0.34f, -1f, 1f);
                    practiceStateText.text = "轻音：反重力向上 · 偏移 " + demoOffset.ToString("+0.00;-0.00;0.00");
                    practiceStateText.color = new Color32(137, 237, 194, 255);
                    break;
                case WeightType.Stabilize:
                    demoOffset *= 0.3f;
                    practiceStateText.text = "稳定音：压低偏移 · 偏移 " + demoOffset.ToString("+0.00;-0.00;0.00");
                    practiceStateText.color = new Color32(255, 226, 133, 255);
                    break;
            }
        }

        bool stabilizeSuccess = type == WeightType.Stabilize && !stabilizePair &&
                                Mathf.Abs(previousOffset) > 0.1f &&
                                Mathf.Abs(demoOffset) < Mathf.Abs(previousOffset);
        bool justCompleted = !practiceComplete && (reversePair || stabilizePair || stabilizeSuccess);
        practiceComplete = practiceComplete || justCompleted;
        lastDemoType = type;
        hasLastDemoType = true;

        practiceMarker.anchoredPosition = new Vector2(demoOffset * 245f, 0f);
        Image markerImage = practiceMarker.GetComponent<Image>();
        if (markerImage != null)
        {
            markerImage.color = Mathf.Abs(demoOffset) < 0.1f
                ? new Color32(95, 221, 226, 255)
                : demoOffset < 0f
                    ? new Color32(255, 166, 88, 255)
                    : new Color32(97, 180, 255, 255);
        }

        if (justCompleted)
        {
            practiceStateText.text += (reversePair || stabilizePair)
                ? "  ·  配平成功，可以继续"
                : "  ·  偏移得到抑制，可以继续";
        }
        primaryButton.interactable = practiceComplete;
        primaryButtonText.text = practiceComplete ? "下一步" : "先试一次重力";
    }

    private static TMP_FontAsset FindFontAsset()
    {
        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].font != null)
            {
                return texts[i].font;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }

    private TMP_Text CreateText(
        string objectName,
        RectTransform parent,
        TMP_FontAsset font,
        string content,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        bool wrapping)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (font != null)
        {
            text.font = font;
        }
        text.text = content;
        text.fontSize = fontSize;
        text.fontSizeMin = fontSize - 2f;
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.enableWordWrapping = wrapping;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private Image CreateImage(string objectName, RectTransform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Button CreateButton(
        string objectName,
        string label,
        Vector2 position,
        TMP_FontAsset font,
        Color backgroundColor,
        Color labelColor,
        RectTransform parent,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Button button = buttonObject.GetComponent<Button>();
        RhythmUITheme.StyleButton(button, backgroundColor, labelColor);

        TMP_Text text = CreateText("Label", rect, font, label, Vector2.zero, size - new Vector2(10f, 4f), 17f,
            labelColor, TextAlignmentOptions.Center, false);
        text.raycastTarget = false;
        return button;
    }
}
