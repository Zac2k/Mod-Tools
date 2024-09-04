#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using System.IO;
using System;
using Newtonsoft.Json;

using UnityEditor;
using static CWModUtility;

public class TextureAtlaser
{

    public static string SceneName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    public static string TexRoot = "Assets/WorkSpace/PackedTextures/";

    public static Shader AtlasOpaqueShader => Shader.Find("ZicZac/HighGraphic/Atlas/Opaque");
    public static Shader AtlasCutoutShader => Shader.Find("ZicZac/HighGraphic/Atlas/Cutout");

    public static int Padding = 8;

    public static TextureImporterFormat DTF = TextureImporterFormat.ASTC_10x10;
    public static TextureImporterFormat PTF = TextureImporterFormat.ASTC_10x10;
    public static TextureImporterFormat NTF = TextureImporterFormat.ASTC_10x10;

    public static TextureAtlaser Instance;



    public class SaveData
    {
        public Dictionary<string, TextureGroup> T_Groups = new Dictionary<string, TextureGroup>();
        public List<PackedTexture> PackedTextures = new List<PackedTexture>();
    }


    public class PackedTexture
    {
        [JsonIgnore] public Texture2D[] Textures = new Texture2D[3];

        public int ID;
        public Dictionary<int, TextureSpace> Spaces = new Dictionary<int, TextureSpace>();
        public List<TextureSpace> Spaces_List = new List<TextureSpace>();

        [JsonIgnore] public Material OpaqueMat;
        [JsonIgnore] public Material CutoutMat;
        [NonSerialized] public bool Initialized;
        public void Initialize(int size)
        {
            if (Initialized) return;
            Initialized = true;
            if (Spaces.Count == 0)
            {
                Spaces.Add(Cells, new TextureSpace());
                Spaces[_Cells].Initialize(size, Vector2.zero);
                Spaces[_Cells].ID = _Cells;
                Spaces_List = Spaces.Values.ToList();
            }

            Textures[0] = new Texture2D(size, size, TextureFormat.ARGB32, true, false);
            Textures[1] = new Texture2D(size, size, TextureFormat.ARGB32, true, true);
            Textures[2] = new Texture2D(size, size, TextureFormat.ARGB32, true, false);
            CreateDirectory(TexRoot);

            if (OpaqueMat == null)
            {
                if (File.Exists(TexRoot + SceneName + "_" + ID + "_OpaqueMat.mat"))
                {
                    OpaqueMat = AssetDatabase.LoadAssetAtPath<Material>(TexRoot + SceneName + "_" + ID + "_OpaqueMat.mat");
                }

            }

            if (CutoutMat == null)
            {
                if (File.Exists(TexRoot + SceneName + "_" + ID + "_CutoutMat.mat"))
                {
                    CutoutMat = AssetDatabase.LoadAssetAtPath<Material>(TexRoot + SceneName + "_" + ID + "_CutoutMat.mat");
                }

            }




            for (int i = 0; i < Textures[0].height; i++) { for (int j = 0; j < Textures[0].width; j++) { Textures[0].SetPixel(i, j, new Color(1, 0, 1, 1)); } }
            for (int i = 0; i < Textures[1].height; i++) { for (int j = 0; j < Textures[1].width; j++) { Textures[1].SetPixel(i, j, new Color(0.5f, 0.5f, 1, 1)); } }
            for (int i = 0; i < Textures[2].height; i++) { for (int j = 0; j < Textures[2].width; j++) { Textures[2].SetPixel(i, j, new Color(1, 0.5f, 0, 0.5f)); } }

        }

