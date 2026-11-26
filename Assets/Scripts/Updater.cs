using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Full in-app updater: checks the GitHub Releases manifest, downloads the
/// APK inside the app with progress, verifies size + SHA-256, then hands it
/// to Android's package installer via FileProvider. Offline-first: silent
/// failures, never blocks the game.
/// </summary>
public class Updater : MonoBehaviour
{
    const string ManifestUrl = "https://github.com/maximoseo/kartopia-gp/releases/latest/download/version.json";
    const string DownloadRoot = "https://github.com/maximoseo/kartopia-gp/releases/latest/download/";
    const string ApkPathName = "kartopia-update.apk";

    [Serializable] class Manifest
    {
        public string version;
        public string apk;
        public string sha256;
        public long size;
        public string notes;
    }

    Manifest _offer;
    GameObject _panel;
    Text _bodyText;
    Button _primaryButton;
    Text _primaryLabel;
    Image _progressFill;
    bool _busy;

    string ApkFilePath => Path.Combine(Application.persistentDataPath, ApkPathName);

    bool _manualCheck;

    void Start()
    {
        if (File.Exists(ApkFilePath)) File.Delete(ApkFilePath);
        StartCoroutine(Check());
    }

    public void CheckNow(bool manual)
    {
        _manualCheck = manual;
        StartCoroutine(Check());
    }

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
        Manifest manifest = null;
        for (int attempt = 0; attempt < 3 && manifest == null; attempt++)
        {
            using var req = UnityWebRequest.Get(ManifestUrl);
            req.timeout = 12;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                manifest = JsonUtility.FromJson<Manifest>(req.downloadHandler.text);
            if (manifest == null && attempt < 2)
                yield return new WaitForSeconds(4f);
        }
        if (manifest == null || string.IsNullOrEmpty(manifest.version))
        {
            if (_manualCheck) ShowToast($"Update check failed. Check your connection.\nYou have v{Application.version}");
            yield break;
        }
        if (CompareVersions(manifest.version, Application.version) <= 0)
        {
            if (_manualCheck) ShowToast($"You're up to date (v{Application.version})");
            yield break;
        }
        _offer = manifest;
        ShowPanel(
            $"UPDATE AVAILABLE\nKartopia GP v{manifest.version} (you have v{Application.version})\n{manifest.notes}",
            "DOWNLOAD", StartDownload);
    }

    void ShowToast(string message)
    {
        var canvas = EnsureCanvas();
        var obj = new GameObject("UpdateToast");
        obj.transform.SetParent(canvas.transform);
        var img = obj.AddComponent<Image>(); img.color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(560f, 120f);
        MakeText(obj.transform, message, Vector2.zero, new Vector2(520f, 100f), 22);
        Destroy(obj, 3.5f);
    }

    Canvas EnsureCanvas()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas;
        var obj = new GameObject("UpdaterCanvas");
        canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = obj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        obj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    void StartDownload()
    {
        if (_busy || _offer == null) return;
        _busy = true;
        StartCoroutine(Download());
    }

    IEnumerator Download()
    {
        var url = DownloadRoot + _offer.apk;
        using var req = new UnityWebRequest(url, "GET", new DownloadHandlerFile(ApkFilePath), null);
        req.timeout = 300;
        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            SetBody($"DOWNLOADING UPDATE\n{Mathf.RoundToInt(req.downloadProgress * 100f)}%");
            SetProgress(req.downloadProgress);
            yield return null;
        }
        if (req.result != UnityWebRequest.Result.Success || !VerifyDownload())
        {
            if (File.Exists(ApkFilePath)) File.Delete(ApkFilePath);
            SetBody("Download failed. Check your connection and try again.");
            SetPrimary("RETRY", StartDownload);
            _busy = false;
            yield break;
        }
        SetProgress(1f);
        ShowInstallStep();
    }

    bool VerifyDownload()
    {
        var info = new FileInfo(ApkFilePath);
        if (_offer.size > 0 && info.Length != _offer.size) return false;
        if (string.IsNullOrEmpty(_offer.sha256)) return true;
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(ApkFilePath);
        var hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        return hash == _offer.sha256.ToLowerInvariant();
    }

    void ShowInstallStep()
    {
        ShowPanel(
            $"UPDATE READY\nKartopia GP v{_offer.version} is downloaded and verified.\nInstall it now?",
            "INSTALL", InstallUpdate);
    }

    void InstallUpdate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var packageManager = activity.Call<AndroidJavaObject>("getPackageManager");
            if (!packageManager.Call<bool>("canRequestPackageInstalls"))
            {
                using var settingsIntent = new AndroidJavaObject("android.content.Intent",
                    "android.settings.MANAGE_UNKNOWN_APP_SOURCES",
                    new AndroidJavaObject("android.net.Uri", "package:" + Application.identifier));
                settingsIntent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                activity.Call("startActivity", settingsIntent);
                SetBody("Allow installs from Kartopia in Settings,\nthen come back and tap INSTALL again.");
                return;
            }
            using var file = new AndroidJavaObject("java.io.File", ApkFilePath);
            using var providerClass = new AndroidJavaClass("androidx.core.content.FileProvider");
            using var uri = providerClass.CallStatic<AndroidJavaObject>("getUriForFile",
                activity, Application.identifier + ".fileprovider", file);
            using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
            intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
            intent.Call<AndroidJavaObject>("addFlags", 0x00000001); // FLAG_GRANT_READ_URI_PERMISSION
            intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
            activity.Call("startActivity", intent);
        }
        catch (Exception)
        {
            Application.OpenURL(DownloadRoot + _offer.apk);
        }
