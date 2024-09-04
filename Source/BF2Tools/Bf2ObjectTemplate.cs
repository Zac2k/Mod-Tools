using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static BF2FileManager;

public class Bf2ObjectTemplate : MonoBehaviour
{
    [System.Serializable]
    public class RoadSettings
    {
        public string Name;
        public string SurfaceMaterial;
        public string PrimaryTexturePath;
        public Vector2 PrimaryTextureScale;
        public string SecondaryTexturePath;
        public Vector2 SecondaryTextureScale;

        public RoadSettings(string conFilePath)
        {
            ParseConFile(conFilePath);
        }

        private void ParseConFile(string filePath)
        {
            string[] lines = FileManager.ReadAllLines(filePath);
            bool isPrimaryTexture = false;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

                string[] parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                string command = parts[0];
                string argument = parts[1].Trim('"');

                switch (command)
                {
                    case "RoadTemplate.SetName":
                        Name = argument;
                        break;
                    case "RoadTemplate.SetMaterial":
                        SurfaceMaterial = argument;
                        break;
                    case "RoadTemplate.SetIsPrimaryTexture":
                        isPrimaryTexture = argument == "1";
                        break;
                    case "RoadTemplateTexture.SetTextureFile":
                        if (isPrimaryTexture)
                        {
                            PrimaryTexturePath = argument;
                        }
                        else
                        {
                            SecondaryTexturePath = argument;
                        }
                        break;
                    case "RoadTemplateTexture.SetScale":
                        if (isPrimaryTexture)
                        {
                            PrimaryTextureScale = ParseScale(argument);
                        }
                        else
                        {
                            SecondaryTextureScale = ParseScale(argument);
                        }
                        break;
                }
            }

        }

