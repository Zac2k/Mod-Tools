using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class LODCombiner : MonoBehaviour
{
#if UNITY_EDITOR
[System.Serializable]
    public class MatGroup{
        public Material Mat;
        public List<LODGroup> LODGroups=new List<LODGroup>();
        public List<MeshRenderer> MeshRenderers=new List<MeshRenderer>();
        public List<ParticleSystemRenderer> Particles=new List<ParticleSystemRenderer>();
    }
    public bool OrderByMaterial;
    public bool GenerateShadowProxy;
    public List<LODGroup> BrokenLodGroups = new List<LODGroup>();
    public List<Material> Mats = new List<Material>();
    public List<MatGroup> MatGroups = new List<MatGroup>();
    public List<Transform> ObjectsToOrder= new List<Transform>();

    public Transform root;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(OrderByMaterial){
            OrderByMaterial=false;
            Undo.RecordObject(root,"");
            if(root==null)root=new GameObject(gameObject.name+"_Ordered").transform;
            ExtractRenderesFromChild(transform);

            foreach(Material M in Mats){
                foreach(LODGroup LG in root.GetComponentsInChildren<LODGroup>()){
                    if(LG.GetLODs()[0].renderers[0].sharedMaterial==M)LG.transform.SetSiblingIndex(0);
                }

                foreach(ParticleSystemRenderer PSR in root.GetComponentsInChildren<ParticleSystemRenderer>()){
                    if(PSR.sharedMaterial==M)PSR.transform.SetSiblingIndex(0);
                }

                foreach(MeshRenderer MR in root.GetComponentsInChildren<MeshRenderer>()){
                    if(MR.sharedMaterial==M)MR.transform.SetSiblingIndex(0);
                }
            }

        }

                if(GenerateShadowProxy){
            GenerateShadowProxy=false;
            Transform Shadows = new GameObject(root.name+"_Shadows").transform;
            Transform Tmp = Instantiate(root);
            

                foreach(LODGroup LG in Tmp.GetComponentsInChildren<LODGroup>()){
                    foreach(Renderer R in LG.GetLODs()[LG.lodCount-1].renderers){
                        Instantiate(R.gameObject,Shadows,true);
                        break;
                    }
                    DestroyImmediate(LG.gameObject);
                }

                foreach(MeshRenderer MR in Tmp.GetComponentsInChildren<MeshRenderer>().ToList()){
                    if(!MR)continue;
                        Instantiate(MR.gameObject,Shadows,true);
                    DestroyImmediate(MR.gameObject);
                }
                    DestroyImmediate(Tmp.gameObject);
        }
    }

    public void ExtractRenderesFromChild(Transform T){
        if(T.childCount==0)return;
                    for(int i=T.childCount-1; i>=0; i--){
                Transform child = T.GetChild(i);
                if(child.GetComponent<LODGroup>()){
                    if(child.GetComponent<LODGroup>().GetLODs().Length==0||child.GetComponent<LODGroup>().GetLODs()[0].renderers.Length==0){BrokenLodGroups.Add(child.GetComponent<LODGroup>()); continue;}
                    if(!Mats.Contains(child.GetComponent<LODGroup>().GetLODs()[0].renderers[0].sharedMaterial))Mats.Add(child.GetComponent<LODGroup>().GetLODs()[0].renderers[0].sharedMaterial);
                    Undo.SetTransformParent(child,root,"Undo");ObjectsToOrder.Add(child);
                    }else
                if(child.GetComponent<MeshRenderer>()){
                    if(!Mats.Contains(child.GetComponent<MeshRenderer>().sharedMaterial))Mats.Add(child.GetComponent<MeshRenderer>().sharedMaterial);
                    Undo.SetTransformParent(child,root,"Undo");ObjectsToOrder.Add(child);
                    }else
                if(child.GetComponent<ParticleSystemRenderer>()){
                    if(!Mats.Contains(child.GetComponent<ParticleSystemRenderer>().sharedMaterial))Mats.Add(child.GetComponent<ParticleSystemRenderer>().sharedMaterial);
                    Undo.SetTransformParent(child,root,"Undo");ObjectsToOrder.Add(child);
                    }else{
                        ExtractRenderesFromChild(child);
                    }
                
            }

    }
    #endif
}
