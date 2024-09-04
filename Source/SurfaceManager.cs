using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CWModUtility;


[System.Serializable]
public class SurfaceInfo
{
    public string Name;
    public GameObject Decal;//Must Be A Saved Prefab//
    public AudioClip[] ShellSounds;
    public AudioClip[] FootstepSounds;
    public AudioClip[] CrawlSounds;
    public AudioClip[] JumpSounds;
    public AudioClip[] LandSounds;
    public AudioClip[] SlideSounds;

    public float Thickness = 1;
    public float VisDist = 100;


    //public List<GameObject>[] DecalCache;

    public bool Initialized;
    int DecalIndex;
    public List<GameObject> DecalCache;

    [System.NonSerialized] public int ID;

    public void PlaceDecal(Vector3 Position, Quaternion Rotation, float Size, Camera cam)
    {
        if (!Initialized)
        {
            // DecalCache=new List<GameObject>[Decals.Length];
            ////  for(int i=0; i<DecalCache.Length;i++){
            //    DecalCache[i]=new List<GameObject>();
            // }
            DecalCache.Clear();
            DecalCache.Add(MonoBehaviour.Instantiate(Decal));
            Initialized = true;
        }

        //Funcs.Log("We Should See A Decal");
        // Type=0;
        float Fov = cam.fieldOfView;
        // float NVisdist=(VisDist*60)/Fov;
        if (FastDist(Position, cam.transform.position) > VisDist * VisDist) {/*Funcs.Log("Returned Because Decal Is Too Far");*/ return; }
        if (Vector3.Angle(cam.transform.forward, Position - cam.transform.position) > Fov) {/*Funcs.Log("Returned Because Decal Is Out Of View");*/return; }
        DecalIndex++; if (DecalIndex >= DecalCache.Count) DecalIndex = 0;
        if (DecalCache[DecalIndex].activeSelf) { DecalCache.Add(MonoBehaviour.Instantiate(Decal)); DecalIndex++; }


        DecalCache[DecalIndex].transform.position = Position;
        DecalCache[DecalIndex].transform.rotation = Rotation;
        DecalCache[DecalIndex].transform.localScale = new Vector3(Size, Size, Size);
        DecalCache[DecalIndex].SetActive(true);


    }
}


public class SurfaceManager : MonoBehaviour
{
    public static SurfaceManager Instance;
    public void Awake()
    {
        Instance = this;
        TaggedsurfaceInfos.Clear();
        foreach (SurfaceInfo SI in surfaceInfos) TaggedsurfaceInfos.Add(SI.Name, SI);
    }
    public SurfaceInfo[] surfaceInfos;
    public Dictionary<string, SurfaceInfo> TaggedsurfaceInfos = new Dictionary<string, SurfaceInfo>();

    public bool HasModSurface;
    [System.NonSerialized] public SurfaceInfo[] ModSurfaceInfos;
    [System.NonSerialized] public Dictionary<string, SurfaceInfo> ModTaggedsurfaceInfos = new Dictionary<string, SurfaceInfo>();


    public SurfaceInfo GetSurface(string Tag)
    {
        if(HasModSurface)
        if (ModTaggedsurfaceInfos.TryGetValue(Tag, out SurfaceInfo MSI)) return MSI;

        if (TaggedsurfaceInfos.TryGetValue(Tag, out SurfaceInfo SI)) return SI;
        return null;
    }

}
