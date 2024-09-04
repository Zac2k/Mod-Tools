#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.IO.Compression;
using static BF2FileManager;
using static ConFileParser;

public enum ColliderType
{
    projectile = 0,
    vehicle = 1,
    soldier = 2,
    AI = 3
}
[ExecuteInEditMode]
public class MapLoader : MonoBehaviour
{



    [Serializable]
    public class ObjectsLightMapInfo
    {
        public string name = "";
        public string TAILine = "";
        public string TexName = "";
        public int LODID;
        public Vector3Int Pos;
        public string AtlasTexPath;
        public int LMID;
        public Vector4 ScaleOffset;

        public GameObject DetectedObject;
        [NonSerialized] public Texture2D AtlasTex;
    }


    [System.NonSerialized] public List<ObjectsLightMapInfo> objectsLightMapInfos = new List<ObjectsLightMapInfo>();
    [System.NonSerialized] public List<Texture2D> LightMaps = new List<Texture2D>();
    [System.NonSerialized] public List<Texture2D> LightMapsLum = new List<Texture2D>();
    [System.NonSerialized] public List<Texture2D> ShadowMasks = new List<Texture2D>();

    public string Bf2Root;

    [ModFolderDropdown]
    public string SelectedModFolder;
    [LevelDropdown]
    public string SelectedLevel;
    [GameModeDropDown]
    public string SelectedMode;
    public static string CSelectedLevel => FindObjectOfType<MapLoader>().SelectedLevel;

    public static MapLoader _Instance;
    public static MapLoader Instance { get { if (!_Instance) _Instance = FindObjectOfType<MapLoader>(); return _Instance; } set { _Instance = value; } }

    public TextureFormat TextureCompressionFormat = TextureFormat.ASTC_12x12;

    [HideInInspector] public List<string> ModFolders = new List<string>();
    [HideInInspector] public List<string> Levels = new List<string>();
    [HideInInspector] public List<string> Modes = new List<string>();

    public static Terrain PrimaryTerrain;


    [HideInInspector] public string[] TestConData;

    [Range(0, 1)] public float TerrainQuality = 0.5f;
    public ColliderType colliderType = ColliderType.soldier;

    public void Start()
    {
    }
    [Button("Select Bf2 Installation Root")]
    public void SelectRoot()
    {
#if UNITY_EDITOR
        Bf2Root = NormalizePath(EditorUtility.OpenFolderPanel("Select Bf2 Installation Root", "", ""));
        if (!string.IsNullOrEmpty(Bf2Root))
        {
            RefreshModFolders();
        }
#endif
    }

    public string modRoot;
    public static string Root;

    [System.NonSerialized] public bool Initialized;

    [Button("Mount Paths")]
    public void InitMod()
    {
        // Normalize path and read the ClientArchives.con file
        modRoot = NormalizePath(Path.Combine(Bf2Root, "mods", SelectedModFolder));
        Root = modRoot;

        FileManager = new BF2FileManager();

        string clientArchivesPath = Path.Combine(modRoot, "ClientArchives.con");
        string serverArchivesPath = Path.Combine(modRoot, "ServerArchives.con");

        if (!File.Exists(clientArchivesPath))
        {
            Debug.LogError("ClientArchives.con file not found at: " + clientArchivesPath);
            return;
        }
        if (!File.Exists(serverArchivesPath))
        {
            Debug.LogError("ServerArchives.con file not found at: " + serverArchivesPath);
            return;
        }
        ParseCon(clientArchivesPath);
        ParseCon(serverArchivesPath);

        if (!string.IsNullOrEmpty(LastConPath)) TestConData = FileManager.ReadAllLines(LastConPath);
        Initialized = true;

        levelsPath = NormalizePath(Path.Combine(Bf2Root, "mods", SelectedModFolder, "Levels", SelectedLevel));

        FileManager.Mount(levelsPath + "/client.zip", "");
        FileManager.Mount(levelsPath + "/server.zip", "");

        FileManager.Mount(levelsPath + "/client.zip/", "Levels/" + SelectedLevel);
        FileManager.Mount(levelsPath + "/server.zip/", "Levels/" + SelectedLevel);

        FileManager.Mount(levelsPath + "/client.zip/objects", "/objects");
        FileManager.Mount(levelsPath + "/server.zip/objects", "/objects");
    }

