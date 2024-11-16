using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using static CWModUtility;
using Object = UnityEngine.Object;
using System.Linq;

public static class StandardShaderConverter
{
#if UNITY_EDITOR
    public static Dictionary<string,Material> ConvertedMats= new Dictionary<string, Material>(); 

    public static List<Material> MaterialsWithNonStandardShader=new List<Material>();
    public static Shader OpaqueShader=>Shader.Find("ZicZac/HighGraphic/Opaque");
    public static Shader CutoutShader=>Shader.Find("ZicZac/HighGraphic/Cutout");
    public static Shader FadeShader=>Shader.Find("ZicZac/HighGraphic/Cutout");
    public static Shader TransparentShader=>Shader.Find("ZicZac/HighGraphic/Cutout");
    public static Shader GLTFShader=>Shader.Find("GLTF");
    public static List<Shader> DiffuseShaders;
    public static List<Shader> SpecularShaders;
    public static float MaxSize=1024;

    public static TextureImporterFormat TF = TextureImporterFormat.ASTC_10x10;

    public static bool IsValidShader(Material Mat){
                string MName=Mat.shader.name;
                    if(
                    MName.CompareTo("Standard")!=0&&
                    MName.CompareTo("Standard (Specular setup)")!=0&&
                    MName.CompareTo("Standard (Roughness setup)")!=0&&
                    MName.CompareTo("Autodesk Interactive")!=0&&
                    MName.CompareTo("GLTF")!=0&&
                    MName.CompareTo("Legacy Shaders/Diffuse")!=0&&
                    MName.CompareTo("Legacy Shaders/Bumped Diffuse")!=0&&
                    MName.CompareTo("Mobile/Diffuse")!=0&&
                    MName.CompareTo("Mobile/Bumped Diffuse")!=0&&
                    MName.CompareTo("Legacy Shaders/VertexLit")!=0&&
                    MName.CompareTo("Mobile/VertexLit")!=0&&
                    MName.CompareTo("Legacy Shaders/Specular")!=0&&
                    MName.CompareTo("Mobile/Bumped Specular")!=0&&
                    MName.CompareTo("Legacy Shaders/Bumped Specular")!=0&&

                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Diffuse")!=0&&
                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Bumped Diffuse")!=0&&
                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Specular")!=0&&
                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Bumped Specular")!=0&&
                    MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/VertexLit")!=0&&
                    MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/Detail")!=0&&
                    MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/Parallax")!=0&&
                    MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/Reflective")!=0&&
                    MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/Parallax Specular")!=0
                    ){return false;}
                    return true;
    }

    public static bool IsCutoutShader(string MName){
                    return(
                    MName.CompareTo("ZicZac/HighGraphic/Cutout")==0||
                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Diffuse")==0||
                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Bumped Diffuse")==0||
                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Specular")==0||
                    MName.CompareTo("Legacy Shaders/Transparent/Cutout/Bumped Specular")==0||
                    MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/VertexLit")==0||
                    MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/Detail")==0
                    );
    }