        private Vector2 ParseScale(string scaleString)
        {
            string[] scaleParts = scaleString.Split('/');
            if (scaleParts.Length == 2 &&
                float.TryParse(scaleParts[0], out float x) &&
                float.TryParse(scaleParts[1], out float y))
            {
                return new Vector2(x, y);
            }
            return new Vector2(1, 1); // default scale
        }
    }
    public enum ObjType
    {
        SimpleObject,
        Bundle,
        Ladder,
        SupplyObject,
        DestroyableObject,
        BundledMesh,
        SkinnedMesh,
        EffectBundle,
        TerrainCluster,
        Sound,
        Light,
        Road,
        SpawnPoint,
        ControlPoint,
    }
    public static string TerrainClusterName;
    public static Bf2ObjectTemplate Instance;
    public ObjType ObjectType;
    public List<int> terrainBitResolutions = new List<int>();
    public List<Vector2> terrainAnchors = new List<Vector2>();
    public List<TerrainData> terraindatas = new List<TerrainData>();
    public int CurrentTerrainIndex = -1;
    public TerrainData CurrentTerrainData => terraindatas[CurrentTerrainIndex];

    public float ConquestPointRadius { get; internal set; }

    public RoadSettings roadSettings;

    public AudioSource audioSource;

    public void Init(ObjType objType)
    {
        Init();
        ObjectType = objType;
        if (objType == ObjType.Sound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void Init()
    {
        Instance = this;
        GameObject ObjTemplates;
        if (!GameObject.Find("ObjTemplates")) ObjTemplates = new GameObject("ObjTemplates");
        else
            ObjTemplates = GameObject.Find("ObjTemplates");

        transform.parent = ObjTemplates.transform;

    }

    public static GameObject GetNewObjTemplate(string name)
    {
        GameObject ObjTemplates;
        if (!GameObject.Find("ObjTemplates")) ObjTemplates = new GameObject("ObjTemplates");
        else
            ObjTemplates = GameObject.Find("ObjTemplates");
        foreach (Transform T in ObjTemplates.transform) if (T.name == name) return Instantiate(T.gameObject);

        return null;
    }

    public static GameObject GetObjTemplate(string name)
    {
        GameObject ObjTemplates;
        if (!GameObject.Find("ObjTemplates")) ObjTemplates = new GameObject("ObjTemplates");
        else
            ObjTemplates = GameObject.Find("ObjTemplates");
        foreach (Transform T in ObjTemplates.transform) if (T.name == name) return T.gameObject;

        return null;
    }

    public void SetColliderMaterial(int ID, string name)
    {
        foreach (Transform T in GetComponentsInChildren<Transform>())
        {
            if (T.name.Equals($"Material : {ID}"))
            {
                T.name = name;

                if (name.ToLower().Contains("metal")) T.tag = "Metal";
                else
                if (name.ToLower().Contains("wire")) T.tag = "Metal";
                else
                if (name.ToLower().Contains("concre")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("trama")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("grav")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("roc")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("brick")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("cera")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("pors")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("slip")) T.tag = "Concrete";
                else
                if (name.ToLower().Contains("wood")) T.tag = "Wood";
                else
                if (name.ToLower().Contains("card")) T.tag = "Wood";
                else
                if (name.ToLower().Contains("plast")) T.tag = "Wood";
                else
                if (name.ToLower().Contains("glass")) T.tag = "Glass";
                else
                if (name.ToLower().Contains("dirt")) T.tag = "Sand";
                else
                if (name.ToLower().Contains("sand")) T.tag = "Sand";
                else
                if (name.ToLower().Contains("mud")) T.tag = "Sand";
                else
                if (name.ToLower().Contains("water")) T.tag = "Water";
            }
        }
    }

    public void AddTerrain(Vector2Int Anchor)
    {
        string TerrName = $"Cluster{CurrentTerrainIndex + 1}";
        if (!Directory.Exists("Assets/Cache/TerrainData/")) Directory.CreateDirectory("Assets/Cache/TerrainData/");
        TerrainData TD = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Cache/TerrainData/" + MapLoader.Instance.SelectedLevel + "_" + TerrName + ".asset");
        if (TD == null)
        {
            TD = new TerrainData();
            TD.name = TerrName;
            AssetDatabase.CreateAsset(TD, "Assets/Cache/TerrainData/" + MapLoader.Instance.SelectedLevel + "_" + TerrName + ".asset");
        }
        //TD.size = new Vector3(transform.localScale.x,transform.localScale.x,transform.localScale.x);
        //GameObject TerrainCluster = Terrain.CreateTerrainGameObject(TD);

        //TerrainCluster.name = $"Cluster{terrains.Count}";
        //TerrainCluster.transform.parent=transform;

        terraindatas.Add(TD);
        terrainAnchors.Add(new Vector2(Anchor.x, Anchor.y));
        terrainBitResolutions.Add(16);
        //terrains.Add(TerrainCluster.AddComponent<Terrain>());
        CurrentTerrainIndex = terraindatas.Count - 1;
        //TerrainCluster.transform.localPosition=new Vector3(Anchor.x,0,Anchor.y);

    }

    public void LoadCurrentTerrain()
    {
        CurrentTerrainData.size = new Vector3(CurrentTerrainData.size.x, CurrentTerrainData.size.y * Mathf.Pow(2, terrainBitResolutions[CurrentTerrainIndex]), CurrentTerrainData.size.z);

        GameObject TerrainCluster = Terrain.CreateTerrainGameObject(CurrentTerrainData);
        ConFileParser.SetTag(TerrainCluster, "Terrain");

        TerrainCluster.name = $"Cluster{CurrentTerrainIndex}";
        TerrainCluster.transform.parent = transform;
        if (CurrentTerrainIndex == 0)
        {
            transform.localScale = CurrentTerrainData.size;
            transform.position = new Vector3(-CurrentTerrainData.size.x / 2, 0, -CurrentTerrainData.size.z / 2);
            MapLoader.PrimaryTerrain = TerrainCluster.GetComponent<Terrain>();
        }
        TerrainCluster.transform.localPosition = new Vector3(terrainAnchors[CurrentTerrainIndex].x, 0, terrainAnchors[CurrentTerrainIndex].y);

    }
}