    public static string levelsPath;
    [HideInInspector] public GameObject SceneRoot;
    [HideInInspector] public GameObject TempRoot;
    [Button("LoadMap")]
    public void LoadMap()
    {
        InitMod();
        if (SceneRoot)
        {
            DestroyImmediate(SceneRoot);
        }
        SceneRoot = new GameObject(SelectedLevel);
        TempRoot = new GameObject("Temp");
        TempRoot.transform.parent = SceneRoot.transform;

        if (FileManager.Exists("Init.con"))
        {
            ParseCon("Init.con");
            if (!string.IsNullOrEmpty(LastConPath)) TestConData = FileManager.ReadAllLines(LastConPath);
        }
        else
        {
            throw new FileNotFoundException("Failed To Find Map Init.con in " + levelsPath);
        }
        ApplyLightMaps();
        LoadMiniMap();
        LoadGameMode();
        LoadSurfaces();
        LoadEnvMaps();

        DestroyImmediate(GameObject.Find("ObjTemplates"));
        DestroyImmediate(GameObject.Find("MeshTemplates"));
        DestroyImmediate(GameObject.Find("CollTemplates"));
        DestroyImmediate(GameObject.Find("RoadTemplates"));
        GameObject.Find("TreeTemplates").SetActive(false);
        GameObject.Find("HeighmapCluster").transform.position += new Vector3(0, -0.01f, 0);

        Vector3 BoundExtents = Vector3.one * (PrimaryTerrain.transform.parent.lossyScale.x / 2) * 0.9f;
        BoundExtents.y = PrimaryTerrain.terrainData.size.y;

        ModMapManager.Instance.PlayableArea.center = new Vector3(0, BoundExtents.y * 0.9f, 0);
        ModMapManager.Instance.PlayableArea.extents = BoundExtents;
    }

