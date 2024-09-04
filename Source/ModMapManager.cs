using System.Collections.Generic;
using UnityEngine;
using MessagePack;
using System.IO;
using System.Linq;
using System;
using static CWModUtility;
using Unity.AI.Navigation;
using UnityEditor;
using UnityMeshSimplifier;
using Newtonsoft.Json;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;
using idbrii.navgen;
using System.Threading;

public class ModMapManager : MonoBehaviour
{
    public string MapPath = "Maps/Exported/";
    List<Shader> shaders;
    List<Texture> textures;
    List<AudioClip> audios;
    List<Terrain> terrains;
    //public Mesh TestMesh;
    CWMap cWMap;

    public GameObject LoadedMap;


    [Serializable]
    public struct Platforms
    {
        public bool Android;
        bool IPhonePlayer;
        bool WindowsPlayer;

        public Platforms(bool android = true)
        {
            Android = android;
            IPhonePlayer = false;
            WindowsPlayer = false;
        }

    }
    [Serializable]
    public struct TeamSettings
    {
        [NonSerialized] public bool EditingPosition;
        [NonSerialized] public bool EditingFlagPositions;
        public Vector3 BasePosition;//should be editable in scene like a Single Point.
        //public Vector3[] SpawnPoints;//should be editable in scene like a PointsGroup.
        public Vector3 FlagPosition;//should be editable in scene like a Single Point.
    }


    [Serializable]
    public class PatrolPoint
    {
        [NonSerialized] public bool Editing;
        public Vector3[] Points = new Vector3[0];//should be editable in scene like a PointsGroup.

        public Vector3[] Wrapped => GiftWrapping(Points);
    }

    [Serializable]
    public class ConquestPoint
    {
        [NonSerialized] public bool Editing;
        public Vector3 Position; //should be editable in scene like a PointsGroup.

        public GameObject Obj;

    }

    [Serializable]
    public class BombPoint
    {
        [NonSerialized] public bool Editing;
        public Vector3 Position;//should be editable in scene like a Single Point.
    }


    //{ -    Genaral Settings(dropdown Or Panel)        -
    public Bounds PlayableArea = new Bounds(Vector3.zero, new Vector3(10, 10, 10));//should be editable in scene like a box collider.
    public Vector3[] SpawnPoints;//should be editable in scene like a PointsGroup.
    public Vector3[] WeaponSpawnPoints;//should be editable in scene like a PointsGroup.
    [NonSerialized] public bool EditingSpawnPoints;
    [NonSerialized] public bool EditingWeaponSpawnPoints;
    public TeamSettings Team1;
    public TeamSettings Team2;
    [Range(2, 50)] public float MiniMapScale;

    //}

    //{ -    Bots Settings(dropdown Or Panel)        -
    [NonSerialized] public bool EditingBotsReferencePoints;
    public Vector3[] BotsReferencePoints;//should be editable in scene like a PointsGroup.
    //}


    //{ -    Conquest Settings(dropdown Or Panel)        -
    public bool EnableConquest;
    //{Only Shows If EnableConquest
    [Range(1.5f, 50)] public float ConquestRadius = 2;
    [Range(2, 100)] public float RespawnRadius = 10;
    public List<GameObject> ConquestOnlyObjects = new List<GameObject>();
    public List<ConquestPoint> ConquestPoints = new List<ConquestPoint>();//should Draw A Flat 2D circular White mesh showing its radius The Mesh Should Be Blue If Its Being Edited.
                                                                          //}

    //}

    //{ -    Patrol Settings(dropdown Or Panel)        -
    public bool EnablePatrol;
    //{Only Shows If EnablePatrol
    public List<GameObject> PatrolOnlyObjects = new List<GameObject>();
    public List<PatrolPoint> PatrolPoints = new List<PatrolPoint>();
    //}

    //}

    //{ -    Sabotage Settings(dropdown Or Panel)        -
    public bool EnableSabotage;
    //{Only Shows If EnableSabotage
    public List<GameObject> SabotageOnlyObjects = new List<GameObject>();
    public List<BombPoint> BombPoints = new List<BombPoint>();
    [Range(1, 100)] public float SabotageRadius = 2.5f;
    //}

    //}



    //{ -    Export Settings(dropdown Or Panel)        -

    public Platforms platforms;
    public MapInfo Info;
    public bool CompressMeshUVs = true;
    public bool CompressMeshVertices = true;
    public bool ConvertShaders = true;
    public bool PackTextures = true;
    //{Only Shows If PackTextures
    public int AtlasSize = 2048;
    public int MaxTextureSize = 512;
    [Space(30)]
    public bool SetStageVertexLimit = false;
    public int VertexLimit = 100000;

    //}

    //}

    public bool GeneralSettingsFoldout = true;
    public bool BotsSettingsFoldout = true;
    public bool ConquestSettingsFoldout = true;
    public bool PatrolSettingsFoldout = true;
    public bool SabotageSettingsFoldout = true;
    public bool ExportSettingsFoldout = true;

    public float WaterHeight;
    [System.NonSerialized] public CWTransform CamTrans;

    public static ModMapManager _Instance;
    public static ModMapManager Instance { get { if (!_Instance) _Instance = FindObjectOfType<ModMapManager>(); return _Instance; } set { _Instance = value; } }

    public bool LazyLoad = true;

    public int Memory;
    public Dictionary<GameObject, string> TaggedObjects = new Dictionary<GameObject, string>();
    public Dictionary<Transform, string> TaggedTransforms = new Dictionary<Transform, string>();
    void Awake()
    {
        Memory = SystemInfo.systemMemorySize;
        Instance = this;
        Destroy(transform.GetChild(2).gameObject);
        LoadMap();
    }

    int TotalCount;
    public void Update()
    {
        if (SlowLoading)
        {
            int Count = (int)Mathf.Min(CWMap.Instance.GameObjectsOnStage.Count / 45f, 45);
            if (Count <= 0) Count = 1;
            for (int i = 0; i < Count; i++)
            {
                LoadMapSlowly();
            }
            PercentLoaded = ((float)TotalCount / (float)CWMap.Instance.GameObjectsOnStage.Count) * 100f;
            if (clearCount > 50) clearUnusedAssets();
            clearCount = 0;
        }
    }

