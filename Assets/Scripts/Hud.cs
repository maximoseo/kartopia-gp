using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime-built race HUD: position, time, speed, boost bar, countdown, results.</summary>
public class Hud : MonoBehaviour
{
    public RaceManager race;
    public KartController kart;
    public Rigidbody kartBody;

    Text _posText, _timeText, _speedText, _centerText, _resultsText, _standingsText;
    Image _boostFill;
    GameObject _resultsPanel;
    RectTransform _mapRect;
    readonly System.Collections.Generic.List<RectTransform> _mapDots = new();
    Vector2 _mapMin, _mapSize;

    void Start()
    {
        var canvasObj = new GameObject("HudCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        _posText = MakeText(canvasObj.transform, "POS", new Vector2(0.5f, 1f), new Vector2(-160f, -30f), 44);
        _standingsText = MakeText(canvasObj.transform, "STANDINGS", new Vector2(0f, 1f), new Vector2(95f, -110f), 22);
        _standingsText.alignment = TextAnchor.UpperLeft;
        _timeText = MakeText(canvasObj.transform, "TIME", new Vector2(0.5f, 1f), new Vector2(60f, -30f), 26);
        _speedText = MakeText(canvasObj.transform, "SPEED", new Vector2(1f, 0f), new Vector2(-150f, 95f), 52);
        _centerText = MakeText(canvasObj.transform, "CENTER", new Vector2(0.5f, 0.55f), Vector2.zero, 90);
        _resultsPanel = MakePanel(canvasObj.transform);
        _resultsText = MakeText(_resultsPanel.transform, "RESULTS", new Vector2(0.5f, 0.5f), Vector2.zero, 34);
        _resultsPanel.SetActive(false);

        BuildMinimap(canvasObj.transform);

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

    void BuildMinimap(Transform parent)
    {
        if (race == null || race.track == null || race.track.points == null) return;
        var pts = race.track.points;
        _mapMin = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var pt in pts)
        {
            _mapMin = Vector2.Min(_mapMin, new Vector2(pt.x, pt.z));
            max = Vector2.Max(max, new Vector2(pt.x, pt.z));
        }
        _mapSize = max - _mapMin;
        var tex = new Texture2D(220, 220, TextureFormat.RGBA32, false);
        var clear = new Color(0.02f, 0.03f, 0.08f, 0.75f);
        for (int y = 0; y < 220; y++) for (int x = 0; x < 220; x++) tex.SetPixel(x, y, clear);
        foreach (var pt in pts)
        {
            var uv = WorldToMap(pt);
            for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
            {
                int px = Mathf.Clamp(Mathf.RoundToInt(uv.x * 219f) + dx, 0, 219);
                int py = Mathf.Clamp(Mathf.RoundToInt(uv.y * 219f) + dy, 0, 219);
                tex.SetPixel(px, py, Color.white);
            }
        }
        tex.Apply();
        var mapObj = new GameObject("Minimap");
        mapObj.transform.SetParent(parent);
        var raw = mapObj.AddComponent<RawImage>(); raw.texture = tex;
        _mapRect = mapObj.GetComponent<RectTransform>();
        _mapRect.anchorMin = _mapRect.anchorMax = new Vector2(1f, 1f);
        _mapRect.anchoredPosition = new Vector2(-125f, -125f);
        _mapRect.sizeDelta = new Vector2(210f, 210f);
        var dotColors = new[] { Color.white, new Color(1f, 0.25f, 0.2f), new Color(0.3f, 0.9f, 0.4f), new Color(1f, 0.75f, 0.15f) };
        var racers = new System.Collections.Generic.List<Transform> { race.player };
        racers.AddRange(race.aiCars);
        for (int i = 0; i < racers.Count && i < dotColors.Length; i++)
        {
            var dot = new GameObject("MapDot");
            dot.transform.SetParent(mapObj.transform);
            var img = dot.AddComponent<Image>(); img.color = dotColors[i];
            var rt = dot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(i == 0 ? 13f : 10f, i == 0 ? 13f : 10f);
            _mapDots.Add(rt);
        }
    }

    Vector2 WorldToMap(Vector3 world)
    {
        return new Vector2(
            (world.x - _mapMin.x) / Mathf.Max(_mapSize.x, 1f),
            (world.z - _mapMin.y) / Mathf.Max(_mapSize.y, 1f));
    }

    void UpdateMinimap()
    {
        if (_mapRect == null || race == null) return;
        var racers = new System.Collections.Generic.List<Transform> { race.player };
        racers.AddRange(race.aiCars);
        for (int i = 0; i < _mapDots.Count && i < racers.Count; i++)
        {
            if (racers[i] == null) continue;
            var uv = WorldToMap(racers[i].position);
            _mapDots[i].anchoredPosition = new Vector2(uv.x * 210f, uv.y * 210f);
        }
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

    bool _paused;

    public void TogglePause()
    {
        _paused = !_paused;
        Time.timeScale = _paused ? 0f : 1f;
        if (_centerText != null) _centerText.text = _paused ? "PAUSED" : "";
    }

    void Update()
    {
        if (race == null) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (_paused) return;
        _posText.text = $"P{race.PlayerPosition()} / {race.RacerCount}  LAP {race.PlayerLapDisplay}/{race.totalLaps}";
        if (_standingsText != null) _standingsText.text = race.StandingsText();
        _timeText.text = RaceManager.FormatTime(race.RaceTime);
        if (kartBody != null)
            _speedText.text = $"{Mathf.RoundToInt(kartBody.linearVelocity.magnitude * 3.6f)}";
        if (kart != null && kart.Slipstreaming) _speedText.text += " SLIP";
        if (kart != null && _boostFill != null)
        {
            float ratio = kart.BoostActive ? 1f : kart.ChargeRatio;
            _boostFill.color = kart.BoostActive ? new Color(1f, 0.55f, 0.15f) : new Color(0.3f, 0.85f, 1f, 0.95f);
            _boostFill.rectTransform.sizeDelta = new Vector2(220f * ratio, 0f);
        }
        if (race.PlayerWrongWay && race.Phase == RacePhase.Racing)
            _centerText.text = "WRONG WAY!";
        else if (race.Phase == RacePhase.Finished && Input.GetKeyDown(KeyCode.R))
            race.RestartRace();
        else
        _centerText.text = race.Phase == RacePhase.Countdown
            ? (race.CountdownLeft > 0.5f ? Mathf.CeilToInt(race.CountdownLeft).ToString() : "GO!")
            : "";
        UpdateMinimap();
        if (race.Phase == RacePhase.Finished && !_resultsPanel.activeSelf)
        {
            _resultsText.text = "FINISH\n" + race.ResultsText();
            _resultsPanel.SetActive(true);
        }
    }
}