    //[Button("ApplyLightMaps")]
    public void ApplyLightMaps()
    {
        ApplyLightMaps("levels/" + SelectedLevel + "/lightmaps/objects/LightmapAtlas.tai");
    }
    public void ApplyLightMaps(string taiFilePath)
    {

        objectsLightMapInfos.Clear();
        if (!FileManager.Exists(taiFilePath))
        {
            Debug.LogError("File not found: " + taiFilePath);
            return;
        }

        string[] lines = FileManager.ReadAllLines(taiFilePath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue; // Skip empty lines and comments

            string[] parts = line.Split(new char[] { '\t', ',', ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 11)
            {
                Debug.LogWarning("Line has an incorrect format: " + line);
                continue;
            }

            ObjectsLightMapInfo info = new ObjectsLightMapInfo
            {
                TAILine = line,
                TexName = parts[0].Trim(),
                LODID = int.Parse(parts[1].Trim()),
                Pos = new Vector3Int(int.Parse(parts[2].Trim()), int.Parse(parts[3].Trim()), int.Parse(parts[4].Split('.')[0].Trim())),
                AtlasTexPath = parts[5],
                LMID = int.Parse(parts[6].Trim()),
                ScaleOffset = new Vector4(
                    float.Parse(parts[9].Trim()),
                    float.Parse(parts[10].Trim()),
                    float.Parse(parts[7].Trim()),
                    float.Parse(parts[8].Trim()))
            };

            // Optional: Load Atlas Texture here if needed
            // info.AtlasTex = LoadTexture(info.AtlasTexPath);
            info.AtlasTex = DDSLoader.LoadDDSTexture(info.AtlasTexPath, false, true);
            while (LightMaps.Count < info.LMID + 1) { LightMaps.Add(null); }

            LightMaps[info.LMID] = info.AtlasTex;

            info.name = Path.GetFileNameWithoutExtension(info.TexName);
            GameObject GO = GameObject.Find("StageObjects");
            foreach (Transform T in GO.transform)
            {
                if (T.name.ToLower() == (info.name + "(Clone)").ToLower())
                {
                    Vector3Int IPos = new Vector3Int((int)T.position.x, (int)T.position.y, (int)T.position.z);
                    if (IPos == info.Pos)
                        info.DetectedObject = T.gameObject;
                }
            }
            if (!info.DetectedObject) continue;
            info.DetectedObject.isStatic = true;
            if (info.DetectedObject.transform.childCount < 1) continue;
            LODGroup LG = info.DetectedObject.GetComponentInChildren<LODGroup>();
            if (!LG) continue;
            MeshRenderer MR = (MeshRenderer)LG.GetLODs()[info.LODID].renderers[0];
            MR.lightmapIndex = info.LMID + 1;
            MR.lightmapScaleOffset = info.ScaleOffset;
            MR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            objectsLightMapInfos.Add(info);
        }

        ShadowMasks = new List<Texture2D>();
        LightMapsLum = new List<Texture2D>();
        foreach (Texture2D TX in LightMaps)
        {
            if (TX.format != TextureFormat.ARGB32)
            {

                ShadowMasks.Add(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cache/Textures/" + (TX.name + "_SM").Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Texture2D"));
                LightMapsLum.Add(TX);
                continue;
            }
            // Create a new texture with the same width and height but with R8 format
            Texture2D STX = new Texture2D(TX.width, TX.height, TextureFormat.R8, true, true);
            STX.name = TX.name + "_ShaodwMask";

            // Get all the pixels from the original texture
            Color[] pixels = TX.GetPixels();

            // Create an array to hold the new pixel data
            Color[] newPixels = new Color[pixels.Length];

            // Convert the green channel of the original pixels to white and set to new pixel data
            for (int i = 0; i < pixels.Length; i++)
            {
                newPixels[i] = Color.white * pixels[i].g;
            }

            // Set all the pixels at once
            STX.SetPixels(newPixels);
            EditorUtility.CompressTexture(STX, TextureCompressionFormat, TextureCompressionQuality.Normal);
            // Apply the changes to the texture
            STX.Apply(true);
            AssetDatabase.CreateAsset(STX, "Assets/Cache/Textures/" + (TX.name + "_SM").Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".Texture2D");
            // Add the new texture to the list
            ShadowMasks.Add(STX);

            // Texture2D LUMTX = TX;

            // Get all pixels from the original texture
            Color[] Pixels = TX.GetPixels();

            // Create an array for the new pixel data
            Color[] LUMPixels = new Color[Pixels.Length];

            // Swap the Red and Blue channels
            for (int i = 0; i < Pixels.Length; i++)
            {
                Color pixel = Pixels[i];
                LUMPixels[i] = Color.white * pixel.b;
                LUMPixels[i].r += pixel.r / 3;
                LUMPixels[i].g += pixel.r / 3;
                LUMPixels[i].b += pixel.r / 3;
            }

            // Set the new pixel data to the new texture
            TX.SetPixels(LUMPixels);
            TX.Apply(true);
            EditorUtility.CompressTexture(TX, TextureCompressionFormat, TextureCompressionQuality.Normal);

            // Apply the changes to the new texture
            LightMapsLum.Add(TX);
        }
        LightmapData[] LMDs = new LightmapData[1 + LightMapsLum.Count];
        for (int i = 0; i < LMDs.Length; i++)
        {
            LMDs[i] = new LightmapData();
            LMDs[i].lightmapDir = null;
            if (i == 0)
            {
                LMDs[i].lightmapColor = new Texture2D(2, 2, TextureFormat.ARGB32, true, true);
                LMDs[i].shadowMask = LoadImageFromRaw("SimpleShadowmap.raw");
            }
            else
            {
                LMDs[i].lightmapColor = LightMapsLum[i - 1];
                LMDs[i].shadowMask = ShadowMasks[i - 1];
            }
        }
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = LMDs;

        DynamicGI.UpdateEnvironment();
        // You may want to do additional processing here, such as associating the loaded textures with game objects
    }


    Sprite MiniMapTX;
    [Button("LoadMiniMap")]
    public void LoadMiniMap()
    {
        Texture2D TX = DDSLoader.LoadDDSTexture("levels/" + SelectedLevel + "/Hud/Minimap/ingameMap");
        MiniMapTX = Sprite.Create(TX, new Rect(0.0f, 0.0f, TX.width, TX.height), new Vector2(0.5f, 0.5f), 100.0f);
        SpriteRenderer SR = new GameObject("Minimap").AddComponent<SpriteRenderer>();
        SR.transform.parent = SceneRoot.transform;
        SR.sprite = MiniMapTX;
        SR.transform.position = Vector3.zero;
        SR.transform.rotation = Quaternion.LookRotation(Vector3.up);
        //float Scale = PrimaryTerrain.transform.parent.lossyScale.x / TX.width;
        float Scale = (2050f / TX.width)*100;
        SR.transform.localScale = new Vector3(Scale, -Scale, Scale);
        SR.gameObject.layer = LayerMask.NameToLayer("UI");
    }