    static Material NullMat=new Material(OpaqueShader);
    public static Material GetConvertedMat(Material Mat)
    {   
        Shader ShaderToUse=OpaqueShader;
        if(Mat==null)return null;
                string ConvertedMatPath=CreateDirectory("Assets/WorkSpace/CovertedMaterials/");
                ConvertedMatPath+=$"{Mat.name}";
                string MName=Mat.shader.name;
                if(!IsValidShader(Mat))return Mat;
                int SmoothnessSource=0;

                    if(
                    MName.CompareTo("Standard")==0||
                    MName.CompareTo("Standard (Specular setup)")==0||
                    MName.CompareTo("Standard (Roughness setup)")==0||
                    MName.CompareTo("Autodesk Interactive")==0
                    ){
                        float mode=Mat.GetFloat("_Mode");
                        SmoothnessSource=Mat.GetInt("_SmoothnessTextureChannel");
                        if(mode==0)ShaderToUse=OpaqueShader;else
                        if(mode==1)ShaderToUse=CutoutShader;else
                        if(mode==2)ShaderToUse=FadeShader;else
                        if(mode==3)ShaderToUse=TransparentShader;else
                        return Mat;
                    }else
                    if(IsCutoutShader(Mat.shader.name)){
                        ShaderToUse=CutoutShader;
                    }
                    Material NMat;
                if(!ConvertedMats.TryGetValue(GetMaterialChecksum(Mat),out NMat)){
                NMat=AssetDatabase.LoadAssetAtPath<Material>(ConvertedMatPath+GetMaterialChecksum(Mat)+".mat");
                if(NMat!=null){ConvertedMats.Add(GetMaterialChecksum(Mat),NMat);
                //AssetDatabase.CreateAsset(NMat,ConvertedMatPath+GetMaterialChecksum(Mat)+".mat");
                 return NMat;}

                Debug.Log("Converting "+Mat.name+" From "+Mat.shader.name);
                Texture TX=Mat.GetTexture("_MainTex");
                Color col = Mat.color;
                Texture NRM=Mat.GetTexture("_BumpMap");

                int TexSize=0;

                if(TX!=null){

                Debug.Log($"Found DiffTex {TX.name}");
                TexSize = ToPowerOf2((int)Math.Sqrt(TX.width*TX.height));

                }else TexSize=2;

                        NMat= new Material(ShaderToUse);
                        string PBRPath=AssetDatabase.GetAssetPath(TX);
                        if(string.IsNullOrEmpty(PBRPath))PBRPath=AssetDatabase.GetAssetPath(Mat);
                        if(string.IsNullOrEmpty(PBRPath))return NullMat;
                        string[] PathSplit=PBRPath.Split('.');

                        if (PathSplit.Length < 2)
                        {
                            Debug.LogError("Path : " + PBRPath);
                            Debug.LogError($"SplitCount : {PathSplit.Length}");
                            PBRPath = "Assets/WorkSpace/CovertedMaterials/" + Mat.name + "_PBR.png";
                        }
                        else
                            PBRPath = PathSplit[PathSplit.Length - 2] + "_PBR.png";

                        Texture2D PBR=null;
                        if(!File.Exists(PBRPath)){
                        Texture2D Occ=new Texture2D(2,2);
                        Texture2D MetalicSpecular=new Texture2D(2,2);
                        Texture2D SpecGloss=new Texture2D(2,2);
                        Texture2D Emit=new Texture2D(2,2);
                        Texture2D GLTFPBR=new Texture2D(2,2);
                            if(MName.CompareTo("Standard")==0){
                            if(Mat.GetTexture("_OcclusionMap"))Occ.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_OcclusionMap"))));
                            if(Mat.GetTexture("_MetallicGlossMap"))MetalicSpecular.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_MetallicGlossMap"))));
                            if(Mat.GetTexture("_EmissionMap"))Emit.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_EmissionMap"))));
                            PBR=GetStandardPBR((int)TexSize,Occ.height>5?Occ:null,MetalicSpecular.height>5?MetalicSpecular:null,Emit.height>5?Emit:null,SmoothnessSource==1?TX as Texture2D:null);
                            NMat.SetFloat("_Glossiness",Mat.GetFloat(Mat.GetTexture("_MetallicGlossMap")?"_GlossMapScale":"_Glossiness"));
                            NMat.SetFloat("_Metallic",Mat.GetFloat("_Metallic"));
                            }else
                            if(MName.CompareTo("Standard (Specular setup)")==0){
                            if(Mat.GetTexture("_OcclusionMap"))Occ.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_OcclusionMap"))));
                            if(Mat.GetTexture("_SpecGlossMap"))SpecGloss.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_SpecGlossMap"))));
                            if(Mat.GetTexture("_EmissionMap"))Emit.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_EmissionMap"))));
                            PBR=GetStandardSpecPBR((int)TexSize,Occ.height>5?Occ:null,SpecGloss.height>5?SpecGloss:null,Emit.height>5?Emit:null,SmoothnessSource==1?TX as Texture2D:null);
                            NMat.SetFloat("_Glossiness",Mat.GetFloat(Mat.GetTexture("_MetallicGlossMap")?"_GlossMapScale":"_Glossiness"));
                            NMat.SetFloat("_Metallic",Mat.GetFloat("_Metallic"));
                            }else
                            if(MName.CompareTo("Standard (Roughness setup)")==0||MName.CompareTo("Autodesk Interactive")==0){
                            if(Mat.GetTexture("_OcclusionMap"))Occ.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_OcclusionMap"))));
                            if(Mat.GetTexture("_MetallicGlossMap"))MetalicSpecular.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_MetallicGlossMap"))));
                            if(Mat.GetTexture("_SpecGlossMap"))SpecGloss.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_SpecGlossMap"))));
                            if(Mat.GetTexture("_EmissionMap"))Emit.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_EmissionMap"))));
                            PBR=GetStandardRoughPBR((int)TexSize,Occ.height>5?Occ:null,MetalicSpecular.height>5?MetalicSpecular:null,Emit.height>5?Emit:null,SpecGloss.height>5?SpecGloss:null);
                            }else
                            if(MName.CompareTo("GLTF")==0){
                            if(Mat.GetTexture("_PBR"))GLTFPBR.TryLoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(Mat.GetTexture("_PBR"))));
                            PBR=GetGLTFPBR((int)TexSize,GLTFPBR.height>5?GLTFPBR:null);
                            NMat.SetFloat("_Glossiness",Mat.GetFloat("_Glossiness"));
                            NMat.SetFloat("_Metallic",Mat.GetFloat("_Metallic"));
                            }else
                            if(
                            MName.CompareTo("Mobile/Diffuse")==0||
                            MName.CompareTo("Mobile/Bumped Diffuse")==0||
                            MName.CompareTo("Legacy Shaders/VertexLit")==0||
                            MName.CompareTo("Mobile/VertexLit")==0||
                            MName.CompareTo("Legacy Shaders/Transparent/Cutout/Diffuse")==0||
                            MName.CompareTo("Legacy Shaders/Transparent/Cutout/Bumped Diffuse")==0||
                            MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/VertexLit")==0||
                            MName.CompareTo("Legacy Shaders/VertexLit/Transparent/Cutout/Detail")==0
                            ){
                            PBR=GetBlankPBR(1,0,0,0);
                            NMat.SetFloat("_Glossiness",0);
                            NMat.SetFloat("_Metallic",0);
                            }else
                            if(
                            MName.CompareTo("Legacy Shaders/Specular")==0||
                            MName.CompareTo("Mobile/Bumped Specular")==0||
                            MName.CompareTo("Legacy Shaders/Bumped Specular")==0||
                            MName.CompareTo("Legacy Shaders/Transparent/Cutout/Specular")==0||
                            MName.CompareTo("Legacy Shaders/Transparent/Cutout/Bumped Specular")==0
                            ){
                            PBR=GetBlankPBR(1,0,0,0);
                            NMat.SetFloat("_Glossiness",0);
                            NMat.SetFloat("_Metallic",0);
                            }else{
                            PBR=GetBlankPBR(1,0,0,0);
                            NMat.SetFloat("_Glossiness",0);
                            NMat.SetFloat("_Metallic",0);
                            }

                        File.WriteAllBytes(PBRPath,PBR.EncodeToPNG());
                        AssetDatabase.Refresh();
                                // Get the texture importer
                        SetTextureFormat(PBRPath,TF);
                        Texture2D.DestroyImmediate(Occ);
                        Texture2D.DestroyImmediate(MetalicSpecular);
                        Texture2D.DestroyImmediate(Emit);
                        Texture2D.DestroyImmediate(SpecGloss);
                        Texture2D.DestroyImmediate(GLTFPBR);
                        //Texture2D.DestroyImmediate(PBR);
                        } 
                        PBR=AssetDatabase.LoadAssetAtPath<Texture2D>(PBRPath);