    public static float Progress;
    public static float FullProgress;
    public static bool Stop;

#if UNITY_EDITOR
    public BuildTarget buildTarget = BuildTarget.Android;
    // [Button("Save Map")]
    public TextureAtlaser Atlases;
    public void SaveMap()
    {
        ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Building Navigation", $"", 1);
        if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
        NavMeshSurface NMS = transform.GetChild(0).GetComponent<NavMeshSurface>();
        NavMeshSurface VNMS = transform.GetChild(0).GetComponent<NavMeshSurface>();

        if (NMS.navMeshData == null || NMS.transform.position != Vector3.zero)
        {
            if (Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(NMS))
            {
                Invoke(nameof(SaveMap), (0.5f));
                AssetDatabase.Refresh();
            }
            else
            {
                NMS.collectObjects = CollectObjects.Volume;
                NMS.transform.position = Vector3.zero;
                NMS.center = PlayableArea.center;
                NMS.size = PlayableArea.size;
                Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(new NavMeshSurface[] { NMS });
                Invoke(nameof(SaveMap), (0.5f));
            }
        }
        else
        if (VNMS.navMeshData == null || VNMS.transform.position != Vector3.zero)
        {
            if (Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(VNMS))
            {
                Invoke(nameof(SaveMap), (0.5f));
                AssetDatabase.Refresh();
            }
            else
            {
                VNMS.collectObjects = CollectObjects.Volume;
                VNMS.transform.position = Vector3.zero;
                VNMS.center = PlayableArea.center;
                VNMS.size = PlayableArea.size;
                Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(new NavMeshSurface[] { VNMS });
                Invoke(nameof(SaveMap), (0.5f));
            }
        }
        else
        {
            AssetDatabase.Refresh();
            SaveMap_1(NMS, VNMS);
        }



    }


    public void SaveMap_1(NavMeshSurface NMS, NavMeshSurface VNMS)
    {
        try
        {
            string scenename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Exporting Map({scenename})", "", 0);
            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
            if (!Directory.Exists(MapPath)) Directory.CreateDirectory(MapPath);
            if (!Directory.Exists("MapBuildChace/AssetBundles/")) Directory.CreateDirectory("MapBuildChace/AssetBundles/");
            AssetBundle.UnloadAllAssetBundles(true);
            StandardShaderConverter.MaxSize = MaxTextureSize;

            cWMap = new CWMap();
            cWMap.name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + AssetDatabase.AssetPathToGUID(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);


            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving Navigation", $"", 1);
            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }

            BuildPipeline.BuildAssetBundle(VNMS.navMeshData, null, "MapBuildChace/AssetBundles/TmpNMD.bundle", BuildAssetBundleOptions.None, buildTarget);
            cWMap.VNavMeshData = File.ReadAllBytes("MapBuildChace/AssetBundles/TmpNMD.bundle");

            BuildPipeline.BuildAssetBundle(NMS.navMeshData, null, "MapBuildChace/AssetBundles/TmpNMD.bundle", BuildAssetBundleOptions.None, buildTarget);
            cWMap.NavMeshData = File.ReadAllBytes("MapBuildChace/AssetBundles/TmpNMD.bundle");

            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Generating Navigation Links", $"", 1);
            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
            NMS.GetComponent<NavLinkGenerator>().GenerateLinks();


            PostProcessVolume PPV = GetComponentInChildren<PostProcessVolume>();
            if (PPV && PPV.sharedProfile != null)
            {
                BuildPipeline.BuildAssetBundle(PPV.sharedProfile, null, "MapBuildChace/AssetBundles/TmpPPP.bundle", BuildAssetBundleOptions.CollectDependencies, buildTarget);
                cWMap.PostProcessingProfile = File.ReadAllBytes("MapBuildChace/AssetBundles/TmpPPP.bundle");
            }

            TextureAtlaser.Instance = null;
            if (ConvertShaders && PackTextures)
            {
                Atlases = new TextureAtlaser(AtlasSize, MaxTextureSize);
                TextureAtlaser.Instance = Atlases;
                HashSet<Material> Mats = new HashSet<Material>();
                foreach (MeshRenderer MR in FindObjectsOfType<MeshRenderer>())
                {
                    if (!MR.enabled) continue;
                    if (MR.sharedMaterials != null && MR.sharedMaterials.Length > 0)
                    {
                        Progress = 0;
                        foreach (Material M in MR.sharedMaterials)
                        {
                            Progress++;
                            if (M == null) continue;
                            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Converting Materials Shader", $"Converting Shader For {M.name}", Progress / MR.sharedMaterials.Length);

                            Material Mat = StandardShaderConverter.GetConvertedMat(M);
                            Mats.Add(Mat);
                        }
                    }
                }
                Atlases.PackMaterials(Mats.ToArray());
            }

            foreach (Renderer R in FindObjectsOfType<Renderer>())
            {
                if (!R.enabled) continue;
                if (R.sharedMaterials != null && R.sharedMaterials.Length > 0)
                {
                    foreach (Material M in R.sharedMaterials)
                    {
                        Material Mat = ConvertShaders ? StandardShaderConverter.GetConvertedMat(M) : M;
                        if (ConvertShaders && PackTextures) Mat = Atlases.GetMat(Mat);
                        if (Mat == null) continue;
                        if (!cWMap.MaterialsOnStage.ContainsKey(GetMaterialChecksum(Mat)))
                        {
                            cWMap.MaterialsOnStage.Add(GetMaterialChecksum(Mat), new CWMaterial(Mat));
                        }
                    }
                }
            }
            int totalVertexCount = 0;
            Dictionary<Mesh, int> MeshCount = new Dictionary<Mesh, int>();
            MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
            if (SetStageVertexLimit)
            {

                Progress = 0;
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    Progress++;
                    if (meshFilter.sharedMesh == null) continue;
                    if (MeshCount.ContainsKey(meshFilter.sharedMesh)) MeshCount[meshFilter.sharedMesh]++;
                    else
                        MeshCount.Add(meshFilter.sharedMesh, 1);
                    totalVertexCount += meshFilter.sharedMesh.vertexCount;
                    if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                    ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Decimating Map", $"Initializing", Progress / meshFilters.Length);
                }
            }
            float ReductionAmmount = (float)VertexLimit / totalVertexCount;

