using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    static readonly Color[] KartPaintColors =
    {
        new Color(0.91f, 0.22f, 0.16f), new Color(0.15f, 0.5f, 0.95f),
        new Color(0.2f, 0.8f, 0.35f), new Color(0.98f, 0.65f, 0.1f)
    };
    static int _kartPaintIndex;

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
            var paintColor = KartPaintColors[_kartPaintIndex++ % KartPaintColors.Length];
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                string objectName = renderer.gameObject.name;
                if (objectName.StartsWith("Wheel_tyre") || objectName.StartsWith("Wheel_tread"))
                    renderer.sharedMaterial = tyreMat;
                else if (objectName.StartsWith("Wheel_"))
                    renderer.sharedMaterial = rimMat;
                else if (renderer.sharedMaterial != null && renderer.sharedMaterial.name.Contains("KartPaint"))
                {
                    var tinted = new Material(renderer.sharedMaterial);
                    tinted.SetColor("_BaseColor", paintColor);
                    renderer.sharedMaterial = tinted;
                }
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

        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.35f; bloom.threshold.value = 0.85f;
            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.saturation.value = 18f; colorAdj.contrast.value = 6f;
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.22f;
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;
            Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.CreateAsset(profile, "Assets/Settings/KartopiaVolumeProfile.asset");
            var volumeObj = new GameObject("GlobalVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

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

        var roadTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/road.png");
        var asphalt = Mat("Asphalt", new Color(0.55f, 0.55f, 0.62f), 0.05f, 0.35f, roadTex);
        if (roadTex != null) asphalt.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
        BuildRoad(pts, 11f, asphalt);
        // ocean + beach background
        var ocean = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ocean.name = "Ocean";
        ocean.transform.position = new Vector3(0f, -1.7f, 0f);
        ocean.transform.localScale = new Vector3(190f, 0.5f, 190f);
        var waterTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/water.png");
        var oceanMat = Mat("Ocean", Color.white, 0.1f, 0.7f, waterTex);
        if (waterTex != null) oceanMat.SetTextureScale("_BaseMap", new Vector2(22f, 22f));
        ocean.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;
        ocean.GetComponent<Collider>().enabled = false;
        var sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/sand.png");
        var sandMat = Mat("Beach", Color.white, 0f, 0.3f, sandTex);
        {
            int n2 = pts.Count;
            var verts = new Vector3[n2 * 2];
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n2; i++)
            {
                var tanB = (pts[(i + 1) % n2] - pts[(i - 1 + n2) % n2]).normalized;
                var sideB = new Vector3(tanB.z, 0f, -tanB.x).normalized;
                verts[i * 2] = pts[i] - sideB * 10.5f + Vector3.up * 0.01f;
                verts[i * 2 + 1] = pts[i] + sideB * 10.5f + Vector3.up * 0.01f;
                int a = i * 2, b = ((i + 1) % n2) * 2;
                tris.Add(a); tris.Add(b); tris.Add(a + 1);
                tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
            }
            var beachMesh = new Mesh { name = "BeachRibbon" };
            var beachUvs = new Vector2[n2 * 2];
            for (int i2 = 0; i2 < n2; i2++) { beachUvs[i2 * 2] = new Vector2(0f, i2 * 0.35f); beachUvs[i2 * 2 + 1] = new Vector2(1f, i2 * 0.35f); }
            beachMesh.vertices = verts; beachMesh.uv = beachUvs; beachMesh.triangles = tris.ToArray();
            beachMesh.RecalculateNormals();
            var beach = new GameObject("Beach");
            beach.AddComponent<MeshFilter>().sharedMesh = beachMesh;
            beach.AddComponent<MeshRenderer>().sharedMaterial = sandMat;
        }
        var tirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/tire_stack.glb");
        var conePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/cone.glb");
        var lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/lighthouse.glb");
        for (int i = 3; i < pts.Count; i += 9)
        {
            var tanT = (pts[(i + 1) % pts.Count] - pts[i]).normalized;
            var sideT = new Vector3(tanT.z, 0f, -tanT.x).normalized;
            float s = (i % 18 == 3) ? -1f : 1f;
            if (tirePrefab != null)
            {
                var tires = Object.Instantiate(tirePrefab);
                tires.name = "TireStack";
                tires.transform.position = pts[i] + sideT * (s * 8.2f);
                tires.transform.localScale = Vector3.one * 1.3f;
            }
            if (conePrefab != null)
            {
                var cone = Object.Instantiate(conePrefab);
                cone.name = "Cone";
                cone.transform.position = pts[i] + sideT * (-s * 4.6f) + Vector3.up * 0.05f;
                cone.transform.localScale = Vector3.one * 1.2f;
            }
        }
        if (lightPrefab != null)
        {
            var light1 = Object.Instantiate(lightPrefab);
            light1.name = "Lighthouse";
            light1.transform.position = new Vector3(-118f, -1f, 60f);
            light1.transform.localScale = Vector3.one * 1.6f;
            var light2 = Object.Instantiate(lightPrefab);
            light2.name = "Lighthouse2";
            light2.transform.position = new Vector3(105f, -1f, -95f);
            light2.transform.rotation = Quaternion.Euler(0f, 140f, 0f);
            light2.transform.localScale = Vector3.one * 1.3f;
        }
        var hutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/beach_hut.glb");
        var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/rocks.glb");
        for (int i = 8; i < pts.Count; i += 24)
        {
            var tanH = (pts[(i + 1) % pts.Count] - pts[i]).normalized;
            var sideH = new Vector3(tanH.z, 0f, -tanH.x).normalized;
            float sH = (i % 48 == 8) ? -1f : 1f;
            if (hutPrefab != null)
            {
                var hut = Object.Instantiate(hutPrefab);
                hut.name = "BeachHut";
                hut.transform.position = pts[i] + sideH * (sH * 15f);
                hut.transform.rotation = Quaternion.LookRotation(-sideH * sH);
                hut.transform.localScale = Vector3.one * 1.5f;
            }
            if (rockPrefab != null)
            {
                var rocks = Object.Instantiate(rockPrefab);
                rocks.name = "Rocks";
                rocks.transform.position = pts[(i + 5) % pts.Count] + sideH * (-sH * 13.5f);
                rocks.transform.rotation = Quaternion.Euler(0f, i * 29f, 0f);
                rocks.transform.localScale = Vector3.one * 1.6f;
            }
        }

        // sand slow zones (risk/reward patches on two corners)
        var sandZoneMat = Mat("SandZone", new Color(0.85f, 0.68f, 0.38f, 0.9f), 0f, 0.2f);
        foreach (var zi in new[] { 24, 63 })
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = "SandZone";
            var tanZ = (pts[(zi + 1) % pts.Count] - pts[zi]).normalized;
            zone.transform.position = pts[zi] + Vector3.up * 0.12f;
            zone.transform.rotation = Quaternion.LookRotation(tanZ);
            zone.transform.localScale = new Vector3(8.5f, 0.1f, 7f);
            zone.GetComponent<MeshRenderer>().sharedMaterial = sandZoneMat;
            zone.GetComponent<BoxCollider>().isTrigger = true;
            zone.AddComponent<SlowZone>();
        }

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
        var gantryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/start_gantry.glb");
        if (gantryPrefab != null)
        {
            var gantry = Object.Instantiate(gantryPrefab);
            gantry.name = "StartGantry";
            gantry.transform.position = pts[2] + Vector3.up * 0.02f;
            gantry.transform.rotation = Quaternion.LookRotation((pts[4] - pts[0]).normalized);
            gantry.transform.localScale = Vector3.one * 1.02f;
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
            flag.AddComponent<FlagWave>();
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

        var palmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/palm_tree.glb");
        var hillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/candy_hill.glb");
        var cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/cloud_cluster.glb");
        var standPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/grandstand.glb");
        // Blender environment kit: palms, candy hills, clouds, grandstands
        for (int i = 0; i < pts.Count; i += 6)
        {
            if (palmPrefab == null) break;
            var tanP = (pts[(i + 1) % pts.Count] - pts[i]).normalized;
            var sideP = new Vector3(tanP.z, 0f, -tanP.x).normalized;
            float s = (i % 12 == 0) ? -1f : 1f;
            var palm = Object.Instantiate(palmPrefab);
            palm.name = "PalmTree";
            palm.transform.position = pts[i] + sideP * (s * 10.5f);
            palm.transform.rotation = Quaternion.Euler(0f, i * 37f, 0f);
            palm.transform.localScale = Vector3.one * (1.15f + (i % 3) * 0.18f);
        }
        for (int i = 0; i < 6; i++)
        {
            if (hillPrefab == null) break;
            float a = i / 6f * Mathf.PI * 2f + 0.4f;
            var hill = Object.Instantiate(hillPrefab);
            hill.name = "CandyHill";
            hill.transform.position = new Vector3(Mathf.Cos(a) * 128f, -1.2f, Mathf.Sin(a) * 128f);
            hill.transform.rotation = Quaternion.Euler(0f, i * 53f, 0f);
            hill.transform.localScale = Vector3.one * (5.2f + (i % 3) * 1.1f);
        }
        for (int i = 0; i < 7; i++)
        {
            if (cloudPrefab == null) break;
            float a = i / 7f * Mathf.PI * 2f + 0.9f;
            var cloud = Object.Instantiate(cloudPrefab);
            cloud.name = "CloudCluster";
            cloud.transform.position = new Vector3(Mathf.Cos(a) * 95f, 26f + (i % 3) * 4f, Mathf.Sin(a) * 95f);
            cloud.transform.rotation = Quaternion.Euler(0f, i * 41f, 0f);
            cloud.transform.localScale = Vector3.one * 3.4f;
            cloud.AddComponent<CloudDrift>();
        }
        for (int i = 0; i < 2; i++)
        {
            if (standPrefab == null) break;
            int at = i == 0 ? 14 : 52;
            var tanS = (pts[(at + 1) % pts.Count] - pts[at]).normalized;
            var sideS = new Vector3(tanS.z, 0f, -tanS.x).normalized;
            var stand = Object.Instantiate(standPrefab);
            stand.name = "Grandstand";
            stand.transform.position = pts[at] + sideS * -16f;
            stand.transform.rotation = Quaternion.LookRotation(sideS);
            stand.transform.localScale = Vector3.one * 1.5f;
        }

        var kartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/kartopia_kart.glb");
        var gridTan = (pts[4] - pts[0]).normalized;
        var gridRot = Quaternion.LookRotation(new Vector3(gridTan.x, 0f, gridTan.z).normalized);
        var gridSide = new Vector3(gridTan.z, 0f, -gridTan.x).normalized;

        var player = MakeKart("Kart", kartPrefab, pts[2] + Vector3.up * 0.55f - gridTan * 5f, gridRot);
        var playerController = player.AddComponent<KartController>();
        playerController.engineClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/engine_loop.wav");

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
        menu.logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/icon_1024.png");
        new GameObject("Updater").AddComponent<Updater>();
        EditorSceneManager.SaveScene(menuScene, "Assets/Scenes/MainMenu.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/KartTrack.unity", true)
        };

        PlayerSettings.bundleVersion = "0.6.0";
        PlayerSettings.Android.bundleVersionCode = 9;
        var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/icon_1024.png");
        if (appIcon != null)
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { appIcon });
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
