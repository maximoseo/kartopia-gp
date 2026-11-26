using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime-built race HUD: position, time, speed, boost bar, countdown, results.</summary>
public class Hud : MonoBehaviour
{
    public RaceManager race;
    public KartController kart;
    public Rigidbody kartBody;

    Text _posText, _timeText, _speedText, _centerText, _resultsText;
    Image _boostFill;
    GameObject _resultsPanel;

    void Start()
    {
        var canvasObj = new GameObject("HudCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        _posText = MakeText(canvasObj.transform, "POS", new Vector2(0.5f, 1f), new Vector2(-160f, -30f), 44);
        _timeText = MakeText(canvasObj.transform, "TIME", new Vector2(0.5f, 1f), new Vector2(60f, -30f), 26);
        _speedText = MakeText(canvasObj.transform, "SPEED", new Vector2(1f, 0f), new Vector2(-150f, 95f), 52);
        _centerText = MakeText(canvasObj.transform, "CENTER", new Vector2(0.5f, 0.55f), Vector2.zero, 90);
        _resultsPanel = MakePanel(canvasObj.transform);
        _resultsText = MakeText(_resultsPanel.transform, "RESULTS", new Vector2(0.5f, 0.5f), Vector2.zero, 34);
        _resultsPanel.SetActive(false);

        // boost bar bottom-right
        var bg = new GameObject("BoostBg");
        bg.transform.SetParent(canvasObj.transform);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(1, 1, 1, 0.15f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(1f, 0f);
        bgRt.anchoredPosition = new Vector2(-150f, 62f);
        bgRt.sizeDelta = new Vector2(220f, 16f);
        var fill = new GameObject("BoostFill");
        fill.transform.SetParent(bg.transform);
        _boostFill = fill.AddComponent<Image>(); _boostFill.color = new Color(0.3f, 0.85f, 1f, 0.95f);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f); fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = new Vector2(0f, 0f);
        fillRt.sizeDelta = new Vector2(0f, 0f);
    }

    Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 pos, int size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(520f, 110f);
        return text;
    }

    GameObject MakePanel(Transform parent)
    {
        var obj = new GameObject("ResultsPanel");
        obj.transform.SetParent(parent);
        var img = obj.AddComponent<Image>(); img.color = new Color(0.01f, 0.02f, 0.05f, 0.82f);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(560f, 380f);
        return obj;
    }

    void Update()
    {
        if (race == null) return;
        _posText.text = $"P{race.PlayerPosition()} / {race.RacerCount}  LAP {race.PlayerLapDisplay}/{race.totalLaps}";
        _timeText.text = RaceManager.FormatTime(race.RaceTime);
        if (kartBody != null)
            _speedText.text = $"{Mathf.RoundToInt(kartBody.linearVelocity.magnitude * 3.6f)}";
        if (kart != null && _boostFill != null)
        {
            float ratio = kart.BoostActive ? 1f : kart.ChargeRatio;
            _boostFill.color = kart.BoostActive ? new Color(1f, 0.55f, 0.15f) : new Color(0.3f, 0.85f, 1f, 0.95f);
            _boostFill.rectTransform.sizeDelta = new Vector2(220f * ratio, 0f);
        }
        _centerText.text = race.Phase == RacePhase.Countdown
            ? (race.CountdownLeft > 0.5f ? Mathf.CeilToInt(race.CountdownLeft).ToString() : "GO!")
            : "";
        if (race.Phase == RacePhase.Finished && !_resultsPanel.activeSelf)
        {
            _resultsText.text = "FINISH\n" + race.ResultsText();
            _resultsPanel.SetActive(true);
        }
    }
}