            Progress = 0;
            foreach (MeshFilter MF in meshFilters)
            {
                Progress++;
                FullProgress = Progress / meshFilters.Length;
                if (MF.sharedMesh == null) continue;
                Mesh mesh = MF.sharedMesh;
                string Checksum = GetMeshChecksum(mesh);
                if (cWMap.MeshesOnStage.ContainsKey(Checksum)) continue;
                if (SetStageVertexLimit && ReductionAmmount < 1)
                {
                    MeshSimplifier MS = new MeshSimplifier();
                    ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Decimating {mesh.name} by {ReductionAmmount / MeshCount[mesh] * 100} %", FullProgress);
                    MS.Initialize(mesh);
                    //MS.Verbose=true;
                    SimplificationOptions SO = new SimplificationOptions();
                    SO.PreserveBorderEdges = true;
                    SO.VertexLinkDistance = 0.001f;
                    SO.PreserveSurfaceCurvature = true;
                    SO.PreserveUVSeamEdges = true;
                    SO.MaxIterationCount = 10;
                    SO.Agressiveness = 15;
                    MS.SimplificationOptions = SO;
                    MS.SimplifyMesh(ReductionAmmount / MeshCount[mesh]);
                    mesh = MS.ToMesh();
                    mesh.name = MF.sharedMesh.name;

                }

                if (ConvertShaders && PackTextures) mesh = Atlases.GetMesh(MF.GetComponent<MeshRenderer>(), mesh);
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Simplifying {mesh.name}", FullProgress);

                //MS.Initialize(mesh);
                //MS.SimplifyMeshLossless();
                //mesh=MS.ToMesh();
                //mesh.Optimize();
                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                Debug.Log("Mesh Checksum: " + Checksum);
                cWMap.MeshesOnStage.Add(Checksum, SerializeMesh(mesh, true, true, CompressMeshVertices, CompressMeshVertices, CompressMeshVertices, CompressMeshUVs));
            }
            Progress = 0;
            MeshCollider[] meshColliders = FindObjectsOfType<MeshCollider>();
            foreach (MeshCollider MC in meshColliders)
            {
                if (!MC.enabled) continue;
                Progress++; FullProgress = Progress / meshColliders.Length;
                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                if (MC.sharedMesh == null) continue;
                string Checksum = GetMeshChecksum(MC.sharedMesh);
                if (cWMap.MeshesOnStage.ContainsKey(Checksum)) continue;
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"PreProcessing Meshes Colliders", $"Saving {MC.sharedMesh.name} ", FullProgress);
                Debug.Log("Mesh Checksum: " + Checksum);
                cWMap.MeshesOnStage.Add(Checksum, SerializeMesh(MC.sharedMesh, true, true, CompressMeshVertices, CompressMeshVertices, CompressMeshVertices, CompressMeshUVs));
            }


            Progress = 0;
            GameObject[] gameObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject GO in gameObjects)
            {
                if (GO.transform.root == transform) continue;
                Progress++; FullProgress = Progress / gameObjects.Length;
                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving GameObjects", $"Saving {GO.name} ", FullProgress);

                CWGameObject CWGO = new CWGameObject(GO, ConvertShaders && PackTextures);
                cWMap.GameObjectsOnStage.Add(GetTransformChecksum(GO.transform), CWGO);
            }
            // return;
            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }

            if (RenderSettings.skybox)
            {
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving Skybox", $"Saving {RenderSettings.skybox.name} ", 1);
                if (!cWMap.MaterialsOnStage.ContainsKey(GetMaterialChecksum(RenderSettings.skybox)))
                    cWMap.MaterialsOnStage.Add(GetMaterialChecksum(RenderSettings.skybox), new CWMaterial(RenderSettings.skybox));
            }
            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }

            cWMap.renderSettings = new CWRenderSettings(null);

            Directory.CreateDirectory("MapBuildChace/AssetBundles/Android/Textures/");
            Progress = 0;
            textures = GetUniqueTexturesInScene().ToList();
            foreach (Texture tex in textures)
            {
                Texture nTex = tex;
                Progress++; FullProgress = Progress / textures.Count;

                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                if (nTex == null) continue;
                string Checksum = GetTextureChecksum(nTex);
                if (cWMap.TexturesOnStage.ContainsKey(Checksum)) continue;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(nTex)))
                {
                    nTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cache/Textures/" + Checksum + ".Texture2D");
                    if (!nTex) { AssetDatabase.CreateAsset(tex, "Assets/Cache/Textures/" + Checksum + ".Texture2D"); AssetDatabase.Refresh(); }
                    nTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cache/Textures/" + Checksum + ".Texture2D");
                }
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving Textures", $"Saving {nTex.name} ", FullProgress);

                if (!File.Exists("MapBuildChace/AssetBundles/Android/Textures/" + Checksum + ".bundle"))
                    BuildPipeline.BuildAssetBundle(nTex, null, "MapBuildChace/AssetBundles/Android/Textures/" + Checksum + ".bundle", BuildAssetBundleOptions.None, buildTarget);

                cWMap.TexturesOnStage.Add(Checksum, File.ReadAllBytes("MapBuildChace/AssetBundles/Android/Textures/" + Checksum + ".bundle"));
            }

            Directory.CreateDirectory("MapBuildChace/AssetBundles/Android/Audios/");
            audios = GetUniqueAudiosInScene().ToList();
            Progress = 0;
            foreach (AudioClip AC in audios)
            {
                Progress++; FullProgress = Progress / audios.Count;

                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                if (AC == null) continue;
                string Checksum = GetAudioChecksum(AC);
                if (cWMap.AudiosOnStage.ContainsKey(Checksum)) continue;
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving Audios", $"Saving {AC.name} ", FullProgress);
                if (!File.Exists("MapBuildChace/AssetBundles/Android/Audios/" + Checksum + ".bundle"))
                    BuildPipeline.BuildAssetBundle(AC, null, "MapBuildChace/AssetBundles/Android/Audios/" + Checksum + ".bundle", BuildAssetBundleOptions.None, buildTarget);

                cWMap.AudiosOnStage.Add(Checksum, File.ReadAllBytes("MapBuildChace/AssetBundles/Android/Audios/" + Checksum + ".bundle"));
            }

            Directory.CreateDirectory("MapBuildChace/AssetBundles/Android/Terrains/");
            terrains = FindObjectsOfType<Terrain>().ToList();
            Progress = 0;
            foreach (Terrain T in terrains)
            {
                Progress++; FullProgress = Progress / terrains.Count;
                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                if (T == null || T.terrainData == null) continue;
                string Checksum = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(T.terrainData));
                if (cWMap.TerrainsOnStage.ContainsKey(Checksum)) continue;
                if (T.materialTemplate != null && T.drawHeightmap) cWMap.MaterialsOnStage.TryAdd(GetMaterialChecksum(T.materialTemplate), new CWMaterial(T.materialTemplate));
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving Terrains", $"Saving {T.name} ", FullProgress);
                if (!File.Exists("MapBuildChace/AssetBundles/Android/Terrains/" + Checksum + ".bundle"))
                    BuildPipeline.BuildAssetBundle(T.terrainData, null, "MapBuildChace/AssetBundles/Android/Terrains/" + Checksum + ".bundle", BuildAssetBundleOptions.CollectDependencies, buildTarget);

                cWMap.TerrainsOnStage.Add(Checksum, File.ReadAllBytes("MapBuildChace/AssetBundles/Android/Terrains/" + Checksum + ".bundle"));
            }

            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving LightProbes", $"", 1);
            if (LightmapSettings.lightProbes != null)
            {
                BuildPipeline.BuildAssetBundle(LightmapSettings.lightProbes, null, "MapBuildChace/AssetBundles/TmpLP.bundle", BuildAssetBundleOptions.None, buildTarget);
                cWMap.lightProbesData = File.ReadAllBytes("MapBuildChace/AssetBundles/TmpLP.bundle");
            }
            else
            {
                cWMap.lightProbesData = new byte[0];
            }




            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving Shaders", $"", FullProgress);
            shaders = GetUniqueShadersInScene().ToList();

            for (int i = shaders.Count - 1; i >= 0; i--)
            {
                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                string Shaderpath = AssetDatabase.GetAssetPath(shaders[i]);
                if (string.IsNullOrEmpty(Shaderpath)) shaders.Remove(shaders[i]);
                else
                if (Path.GetExtension(Shaderpath).ToLower() != ".shader") shaders.Remove(shaders[i]);
            }

            // Create new AssetBundles
            AssetBundleBuild[] builds = new AssetBundleBuild[shaders.Count];
            for (int i = 0; i < builds.Length; i++)
            {
                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                // Specify the asset bundle name and the assets (shaders in this case) to include
                builds[i] = new AssetBundleBuild();
                builds[i].assetBundleName = SimpleEncode(shaders[i].name) + ".bundle";
                builds[i].assetNames = new string[] { AssetDatabase.GetAssetPath(shaders[i]) }; // Implement the method to get shader asset paths
            }
            if (platforms.Android)
            {
                if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
                BuildAssetBundleOptions options = BuildAssetBundleOptions.DisableLoadAssetByFileName | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
                Directory.CreateDirectory("MapBuildChace/AssetBundles/Android/Shaders/");
                BuildPipeline.BuildAssetBundles("MapBuildChace/AssetBundles/Android/Shaders/", builds, options, buildTarget);
            }
            /*if(platforms.IPhonePlayer){
                if(!Directory.Exists("Assets/AssetBundles/IOS/Shaders/"))Directory.CreateDirectory("Assets/AssetBundles/IOS/Shaders/");
                BuildPipeline.BuildAssetBundles("Assets/AssetBundles/IOS/Shaders/", builds, BuildAssetBundleOptions.None, BuildTarget.iOS);
            }
            if(platforms.WindowsPlayer){
                if(!Directory.Exists("Assets/AssetBundles/Windows/Shaders/"))Directory.CreateDirectory("Assets/AssetBundles/Windows/Shaders/");
                BuildPipeline.BuildAssetBundles("Assets/AssetBundles/Windows/Shaders/", builds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
            }*/

            for (int i = 0; i < shaders.Count; i++)
            {
                Debug.Log($"Now Decoding : {shaders[i].name}");
                cWMap.ShadersOnStage.Add(SimpleEncode(shaders[i].name), File.ReadAllBytes("MapBuildChace/AssetBundles/Android/Shaders/" + SimpleEncode(shaders[i].name) + ".bundle"));
            }

            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }
            cWMap.lightmapDatas = new CWLightmapData[LightmapSettings.lightmaps.Length];
            for (int i = 0; i < cWMap.lightmapDatas.Length; i++)
            {
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Saving LightMaps", $"Saving Lightmap {i} ", i / cWMap.lightmapDatas.Length);
                cWMap.lightmapDatas[i] = new CWLightmapData(LightmapSettings.lightmaps[i]);
            }
            cWMap.lightmapsModes = LightmapSettings.lightmapsMode;
            cWMap.Info = Info;
            cWMap.SaveMapConfig(this);

            if (Stop) { Debug.LogError("Map Export Operation Canceled By User"); return; }

            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Finalizing", $"Exporting Map", 1);
            File.WriteAllBytes(MapPath + cWMap.name + ".map", MessagePackSerializer.Serialize(cWMap));
            File.WriteAllBytes(MapPath + cWMap.name + ".cmap", Compress(MessagePackSerializer.Serialize(cWMap)));
            //File.WriteAllText(MapPath+cWMap.name+".json", JsonConvert.SerializeObject(cWMap));
            System.Diagnostics.Process.Start("explorer.exe", new DirectoryInfo(MapPath).FullName);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    //[Button("Load Map")]