#else
        Application.OpenURL(DownloadRoot + _offer.apk);
#endif
    }

    // ---------- UI ----------

    void ShowPanel(string body, string primaryLabel, UnityEngine.Events.UnityAction primaryAction)
    {
        var canvas = EnsureCanvas();
        if (_panel != null) Destroy(_panel);
        _panel = new GameObject("UpdatePanel");
        _panel.transform.SetParent(canvas.transform);
        var img = _panel.AddComponent<Image>(); img.color = new Color(0.02f, 0.03f, 0.08f, 0.94f);
        var rt = _panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(640f, 340f);

        _bodyText = MakeText(_panel.transform, body, new Vector2(0f, 62f), new Vector2(580f, 150f), 23);
        var barBg = new GameObject("ProgressBg");
        barBg.transform.SetParent(_panel.transform);
        var bgImg = barBg.AddComponent<Image>(); bgImg.color = new Color(1, 1, 1, 0.15f);
        var brt = barBg.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, -28f);
        brt.sizeDelta = new Vector2(520f, 18f);
        var fillObj = new GameObject("ProgressFill");
        fillObj.transform.SetParent(barBg.transform);
        _progressFill = fillObj.AddComponent<Image>(); _progressFill.color = new Color(0.3f, 0.85f, 1f);
        var frt = fillObj.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f);
        frt.sizeDelta = new Vector2(0f, 0f);
        SetProgress(0f);

        _primaryButton = MakeButton(_panel.transform, primaryLabel, new Vector2(-135f, -95f), primaryAction);
        _primaryLabel = _primaryButton.GetComponentInChildren<Text>();
        MakeButton(_panel.transform, "LATER", new Vector2(135f, -95f), () => Destroy(_panel));
    }

    void SetBody(string text) { if (_bodyText != null) _bodyText.text = text; }
    void SetProgress(float ratio)
    {
        if (_progressFill != null)
            _progressFill.rectTransform.sizeDelta = new Vector2(520f * Mathf.Clamp01(ratio), 0f);
    }
    void SetPrimary(string label, UnityEngine.Events.UnityAction action)
    {
        if (_primaryLabel != null) _primaryLabel.text = label;
        if (_primaryButton != null)
        {
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(action);
        }
    }

    Text MakeText(Transform parent, string content, Vector2 pos, Vector2 size, int fontSize)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent);
        var text = obj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return text;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(parent);
        var img = obj.AddComponent<Image>(); img.color = new Color(0.85f, 0.15f, 0.1f);
        var btn = obj.AddComponent<Button>();
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(230f, 64f);
        btn.onClick.AddListener(action);
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform);
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 25; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(220f, 54f);
        return btn;
    }
}
