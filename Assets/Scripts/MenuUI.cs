using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public Texture2D backdrop;
    public Texture2D logo;
    Transform _showroomCar;

    void Update()
    {
        if (_showroomCar == null)
        {
            var found = GameObject.Find("ShowroomCar");
            if (found != null) _showroomCar = found.transform;
        }
        if (_showroomCar != null) _showroomCar.Rotate(0f, 16f * Time.deltaTime, 0f);
    }

    void Start()
    {
        var canvasObj = new GameObject("MenuCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        // dim panel behind controls for readability
        var shade = new GameObject("Shade");
        shade.transform.SetParent(canvasObj.transform);
        var shadeImg = shade.AddComponent<Image>();
        shadeImg.color = new Color(0.01f, 0.02f, 0.07f, 0.42f);
        var shadeRt = shade.GetComponent<RectTransform>();
        shadeRt.anchorMin = new Vector2(0f, 0f); shadeRt.anchorMax = new Vector2(1f, 0.52f);
        shadeRt.offsetMin = shadeRt.offsetMax = Vector2.zero;

        if (backdrop != null)
        {
            var bg = new GameObject("Backdrop");
            bg.transform.SetParent(canvasObj.transform);
            var raw = bg.AddComponent<RawImage>(); raw.texture = backdrop;
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            bg.transform.SetAsFirstSibling();
        }
        if (logo != null)
        {
            var logoObj = new GameObject("Logo");
            logoObj.transform.SetParent(canvasObj.transform);
            var raw = logoObj.AddComponent<RawImage>(); raw.texture = logo;
            var lrt = logoObj.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = new Vector2(0f, 205f);
            lrt.sizeDelta = new Vector2(240f, 240f);
        }
        MakeText(canvasObj.transform, "KARTOPIA GP", 72, new Vector2(0f, 62f), new Color(1f, 0.8f, 0.15f), true);
        MakeText(canvasObj.transform, "3 LAPS - DRIFT BOOST - ITEM BOXES", 24, new Vector2(0f, 14f), Color.white, false);
        var start = MakeButton(canvasObj.transform, "SEASIDE LOOP", new Vector2(-200f, -40f));
        start.onClick.AddListener(() => SceneManager.LoadScene("KartTrack"));
        var canyon = MakeButton(canvasObj.transform, "CANDY CANYON", new Vector2(200f, -40f));
        canyon.onClick.AddListener(() => SceneManager.LoadScene("CanyonTrack"));
        MakeText(canvasObj.transform, "WASD / Arrows - drive   SPACE - drift   Touch - steer", 20, new Vector2(0f, -300f), new Color(1, 1, 1, 0.8f), false);
        var checkBtn = MakeButton(canvasObj.transform, "CHECK UPDATE", new Vector2(0f, -225f));
        checkBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 56f);
        checkBtn.onClick.AddListener(() =>
        {
            var updater = FindFirstObjectByType<Updater>();
            if (updater != null) updater.CheckNow(true);
        });
    }

    Text MakeText(Transform parent, string content, int size, Vector2 pos, Color color, bool bold)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent);
        var text = obj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        var shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(2f, -2f);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1000f, 130f);
        return text;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos)
    {
        var obj = new GameObject("Button");
        obj.transform.SetParent(parent);
        var img = obj.AddComponent<Image>(); img.color = new Color(0.9f, 0.15f, 0.1f);
        var btn = obj.AddComponent<Button>();
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(340f, 90f);
        var text = MakeText(obj.transform, label, 36, Vector2.zero, Color.white, true);
        return btn;
    }
}