    //[Button("LoadGameMode")]
    public void LoadGameMode()
    {
        string ModesPath = "GameModes";
        string[] lines = null;
        string[] AIlines = null;
        if (SelectedMode == "SP_16") { lines = FileManager.ReadAllLines(Path.Combine(ModesPath, "sp1/16/GamePlayObjects.con")); AIlines = FileManager.ReadAllLines(Path.Combine(ModesPath, "sp1/16/AI/StrategicAreas.ai")); }
        else
        if (SelectedMode == "coop_16") { lines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_coop/16/GamePlayObjects.con")); AIlines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_coop/16/AI/StrategicAreas.ai")); }
        else
        if (SelectedMode == "conquest_16") { lines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_cq/16/GamePlayObjects.con")); AIlines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_cq/16/AI/StrategicAreas.ai")); }
        else
        if (SelectedMode == "conquest_32") { lines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_cq/32/GamePlayObjects.con")); AIlines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_cq/32/AI/StrategicAreas.ai")); }
        else
        if (SelectedMode == "conquest_64") { lines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_cq/64/GamePlayObjects.con")); AIlines = FileManager.ReadAllLines(Path.Combine(ModesPath, "gpm_cq/64/AI/StrategicAreas.ai")); }
        else
            return;

        ModMapManager.Instance.SpawnPoints = new Vector3[0];
        ModMapManager.Instance.BotsReferencePoints = new Vector3[0];
        ModMapManager.Instance.ConquestPoints.Clear();
        ModMapManager.Instance.PatrolPoints.Clear();
        ModMapManager.Instance.BombPoints.Clear();

        float ToatalConquestPointsRad = 0;
        int TotalConquestPoints = 0;
        int BlockState = 0;
        foreach (var line in lines)
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

            else if (parts[0] == "ObjectTemplate.create")
            {
                if (parts[1].Equals("SpawnPoint"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.SpawnPoint);
                }
                else if (parts[1].Equals("ControlPoint"))
                {
                    new GameObject(parts[2]).AddComponent<Bf2ObjectTemplate>().Init(Bf2ObjectTemplate.ObjType.ControlPoint);
                }
                else
                {
                    Debug.LogError($"Unknown Type Of ObjectTemplate.create : {parts[1]}");
                    Bf2ObjectTemplate.Instance = null;
                    continue;
                }

            }
            else if (parts[0] == "ObjectTemplate.radius")
            {
                if (Bf2ObjectTemplate.Instance == null) continue;
                Bf2ObjectTemplate.Instance.ConquestPointRadius = float.Parse(parts[1]);
                ToatalConquestPointsRad += float.Parse(parts[1]);
                TotalConquestPoints++;
            }
            else if (parts[0] == "Object.create")
            {
                GameObject StageObjects;
                if (!GameObject.Find("StageObjects"))
                { StageObjects = new GameObject("StageObjects"); StageObjects.transform.parent = MapLoader.Instance.SceneRoot.transform; }
                else
                    StageObjects = GameObject.Find("StageObjects");

                string name = parts[1];
                GameObject OBJ = Bf2ObjectTemplate.GetObjTemplate(name);
                if (OBJ == null)
                {
                    Bf2ObjectTemplate.Instance = null;
                    Debug.LogError($"OBJTemplate Not Found For : {name}");
                    continue;
                }
                Bf2ObjectTemplate Template = OBJ.GetComponent<Bf2ObjectTemplate>();

                if (Template.ObjectType == Bf2ObjectTemplate.ObjType.SpawnPoint || Template.ObjectType == Bf2ObjectTemplate.ObjType.ControlPoint)
                {
                    Bf2ObjectTemplate.Instance = Template;
                }
                else
                {
                    OBJ.GetComponent<Bf2ObjectTemplate>().Init();
                    OBJ.transform.parent = StageObjects.transform;
                }

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
                    if (Bf2ObjectTemplate.Instance.ObjectType == Bf2ObjectTemplate.ObjType.SpawnPoint)
                    {
                        System.Array.Resize(ref ModMapManager.Instance.SpawnPoints, ModMapManager.Instance.SpawnPoints.Length + 1);
                        ModMapManager.Instance.SpawnPoints[ModMapManager.Instance.SpawnPoints.Length - 1] = new Vector3(x, y, z);

                        System.Array.Resize(ref ModMapManager.Instance.BotsReferencePoints, ModMapManager.Instance.BotsReferencePoints.Length + 1);
                        ModMapManager.Instance.BotsReferencePoints[ModMapManager.Instance.BotsReferencePoints.Length - 1] = new Vector3(x, y, z);
                    }
                    else if (Bf2ObjectTemplate.Instance.ObjectType == Bf2ObjectTemplate.ObjType.ControlPoint)
                    {
                        ModMapManager.ConquestPoint CP = new ModMapManager.ConquestPoint();
                        CP.Position = new Vector3(x, y, z);
                        ModMapManager.Instance.ConquestPoints.Add(CP);
                    }
                    else
                        Bf2ObjectTemplate.Instance.transform.localPosition = new Vector3(x, y, z);
                    //Debug.Log("Converted Vector3: " + new Vector3(x, y, z));
                }
                else
                {
                    Debug.LogError("String format is incorrect. Expected format: 'x/y/z'");
                }
            }
        }

        foreach (var line in AIlines)
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

            else if (parts[0] == "aiStrategicArea.addWayPoint")
            {
                string[] splitString = parts[1].Split('/');
                if (splitString.Length == 3)
                {
                    float x = float.Parse(splitString[0]);
                    float y = float.Parse(splitString[1]);
                    float z = float.Parse(splitString[2]);

                    System.Array.Resize(ref ModMapManager.Instance.BotsReferencePoints, ModMapManager.Instance.BotsReferencePoints.Length + 1);
                    ModMapManager.Instance.BotsReferencePoints[ModMapManager.Instance.BotsReferencePoints.Length - 1] = new Vector3(x, y, z);
                }
                else
                {
                    Debug.LogError("String format is incorrect. Expected format: 'x/y/z'");
                }
            }
        }

        ModMapManager.Instance.ConquestRadius = ToatalConquestPointsRad / TotalConquestPoints;
        ModMapManager.Instance.RespawnRadius = (ToatalConquestPointsRad / TotalConquestPoints) * 5;

        ModMapManager.ConquestPoint BaseA = null;
        ModMapManager.ConquestPoint BaseB = null;
        float maxDistance = 0f;

        var conquestPoints = ModMapManager.Instance.ConquestPoints;

        for (int i = 0; i < conquestPoints.Count; i++)
        {
            for (int j = i + 1; j < conquestPoints.Count; j++)
            {
                float distance = Vector3.Distance(conquestPoints[i].Position, conquestPoints[j].Position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    BaseA = conquestPoints[i];
                    BaseB = conquestPoints[j];
                }
            }

        }
        ModMapManager.Instance.Team1.BasePosition = BaseA.Position;
        ModMapManager.Instance.Team2.BasePosition = BaseB.Position;

        ModMapManager.Instance.Team1.FlagPosition = BaseA.Position;
        ModMapManager.Instance.Team2.FlagPosition = BaseB.Position;

        for (int i = 0; i < conquestPoints.Count; i++)
        {
            ModMapManager.ConquestPoint CP = ModMapManager.Instance.ConquestPoints[i];

            if (CP != BaseA && CP != BaseB)
            {
                ModMapManager.BombPoint BP = new ModMapManager.BombPoint();
                BP.Position = CP.Position;
                ModMapManager.Instance.BombPoints.Add(BP);
            }
            int numberOfPoints = 16;
            float radius = ModMapManager.Instance.ConquestRadius * 2;
            ModMapManager.PatrolPoint PP = new ModMapManager.PatrolPoint();
            for (int j = 0; j < numberOfPoints; j++)
            {
                // Calculate the angle for this point
                float angle = j * Mathf.PI * 2 / numberOfPoints;

                // Determine the position of the point
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                Vector3 position = new Vector3(x, 1f, z) + CP.Position;

                // Raycast downward to find the ground
                RaycastHit hit;
                if (Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity, LayerMask.NameToLayer("Default")))
                {
                    // Place the point at the hit position
                    System.Array.Resize(ref PP.Points, PP.Points.Length + 1);
                    PP.Points[PP.Points.Length - 1] = hit.point;
                }
                else
                {
                    Debug.LogWarning("No ground detected at " + position);
                    System.Array.Resize(ref PP.Points, PP.Points.Length + 1);
                    PP.Points[PP.Points.Length - 1] = position;
                }
            }
            ModMapManager.Instance.PatrolPoints.Add(PP);
        }

        ModMapManager.Instance.Info.ShowInBR = true;
        ModMapManager.Instance.Info.ShowInMP = true;
        ModMapManager.Instance.EnablePatrol = true;
        ModMapManager.Instance.EnableConquest = true;
        ModMapManager.Instance.EnableSabotage = true;
        ModMapManager.Instance.Info.Size = 2;


        ModMapManager.Instance.Info.MapIcon = MiniMapTX;
        Texture2D Splash = new Texture2D(2, 2);
        if (File.Exists(Path.Combine(levelsPath, "Info/loadmap.png")))
        {
            Splash.LoadImage(File.ReadAllBytes(Path.Combine(levelsPath, "Info/loadmap.png")));
            if (Splash) ModMapManager.Instance.Info.Icon = Sprite.Create(Splash, new Rect(0.0f, 0.0f, Splash.width, Splash.height), new Vector2(0.5f, 0.5f), 1);
        }
    }

    public List<SurfaceInfo> Surfaces = new List<SurfaceInfo>();
    //[Button("Load Surface Material Infos")]
    public void LoadSurfaces()
    {
        string[] lines = null;
        lines = FileManager.ReadAllLines("Common/Material/materialManagerDefine.con");

        int BlockState = 0;

        SurfaceInfo CurrentSurface = null;
        int CurrentSurfaceType = -1;
        foreach (var line in lines)
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

            else if (parts[0] == "Material.name")
            {
                if (CurrentSurface != null && CurrentSurfaceType == 0) Surfaces.Add(CurrentSurface);
                CurrentSurface = new SurfaceInfo();
                CurrentSurface.Name = parts[1].Replace("\"", "");
            }
            else if (parts[0] == "Material.type")
            {
                CurrentSurfaceType = int.Parse(parts[1]);
            }
            else if (parts[0] == "Material.resistance")
            {
                CurrentSurface.Thickness = float.Parse(parts[1]);
                CurrentSurface.Thickness *= 10;
                CurrentSurface.Thickness = 1 - CurrentSurface.Thickness;
            }
        }

        lines = FileManager.ReadAllLines("Common/Material/materialManagerDefine.con");
        foreach (var line in lines)
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

            else if (parts[0] == "Material.name")
            {
                if (CurrentSurface != null && CurrentSurfaceType == 0) Surfaces.Add(CurrentSurface);
                CurrentSurface = new SurfaceInfo();
                CurrentSurface.Name = parts[1].Replace("\"", "");
            }
        }
    }

    //[Button("Load Env Maps")]
    public void LoadEnvMaps()
    {
        if (FileManager.Exists("Envmaps/EnvMap0.dds"))
        {
            Cubemap EnvMap = DDSLoader.LoadDDSCubemap("Envmaps/EnvMap0.dds");
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflection = EnvMap;
        }
        else
        {
            Debug.LogWarning("EnvMap Not Found For " + SelectedLevel);
        }
    }



    /*private void MountArchive(string archivePath, string mountPath)
    {
        try
        {
        if(Directory.Exists(mountPath))Directory.CreateDirectory(mountPath);
            ZipFile.ExtractToDirectory(archivePath,mountPath,System.Text.Encoding.ASCII,true);

            Debug.Log($"Successfully mounted {archivePath} to {mountPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to mount archive {archivePath} to {mountPath}: {ex.Message}");
        }
    }*/

    public void RefreshModFolders()
    {
        ModFolders.Clear();
        string modsPath = NormalizePath(Path.Combine(Bf2Root, "mods"));

        if (Directory.Exists(modsPath))
        {
            string[] folders = Directory.GetDirectories(modsPath);
            foreach (string folder in folders)
            {
                ModFolders.Add(Path.GetFileName(NormalizePath(folder)));
            }

            // Sort ModFolders case-insensitively
            ModFolders.Sort(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Debug.LogError("Mods folder not found in the selected root.");
        }

        RefreshLevels();  // Refresh levels when mod folder changes

    }

    public void RefreshLevels()
    {
        Levels.Clear();
        if (!string.IsNullOrEmpty(SelectedModFolder))
        {
            string levelsPath = NormalizePath(Path.Combine(Bf2Root, "mods", SelectedModFolder, "Levels"));
            Debug.LogError($"Levels Path : {levelsPath}");
            if (Directory.Exists(levelsPath))
            {
                string[] folders = Directory.GetDirectories(levelsPath);
                foreach (string folder in folders)
                {
                    if (!File.Exists(Path.Combine(levelsPath, folder) + "/client.zip") && !File.Exists(Path.Combine(levelsPath, folder) + "/server.zip")) continue;

                    Levels.Add(Path.GetFileName(NormalizePath(folder)));
                }
                InitMod();
                RefreshModes();
                // Sort Levels case-insensitively
                // Levels.Sort(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                Debug.LogError("Levels folder not found in the selected mod.");
            }
        }
    }

    public void RefreshModes()
    {
        Modes.Clear();
        if (!string.IsNullOrEmpty(SelectedModFolder))
        {
            //string ModesPath = NormalizePath(Path.Combine(Bf2Root, "mods", SelectedModFolder, "Levels", "GameModes"));
            string ModesPath = "GameModes";
            Debug.LogError($"Modes Path : {ModesPath}");
            if (FileManager.Exists(Path.Combine(ModesPath, "sp1/16/GamePlayObjects.con")))
                Modes.Add("SP_16");

            if (FileManager.Exists(Path.Combine(ModesPath, "gpm_coop/16/GamePlayObjects.con")))
                Modes.Add("coop_16");

            if (FileManager.Exists(Path.Combine(ModesPath, "gpm_cq/16/GamePlayObjects.con")))
                Modes.Add("conquest_16");

            if (FileManager.Exists(Path.Combine(ModesPath, "gpm_cq/32/GamePlayObjects.con")))
                Modes.Add("conquest_32");

            if (FileManager.Exists(Path.Combine(ModesPath, "gpm_cq/64/GamePlayObjects.con")))
                Modes.Add("conquest_64");

        }
    }

    private string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

public class ModFolderDropdownAttribute : PropertyAttribute
{
}

public class LevelDropdownAttribute : PropertyAttribute
{
}

public class GameModeDropDownAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ModFolderDropdownAttribute))]
public class ModFolderDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MapLoader mapLoader = (MapLoader)property.serializedObject.targetObject;

        if (mapLoader.ModFolders == null || mapLoader.ModFolders.Count == 0)
        {
            EditorGUI.LabelField(position, label.text, "No mod folders available");
            return;
        }

        int index = Mathf.Max(0, mapLoader.ModFolders.FindIndex(s => s.Equals(property.stringValue, System.StringComparison.OrdinalIgnoreCase)));
        int newIndex = EditorGUI.Popup(position, label.text, index, mapLoader.ModFolders.ToArray());
        if (newIndex != index)
        {
            property.stringValue = mapLoader.ModFolders[newIndex];
            mapLoader.Invoke(nameof(mapLoader.RefreshLevels), 0.1f);  // Refresh levels when mod folder changes
        }
    }
}

