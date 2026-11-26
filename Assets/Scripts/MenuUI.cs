using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public Texture2D backdrop;

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
        MakeText(canvasObj.transform, "KARTOPIA GP", 96, new Vector2(0f, 170f), new Color(1f, 0.8f, 0.15f), true);
        MakeText(canvasObj.transform, "3 LAPS - DRIFT BOOST - ITEM BOXES", 26, new Vector2(0f, 105f), Color.white, false);
        var start = MakeButton(canvasObj.transform, "START RACE", new Vector2(0f, -40f));
        start.onClick.AddListener(() => SceneManager.LoadScene("KartTrack"));
        MakeText(canvasObj.transform, "WASD / Arrows - drive   SPACE - drift   Touch - steer", 20, new Vector2(0f, -300f), new Color(1, 1, 1, 0.8f), false);
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
        rt.sizeDelta = new Vector2(380f, 90f);
        var text = MakeText(obj.transform, label, 36, Vector2.zero, Color.white, true);
        return btn;
    }
}
