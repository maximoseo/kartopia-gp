using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CaptureShots
{
    static void Shot(Camera cam, string path)
    {
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Debug.Log($"SHOT_SAVED {path}");
    }

    [MenuItem("KritiKart/Capture Shots")]
    public static void CaptureAll()
    {
        Directory.CreateDirectory("/tmp/unity_shots");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/KartTrack.unity", OpenSceneMode.Single);
        var kart = GameObject.Find("Kart");
        var camObj = GameObject.Find("Main Camera");
        var cam = camObj.GetComponent<Camera>();

        // keep the start grid: player + AI field visible
        // grid pose: leave kart where BuildKart placed it (AI field behind)

        // 1) chase view (what the player sees)
        cam.transform.position = kart.transform.position + kart.transform.rotation * new Vector3(0f, 4.2f, -8.5f);
        cam.transform.LookAt(kart.transform.position + Vector3.up * 1.2f + kart.transform.forward * 4f);
        Shot(cam, "/tmp/unity_shots/01_chase.png");

        // 2) front three-quarter hero
        cam.transform.position = kart.transform.position + new Vector3(4.6f, 2.0f, 5.2f);
        cam.transform.LookAt(kart.transform.position + Vector3.up * 0.6f);
        Shot(cam, "/tmp/unity_shots/02_hero.png");

        // 3) track overview
        cam.transform.position = new Vector3(26f, 30f, -70f);
        cam.transform.LookAt(new Vector3(0f, 0f, 10f));
        Shot(cam, "/tmp/unity_shots/03_track.png");

        Debug.Log("CAPTURE_ALL_DONE");
    }
}