[CustomPropertyDrawer(typeof(LevelDropdownAttribute))]
public class LevelDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MapLoader mapLoader = (MapLoader)property.serializedObject.targetObject;

        if (mapLoader.Levels == null || mapLoader.Levels.Count == 0)
        {
            EditorGUI.LabelField(position, label.text, "No levels available");
            return;
        }

        int index = Mathf.Max(0, mapLoader.Levels.FindIndex(s => s.Equals(property.stringValue, System.StringComparison.OrdinalIgnoreCase)));
        int newIndex = EditorGUI.Popup(position, label.text, index, mapLoader.Levels.ToArray());
        if (newIndex != index)
        {
            property.stringValue = mapLoader.Levels[newIndex];
            mapLoader.Invoke(nameof(mapLoader.RefreshModes), 0.1f);  // Refresh levels when mod folder changes
        }
    }
}

[CustomPropertyDrawer(typeof(GameModeDropDownAttribute))]
public class GameModeDropDownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MapLoader mapLoader = (MapLoader)property.serializedObject.targetObject;

        if (mapLoader.Modes == null || mapLoader.Modes.Count == 0)
        {
            EditorGUI.LabelField(position, label.text, "No Modes available");
            return;
        }

        int index = Mathf.Max(0, mapLoader.Modes.FindIndex(s => s.Equals(property.stringValue, System.StringComparison.OrdinalIgnoreCase)));
        int newIndex = EditorGUI.Popup(position, label.text, index, mapLoader.Modes.ToArray());
        if (newIndex != index)
        {
            property.stringValue = mapLoader.Modes[newIndex];
        }
    }
}
#endif