        public void Save(Shader shader)
        {
            byte[] Tex0 = Textures[0].EncodeToPNG();
            if (File.Exists(TexRoot + SceneName + "_" + ID + "_Diff.png") && new FileInfo(TexRoot + SceneName + "_" + ID + "_Diff.png").Length == Tex0.Length) return;
            File.WriteAllBytes(TexRoot + SceneName + "_" + ID + "_Diff.png", Tex0);
            File.WriteAllBytes(TexRoot + SceneName + "_" + ID + "_NRM.png", Textures[1].EncodeToPNG());
            File.WriteAllBytes(TexRoot + SceneName + "_" + ID + "_PBR.png", Textures[2].EncodeToPNG());

            AssetDatabase.Refresh();
            SetTextureFormat(TexRoot + SceneName + "_" + ID + "_Diff.png", DTF, -5);
            SetTextureFormat(TexRoot + SceneName + "_" + ID + "_NRM.png", NTF);
            SetTextureFormat(TexRoot + SceneName + "_" + ID + "_PBR.png", PTF);


            if (Textures[1] != AssetDatabase.LoadAssetAtPath<Texture2D>(TexRoot + SceneName + "_" + ID + "_NRM.png"))
            {
                TextureImporter TI = AssetImporter.GetAtPath(TexRoot + SceneName + "_" + ID + "_NRM.png") as TextureImporter;
                if (TI != null)
                {
                    TI.textureType = TextureImporterType.NormalMap;
                    TI.isReadable = true;
                    TI.sRGBTexture = false;
                    TI.textureCompression = TextureImporterCompression.Uncompressed;
                    AssetDatabase.ImportAsset(TexRoot + SceneName + "_" + ID + "_NRM.png");
                }


                Debug.LogError("Pass_");
                Textures[0] = AssetDatabase.LoadAssetAtPath<Texture2D>(TexRoot + SceneName + "_" + ID + "_Diff.png");

                Textures[1] = AssetDatabase.LoadAssetAtPath<Texture2D>(TexRoot + SceneName + "_" + ID + "_NRM.png");

                Textures[2] = AssetDatabase.LoadAssetAtPath<Texture2D>(TexRoot + SceneName + "_" + ID + "_PBR.png");


                //SetTextureToReadable(Textures[0]);
                //SetTextureToReadable(Textures[2]);
                Debug.LogError("Pass_All");
            }



            if (OpaqueMat == null)
            {
                if (!File.Exists(TexRoot + SceneName + "_" + ID + "_OpaqueMat.mat"))
                {
                    AssetDatabase.CreateAsset(new Material(AtlasOpaqueShader), TexRoot + SceneName + "_" + ID + "_OpaqueMat.mat");
                }
                OpaqueMat = AssetDatabase.LoadAssetAtPath<Material>(TexRoot + SceneName + "_" + ID + "_OpaqueMat.mat");
                OpaqueMat.SetTexture("_MainTex", Textures[0]);
                OpaqueMat.SetTexture("_BumpMap", Textures[1]);
                OpaqueMat.SetTexture("_PBR", Textures[2]);
            }

            if (CutoutMat == null)
            {
                if (!File.Exists(TexRoot + SceneName + "_" + ID + "_CutoutMat.mat"))
                {
                    AssetDatabase.CreateAsset(new Material(AtlasCutoutShader), TexRoot + SceneName + "_" + ID + "_CutoutMat.mat");
                }
                CutoutMat = AssetDatabase.LoadAssetAtPath<Material>(TexRoot + SceneName + "_" + ID + "_CutoutMat.mat");
                CutoutMat.SetTexture("_MainTex", Textures[0]);
                CutoutMat.SetTexture("_BumpMap", Textures[1]);
                CutoutMat.SetTexture("_PBR", Textures[2]);
            }
        }