                        /*if(!TX){
                        TX=new Texture2D(2,2);
                        File.WriteAllBytes(PathSplit[PathSplit.Length-2]+"_Diff.jpg",PBR.EncodeToJPG());
                        }
                        if(!TX){
                        TX=new Texture2D(2,2);
                        File.WriteAllBytes(PathSplit[PathSplit.Length-2]+"_Diff.jpg",PBR.EncodeToJPG());
                        }
                        AssetDatabase.Refresh();*/
                        NMat.SetTexture("_MainTex",TX);
                        NMat.SetTexture("_BumpMap",NRM);
                        NMat.SetTexture("_PBR",PBR);
                        NMat.color=Mat.color;



                        ConvertedMats.Add(GetMaterialChecksum(Mat),NMat);

                        AssetDatabase.CreateAsset(NMat,ConvertedMatPath+GetMaterialChecksum(Mat)+".mat");
                        AssetDatabase.Refresh();
                    }

                    
                        //TX.GetReadable((int)Mathf.Min(MaxSize,TX.height), (int)Mathf.Min(MaxSize,TX.height));
                
                        return NMat;
               
    }


    public static Texture2D GetStandardPBR(int Size,Texture2D Occ,Texture2D MetalSpec, Texture2D Emit,Texture2D Diff=null){
        Texture2D PBR = new Texture2D(Size,Size);
        if(Diff!=null)Diff=Diff.GetReadable(Size,Size);
        if(Occ!=null)Occ=Occ.GetReadable(Size,Size);else Occ = new Texture2D(Size,Size);
        if(MetalSpec!=null)MetalSpec=MetalSpec.GetReadable(Size,Size);else MetalSpec = new Texture2D(Size,Size);
        if(Emit!=null)Emit=Emit.GetReadable(Size,Size);else{

            Emit = new Texture2D(Size,Size);
            Color32[] colorArray = new Color32[Size * Size];
            Color32 fillColor32 = (Color32)Color.black;

        for (int i = 0; i < colorArray.Length; i++)
        {
            colorArray[i] = fillColor32;
        }
        Emit.SetPixels32(colorArray);
        Emit.Apply();
        }
        bool useDiff=Diff!=null;
        Color[] diffPixels = Diff?Diff.GetPixels():null;
        Color[] occPixels = Occ.GetPixels();
        Color[] metalSpecPixels = MetalSpec.GetPixels();
        Color[] emitPixels = Emit.GetPixels();

        for (int i = 0; i < occPixels.Length; i++)
        {
            float occValue = (occPixels[i].r + occPixels[i].g + occPixels[i].b) / 3f;
            float metallicValue = (metalSpecPixels[i].r + metalSpecPixels[i].g + metalSpecPixels[i].b) / 3f;
            float emitValue = (emitPixels[i].r + emitPixels[i].g + emitPixels[i].b) / 3f;

            // Set Red channel with Occ, Green channel with MetalSpec, and Blue channel with Emit
            PBR.SetPixel(i % Size, i / Size, new Color(occValue, metallicValue, emitValue,useDiff?diffPixels[i].a:metalSpecPixels[i].a));
        }

    PBR.Apply();
    Texture2D.DestroyImmediate(Occ);Occ=null;
    Texture2D.DestroyImmediate(MetalSpec);MetalSpec=null;
    Texture2D.DestroyImmediate(Emit);Emit=null;
    occPixels=null;
    metalSpecPixels=null;
    emitPixels=null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return PBR;
    }


        public static Texture2D GetStandardSpecPBR(int Size,Texture2D Occ,Texture2D SpecGloss, Texture2D Emit,Texture2D Diff=null){
        Texture2D PBR = new Texture2D(Size,Size);
        if(Diff!=null)Diff=Diff.GetReadable(Size,Size);
        if(Occ!=null)Occ=Occ.GetReadable(Size,Size);else Occ = new Texture2D(Size,Size);
        if(SpecGloss!=null)SpecGloss=SpecGloss.GetReadable(Size,Size);else SpecGloss = new Texture2D(Size,Size);
        if(Emit!=null)Emit=Emit.GetReadable(Size,Size);else{

            Emit = new Texture2D(Size,Size);
            Color32[] colorArray = new Color32[Size * Size];
            Color32 fillColor32 = (Color32)Color.black;

        for (int i = 0; i < colorArray.Length; i++)
        {
            colorArray[i] = fillColor32;
        }
        Emit.SetPixels32(colorArray);
        Emit.Apply();
        }
        bool useDiff=Diff!=null;
        Color[] diffPixels = Diff?Diff.GetPixels():null;
        Color[] occPixels = Occ.GetPixels();
        Color[] specGlossPixels = SpecGloss.GetPixels();
        Color[] emitPixels = Emit.GetPixels();

        for (int i = 0; i < occPixels.Length; i++)
        {
            float occValue = (occPixels[i].r + occPixels[i].g + occPixels[i].b) / 3f;
            float metallicValue = 0;
            float emitValue = (emitPixels[i].r + emitPixels[i].g + emitPixels[i].b) / 3f;

            // Set Red channel with Occ, Green channel with MetalSpec, and Blue channel with Emit
            PBR.SetPixel(i % Size, i / Size, new Color(occValue, metallicValue, emitValue,useDiff?diffPixels[i].a:specGlossPixels[i].a));
        }

            PBR.Apply();
            Texture2D.DestroyImmediate(Occ);Occ=null;
            Texture2D.DestroyImmediate(SpecGloss);SpecGloss=null;
            Texture2D.DestroyImmediate(Emit);Emit=null;
            occPixels=null;
            specGlossPixels=null;
            emitPixels=null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return PBR;
    }


        public static Texture2D GetStandardRoughPBR(int Size,Texture2D Occ,Texture2D Metal, Texture2D Emit,Texture2D Rough){
        Texture2D PBR = new Texture2D(Size,Size);
        if(Occ!=null)Occ=Occ.GetReadable(Size,Size);else Occ = new Texture2D(Size,Size);
        if(Metal!=null)Metal=Metal.GetReadable(Size,Size);else Metal = new Texture2D(Size,Size);
        if(Rough!=null)Rough=Rough.GetReadable(Size,Size);else Rough = new Texture2D(Size,Size);
        if(Emit!=null)Emit=Emit.GetReadable(Size,Size);else{

            Emit = new Texture2D(Size,Size);
            Color32[] colorArray = new Color32[Size * Size];
            Color32 fillColor32 = (Color32)Color.black;

        for (int i = 0; i < colorArray.Length; i++)
        {
            colorArray[i] = fillColor32;
        }
        Emit.SetPixels32(colorArray);
        Emit.Apply();
        }
        

        Color[] occPixels = Occ.GetPixels();
        Color[] metalPixels = Metal.GetPixels();
        Color[] roughPixels = Rough.GetPixels();
        Color[] emitPixels = Emit.GetPixels();

        for (int i = 0; i < occPixels.Length; i++)
        {
            float occValue = (occPixels[i].r + occPixels[i].g + occPixels[i].b) / 3f;
            float metallicValue = (metalPixels[i].r + metalPixels[i].g + metalPixels[i].b) / 3f;
            float roughValue = (roughPixels[i].r + roughPixels[i].g + roughPixels[i].b) / 3f;
            float emitValue = (emitPixels[i].r + emitPixels[i].g + emitPixels[i].b) / 3f;

            // Set Red channel with Occ, Green channel with Metal, and Blue channel with Emit
            PBR.SetPixel(i % Size, i / Size, new Color(occValue, metallicValue, emitValue, roughValue));
        }

    PBR.Apply();
    Texture2D.DestroyImmediate(Occ);Occ=null;
    Texture2D.DestroyImmediate(Metal);Metal=null;
    Texture2D.DestroyImmediate(Rough);Metal=null;
    Texture2D.DestroyImmediate(Emit);Emit=null;
    occPixels=null;
    metalPixels=null;
    roughPixels=null;
    emitPixels=null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return PBR;
    }

    public static Texture2D GetGLTFPBR(int Size,Texture2D GLTFPBR){
        Texture2D PBR = new Texture2D(Size,Size);
        if(GLTFPBR!=null)GLTFPBR=GLTFPBR.GetReadable(Size,Size);else GLTFPBR = new Texture2D(Size,Size);
        

        Color[] GLTFPBRPixels = GLTFPBR.GetPixels();

        for (int i = 0; i < GLTFPBRPixels.Length; i++)
        {
            float occValue =GLTFPBRPixels[i].r;
            float metallicValue = GLTFPBRPixels[i].b;
            float roughValue = 1-GLTFPBRPixels[i].g;
            float emitValue = 0;

            // Set Red channel with Occ, Green channel with Metal, and Blue channel with Emit
            PBR.SetPixel(i % Size, i / Size, new Color(occValue, metallicValue, emitValue, roughValue));
        }

    PBR.Apply();
    Texture2D.DestroyImmediate(GLTFPBR);GLTFPBR=null;
    GLTFPBRPixels=null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return PBR;
    }

    public static Texture2D GetBlankPBR(float occ,float metal,float emit, float spec){
        Texture2D PBR = new Texture2D(2,2);
        

        Color[] pixels = PBR.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            float occValue =occ;
            float metallicValue = metal;
            float roughValue = spec;
            float emitValue = emit;

            // Set Red channel with Occ, Green channel with Metal, and Blue channel with Emit
            PBR.SetPixel(i % 2, i / 2, new Color(occValue, metallicValue, emitValue, roughValue));
        }

    PBR.Apply();
    pixels=null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return PBR;
    }

    

#endif
}
