using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Apple Guideline 5.1.2 대응: 글로벌 랭킹 서버로 점수(닉네임 + 기록)를 업로드하기 전에
/// 사용자 동의를 받기 위한 런타임 생성 모달 팝업.
/// 씬/프리팹 수정 없이 코드만으로 UI를 구성하므로 재빌드만 하면 적용된다.
/// </summary>
public static class RankingConsent
{
    // 동의 상태 저장 키 (0 = 미동의, 1 = 동의함)
    const string ConsentKey = "RankingConsent";

    // 개인정보처리방침 URL. 값이 비어 있으면 팝업에 링크 버튼이 표시되지 않는다.
    // App Store Connect에 등록한 것과 동일한 URL을 넣으면 앱 내에서도 열람 가능.
    public const string PrivacyPolicyUrl = "";

    /// <summary>이전에 글로벌 랭킹 업로드에 동의한 적이 있는지.</summary>
    public static bool HasConsent => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

    static void SetConsent(bool granted)
    {
        PlayerPrefs.SetInt(ConsentKey, granted ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 동의 팝업을 띄운다. 이미 동의했다면 팝업 없이 곧바로 onAgree 를 호출한다.
    /// </summary>
    public static void Show(string nickname, string record, Action onAgree, Action onDecline = null)
    {
        if (HasConsent)
        {
            onAgree?.Invoke();
            return;
        }

        EnsureEventSystem();

        // ---- 오버레이 캔버스 ----
        var canvasGO = new GameObject("RankingConsentCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000; // 다른 모든 UI 위에 표시
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // 가로/세로 모두 전체가 보이도록
        canvasGO.AddComponent<GraphicRaycaster>();

        // ---- 뒷배경 어둡게 + 뒤쪽 입력 차단 ----
        var dim = CreateStretchImage("Dim", canvasGO.transform, new Color(0f, 0f, 0f, 0.75f));
        dim.raycastTarget = true;

        // ---- 다이얼로그 패널 (화면 비율 기준 중앙, 방향 무관 반응형) ----
        var panelGO = new GameObject("Panel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = (RectTransform)panelGO.transform;
        panelRT.anchorMin = new Vector2(0.2f, 0.28f);
        panelRT.anchorMax = new Vector2(0.8f, 0.72f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.16f, 0.16f, 0.22f, 1f);

        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 36, 36);
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // ---- 제목 ----
        var title = CreateText("Title", panelGO.transform, "Global Ranking",
            48, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        AddLayoutElement(title.gameObject, minHeight: 60, flexibleHeight: 0);

        // ---- 본문 (동의 고지) ----
        string body =
            $"Your nickname \"{nickname}\" and your record ({record}) will be " +
            "uploaded to our online leaderboard and shared publicly with other players.\n\n" +
            "Do you agree to upload your score?";
        var msg = CreateText("Body", panelGO.transform, body,
            30, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f, 1f), TextAlignmentOptions.Center);
        AddLayoutElement(msg.gameObject, minHeight: 120, flexibleHeight: 1);

        // ---- 개인정보처리방침 링크 (URL 설정 시에만) ----
        if (!string.IsNullOrEmpty(PrivacyPolicyUrl))
        {
            var linkBtn = CreateButton("PrivacyLink", panelGO.transform, "Privacy Policy",
                new Color(0f, 0f, 0f, 0f), new Color(0.6f, 0.8f, 1f, 1f), 26);
            linkBtn.onClick.AddListener(() => Application.OpenURL(PrivacyPolicyUrl));
            AddLayoutElement(linkBtn.gameObject, minHeight: 44, flexibleHeight: 0);
        }

        // ---- 버튼 행 (거부 / 동의) ----
        var rowGO = new GameObject("Buttons", typeof(RectTransform));
        rowGO.transform.SetParent(panelGO.transform, false);
        var row = rowGO.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 24;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;
        AddLayoutElement(rowGO, minHeight: 96, flexibleHeight: 0);

        var declineBtn = CreateButton("Decline", rowGO.transform, "Decline",
            new Color(0.33f, 0.33f, 0.36f, 1f), Color.white, 34);
        var agreeBtn = CreateButton("Agree", rowGO.transform, "Agree",
            new Color(0.30f, 0.69f, 0.31f, 1f), Color.white, 34);

        declineBtn.onClick.AddListener(() =>
        {
            SetConsent(false);
            UnityEngine.Object.Destroy(canvasGO);
            onDecline?.Invoke();
        });

        agreeBtn.onClick.AddListener(() =>
        {
            SetConsent(true);
            UnityEngine.Object.Destroy(canvasGO);
            onAgree?.Invoke();
        });
    }

    // ---------------- UI 헬퍼 ----------------

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static Image CreateStretchImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text,
        float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 16;
        tmp.fontSizeMax = fontSize;
        return tmp;
    }

    static Button CreateButton(string name, Transform parent, string label,
        Color bgColor, Color textColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var txt = CreateText("Text", go.transform, label,
            fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
        var txtRT = (RectTransform)txt.transform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        return btn;
    }

    static void AddLayoutElement(GameObject go, float minHeight, float flexibleHeight)
    {
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = minHeight;
        le.flexibleHeight = flexibleHeight;
    }
}
