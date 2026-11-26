using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildGame
{
    static Material Mat(string name, Color color, float metallic = 0f, float smoothness = 0.4f, Texture2D tex = null)
    {
        var path = $"Assets/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        Directory.CreateDirectory("Assets/Materials");
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        if (tex != null) mat.SetTexture("_BaseMap", tex);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static List<Vector3> LoopPoints()
    {
        var pts = new List<Vector3>();
        int n = 96;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            float r = 72f + 15f * Mathf.Sin(a * 3f) + 8f * Mathf.Cos(a * 5f);
            pts.Add(new Vector3(Mathf.Cos(a) * r, 1.6f * Mathf.Sin(a * 2f), Mathf.Sin(a) * r));
        }
        return pts;
    }

    static void BuildRoad(List<Vector3> pts, float width, Material mat)
    {
        int n = pts.Count;
        var verts = new Vector3[n * 2];
        var uvs = new Vector2[n * 2];
        var tris = new List<int>();
        for (int i = 0; i < n; i++)
        {
            var tan = (pts[(i + 1) % n] - pts[(i - 1 + n) % n]).normalized;
            var side = new Vector3(tan.z, 0f, -tan.x).normalized;
            verts[i * 2] = pts[i] - side * (width * 0.5f) + Vector3.up * 0.05f;
            verts[i * 2 + 1] = pts[i] + side * (width * 0.5f) + Vector3.up * 0.05f;
            uvs[i * 2] = new Vector2(0f, i * 0.5f);
            uvs[i * 2 + 1] = new Vector2(1f, i * 0.5f);
            int a = i * 2, b = ((i + 1) % n) * 2;
            tris.Add(a); tris.Add(b); tris.Add(a + 1);
            tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
        }
        var mesh = new Mesh { name = "LoopRoad" };
        mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        var obj = new GameObject("LoopRoad");
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterial = mat;
        obj.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    static GameObject MakeKart(string name, GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var kart = new GameObject(name);
        kart.transform.position = pos;
        kart.transform.rotation = rot;
        var rb = kart.AddComponent<Rigidbody>();
        rb.mass = 110f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var col = kart.AddComponent<BoxCollider>();
        col.size = new Vector3(1.1f, 0.7f, 1.9f);
        col.center = new Vector3(0f, 0.45f, 0f);
        if (prefab != null)
        {
            var model = Object.Instantiate(prefab);
            model.name = "KartModel";
            model.transform.SetParent(kart.transform);
            model.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = Vector3.one * 1.15f;
            var tyreMat = Mat("KartTyre", new Color(0.015f, 0.015f, 0.018f), 0f, 0.25f);
            var rimMat = Mat("KartRim", new Color(0.75f, 0.78f, 0.82f), 0.85f, 0.45f);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                string objectName = renderer.gameObject.name;
                if (objectName.StartsWith("Wheel_tyre") || objectName.StartsWith("Wheel_tread"))
                    renderer.sharedMaterial = tyreMat;
                else if (objectName.StartsWith("Wheel_"))
                    renderer.sharedMaterial = rimMat;
            }
        }
        return kart;
    }

    [MenuItem("Kartopia/Build Game + Android APK")]
    public static void BuildAll()
    {
        var pts = LoopPoints();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        sun.intensity = 2.8f;
        sun.color = new Color(1f, 0.92f, 0.78f);
        sun.shadows = LightShadows.Soft;

        var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/sky.png");
        if (skyTex != null)
        {
            var skyMat = new Material(Shader.Find("Skybox/Panoramic"));
            skyMat.SetTexture("_MainTex", skyTex);
            skyMat.SetFloat("_Exposure", 1.15f);
            RenderSettings.skybox = skyMat;
        }
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.62f, 0.9f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.4f, 0.2f);
        RenderSettings.ambientIntensity = 1.1f;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(34f, 1f, 34f);
        ground.transform.position = new Vector3(0f, -1.2f, 0f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = Mat("Grass", new Color(0.2f, 0.65f, 0.3f), 0f, 0.2f);

        BuildRoad(pts, 11f, Mat("Asphalt", new Color(0.12f, 0.12f, 0.16f), 0.05f, 0.35f));
        var trackObj = new GameObject("TrackPath");
        var track = trackObj.AddComponent<TrackPath>();
        track.Build(pts.ToArray(), 11f);

        // start line
        var startTan = (pts[3] - pts[1]).normalized;
        var startLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startLine.name = "StartLine";
        startLine.transform.position = pts[2] + Vector3.up * 0.09f;
        startLine.transform.rotation = Quaternion.LookRotation(startTan);
        startLine.transform.localScale = new Vector3(11f, 0.05f, 1.6f);
        startLine.GetComponent<MeshRenderer>().sharedMaterial = Mat("StartLine", new Color(0.95f, 0.95f, 0.95f), 0f, 0.4f);
        startLine.GetComponent<BoxCollider>().enabled = false;

        var curbRed = Mat("CurbRed", new Color(0.9f, 0.08f, 0.08f), 0.1f, 0.5f);
        var curbWhite = Mat("CurbWhite", new Color(0.95f, 0.95f, 0.92f), 0.1f, 0.5f);
        for (int i = 0; i < pts.Count; i++)
        {
            var tanC = (pts[(i + 1) % pts.Count] - pts[i]).normalized;
            var sideC = new Vector3(tanC.z, 0f, -tanC.x).normalized;
            foreach (var s in new[] { -1f, 1f })
            {
                var curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = "Curb";
                curb.transform.position = pts[i] + sideC * (s * 5.9f) + Vector3.up * 0.08f;
                curb.transform.rotation = Quaternion.LookRotation(tanC);
                curb.transform.localScale = new Vector3(0.7f, 0.12f, 3.4f);
                curb.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? curbRed : curbWhite;
                curb.GetComponent<BoxCollider>().enabled = false;
            }
        }
        // start arch with key-art banner
        var archMat = Mat("Arch", new Color(0.08f, 0.1f, 0.35f), 0.3f, 0.6f);
        foreach (var s in new[] { -1f, 1f })
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "ArchPost";
            post.transform.position = pts[2] + new Vector3(1f, 0f, 0f) * 0f + Vector3.up * 3.4f + new Vector3(s * 6.4f, 0f, 0f);
            post.transform.localScale = new Vector3(0.4f, 3.4f, 0.4f);
            post.GetComponent<MeshRenderer>().sharedMaterial = archMat;
            var pc2 = post.GetComponent<Collider>(); if (pc2 != null) pc2.enabled = false;
        }
        var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = "ArchBanner";
        banner.transform.position = pts[2] + Vector3.up * 6.9f;
        banner.transform.rotation = Quaternion.LookRotation((pts[4] - pts[0]).normalized);
        banner.transform.localScale = new Vector3(13.5f, 2.2f, 0.3f);
        var keyartTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/keyart.png");
        banner.GetComponent<MeshRenderer>().sharedMaterial = Mat("ArchBanner", Color.white, 0f, 0.5f, keyartTex);
        banner.GetComponent<BoxCollider>().enabled = false;
        // flags
        var flagColors = new[] { new Color(1f, 0.3f, 0.2f), new Color(1f, 0.8f, 0.1f), new Color(0.2f, 0.7f, 1f), new Color(0.3f, 0.9f, 0.4f) };
        for (int i = 4; i < pts.Count; i += 8)
        {
            var tanF = (pts[(i + 1) % pts.Count] - pts[i]).normalized;
            var sideF = new Vector3(tanF.z, 0f, -tanF.x).normalized;
            float s = (i % 16 == 4) ? -1f : 1f;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "FlagPole";
            pole.transform.position = pts[i] + sideF * (s * 7.6f) + Vector3.up * 2.6f;
            pole.transform.localScale = new Vector3(0.09f, 2.6f, 0.09f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = archMat;
            var pc3 = pole.GetComponent<Collider>(); if (pc3 != null) pc3.enabled = false;
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.position = pole.transform.position + Vector3.up * 2.2f + new Vector3(0.55f, 0f, 0f);
            flag.transform.localScale = new Vector3(1.1f, 0.65f, 0.05f);
            flag.GetComponent<MeshRenderer>().sharedMaterial = Mat("Flag" + (i % 4), flagColors[(i / 8) % 4], 0f, 0.5f);
            flag.GetComponent<BoxCollider>().enabled = false;
        }

        // boost pads + item boxes + bananas along the loop
        var padMat = Mat("BoostPad", new Color(1f, 0.62f, 0.05f), 0.2f, 0.7f);
        padMat.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.02f) * 1.6f);
        padMat.EnableKeyword("_EMISSION");
        var boxMat = Mat("ItemBox", new Color(1f, 0.8f, 0.1f), 0.3f, 0.8f);
        var bananaMat = Mat("Banana", new Color(1f, 0.9f, 0.15f), 0f, 0.5f);
        for (int i = 6; i < pts.Count; i += 12)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "BoostPad";
            var tan = (pts[(i + 1) % pts.Count] - pts[i]).normalized;
            pad.transform.position = pts[i] + Vector3.up * 0.1f;
            pad.transform.rotation = Quaternion.LookRotation(tan);
            pad.transform.localScale = new Vector3(3.4f, 0.08f, 4.5f);
            pad.GetComponent<MeshRenderer>().sharedMaterial = padMat;
            var pc = pad.GetComponent<BoxCollider>(); pc.isTrigger = true;
            pad.AddComponent<BoostPad>();
        }
        for (int i = 10; i < pts.Count; i += 16)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "ItemBox";
            box.transform.position = pts[i] + Vector3.up * 0.8f;
            box.transform.localScale = Vector3.one * 0.85f;
            box.GetComponent<MeshRenderer>().sharedMaterial = boxMat;
            var bc = box.GetComponent<BoxCollider>(); bc.isTrigger = true;
            box.AddComponent<ItemBox>();
            var peel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            peel.name = "BananaPeel";
            int j = (i + 7) % pts.Count;
            peel.transform.position = pts[j] + Vector3.up * 0.22f;
            peel.transform.localScale = new Vector3(0.55f, 0.3f, 0.55f);
            peel.GetComponent<MeshRenderer>().sharedMaterial = bananaMat;
            peel.GetComponent<SphereCollider>().isTrigger = true;
            peel.AddComponent<BananaPeel>();
        }

        // palms + candy hills + clouds
        var trunkMat = Mat("Trunk", new Color(0.4f, 0.26f, 0.14f), 0f, 0.3f);
        var leafMat = Mat("Leaves", new Color(0.12f, 0.55f, 0.22f), 0f, 0.3f);
        for (int i = 0; i < pts.Count; i += 6)
        {
            var tan = (pts[(i + 1) % pts.Count] - pts[i]).normalized;
            var side = new Vector3(tan.z, 0f, -tan.x).normalized;
            float s = (i % 12 == 0) ? -1f : 1f;
            var basePos = pts[i] + side * (s * 10.5f);
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "PalmTrunk";
            trunk.transform.position = basePos + Vector3.up * 2.2f;
            trunk.transform.rotation = Quaternion.Euler(s * 7f, 0f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 2.4f, 0.35f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMat;
            var tc = trunk.GetComponent<Collider>(); if (tc != null) tc.enabled = false;
            for (int k = 0; k < 3; k++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "PalmLeaf";
                leaf.transform.position = basePos + Vector3.up * 4.6f + new Vector3(Mathf.Sin(k * 2.1f) * 1.1f, 0.25f * k, Mathf.Cos(k * 2.1f) * 1.1f);
                leaf.transform.localScale = new Vector3(1.7f, 0.65f, 1.7f);
                leaf.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
                var lc = leaf.GetComponent<Collider>(); if (lc != null) lc.enabled = false;
            }
        }
        var hillMats = new[] { Mat("HillPink", new Color(0.9f, 0.45f, 0.6f), 0f, 0.25f), Mat("HillMint", new Color(0.35f, 0.8f, 0.6f), 0f, 0.25f), Mat("HillSky", new Color(0.4f, 0.6f, 0.95f), 0f, 0.25f) };
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f + 0.4f;
            var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name = "CandyHill";
            hill.transform.position = new Vector3(Mathf.Cos(a) * 128f, -7f, Mathf.Sin(a) * 128f);
            hill.transform.localScale = new Vector3(52f, 22f, 52f);
            hill.GetComponent<MeshRenderer>().sharedMaterial = hillMats[i % 3];
            var hc = hill.GetComponent<Collider>(); if (hc != null) hc.enabled = false;
        }
        var cloudMat = Mat("Cloud", new Color(1f, 1f, 1f), 0f, 0.15f);
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f + 0.9f;
            var cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "Cloud";
            cloud.transform.position = new Vector3(Mathf.Cos(a) * 95f, 26f + (i % 3) * 4f, Mathf.Sin(a) * 95f);
            cloud.transform.localScale = new Vector3(13f, 3.6f, 8f);
            cloud.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;
            var cc = cloud.GetComponent<Collider>(); if (cc != null) cc.enabled = false;
        }

        var kartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/kartopia_kart.glb");
        var gridTan = (pts[4] - pts[0]).normalized;
        var gridRot = Quaternion.LookRotation(new Vector3(gridTan.x, 0f, gridTan.z).normalized);
        var gridSide = new Vector3(gridTan.z, 0f, -gridTan.x).normalized;

        var player = MakeKart("Kart", kartPrefab, pts[2] + Vector3.up * 0.55f - gridTan * 5f, gridRot);
        player.AddComponent<KartController>();

        var aiNames = new[] { "RUBY", "MINT", "SUNNY" };
        var aiCars = new List<Transform>();
        for (int i = 0; i < 3; i++)
        {
            var pos = pts[2] + Vector3.up * 0.55f + gridTan * (4f + i * 6f) + gridSide * ((i % 2 == 0 ? -1f : 1f) * 2.6f);
            var ai = MakeKart("AI_" + aiNames[i], kartPrefab, pos, gridRot);
            var driver = ai.AddComponent<AiDriver>();
            driver.player = player.transform;
            driver.track = track;
            driver.maxSpeed = 24f + i * 1.1f;
            driver.acceleration = 17f;
            driver.aggression = 0.94f + i * 0.04f;
            aiCars.Add(ai.transform);
        }

        var raceObj = new GameObject("RaceManager");
        var race = raceObj.AddComponent<RaceManager>();
        race.player = player.transform;
        race.aiCars = aiCars;
        race.track = track;
        race.totalLaps = 3;
        race.musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/race_music.wav");
        var hud = raceObj.AddComponent<Hud>();
        hud.race = race;
        hud.kart = player.GetComponent<KartController>();
        hud.kartBody = player.GetComponent<Rigidbody>();

        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = pts[2] - gridTan * 9f + Vector3.up * 4.5f;
        var chase = cam.gameObject.AddComponent<ChaseCamera>();
        chase.target = player.transform;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/KartTrack.unity");

        var menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var menuCam = new GameObject("MenuCamera").AddComponent<Camera>();
        menuCam.tag = "MainCamera";
        menuCam.backgroundColor = new Color(0.35f, 0.65f, 0.95f);
        var menu = new GameObject("MainMenu").AddComponent<MenuUI>();
        menu.backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/keyart.png");
        new GameObject("Updater").AddComponent<Updater>();
        EditorSceneManager.SaveScene(menuScene, "Assets/Scenes/MainMenu.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/KartTrack.unity", true)
        };

        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.Android.bundleVersionCode = 2;
        PlayerSettings.companyName = "MaximoSEO";
        PlayerSettings.productName = "Kartopia GP";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.maximoseo.kartopia");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        var androidPlayer = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
        EditorPrefs.SetString("AndroidSdkRoot", Path.Combine(androidPlayer, "SDK"));
        EditorPrefs.SetString("AndroidNdkRoot", Path.Combine(androidPlayer, "NDK"));
        EditorPrefs.SetString("JdkPath", Path.Combine(androidPlayer, "OpenJDK"));
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/KartTrack.unity" },
            locationPathName = "Builds/KartopiaGP.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log($"BUILD_RESULT: {report.summary.result} size={report.summary.totalSize} errors={report.summary.totalErrors}");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
