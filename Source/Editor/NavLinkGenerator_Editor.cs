
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


namespace idbrii.navgen
{
    [CustomEditor(typeof(NavLinkGenerator), true)]
    public class NavLinkGenerator_Editor : Editor
    {
        const float k_DrawDuration = 1f;
        const string k_LinkRootName = "Generated NavLinks";

        [SerializeField] bool m_AttachDebugToLinks;
        [SerializeField] bool m_ShowCreatedLinks;
        [SerializeField] List<NavMeshLink> m_CreatedLinks = new List<NavMeshLink>();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var gen = target as NavLinkGenerator;



            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Clear All", "Delete generated Interior Volumes, NavMesh, and NavMeshLinks.")))
                {
                    gen.RemoveLinks();
                    SceneView.RepaintAll();
                    Debug.Log($"Removed NavMesh and NavMeshLinks from all NavMeshSurfaces.");
                }

                if (GUILayout.Button(new GUIContent("Bake NavMesh", "Build navmesh for all NavMeshSurface.")))
                {
                    var surfaces = NavEdUtil.GetAllInActiveScene<NavMeshSurface>();
                    Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(surfaces);
                    Debug.Log($"Baked NavMesh for {surfaces.Length} NavMeshSurfaces.");
                }

                if (GUILayout.Button(new GUIContent("Bake Links", "Create NavMeshLinks along your navmesh edges.")))
                {
                    //GenerateLinks(gen);
                    gen.GenerateLinks();
                    Debug.Log($"Baked NavMeshLinks.");
                }

                if (GUILayout.Button(new GUIContent("Select NavMesh", "Selecting the navmesh makes it draw in the Scene view so you can evaluate the quality of the mesh and the links.")))
                {
                    Selection.objects = NavEdUtil.GetAllInActiveScene<NavMeshSurface>();
                }
            }

            EditorGUILayout.Space();
            m_ShowCreatedLinks = EditorGUILayout.Foldout(m_ShowCreatedLinks, "Created Links", toggleOnLabelClick: true);
            if (m_ShowCreatedLinks)
            {
                foreach (var entry in m_CreatedLinks)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(entry, typeof(NavMeshLink), allowSceneObjects: true);
                        using (new EditorGUI.DisabledScope(!m_AttachDebugToLinks))
                        {
                            if (GUILayout.Button("Draw"))
                            {

                                if (SceneView.lastActiveSceneView != null)
                                {
                                    // Prevent losing focus (we'd lose our state).
                                    ActiveEditorTracker.sharedTracker.isLocked = true;

                                    var activeGameObject = Selection.activeGameObject;
                                    activeGameObject = entry.gameObject;
                                    EditorGUIUtility.PingObject(activeGameObject);
                                    SceneView.lastActiveSceneView.FrameSelected();
                                    Selection.activeGameObject = activeGameObject;
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
#endif