        int OverflowLimit = 1000;
        public int AddTextureGroup(TextureGroup TG)
        {

            if (OverflowLimit <= 0) { OverflowLimit = 1000; Debug.LogError("Failed Due To OverFlow"); return -1; }
            int NearestIndex = -1;
            int NearestDiff = int.MaxValue;
            foreach (TextureSpace TS in Spaces.Values)
            {
                if (TS.OccupantGUID == TG.ID) { OverflowLimit = 1000; Debug.LogError("Returned Because TextureGroup Is Already Assigned"); return 1; }
                if (TS.Occupied) continue;
                Texture2D TX = TG.Textures[0];
                if ((TS.Size.x * TS.Size.y) - (TG.Size.x * TG.Size.y) < (NearestDiff) - (TG.Size.x * TG.Size.y))
                {
                    if ((int)((TS.Size.x * TS.Size.y) - (TG.Size.x * TG.Size.y)) < 0) continue;
                    NearestDiff = (int)((TS.Size.x * TS.Size.y) - (TG.Size.x * TG.Size.y));
                    NearestIndex = TS.ID;
                }
            }

            if (NearestIndex == -1) { OverflowLimit = 1000; Debug.LogError("Failed Due To No Higher Match Found"); return 0; }
            if (NearestDiff != 0) { Debug.LogError("Failed Due To No Match : Proceding To Subdivide"); OverflowLimit--; Subdivide(NearestIndex); return AddTextureGroup(TG); }
            OverflowLimit = 1000;


            TextureSpace NTS = Spaces[NearestIndex];
            Texture2D NTD = new Texture2D((int)TG.Size.x, (int)TG.Size.y);
            Texture2D NTN = new Texture2D((int)TG.Size.x, (int)TG.Size.y);
            Texture2D NTP = new Texture2D((int)TG.Size.x, (int)TG.Size.y);


            if (TG.Textures[0]) NTD.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(TG.Textures[0])));
            if (TG.Textures[1]) NTN.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(TG.Textures[1])));
            if (TG.Textures[2]) NTP.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(TG.Textures[2])));

            bool CD = IsColorTexture(TG.Textures[0]);
            //bool CN = IsColorTexture(TG.Textures[1]);
            bool CP = IsColorTexture(TG.Textures[2]);

            NTD = NTD.GetReadable((int)(NTS.Size.x - Padding), (int)(NTS.Size.y - Padding));
            NTN = NTN.GetReadable((int)(NTS.Size.x - Padding), (int)(NTS.Size.y - Padding));
            NTP = NTP.GetReadable((int)(NTS.Size.x - Padding), (int)(NTS.Size.y - Padding));

            if (!TG.Textures[1])
            {
                Debug.Log($"Normal Map Is Null For {TG.mat.name}");

                for (int i = 0; i < NTN.height; i++) { for (int j = 0; j < NTN.width; j++) { NTN.SetPixel(i, j, new Color(0.5f, 0.5f, 1, 1)); } }
            }
            if (!TG.Textures[2])
            {
                Debug.Log($"PBR Map Is Null For {TG.mat.name}");
                for (int i = 0; i < NTP.height; i++) { for (int j = 0; j < NTP.width; j++) { NTP.SetPixel(i, j, TG.PBR_Col); } }
            }

            Vector2 Center = new Vector2(NTS.Pos.x + (NTS.Size.x / 2), NTS.Pos.y + (NTS.Size.y / 2));
            Color BleedCol = Color.black;
            Vector2 N_Pos = new Vector2(0, 0);

            for (int i = (int)NTS.Pos.x; i < NTS.Pos.x + (NTS.Size.x); i++)
            {
                for (int j = (int)NTS.Pos.y; j < NTS.Pos.y + (NTS.Size.y); j++)
                {

                    //int Bleed_Offset_X=(i-(int)NTS.Pos.x)*((j-(int)NTS.Size.y)/(int)NTS.Size.y);//*(i-(int)NTS.Size.x);
                    //int Bleed_Offset_Y=(j-(int)NTS.Pos.y)*((i-(int)NTS.Size.x)/(int)NTS.Size.x);//*(i-(int)NTS.Size.x);

                    //int Bleed_Offset_X=(int)((float)(i-(int)NTS.Pos.x)/Mathf.Max((j-(int)NTS.Size.y),0.0001f));//*(i-(int)NTS.Size.x);
                    //int Bleed_Offset_Y=(int)((float)(j-(int)NTS.Pos.y)/Mathf.Max((i-(int)NTS.Size.x),0.0001f));//*(i-(int)NTS.Size.x);
                    // if(TG.Textures[0].GetPixel(i,j)==null){ Debug.LogError("Failed To Process Texture For "+SM.name); break;}
                    // Debug.LogError(NT.GetPixel(i,j)); break;
                    if (i >= (int)NTS.Pos.x + (Padding / 2) && j >= (int)NTS.Pos.y + (Padding / 2) && i < NTS.Pos.x + (NTS.Size.x - (Padding / 2)) && j <= NTS.Pos.y + (NTS.Size.y - (Padding / 2)))
                    {
                        Textures[0].SetPixel(i, j, NTD.GetPixel((i - ((int)NTS.Pos.x + (Padding / 2))), j - ((int)NTS.Pos.y + (Padding / 2))).ToLinear(!CD) * TG.mat.color);
                        Textures[1].SetPixel(i, j, NTN.GetPixel((i - ((int)NTS.Pos.x + (Padding / 2))), j - ((int)NTS.Pos.y + (Padding / 2))));
                        Textures[2].SetPixel(i, j, NTP.GetPixel((i - ((int)NTS.Pos.x + (Padding / 2))), j - ((int)NTS.Pos.y + (Padding / 2))).ToLinear(!CP) * TG.PBR_Col);
                    }
                    else
                    {
                        N_Pos = new Vector2(i, j);
                        for (int k = 0; k < Padding * 2; k++)
                        {
                            N_Pos = Vector2.MoveTowards(N_Pos, Center, 1);
                            //if(N_Pos.x>=(int)NTS.Pos.x+(Padding/2))N_Pos.x=(int)NTS.Pos.x+(Padding/2);
                            //if(N_Pos.y>=(int)NTS.Pos.y+(Padding/2))N_Pos.y=(int)NTS.Pos.y+(Padding/2);

                            if (N_Pos.x >= (int)NTS.Pos.x + (Padding / 2) && N_Pos.y >= (int)NTS.Pos.y + (Padding / 2) && N_Pos.x < NTS.Pos.x + (NTS.Size.x - (Padding / 2)) && N_Pos.y <= NTS.Pos.y + (NTS.Size.y - (Padding / 2)))
                            {
                                //BleedCol=NTD.GetPixel(((int)N_Pos.x-((int)NTS.Pos.x+(Padding/2))),(int)N_Pos.y-((int)NTS.Pos.y+(Padding/2)));
                                Textures[0].SetPixel(i, j, NTD.GetPixel(((int)N_Pos.x - ((int)NTS.Pos.x + (Padding / 2))), (int)N_Pos.y - ((int)NTS.Pos.y + (Padding / 2))).ToLinear(!CD) * TG.mat.color * new Color(1, 1, 1, 0.5f));
                                Textures[1].SetPixel(i, j, NTN.GetPixel(((int)N_Pos.x - ((int)NTS.Pos.x + (Padding / 2))), (int)N_Pos.y - ((int)NTS.Pos.y + (Padding / 2))) * new Color(1, 1, 1, 0.5f));
                                Textures[2].SetPixel(i, j, NTP.GetPixel(((int)N_Pos.x - ((int)NTS.Pos.x + (Padding / 2))), (int)N_Pos.y - ((int)NTS.Pos.y + (Padding / 2))).ToLinear(!CP) * TG.PBR_Col * new Color(1, 1, 1, 0.5f));
                                break;
                            }
                        }

                        // Textures[0].SetPixel(i,j,BleedCol);
                        //  Textures[1].SetPixel(i,j, NTN.GetPixel((i-((int)NTS.Pos.x+(Padding/2))),j-((int)NTS.Pos.y+(Padding/2))));
                        // Textures[2].SetPixel(i,j, NTP.GetPixel((i-((int)NTS.Pos.x+(Padding/2))),j-((int)NTS.Pos.y+(Padding/2))));
                    }
                    // PackedTextures[0].Textures[0].SetPixel(i,j, Color.green);
                }
            }
            NTS.Occupied = true;
            NTS.OccupantGUID = TG.ID;
            TG.SpaceID = NTS.ID;
            TG.PackedTextureID = ID;
            return 1;
        }

        int _Cells = 0;
        int Cells { get { _Cells++; return _Cells; } }
        void Subdivide(int Index)
        {
            TextureSpace TS0 = Spaces[Index];

            TextureSpace TS1 = new TextureSpace(); TS1.Initialize((int)TS0.Size.x / 2, TS0.Pos); TS1.ID = Cells;
            TextureSpace TS2 = new TextureSpace(); TS2.Initialize((int)TS0.Size.x / 2, TS0.Pos + (new Vector2(0, TS1.Size.x))); TS2.ID = Cells;
            TextureSpace TS3 = new TextureSpace(); TS3.Initialize((int)TS0.Size.x / 2, TS0.Pos + (new Vector2(TS1.Size.x, 0))); TS3.ID = Cells;
            TextureSpace TS4 = new TextureSpace(); TS4.Initialize((int)TS0.Size.x / 2, TS0.Pos + (new Vector2(TS1.Size.x, TS1.Size.x))); TS4.ID = Cells;

            Spaces.Remove(Index); Spaces.Add(TS1.ID, TS1); Spaces.Add(TS2.ID, TS2); Spaces.Add(TS3.ID, TS3); Spaces.Add(TS4.ID, TS4);

            Spaces_List = Spaces.Values.ToList();

        }
    }


    public class TextureSpace
    {
        public int ID;
        public bool Occupied;

        public string OccupantGUID = "";

        //[JsonIgnore]public Vector2 Pos{get{return new Vector2(Pos_X,Pos_Y);}set{Pos_X=value.x;Pos_Y=value.y;}}
        public Vector2 Pos;


        //[JsonIgnore]public Vector2 Size{get{return new Vector2(Size_X,Size_Y);}set{Size_X=value.x;Size_Y=value.y;}}
        public Vector2 Size;



        bool Initialized;
        public void Initialize(int size, Vector2 pos)
        {
            if (Initialized) return;
            Size = new Vector2(size, size);
            Pos = pos;
            Initialized = true;
        }


    }






    public class TextureGroup
    {
        public string Name;
        public string ID;

        [JsonIgnore] public Material mat;

        [JsonIgnore] public Color PBR_Col = new Color(1, 0, 0, 0);
        [JsonIgnore] public Texture2D[] Textures = new Texture2D[3];
        public float Pos_X, Pos_Y;
        [JsonIgnore] public Vector2 Pos { get { return new Vector2(Pos_X, Pos_Y); } set { Pos_X = value.x; Pos_Y = value.y; } }


        public float Size_X, Size_Y;
        [JsonIgnore] public Vector2 Size { get { return new Vector2(Size_X, Size_Y); } set { Size_X = value.x; Size_Y = value.y; } }


        public int SpaceID;
        public int PackedTextureID;

    }

    [JsonIgnore] public List<TextureGroup> T_Groups_List = new List<TextureGroup>();
    public Dictionary<string, TextureGroup> T_Groups = new Dictionary<string, TextureGroup>();
    public List<PackedTexture> PackedTextures = new List<PackedTexture>();

    public Color FloodCol;
    public TextureFormat TFormat;

    public int MaxTexSize = 512;
    public int AtlasSize = 2048;

    public int _Padding = 12;
    public GameObject WorkGroup;


    public TextureAtlaser(int atlasSize, int maxTexSize)
    {
        MaxTexSize = maxTexSize;
        AtlasSize = atlasSize;
        if (File.Exists(TexRoot + SceneName + "_" + "PackedTextureData.json"))
        {
            SaveData SD = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(TexRoot + SceneName + "_" + "PackedTextureData.json"));


            PackedTextures = SD.PackedTextures;
            T_Groups = SD.T_Groups;
            T_Groups_List = T_Groups.Values.ToList();

            /*BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream("assets/TexturePackerData.json", FileMode.Open);

            SaveData SD = bf.Deserialize(stream) as SaveData;
            stream.Close();
         PackedTextures=SD.PackedTextures;
       T_Groups=SD.T_Groups;*/
            foreach (PackedTexture PT in SD.PackedTextures) PT.Initialize(MaxTexSize);

        }
    }

    public void Save()
    {

        // BinaryFormatter bf = new BinaryFormatter();
        //  FileStream stream = new FileStream("assets/TexturePackerData.dat", FileMode.Create);
        SaveData SD = new SaveData();
        SD.PackedTextures = PackedTextures;
        SD.T_Groups = T_Groups;

        // bf.Serialize(stream, SD);
        // stream.Close();
        File.WriteAllText(TexRoot + SceneName + "_" + "PackedTextureData.json", JsonConvert.SerializeObject(SD));

    }


    public void PackMaterials(Material[] Mats)
    {
        Padding = _Padding;
        PackedTextures.Clear();


        Debug.Log("Now Packing Textures For " + Mats.Length + " Materials");

        float Progress = 0;
        foreach (Material SM in Mats)
        {
            Progress++;
            if (
                SM.shader == StandardShaderConverter.OpaqueShader ||
                SM.shader == StandardShaderConverter.CutoutShader
                )
            {

                if (ModMapManager.Stop) return;
                ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Packing Textures", $"Packing Textures For {SM.name}", Progress / Mats.Length);
                long LID = 0; string GUID = "";
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(SM, out GUID, out LID);
                TextureGroup TG = null;
                if (!T_Groups.ContainsKey(GUID))
                {
                    Debug.Log("Now Packing Textures For " + SM.name, SM);

                    TG = new TextureGroup();
                    TG.Textures[0] = SM.GetTexture("_MainTex") as Texture2D;
                    TG.Textures[1] = SM.GetTexture("_BumpMap") as Texture2D;
                    TG.Textures[2] = SM.GetTexture("_PBR") as Texture2D;
                    TG.PBR_Col = new Color(1, SM.GetFloat("_Metallic"), 1, SM.GetFloat("_Glossiness"));
                    TG.ID = GUID;
                    TG.Name = SM.name;
                    TG.mat = SM;
                    T_Groups.Add(GUID, TG);
                }
                else
                {
                    Debug.Log("Found Duplicate Texture Group For Mat With GUID " + GUID, SM);
                    TG = T_Groups[GUID];
                    if (T_Groups[GUID].Textures[0] == null)
                    {
                        TG.Textures[0] = SM.GetTexture("_MainTex") as Texture2D;
                        TG.Textures[1] = SM.GetTexture("_BumpMap") as Texture2D;
                        TG.Textures[2] = SM.GetTexture("_PBR") as Texture2D;
                        TG.mat = SM;
                    }
                }
                int size = TG.Textures[0] ? Mathf.Min(MaxTexSize, ToPowerOf2(Mathf.Min(TG.Textures[0].width, TG.Textures[0].height))) : 16;
                TG.Size = new Vector2(size, size);
                // TG.Size=new Vector2(Mathf.Min(MaxTextureSize,TG.Textures[0].height),Mathf.Min(MaxTextureSize,TG.Textures[0].height));
                PackTextureGroup(TG);

            }
        }
        T_Groups_List = T_Groups.Values.ToList();
        // Save();
        foreach (PackedTexture PT in PackedTextures)
        {
            PT.Save(AtlasOpaqueShader);
            //   foreach(Texture T in PT.Textures)Texture2D.DestroyImmediate(T);
        }
    }

    // Update is called once per frame

    public Mesh GetMesh(MeshRenderer MR, Mesh Mainmesh)
    {

        MeshFilter MF = MR.GetComponent<MeshFilter>();
        if (MF == null) return null;
        if (Mainmesh == null) { Debug.LogError($"{MF.name} Is Missing A Mesh", MF.gameObject); return null; }
        string MUID = GetMRS(MR, MF);
        Vector2[] UVs = Mainmesh.uv;
        Mesh mesh;
        if (!NewMeshes.TryGetValue(MUID, out mesh))
        {

            //for(int i=0; i<UVs.Length; i++){
            //    if(UVs[i].x>1.05f||UVs[i].x<-0.05f||UVs[i].y>1.05f||UVs[i].y<-0.05f){mesh=Mainmesh.GetSlicedMesh();break;}
            //}
            if (mesh == null) mesh = MonoBehaviour.Instantiate(Mainmesh);

            if (ModMapManager.Stop) return null;
            ModMapManager.Stop = EditorUtility.DisplayCancelableProgressBar($"Processing Meshes", $"Preparing {Mainmesh.name} For Atlas", ModMapManager.FullProgress);

            //mesh.SetMeshUvsTo10Bounds();
            //mesh=mesh.GetSlicedMesh();

            Dictionary<int, Vector2> modifiedUVs = new Dictionary<int, Vector2>(); // create a dictionary to store the modified UVs
            for (int i = 0; i < MR.sharedMaterials.Length; i++)
            {
                if (i >= mesh.subMeshCount) break;
                Material SM = StandardShaderConverter.GetConvertedMat(MR.sharedMaterials[i]);
                if (SM == null) continue;

                long LID = 0; string GUID = "";
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(SM, out GUID, out LID);

                Vector2 NormalizedPos = new Vector2(1, 1);
                Vector2 NormalizedScale = new Vector2(1, 1);
                // float NormalizedPadding=0;
                if (T_Groups.ContainsKey(GUID))
                {
                    TextureSpace TS = PackedTextures[T_Groups[GUID].PackedTextureID].Spaces[T_Groups[GUID].SpaceID];

                    //NormalizedPadding=(float)Padding/(float)BaseTextureSize;

                    NormalizedPos.x = (float)(TS.Pos.x + (Padding / 2f)) / (float)AtlasSize;
                    NormalizedPos.y = (float)(TS.Pos.y + (Padding / 2f)) / (float)AtlasSize;

                    NormalizedScale.x = (float)(TS.Size.x - Padding) / (float)AtlasSize;
                    NormalizedScale.y = (float)(TS.Size.y - Padding) / (float)AtlasSize;


                    UVs = mesh.uv; // get the UVs for the first UV set
                    Vector2 MatOffset = MR.sharedMaterials[i].GetTextureOffset("_MainTex");
                    Vector2 Matscale = MR.sharedMaterials[i].GetTextureScale("_MainTex");
                    int[] indices = mesh.GetIndices(i); // get the indices for the submesh i
                    for (int j = 0; j < indices.Length; j++) // loop over the indices
                    {
                        int index = indices[j]; // get the index of the vertex
                        Vector2 modifiedUV; // declare a variable to store the modified UV of the vertex
                        if (modifiedUVs.TryGetValue(index, out modifiedUV)) // check if the vertex has already been modified
                        {
                            UVs[index] = modifiedUV; // use the existing modified UV
                        }
                        else // if the vertex has not been modified yet
                        {
                            UVs[index] += MatOffset;
                            UVs[index] *= Matscale; // modify the UV of the vertex
                            UVs[index] *= NormalizedScale; // modify the UV of the vertex
                            UVs[index] += NormalizedPos;
                            modifiedUVs.Add(index, UVs[index]); // add the modified UV to the dictionary
                        }
                    }
                    mesh.uv = UVs; // set the modified UVs back to the mesh
                    //MF.sharedMesh.UploadMeshData(true);
                }
            }
            NewMeshes.Add(MUID, mesh);
            return mesh;
        }
        return mesh;

    }

    public Vector4 GetUVOffset(Material Mat, Mesh mesh, int SubMesh)
    {
        if (Mat == null || mesh == null) return new Vector4(0, 0, 1, 1);
        bool Fited = true;
        int[] Tris = mesh.GetTriangles(SubMesh);
        if (mesh != null)
        {
            Vector2[] UVs = mesh.uv;
            for (int i = 0; i < Tris.Length; i++)
            {
                if (UVs[Tris[i]].x > 1.05f || UVs[Tris[i]].x < -0.05f || UVs[Tris[i]].y > 1.05f || UVs[Tris[i]].y < -0.05f) { Fited = false; break; }
            }
        }
        if (Fited) return new Vector4(0, 0, 1, 1);
        long LID = 0; string GUID = "";
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Mat, out GUID, out LID);
        foreach (PackedTexture PT in PackedTextures)
        {
            if (PT == null || PT.Spaces == null) continue;
            foreach (TextureSpace TS in PT.Spaces.Values)
            {
                if (TS == null) continue;
                if (TS.OccupantGUID.CompareTo(GUID) == 0) return new Vector4(
                        (float)(TS.Pos.x + (Padding / 2f)) / (float)AtlasSize,
                        (float)(TS.Pos.y + (Padding / 2f)) / (float)AtlasSize,
                        (float)(TS.Size.x - Padding) / (float)AtlasSize,
                        (float)(TS.Size.y - Padding) / (float)AtlasSize
                        );
            }
        }
        return Vector2.zero;
    }

    public Material GetMat(Material Mat)
    {
        if (Mat == null) return null;
        long LID = 0; string GUID = "";
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Mat, out GUID, out LID);
        foreach (PackedTexture PT in PackedTextures)
        {
            if (PT == null || PT.Spaces == null) continue;
            foreach (TextureSpace TS in PT.Spaces.Values)
            {
                if (TS == null) continue;
                if (TS.OccupantGUID.CompareTo(GUID) == 0)
                {
                    if (StandardShaderConverter.IsCutoutShader(Mat.shader.name)) return PT.CutoutMat; else return PT.OpaqueMat;
                }
            }
        }
        return Mat;
    }
    public string GetMRS(MeshRenderer MR, MeshFilter MF)
    {
        string Checksum = GetMeshChecksum(MF.sharedMesh);
        foreach (Material Mat in MR.sharedMaterials) Checksum += GetMaterialChecksum(Mat);
        return Checksum;
    }

    public Dictionary<string, Mesh> NewMeshes = new Dictionary<string, Mesh>();


    int StackLimit = 2;
    public void PackTextureGroup(TextureGroup TG)
    {

        if (StackLimit <= 0) { StackLimit = 2; Debug.LogError("Assignment Failed Due To Exceding Stack Limit"); return; }
        if (PackedTextures.Count == 0)
        {
            PackedTexture PT = new PackedTexture(); PT.ID = PackedTextures.Count; PT.Initialize(AtlasSize);
            PackedTextures.Add(PT);
        }

        foreach (PackedTexture PT in PackedTextures)
        {
            if (PT.AddTextureGroup(TG) == 0) continue; else { StackLimit = 2; return; }
        }

        Debug.LogError("Failed To Find Compatible Texture : Proceeding To Create A New One And Retry");
        PackedTexture NPT = new PackedTexture(); NPT.ID = PackedTextures.Count; NPT.Initialize(AtlasSize);
        PackedTextures.Add(NPT);
        StackLimit--;
        PackTextureGroup(TG);


    }



}
#endif
