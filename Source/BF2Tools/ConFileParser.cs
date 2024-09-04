using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static BF2FileManager;
using static MapLoader;
using static CWModUtility;
using UnityEditor;
using System.Text;
using System.Text.RegularExpressions;
using static TerrainLoader;

public class ConFileParser : MonoBehaviour
{
    public static string LastConPath = "";

    public static Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
    public static void ParseCon(string path)
    {
        // Read lines one by one to avoid loading entire file into memory
        LastConPath = path;
        int BlockState = 0;
        foreach (var line in FileManager.ReadLines(path))
        {
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            if (line.Equals("if v_arg1 == BF2Editor")) { BlockState = 1; }
            if (line.Equals("else")) BlockState = 2;
            if (line.Equals("endIf")) BlockState = 0;
            if (BlockState == 2) continue;

            // Split line into parts
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int Length = parts.Length;
            if (Length == 0) continue;


            else if (parts[0] == "fileManager.mountArchive")
            {
                if (Length == 3)
                {
                    var archivePath = Path.Combine(Root, parts[1]);
                    FileManager.Mount(archivePath, parts[2]);
                }
            }
            else if (parts[0] == "run")
            {
                if (FileManager.Exists(parts[1]))
                    ParseCon(parts[1]);
                else Debug.LogError($"Failed To Find Con File : {parts[1]}");
                //return;
            }
            else if (parts[0] == "heightmapcluster.create")
            {
                new GameObject(parts[1]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.TerrainCluster);
                Bf2ObjectTemplate.Instance.transform.parent = null;
                Bf2ObjectTemplate.TerrainClusterName = parts[1];
                Bf2ObjectTemplate.Instance.transform.parent = MapLoader.Instance.SceneRoot.transform;
            }
            else if (parts[0] == "heightmapcluster.setHeightmapSize")
            {
                Bf2ObjectTemplate.Instance.transform.localScale *= int.Parse(parts[1]);
                Bf2ObjectTemplate.Instance.transform.position = -new Vector3(int.Parse(parts[1]) / 2f, 0, int.Parse(parts[1]) / 2f);
            }
            else if (parts[0] == "heightmapcluster.addHeightmap")
            {
                Bf2ObjectTemplate.Instance.AddTerrain(new Vector2Int(int.Parse(parts[2]), int.Parse(parts[3])));

            }
            else if (parts[0] == "heightmap.setSize")
            {
                Bf2ObjectTemplate.Instance.CurrentTerrainData.heightmapResolution = int.Parse(parts[1]);
            }
            else if (parts[0] == "heightmap.setScale")
            {
                TerrainData TD = Bf2ObjectTemplate.Instance.CurrentTerrainData;
                string[] V3 = parts[1].Split('/');

                TD.size = new Vector3((TD.heightmapResolution * float.Parse(V3[0])), float.Parse(V3[1]), (TD.heightmapResolution * float.Parse(V3[2])));
            }
            else if (parts[0] == "heightmap.setBitResolution")
            {
                Bf2ObjectTemplate.Instance.terrainBitResolutions[Bf2ObjectTemplate.Instance.CurrentTerrainIndex] = int.Parse(parts[1]);

                //TD.size=new Vector3((TD.heightmapResolution*float.Parse(V3[0]))/16,ushort.MaxValue*float.Parse(V3[1]),(TD.heightmapResolution*float.Parse(V3[2])/16));
            }
            else if (parts[0] == "heightmap.loadHeightData")
            {
                TerrainData TD = Bf2ObjectTemplate.Instance.CurrentTerrainData;
                TD.LoadRawHeightmap(FileManager.ReadAllBytes(parts[1]), TD.heightmapResolution, Bf2ObjectTemplate.Instance.terrainBitResolutions[Bf2ObjectTemplate.Instance.CurrentTerrainIndex]);
                Bf2ObjectTemplate.Instance.LoadCurrentTerrain();
            }
            else if (parts[0] == "Lightmanager.sunColor")
            {

                /*LightmapData[] LMDs = new LightmapData[1];
                for (int i = 0; i < LMDs.Length; i++)
                {
                    LMDs[i] = new LightmapData();
                    LMDs[i].lightmapColor = new Texture2D(2, 2);
                    LMDs[i].lightmapDir = null;
                    LMDs[i].shadowMask = LoadImageFromRaw("SimpleShadowmap.raw");
                }
                LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
                LightmapSettings.lightmaps = LMDs;

                DynamicGI.UpdateEnvironment();*/

                new GameObject(parts[1]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Light);
                Bf2ObjectTemplate.Instance.transform.parent = MapLoader.Instance.SceneRoot.transform;

                Bf2ObjectTemplate.Instance.gameObject.name = "StaticLight";
                Light light = Bf2ObjectTemplate.Instance.gameObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.Soft;
                string[] Col = parts[1].Split('/');
                Vector3 colVec = new Vector3(float.Parse(Col[0]), float.Parse(Col[1]), float.Parse(Col[2]));
                float intensity = Mathf.Max(Mathf.Ceil(colVec.x), Mathf.Ceil(colVec.y), Mathf.Ceil(colVec.z));
                light.color = new Color(colVec.x / intensity, colVec.y / intensity, colVec.z / intensity);
                light.intensity = intensity;
                light.lightmapBakeType = LightmapBakeType.Mixed;
                LightBakingOutput bakeOutput = new LightBakingOutput();
                bakeOutput.isBaked = true;
                bakeOutput.lightmapBakeType = LightmapBakeType.Mixed;
                bakeOutput.mixedLightingMode = MixedLightingMode.Shadowmask;
                light.bakingOutput = bakeOutput;
            }
            else if (parts[0] == "Lightmanager.sunDirection")
            {
                string[] Col = parts[1].Split('/');
                Vector3 RFDir = new Vector3(float.Parse(Col[0]), float.Parse(Col[1]), float.Parse(Col[2]));

                Vector3 euler = Quaternion.LookRotation(RFDir.normalized).eulerAngles;
                euler.y *= 1.051836f;
                Bf2ObjectTemplate.Instance.transform.eulerAngles = euler;
            }
            else if (parts[0] == "Skydome.skyTemplate")
            {
                RenderSettings.skybox = new Material(Shader.Find("BF2/Skybox"));
            }
            else if (parts[0] == "Skydome.cloudTexture")
            {
                RenderSettings.skybox.SetTexture("_CloudTex", DDSLoader.LoadDDSTexture(parts[1]));
            }
            else if (parts[0] == "Skydome.skyTexture")
            {
                RenderSettings.skybox.mainTexture = DDSLoader.LoadDDSTexture(parts[1]);
            }
            else if (parts[0] == "Skydome.domeRotation")
            {
                RenderSettings.skybox.SetFloat("_Rotation", float.Parse(parts[1]) - 19);
            }
            else if (parts[0] == "Renderer.fogColor")
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                string[] Col = parts[1].Split('/');
                RenderSettings.fogColor = new Color(float.Parse(Col[0]) / 255, float.Parse(Col[1]) / 255, float.Parse(Col[2]) / 255);
            }
            else if (parts[0] == "Renderer.fogStartEndAndBase")
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                string[] Col = parts[1].Split('/');
                RenderSettings.fogStartDistance = float.Parse(Col[0]);
                RenderSettings.fogEndDistance = float.Parse(Col[1]);
            }
            else if (parts[0] == "Skydome.flareTexture")
            {
                if (Camera.main != null && !Camera.main.GetComponent<FlareLayer>()) Camera.main.gameObject.AddComponent<FlareLayer>();

                Light light = GameObject.Find("StaticLight").GetComponent<Light>();
                Flare flare = AssetDatabase.LoadAssetAtPath<Flare>($"Assets/Cache/{MapLoader.CSelectedLevel}_Flare.flare");

                if (!flare)
                {
                    flare = new Flare();
                    AssetDatabase.CreateAsset(flare, $"Assets/Cache/{MapLoader.CSelectedLevel}_Flare.flare");
                }
                AssetDatabase.Refresh();
                flare.name = MapLoader.CSelectedLevel + "_Flare";
                Texture2D TX = DDSLoader.LoadDDSTexture(parts[1]);

                SerializedObject serializedFlare = new SerializedObject(flare);
                serializedFlare.FindProperty("m_FlareTexture").objectReferenceValue = TX;
                serializedFlare.FindProperty("m_TextureLayout").intValue = 2;
                serializedFlare.FindProperty("m_Elements").GetArrayElementAtIndex(0).FindPropertyRelative("m_Size").floatValue = 75f;
                serializedFlare.FindProperty("m_Elements").GetArrayElementAtIndex(0).FindPropertyRelative("m_UseLightColor").boolValue = false;
                serializedFlare.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                // Assign the created Flare to the Lens Flare component
                light.flare = flare;

            }
            else if (parts[0] == "Undergrowth.load")
            {
                GrowthLoader.Instance.LoadUnderGrowth(MapLoader.PrimaryTerrain, parts[1]);
            }
            else if (parts[0] == "terrain.patchSize")
            {
                GameObject HeightmapCluster = GameObject.Find(Bf2ObjectTemplate.TerrainClusterName);
                foreach (Transform T in HeightmapCluster.transform)
                {
                    Terrain Terr = T.GetComponent<Terrain>();
                    TerrainLoader.Instance.AddTerrain(Terr, Terr == MapLoader.PrimaryTerrain ? int.Parse(parts[1]) : Terr.terrainData.heightmapResolution);
                    if (Terr == MapLoader.PrimaryTerrain) Terr.drawHeightmap = false;
                    else
                    {
                        Terr.drawHeightmap = false;

                        DestroyImmediate(Terr.terrainData);
                        Terrain.DestroyImmediate(Terr);
                        TerrainCollider.DestroyImmediate(T.GetComponent<TerrainCollider>());
                    }
                }
                if (FileManager.Exists("CompiledRoads.con")) ParseCon("CompiledRoads.con");
            }
            else if (parts[0] == "terrain.colormapBaseName")
            {
                parts[1] = parts[1].Replace("\"", "");
                foreach (TerrainLoader.TerrainInfo TI in TerrainLoader.Instance.terrains)
                {
                    if (TI.ID != 0)
                    {
                        int MatID = TI.ID == 1 ? 6 : TI.ID == 2 ? 5 : TI.ID == 3 ? 4 : TI.ID == 4 ? 7 : TI.ID == 5 ? 3 : TI.ID == 6 ? 0 : TI.ID == 7 ? 1 : 2;
                        TI.Mats[0].SetTexture("_MainTex", DDSLoader.LoadDDSTexture(parts[1] + "_s" + (MatID)));
                    }
                    else
                    {
                        int sqrt = (int)Mathf.Sqrt(TI.Mats.Length);
                        for (int x = 0; x < sqrt; x++)
                            for (int y = 0; y < sqrt; y++)
                            {
                                int i = x * sqrt + y;
                                TI.Mats[i].SetTexture("_MainTex", DDSLoader.LoadDDSTexture(parts[1] + x.ToString("D2") + "x" + y.ToString("D2")));

                            }
                    }
                }
            }
            else if (parts[0] == "terrain.lightmapBaseName")
            {
                parts[1] = parts[1].Replace("\"", "");
                foreach (TerrainLoader.TerrainInfo TI in TerrainLoader.Instance.terrains)
                {
                    if (TI.ID != 0)
                    {
                        int MatID = TI.ID == 1 ? 6 : TI.ID == 2 ? 5 : TI.ID == 3 ? 4 : TI.ID == 4 ? 7 : TI.ID == 5 ? 3 : TI.ID == 6 ? 0 : TI.ID == 7 ? 1 : 2;
                        TI.Mats[0].SetTexture("_LightMap", DDSLoader.LoadDDSTexture(parts[1] + "_s" + (MatID)));
                    }
                    else
                    {
                        int sqrt = (int)Mathf.Sqrt(TI.Mats.Length);
                        for (int x = 0; x < sqrt; x++)
                            for (int y = 0; y < sqrt; y++)
                            {
                                int i = x * sqrt + y;
                                TI.Mats[i].SetTexture("_LightMap", DDSLoader.LoadDDSTexture(parts[1] + x.ToString("D2") + "x" + y.ToString("D2")));

                            }
                    }
                }
            }
            else if (parts[0] == "terrain.detailmapBaseName")
            {
                parts[1] = parts[1].Replace("\"", "");

                BF2TerrainData TerrData = TerrainLoader.Instance.ReadTerrainData("terraindata.raw");
                Debug.Log("Extracted Paths:");

                foreach (TerrainLoader.TerrainInfo TI in TerrainLoader.Instance.terrains)
                {


                    if (TI.ID != 0)
                    {
                        int MatID = TI.ID == 1 ? 6 : TI.ID == 2 ? 5 : TI.ID == 3 ? 4 : TI.ID == 4 ? 7 : TI.ID == 5 ? 3 : TI.ID == 6 ? 0 : TI.ID == 7 ? 1 : 2;
                        TI.Mats[0].SetTexture("_DetailtMap1", DDSLoader.LoadDDSTexture(parts[1] + "_s" + (MatID) + "_1"));
                        TI.Mats[0].SetTexture("_DetailtMap2", DDSLoader.LoadDDSTexture(parts[1] + "_s" + (MatID) + "_2"));

                        TI.Mats[0].SetTexture("_Layer_1", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[0].Path));
                        TI.Mats[0].SetFloat("_Scale1", TerrData.TerrainMaterials[0].TopTilling);

                        TI.Mats[0].SetTexture("_Layer_2", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[1].Path));
                        TI.Mats[0].SetFloat("_Scale2", TerrData.TerrainMaterials[1].TopTilling);

                        TI.Mats[0].SetTexture("_Layer_3", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[2].Path));
                        TI.Mats[0].SetFloat("_Scale3", TerrData.TerrainMaterials[2].TopTilling);

