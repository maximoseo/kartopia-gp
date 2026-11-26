using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// In-game update check: fetches the GitHub Releases manifest, and when a
/// newer build exists shows an update panel that opens the APK download.
/// Offline-first: every failure is silent.
/// </summary>
public class Updater : MonoBehaviour
{
    const string ManifestUrl = "https://github.com/maximoseo/kartopia-gp/releases/latest/download/version.json";

    [Serializable] class Manifest { public string version; public string apk; public string notes; }

    string _apkUrl;
    string _newVersion;

    void Start() => StartCoroutine(Check());

    public static int CompareVersions(string a, string b)
    {
        var pa = a.TrimStart('v').Split('.');
        var pb = b.TrimStart('v').Split('.');
        for (int i = 0; i < 3; i++)
        {
            int av = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
            int bv = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
            if (av != bv) return av.CompareTo(bv);
        }
        return 0;
    }

    IEnumerator Check()
    {
        using var req = UnityWebRequest.Get(ManifestUrl);
        req.timeout = 10;
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) yield break;
        var manifest = JsonUtility.FromJson<Manifest>(req.downloadHandler.text);
        if (manifest == null || string.IsNullOrEmpty(manifest.version)) yield break;
        if (CompareVersions(manifest.version, Application.version) <= 0) yield break;
        _newVersion = manifest.version;
        _apkUrl = $"https://github.com/maximoseo/kartopia-gp/releases/latest/download/{manifest.apk}";
        ShowPanel(manifest);
    }

    void ShowPanel(Manifest manifest)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        var panel = new GameObject("UpdatePanel");
        panel.transform.SetParent(canvas.transform);
        var img = panel.AddComponent<Image>(); img.color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(620f, 300f);

        var textObj = new GameObject("UpdateText");
        textObj.transform.SetParent(panel.transform);
        var text = textObj.AddComponent<Text>();
        text.text = $"UPDATE AVAILABLE\nKartopia GP v{_newVersion} (you have v{Application.version})\n{manifest.notes}";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 55f);
        trt.sizeDelta = new Vector2(560f, 150f);

        MakeButton(panel.transform, "UPDATE NOW", new Vector2(-130f, -80f), () => Application.OpenURL(_apkUrl));
        MakeButton(panel.transform, "LATER", new Vector2(130f, -80f), () => Destroy(panel));
    }

    void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(parent);
        var img = obj.AddComponent<Image>(); img.color = new Color(0.85f, 0.15f, 0.1f);
        var btn = obj.AddComponent<Button>();
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220f, 64f);
        btn.onClick.AddListener(action);
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform);
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(210f, 54f);
    }
}