#endif
    public string MapLoadPath;

    public static string MapID = "";
    public void LoadMap()
    {
        CWMap cWMap = null;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            MapLoadPath = EditorUtility.OpenFilePanel("Select A Map", PlayerPrefs.HasKey("LastMapLoadPath") ? PlayerPrefs.GetString("LastMapLoadPath") : "", "map");
            PlayerPrefs.SetString("LastMapLoadPath", MapLoadPath);
            cWMap = MessagePackSerializer.Deserialize<CWMap>(File.ReadAllBytes(MapLoadPath));
            InstallMap(cWMap);
            PlayerPrefs.SetString("CMap", cWMap.name);
        }
        else
        {
            if (PlayerPrefs.GetBool("SlowLoadCMap"))
            {
                LoadMapSlowly();
                return;
            }

        }
#endif
        if (PlayerPrefs.GetBool("SlowLoadCMap"))
        {
            LoadMapSlowly();
            return;
        }
        MapID = PlayerPrefs.GetString("CMap");
        if (File.Exists(Application.persistentDataPath + "/Maps/" + MapID + "/data.dat"))
            cWMap = MessagePackSerializer.Deserialize<CWMap>(File.ReadAllBytes(Application.persistentDataPath + "/Maps/" + MapID + "/data.dat"));
        CWMap.Instance = cWMap;
        LoadedMap = new GameObject(cWMap.name);

        foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values) { CWGO.Load(); }

        foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values) { if (CWGO.PID != null) CWGO.gameobject.transform.parent = cWMap.GameObjectsOnStage[CWGO.PID].gameobject.transform; else CWGO.gameobject.transform.parent = LoadedMap.transform; }
        foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values)
        {
            if (CWGO.MRenderer != null)
            {
                if (CWGO.Filter) CWGO.Filter.sharedMesh = cWMap.GetMesh(CWGO.meshRenderer.meshChecksum);
                Material[] Mats = new Material[CWGO.meshRenderer.MaterialsCRC.Length];
                for (int i = 0; i < Mats.Length; i++)
                {
                    if (CWGO.meshRenderer.MaterialsCRC[i] == null) continue;
                    Mats[i] = cWMap.GetMaterial(CWGO.meshRenderer.MaterialsCRC[i]);
                }
                CWGO.MRenderer.sharedMaterials = Mats;
                if (CWGO.meshRenderer.UVRegion != null)
                {
                    for (int i = 0; i < Mats.Length; i++)
                    {
                        if (CWGO.meshRenderer.MaterialsCRC[i] == null) continue;
                        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                        materialPropertyBlock.SetVector("_Offset", CWGO.meshRenderer.UVRegion[i]);
                        CWGO.MRenderer.SetPropertyBlock(materialPropertyBlock, i);
                    }
                }
            }

            if (CWGO.SPRenderer != null)
            {
                CWGO.SPRenderer.sprite = cWMap.GetSprite(CWGO.spriteRenderer.spriteChecksum);
                Material[] Mats = new Material[CWGO.spriteRenderer.MaterialsCRC.Length];
                for (int i = 0; i < Mats.Length; i++) Mats[i] = cWMap.GetMaterial(CWGO.spriteRenderer.MaterialsCRC[i]);
                CWGO.SPRenderer.sharedMaterials = Mats;
            }

            if (CWGO.meshColliders != null && CWGO.meshColliders.Length > 0)
            {
                foreach (CWMeshCollider CWMC in CWGO.meshColliders)
                {
                    if (CWMC == null) continue;
                    MeshCollider MC = CWGO.gameobject.AddComponent<MeshCollider>();
                    MC.sharedMesh = cWMap.GetMesh(CWMC.meshChecksum);
                    MC.contactOffset = CWMC.contactOffset;
                    MC.hasModifiableContacts = CWMC.hasModifiableContacts;
                    MC.hideFlags = CWMC.hideFlags;
                    MC.isTrigger = CWMC.isTrigger;
                    MC.convex = CWMC.convex;
                }
            }

            if (CWGO.audioSources != null && CWGO.audioSources.Length > 0)
            {
                foreach (CWAudioSource CWAS in CWGO.audioSources)
                {
                    if (CWAS == null || string.IsNullOrEmpty(CWAS.clipChecksum)) continue;
                    AudioSource AS = CWGO.gameobject.AddComponent<AudioSource>();
                    AS.clip = cWMap.GetAudio(CWAS.clipChecksum);

                    AS.bypassEffects = CWAS.bypassEffects;
                    AS.bypassListenerEffects = CWAS.bypassListenerEffects;
                    AS.bypassReverbZones = CWAS.bypassReverbZones;
                    AS.dopplerLevel = CWAS.dopplerLevel;
                    //AS.gamepadSpeakerOutputType=CWAS.gamepadSpeakerOutputType;
                    AS.hideFlags = CWAS.hideFlags;
                    AS.loop = CWAS.loop;
                    AS.maxDistance = CWAS.maxDistance;
                    AS.minDistance = CWAS.minDistance;
                    AS.mute = CWAS.mute;
                    AS.panStereo = CWAS.panStereo;
                    AS.pitch = CWAS.pitch;
                    AS.playOnAwake = CWAS.playOnAwake;
                    AS.priority = CWAS.priority;
                    AS.reverbZoneMix = CWAS.reverbZoneMix;
                    AS.rolloffMode = CWAS.rolloffMode;
                    AS.spatialBlend = CWAS.spatialBlend;
                    AS.spread = CWAS.spread;
                    AS.volume = CWAS.volume;
                    AS.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CWAS.CustomRolloff);
                    AS.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, CWAS.ReverbZoneMix);
                    AS.SetCustomCurve(AudioSourceCurveType.SpatialBlend, CWAS.SpatialBlend);
                    AS.SetCustomCurve(AudioSourceCurveType.Spread, CWAS.Spread);

                    if (AS.playOnAwake) AS.Play();
                }
            }

            if (CWGO.lodGroup != null)
            {
                LOD[] LODS = new LOD[CWGO.lodGroup.Lods.Length];
                for (int i = 0; i < LODS.Length; i++)
                {
                    LODS[i].renderers = new Renderer[CWGO.lodGroup.Lods[i].IDs.Length];
                    for (int j = 0; j < LODS[i].renderers.Length; j++)
                    {
                        if (cWMap.GameObjectsOnStage.ContainsKey(CWGO.lodGroup.Lods[i].IDs[j]))
                            if (cWMap.GameObjectsOnStage[CWGO.lodGroup.Lods[i].IDs[j]].MRenderer != null)
                                LODS[i].renderers[j] = cWMap.GameObjectsOnStage[CWGO.lodGroup.Lods[i].IDs[j]].MRenderer;
                    }
                    LODS[i].screenRelativeTransitionHeight = CWGO.lodGroup.Lods[i].screenRelativeTransitionHeight;
                }
                CWGO.LODG.localReferencePoint = CWGO.lodGroup.localReferencePoint;
                CWGO.LODG.size = CWGO.lodGroup.size;
                CWGO.LODG.SetLODs(LODS);
            }


        }

        if (cWMap.lightmapDatas != null)
        {
            LightmapData[] LMDs = new LightmapData[cWMap.lightmapDatas.Length];
            for (int i = 0; i < LMDs.Length; i++)
            {
                LMDs[i] = new LightmapData();
                LMDs[i].lightmapColor = (Texture2D)cWMap.GetTexture(cWMap.lightmapDatas[i].lightmapColorChecksum);
                LMDs[i].lightmapDir = (Texture2D)cWMap.GetTexture(cWMap.lightmapDatas[i].lightmapDirChecksum);
                LMDs[i].shadowMask = (Texture2D)cWMap.GetTexture(cWMap.lightmapDatas[i].shadowMaskChecksum);
            }
            LightmapSettings.lightmapsMode = cWMap.lightmapsModes;
            LightmapSettings.lightmaps = LMDs;
        }
        cWMap.LoadMapConfig(this);
        cWMap.ApplyLightProbes();
        cWMap.renderSettings.ApplyRenderSettings();



        DynamicGI.UpdateEnvironment();
        NavMeshSurface NMS = transform.GetChild(0).GetComponent<NavMeshSurface>();
        NavMeshData NMD = cWMap.GetNavMesh();
        if (NMD)
        {
            NMS.navMeshData = NMD;
            NMS.transform.position = Vector3.zero;
            //NMS.UpdateNavMesh(NMD);
            //NMS.AddData();
        }

        NavMeshSurface VNMS = transform.GetChild(1).GetComponent<NavMeshSurface>();
        NavMeshData VNMD = cWMap.GetVNavMesh();
        if (VNMD)
        {
            VNMS.navMeshData = VNMD;
            VNMS.transform.position = Vector3.zero;
        }

        //else
        //    NMS.BuildNavMesh();
        //cWMap.lightProbeData.ApplyLightProbes();
        PostProcessVolume PPV = GetComponentInChildren<PostProcessVolume>();
        PostProcessProfile PPP = cWMap.GetPostEffect();
        if (PPV && PPP)
        {
            //PPV.sharedProfile = PPP;
            //PPV.profile = PPP;

            RuntimeUtilities.DestroyProfile(PPV.profile, true);
            PPV.sharedProfile = PPP;
            PPV.profile = null;
            PostProcessProfile junk = PPV.profile;
        }

    }

    public bool SlowLoading;
    public float PercentLoaded = 0;
    public HashSet<object> LoadedObjects = new HashSet<object>();
    public List<GameObject> ObjectsToOptimize = new List<GameObject>();
    CWMap SlowCWMap = null;
    int clearCount = 0;


    public void LoadMapSlowly()
    {
        SlowLoading = true;
        CWMap cWMap = SlowCWMap;

        if (SlowCWMap == null)
        {
            MapID = PlayerPrefs.GetString("CMap");
            if (File.Exists(Application.persistentDataPath + "/Maps/" + MapID + "/data.dat"))
                SlowCWMap = MessagePackSerializer.Deserialize<CWMap>(File.ReadAllBytes(Application.persistentDataPath + "/Maps/" + MapID + "/data.dat"));
            cWMap = SlowCWMap;
            CWMap.Instance = SlowCWMap;
            LoadedMap = new GameObject(cWMap.name);


        }
        if (!LoadedObjects.Contains(cWMap.GameObjectsOnStage))
        {
            foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values) { CWGO.Load(); }
            foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values) { if (CWGO.PID != null) CWGO.gameobject.transform.parent = cWMap.GameObjectsOnStage[CWGO.PID].gameobject.transform; else CWGO.gameobject.transform.parent = LoadedMap.transform; }

            cWMap.LoadMapConfig(this);
            LoadedObjects.Add(cWMap.GameObjectsOnStage);

            foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values)
            {
                if (CWGO.meshColliders != null && CWGO.meshColliders.Length > 0)
                {
                    foreach (CWMeshCollider CWMC in CWGO.meshColliders)
                    {
                        if (CWMC == null) continue;
                        MeshCollider MC = CWGO.gameobject.AddComponent<MeshCollider>();
                        MC.sharedMesh = cWMap.GetMesh(CWMC.meshChecksum);
                        MC.contactOffset = CWMC.contactOffset;
                        MC.hasModifiableContacts = CWMC.hasModifiableContacts;
                        MC.hideFlags = CWMC.hideFlags;
                        MC.isTrigger = CWMC.isTrigger;
                        MC.convex = CWMC.convex;
                    }
                }
            }

            clearUnusedAssets();
            return;
        }


        foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values)
        {
            if (LoadedObjects.Contains(CWGO)) continue;
            LoadedObjects.Add(CWGO);
            TotalCount++;
            bool Optimize = false;
            if (CWGO.MRenderer != null)
            {
                if (CWGO.Filter) CWGO.Filter.sharedMesh = cWMap.GetMesh(CWGO.meshRenderer.meshChecksum);
                Material[] Mats = new Material[CWGO.meshRenderer.MaterialsCRC.Length];
                for (int i = 0; i < Mats.Length; i++)
                {
                    if (CWGO.meshRenderer.MaterialsCRC[i] == null) continue;
                    Mats[i] = cWMap.GetMaterial(CWGO.meshRenderer.MaterialsCRC[i]);
                    Optimize = true;
                }
                CWGO.MRenderer.sharedMaterials = Mats;
                if (CWGO.meshRenderer.UVRegion != null)
                {
                    for (int i = 0; i < Mats.Length; i++)
                    {
                        if (CWGO.meshRenderer.MaterialsCRC[i] == null) continue;
                        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                        materialPropertyBlock.SetVector("_Offset", CWGO.meshRenderer.UVRegion[i]);
                        CWGO.MRenderer.SetPropertyBlock(materialPropertyBlock, i);
                        Optimize = true;
                    }
                }
            }

            if (CWGO.SPRenderer != null)
            {
                CWGO.SPRenderer.sprite = cWMap.GetSprite(CWGO.spriteRenderer.spriteChecksum);
                Material[] Mats = new Material[CWGO.spriteRenderer.MaterialsCRC.Length];
                for (int i = 0; i < Mats.Length; i++) Mats[i] = cWMap.GetMaterial(CWGO.spriteRenderer.MaterialsCRC[i]);
                CWGO.SPRenderer.sharedMaterials = Mats;
                Optimize = true;
            }



            if (CWGO.audioSources != null && CWGO.audioSources.Length > 0)
            {
                foreach (CWAudioSource CWAS in CWGO.audioSources)
                {
                    if (CWAS == null || string.IsNullOrEmpty(CWAS.clipChecksum)) continue;
                    AudioSource AS = CWGO.gameobject.AddComponent<AudioSource>();
                    AS.clip = cWMap.GetAudio(CWAS.clipChecksum);

                    AS.bypassEffects = CWAS.bypassEffects;
                    AS.bypassListenerEffects = CWAS.bypassListenerEffects;
                    AS.bypassReverbZones = CWAS.bypassReverbZones;
                    AS.dopplerLevel = CWAS.dopplerLevel;
                    //AS.gamepadSpeakerOutputType=CWAS.gamepadSpeakerOutputType;
                    AS.hideFlags = CWAS.hideFlags;
                    AS.loop = CWAS.loop;
                    AS.maxDistance = CWAS.maxDistance;
                    AS.minDistance = CWAS.minDistance;
                    AS.mute = CWAS.mute;
                    AS.panStereo = CWAS.panStereo;
                    AS.pitch = CWAS.pitch;
                    AS.playOnAwake = CWAS.playOnAwake;
                    AS.priority = CWAS.priority;
                    AS.reverbZoneMix = CWAS.reverbZoneMix;
                    AS.rolloffMode = CWAS.rolloffMode;
                    AS.spatialBlend = CWAS.spatialBlend;
                    AS.spread = CWAS.spread;
                    AS.volume = CWAS.volume;
                    AS.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CWAS.CustomRolloff);
                    AS.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, CWAS.ReverbZoneMix);
                    AS.SetCustomCurve(AudioSourceCurveType.SpatialBlend, CWAS.SpatialBlend);
                    AS.SetCustomCurve(AudioSourceCurveType.Spread, CWAS.Spread);

                    if (AS.playOnAwake) AS.Play();
                }
            }


            LoadedObjects.Add(CWGO);
            if (Optimize)
            {
                clearCount++;
                ObjectsToOptimize.Add(CWGO.gameobject);
                return;
            }

        }


        if (cWMap.lightmapDatas != null && !LoadedObjects.Contains(cWMap.lightmapDatas))
        {
            LightmapData[] LMDs = new LightmapData[cWMap.lightmapDatas.Length];
            for (int i = 0; i < LMDs.Length; i++)
            {
                LMDs[i] = new LightmapData();
                LMDs[i].lightmapColor = (Texture2D)cWMap.GetTexture(cWMap.lightmapDatas[i].lightmapColorChecksum);
                LMDs[i].lightmapDir = (Texture2D)cWMap.GetTexture(cWMap.lightmapDatas[i].lightmapDirChecksum);
                LMDs[i].shadowMask = (Texture2D)cWMap.GetTexture(cWMap.lightmapDatas[i].shadowMaskChecksum);
            }
            LightmapSettings.lightmapsMode = cWMap.lightmapsModes;
            LightmapSettings.lightmaps = LMDs;
            LoadedObjects.Add(cWMap.lightmapDatas);


            foreach (CWGameObject CWGO in cWMap.GameObjectsOnStage.Values)
            {
                if (CWGO.lodGroup != null)
                {
                    LOD[] LODS = new LOD[CWGO.lodGroup.Lods.Length];
                    for (int i = 0; i < LODS.Length; i++)
                    {
                        LODS[i].renderers = new Renderer[CWGO.lodGroup.Lods[i].IDs.Length];
                        for (int j = 0; j < LODS[i].renderers.Length; j++)
                        {
                            if (cWMap.GameObjectsOnStage.ContainsKey(CWGO.lodGroup.Lods[i].IDs[j]))
                                if (cWMap.GameObjectsOnStage[CWGO.lodGroup.Lods[i].IDs[j]].MRenderer != null)
                                    LODS[i].renderers[j] = cWMap.GameObjectsOnStage[CWGO.lodGroup.Lods[i].IDs[j]].MRenderer;
                        }
                        LODS[i].screenRelativeTransitionHeight = CWGO.lodGroup.Lods[i].screenRelativeTransitionHeight;
                    }
                    CWGO.LODG.localReferencePoint = CWGO.lodGroup.localReferencePoint;
                    CWGO.LODG.size = CWGO.lodGroup.size;
                    CWGO.LODG.SetLODs(LODS);
                }
            }

            return;
        }
        clearUnusedAssets();

        cWMap.ApplyLightProbes();

        cWMap.renderSettings.ApplyRenderSettings();



        DynamicGI.UpdateEnvironment();

        NavMeshSurface NMS = transform.GetChild(0).GetComponent<NavMeshSurface>();
        NavMeshData NMD = cWMap.GetNavMesh();
        if (NMD)
        {
            NMS.navMeshData = NMD;
            NMS.transform.position = Vector3.zero;
            //NMS.UpdateNavMesh(NMD);
            //NMS.AddData();
        }
        //else
        //    NMS.BuildNavMesh();
        //cWMap.lightProbeData.ApplyLightProbes();
        clearUnusedAssets();
        PostProcessVolume PPV = GetComponentInChildren<PostProcessVolume>();

        PostProcessProfile PPP = cWMap.GetPostEffect();
        if (Memory > 2800)
            if (PPV && PPP)
            {
                //PPV.sharedProfile = PPP;
                //PPV.profile = PPP;

                RuntimeUtilities.DestroyProfile(PPV.profile, true);
                PPV.sharedProfile = PPP;
                PPV.profile = null;
                PostProcessProfile junk = PPV.profile;
            }
        SlowLoading = false;
        clearUnusedAssets();

    }


    public static void clearUnusedAssets()
    {
        if (Camera.main) Camera.main.depthTextureMode = DepthTextureMode.None;

        Resources.UnloadUnusedAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }


    Shader[] GetUniqueShadersInScene()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        HashSet<Shader> uniqueShaders = new HashSet<Shader>();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            foreach (Material material in materials)
            {
                if (material != null)
                {
                    Shader shader = material.shader;
                    if (shader != null)
                    {
                        uniqueShaders.Add(shader);
                    }
                }
            }
        }

        if (RenderSettings.skybox)
        {
            uniqueShaders.Add(RenderSettings.skybox.shader);
        }

        // Convert the HashSet to an array
        Shader[] shaderArray = new Shader[uniqueShaders.Count];
        uniqueShaders.CopyTo(shaderArray);

        return shaderArray;
    }

    Texture[] GetUniqueTexturesInScene()
    {
        HashSet<Texture> uniqueTextures = new HashSet<Texture>();
#if UNITY_EDITOR
        // Find all renderers in the scene
        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Iterate through the shared materials of the renderer
            foreach (Material material in renderer.sharedMaterials)
            {
                Material Mat = material;
                if (ConvertShaders && PackTextures) Mat = TextureAtlaser.Instance.GetMat(StandardShaderConverter.GetConvertedMat(Mat));
                if (Mat == null) continue;
                // Collect main texture
                uniqueTextures.Add(Mat.mainTexture);

                // Collect other textures
                for (int i = 0; i < ShaderUtil.GetPropertyCount(Mat.shader); i++)
                {
                    if (ShaderUtil.GetPropertyType(Mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(Mat.shader, i);
                        uniqueTextures.Add(Mat.GetTexture(propertyName));
                    }
                }
            }
        }

        if (RenderSettings.skybox)
        {
            // Collect main texture
            uniqueTextures.Add(RenderSettings.skybox.mainTexture);

            // Collect other textures
            for (int i = 0; i < ShaderUtil.GetPropertyCount(RenderSettings.skybox.shader); i++)
            {
                if (ShaderUtil.GetPropertyType(RenderSettings.skybox.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(RenderSettings.skybox.shader, i);
                    uniqueTextures.Add(RenderSettings.skybox.GetTexture(propertyName));
                }
            }
        }


        foreach (SpriteRenderer SR in FindObjectsOfType<SpriteRenderer>())
        {
            if (SR.sprite != null && SR.sprite.texture != null) uniqueTextures.Add(SR.sprite.texture);
        }
        if (Info.Icon)
        {
            uniqueTextures.Add(Info.Icon.texture);
            Info.IconID = GetTextureChecksum(Info.Icon.texture);
        }

        foreach (TextMesh TM in FindObjectsOfType<TextMesh>())
        {
            if (TM.font) uniqueTextures.Add(TM.font.material.mainTexture);
        }

        foreach (ReflectionProbe RP in FindObjectsOfType<ReflectionProbe>())
        {
            //uniqueTextures.Add(RP.texture);
            uniqueTextures.Add(RP.customBakedTexture);
            uniqueTextures.Add(RP.bakedTexture);
        }
        if (RenderSettings.defaultReflectionMode == UnityEngine.Rendering.DefaultReflectionMode.Custom) uniqueTextures.Add(RenderSettings.customReflection);
        LightmapData[] lightmapDatas = LightmapSettings.lightmaps;
        foreach (LightmapData LMD in lightmapDatas)
        {
            uniqueTextures.Add(LMD.lightmapColor);
            uniqueTextures.Add(LMD.lightmapDir);
            uniqueTextures.Add(LMD.shadowMask);
        }


#endif
        // Convert the HashSet to an array
        Texture[] textureArray = new Texture[uniqueTextures.Count];
        uniqueTextures.CopyTo(textureArray);

        return textureArray;
    }


    AudioClip[] GetUniqueAudiosInScene()
    {
        HashSet<AudioClip> uniqueAudios = new HashSet<AudioClip>();
        AudioSource[] Sources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource AS in Sources)
        {
            uniqueAudios.Add(AS.clip);

        }

        AudioClip[] audioArray = new AudioClip[uniqueAudios.Count];
        uniqueAudios.CopyTo(audioArray);

        return audioArray;
    }



}