                        TI.Mats[0].SetTexture("_Layer_4", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[3].Path));
                        TI.Mats[0].SetFloat("_Scale4", TerrData.TerrainMaterials[3].TopTilling);

                        TI.Mats[0].SetTexture("_Layer_5", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[4].Path));
                        TI.Mats[0].SetFloat("_Scale5", TerrData.TerrainMaterials[4].TopTilling);

                        TI.Mats[0].SetTexture("_Layer_6", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[5].Path));
                        TI.Mats[0].SetFloat("_Scale6", TerrData.TerrainMaterials[5].TopTilling);
                    }
                    else
                    {
                        int sqrt = (int)Mathf.Sqrt(TI.Mats.Length);
                        for (int x = 0; x < sqrt; x++)
                        {
                            for (int y = 0; y < sqrt; y++)
                            {
                                int i = x * sqrt + y;
                                TI.Mats[i].SetTexture("_DetailtMap1", DDSLoader.LoadDDSTexture(parts[1] + x.ToString("D2") + "x" + y.ToString("D2") + "_1"));
                                TI.Mats[i].SetTexture("_DetailtMap2", DDSLoader.LoadDDSTexture(parts[1] + x.ToString("D2") + "x" + y.ToString("D2") + "_2"));

                                TI.Mats[i].SetTexture("_Layer_1", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[0].Path));
                                TI.Mats[i].SetFloat("_Scale1", TerrData.TerrainMaterials[0].TopTilling);

                                TI.Mats[i].SetTexture("_Layer_2", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[1].Path));
                                TI.Mats[i].SetFloat("_Scale2", TerrData.TerrainMaterials[1].TopTilling);

                                TI.Mats[i].SetTexture("_Layer_3", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[2].Path));
                                TI.Mats[i].SetFloat("_Scale3", TerrData.TerrainMaterials[2].TopTilling);

                                TI.Mats[i].SetTexture("_Layer_4", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[3].Path));
                                TI.Mats[i].SetFloat("_Scale4", TerrData.TerrainMaterials[3].TopTilling);

                                TI.Mats[i].SetTexture("_Layer_5", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[4].Path));
                                TI.Mats[i].SetFloat("_Scale5", TerrData.TerrainMaterials[4].TopTilling);

                                TI.Mats[i].SetTexture("_Layer_6", DDSLoader.LoadDDSTexture(TerrData.TerrainMaterials[5].Path));
                                TI.Mats[i].SetFloat("_Scale6", TerrData.TerrainMaterials[5].TopTilling);
                            }
                        }
                    }
                }
            }
            else if (parts[0] == "terrain.lowDetailmapBaseName")
            {
                parts[1] = parts[1].Replace("\"", "");
                foreach (TerrainLoader.TerrainInfo TI in TerrainLoader.Instance.terrains)
                {
                    if (TI.ID != 0)
                    {
                        int MatID = TI.ID == 1 ? 6 : TI.ID == 2 ? 5 : TI.ID == 3 ? 4 : TI.ID == 4 ? 7 : TI.ID == 5 ? 3 : TI.ID == 6 ? 0 : TI.ID == 7 ? 1 : 2;
                        TI.Mats[0].SetTexture("_LowDetailtMap", DDSLoader.LoadDDSTexture(parts[1] + "_s" + (MatID)));
                        TI.Mats[0].SetTexture("_LowDetailLayer", DDSLoader.LoadDDSTexture("lowdetailtexture.dds"));
                    }
                    else
                    {
                        int sqrt = (int)Mathf.Sqrt(TI.Mats.Length);
                        for (int x = 0; x < sqrt; x++)
                            for (int y = 0; y < sqrt; y++)
                            {
                                int i = x * sqrt + y;
                                TI.Mats[i].SetTexture("_LowDetailtMap", DDSLoader.LoadDDSTexture(parts[1] + x.ToString("D2") + "x" + y.ToString("D2")));
                                TI.Mats[i].SetTexture("_LowDetailLayer", DDSLoader.LoadDDSTexture("lowdetailtexture.dds"));

                            }
                    }
                }
            }

            else if (parts[0] == "object.create")
            {
                GameObject StageRoads;
                if (!GameObject.Find("Roads"))
                { StageRoads = new GameObject("Roads"); StageRoads.transform.parent = MapLoader.Instance.SceneRoot.transform; }
                else
                    StageRoads = GameObject.Find("Roads");

                string name = parts[1];
                new GameObject(name).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Road);
                Bf2ObjectTemplate.Instance.transform.parent = StageRoads.transform;
                Bf2ObjectTemplate.Instance.roadSettings = new Bf2ObjectTemplate.RoadSettings(Path.Combine("/objects/roads/splines", parts[1] + ".con"));

            }
            else if (parts[0] == "object.geometry.loadMesh")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    string name = parts[1];
                    GameObject RoadTemplate = RoadLoader.GetRoadTemplate(Path.GetFileNameWithoutExtension(name));
                    if (RoadTemplate == null)
                    {
                        RoadLoader.Instance.LoadBF2Road(name);
                        RoadTemplate = RoadLoader.GetRoadTemplate(Path.GetFileNameWithoutExtension(name));
                    }
                    if (RoadTemplate != null)
                    {
                        RoadTemplate = UnityEngine.Object.Instantiate(RoadLoader.GetRoadTemplate(Path.GetFileNameWithoutExtension(name)));
                        RoadTemplate.transform.parent = Bf2ObjectTemplate.Instance.transform;
                        Material Mat = RoadTemplate.GetComponent<Renderer>().sharedMaterial;
                        Mat.shader = Shader.Find("BF2/Road");
                        Mat.SetTexture("_MainTex", DDSLoader.LoadDDSTexture(Bf2ObjectTemplate.Instance.roadSettings.PrimaryTexturePath));
                        Mat.SetTextureScale("_MainTex", Bf2ObjectTemplate.Instance.roadSettings.PrimaryTextureScale);

                        RoadTemplate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                        RoadTemplate.GetComponent<Renderer>().sharedMaterial.SetTexture("_Detail", DDSLoader.LoadDDSTexture(Bf2ObjectTemplate.Instance.roadSettings.SecondaryTexturePath));
                        Mat.SetTextureScale("_Detail", Bf2ObjectTemplate.Instance.roadSettings.SecondaryTextureScale);
                        //RoadTemplate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    }
                }
            }
            else
            if (parts[0] == "GeometryTemplate.create")
            {
                if (Length == 3)
                {
                    if (parts[1].Equals("StaticMesh") || parts[1].Equals("BundledMesh"))
                    {
                        string ObjectRoot = Path.GetDirectoryName(path);
                        MeshLoader.Instance.LoadBF2Mesh(Path.Combine(ObjectRoot, "meshes", parts[2] + "." + parts[1].ToLower())).ToGameObject();
                    }
                    else
                    {
                        Debug.LogError($"Unknown Type Of GeometryTemplate.create : {parts[1]}");
                        continue;
                    }
                }
            }
            else if (parts[0] == "CollisionManager.createTemplate")
            {
                if (Length == 2)
                {
                    string ObjectRoot = Path.GetDirectoryName(path);
                    ColliderLoader.Instance.LoadBF2Col(Path.Combine(ObjectRoot, "meshes", parts[1] + ".collisionmesh")).ToGameObject();
                }
            }
            else if (parts[0].Equals("gameLogic.setBeforeSpawnCamera", StringComparison.OrdinalIgnoreCase))
            {
                if (Bf2ObjectTemplate.Instance == null) continue;
                //Debug.Log("Original Vector3: " + parts[1]);
                string[] PosSplitString = parts[1].Split('/');
                string[] RotSplitString = parts[2].Split('/');

                Vector3 Pos = new Vector3(float.Parse(PosSplitString[0]), float.Parse(PosSplitString[1]), float.Parse(PosSplitString[2]));
                Vector3 Rot = new Vector3(float.Parse(RotSplitString[1]), float.Parse(RotSplitString[0]), float.Parse(RotSplitString[2]));

                ModMapManager.Instance.GetComponentInChildren<Camera>().transform.SetPositionAndRotation(Pos, Quaternion.Euler(Rot));
                //Debug.Log("Converted Vector3: " + new Vector3(x, y, z));

            }
            else if (parts[0] == "heightmapcluster.setSeaWaterLevel")
            {
                foreach (Transform t in ModMapManager.Instance.transform)
                {
                    if (t.name.ToLower().Contains("water")) t.position = new Vector3(t.position.x, float.Parse(parts[1]), t.position.z);
                }
            }
            else if (parts[0] == "ObjectTemplate.create")
            {
                if (parts[1].Equals("SimpleObject"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.SimpleObject);
                }
                else if (parts[1].Equals("Bundle"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Bundle);
                }
                else if (parts[1].Equals("Ladder"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Bundle);
                }
                else if (parts[1].Equals("SupplyObject"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Bundle);
                }
                else if (parts[1].Equals("DestroyableObject"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Bundle);
                }
                else if (parts[1].Equals("BundledMesh"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Bundle);
                }
                else if (parts[1].Equals("SkinnedMesh"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Bundle);
                }
                else if (parts[1].Equals("EffectBundle"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Bundle);
                }
                else if (parts[1].Equals("Sound"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.Sound);
                }
                else
                {
                    Debug.LogError($"Unknown Type Of ObjectTemplate.create : {parts[1]}");
                    Bf2ObjectTemplate.Instance = null;
                    continue;
                }

            }
            else if (parts[0] == "ObjectTemplate.soundFilename")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    string name = parts[1].Replace("\"", "");

                    // Ensure the directory exists
                    if (!Directory.Exists("Assets/Cache/Audio/"))
                        Directory.CreateDirectory("Assets/Cache/Audio/");

                    AudioClip AC = null;

                    // Try to get the audio clip from the cache
                    if (!audioCache.TryGetValue(name, out AC))
                    {
                        // Load the audio clip from the asset database if it's in the cache directory
                        AC = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Cache/Audio/" + name.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".ogg");
                        if (!AC)
                        {
                            Debug.Log($"AUDIO NAME : {name}");
                            File.WriteAllBytes("Assets/Cache/Audio/" + name.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".ogg", FileManager.ReadAllBytes(name));
                            AssetDatabase.Refresh();
                            AC = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Cache/Audio/" + name.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".ogg");

                        }
                        audioCache.Add(name, AC);

                    }

                    Bf2ObjectTemplate.Instance.audioSource.clip = AC;
                }
            }
            else if (parts[0] == "ObjectTemplate.volume")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    Bf2ObjectTemplate.Instance.audioSource.volume = float.Parse(parts[1]);
                }
            }
            else if (parts[0] == "ObjectTemplate.loopCount")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    Bf2ObjectTemplate.Instance.audioSource.loop = int.Parse(parts[1]) == 0;
                }
            }
            else if (parts[0] == "ObjectTemplate.minDistance")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    Bf2ObjectTemplate.Instance.audioSource.spatialBlend = 1;
                    Bf2ObjectTemplate.Instance.audioSource.minDistance = float.Parse(parts[1]);
                    Bf2ObjectTemplate.Instance.audioSource.maxDistance = float.Parse(parts[1]) * 2;
                }
            }
            else if (parts[0] == "ObjectTemplate.pan")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    Bf2ObjectTemplate.Instance.audioSource.panStereo = Mathf.Lerp(-1, 1, float.Parse(parts[1]));
                }
            }

            else if (parts[0] == "ObjectTemplate.collisionMesh")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    string name = parts[1];
                    GameObject collTemplate = ColliderLoader.GetCollTemplate(name);
                    if (collTemplate == null) continue;
                    collTemplate = UnityEngine.Object.Instantiate(collTemplate);
                    collTemplate.transform.parent = Bf2ObjectTemplate.Instance.transform;
                    collTemplate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
            }
            else if (parts[0] == "ObjectTemplate.mapMaterial")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    Bf2ObjectTemplate.Instance.SetColliderMaterial(int.Parse(parts[1]), parts[2]);
                }
            }
            else if (parts[0] == "ObjectTemplate.geometry")
            {
                if (Bf2ObjectTemplate.Instance != null)
                {
                    string name = parts[1];
                    GameObject MeshTemplate = UnityEngine.Object.Instantiate(MeshLoader.GetMeshTemplate(name, false));
                    MeshTemplate.transform.parent = Bf2ObjectTemplate.Instance.transform;
                    MeshTemplate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
            }
            else if (parts[0] == "Object.create")
            {
                GameObject StageObjects;
                if (!GameObject.Find("StageObjects"))
                { StageObjects = new GameObject("StageObjects"); StageObjects.transform.parent = MapLoader.Instance.SceneRoot.transform; }
                else
                    StageObjects = GameObject.Find("StageObjects");

                string name = parts[1];
                GameObject OBJ = Bf2ObjectTemplate.GetNewObjTemplate(name);
                if (OBJ == null)
                {
                    Bf2ObjectTemplate.Instance = null;
                    Debug.LogError($"OBJTemplate Not Found For : {name}");
                    continue;
                }
                OBJ.GetComponent<Bf2ObjectTemplate>().Init();
                OBJ.transform.parent = StageObjects.transform;

            }
            else if (parts[0].Equals("Object.absolutePosition", StringComparison.OrdinalIgnoreCase))
            {
                if (Bf2ObjectTemplate.Instance == null) continue;
                //Debug.Log("Original Vector3: " + parts[1]);
                string[] splitString = parts[1].Split('/');
                if (splitString.Length == 3)
                {
                    float x = float.Parse(splitString[0]);
                    float y = float.Parse(splitString[1]);
                    float z = float.Parse(splitString[2]);

                    Bf2ObjectTemplate.Instance.transform.localPosition = new Vector3(x, y, z);
                    //Debug.Log("Converted Vector3: " + new Vector3(x, y, z));
                }
                else
                {
                    Debug.LogError("String format is incorrect. Expected format: 'x/y/z'");
                }
            }
            else if (parts[0] == "Overgrowth.viewDistance")
            {
                GrowthLoader.Instance.LoadOverGrowth(MapLoader.PrimaryTerrain, path);
            }

            else if (parts[0] == "Object.rotation")
            {
                if (Bf2ObjectTemplate.Instance == null) continue;
                string[] V3 = parts[1].Split('/');
                Bf2ObjectTemplate.Instance.transform.eulerAngles = new Vector3(float.Parse(V3[1]), float.Parse(V3[0]), float.Parse(V3[2]));

            }


        }


    }


    public static Texture2D LoadImageFromRaw(string RawFilePath)
    {
        if (string.IsNullOrEmpty(RawFilePath))
        {
            Debug.LogError("RAW file path not set.");
            return null;
        }

        byte[] rawFileData = FileManager.ReadAllBytes(RawFilePath);

        // Assuming the file is square and 8-bit grayscale, calculate dimensions
        int TextureSize = Mathf.RoundToInt(Mathf.Sqrt(rawFileData.Length));

        if (TextureSize * TextureSize != rawFileData.Length)
        {
            Debug.LogError("RAW file size does not match expected square texture size.");
            return null;
        }

        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.R8, false, true);
        texture.name = "ShadowMask";
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                int index = y * TextureSize + x; // Corrected index calculation
                float value = rawFileData[index] / 255f;
                texture.SetPixel(x, TextureSize - 1 - y, new Color(value, value, value)); // Setting grayscale value to RGB
            }
        }

        texture.Apply(); // Apply the changes to the texture

        return texture;
    }


    public static void SetTag(GameObject GO, string newTag)
    {

        // Check if the tag already exists
        if (!DoesTagExist(newTag))
        {
            // Load the TagManager asset
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Ensure that the tag doesn't already exist in the list
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(newTag)) { GO.tag = newTag; return; }
            }

            // Add the new tag
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTagProp.stringValue = newTag;

            // Save the changes
            tagManager.ApplyModifiedProperties();

            Debug.Log("Tag " + newTag + " has been added.");
        }
        else
        {
            Debug.LogWarning("Tag " + newTag + " already exists.");
        }
        GO.tag = newTag;
    }

    public static bool DoesTagExist(string tag)
    {
        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
        {
            if (UnityEditorInternal.InternalEditorUtility.tags[i].Equals(tag))
            {
                return true;
            }
        }
        return false;
    }


}
