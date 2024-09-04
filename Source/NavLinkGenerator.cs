using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace idbrii.navgen
{

    //[CreateAssetMenu(fileName = "NavLinkGenerator", menuName = "Navigation/NavLinkGenerator", order = 1)]
    public class NavLinkGenerator : MonoBehaviour
    {
        public Transform m_JumpLinkPrefab;
        public Transform m_FallLinkPrefab;
        public Transform m_ClimbLinkPrefab;
        public Transform m_VautLinkPrefab;
        public Transform m_LadderLinkPrefab;
        public float m_MaxHorizontalJump = 5f;
        [Tooltip("Max distance we can jump up.")]
        public float m_MaxVerticalJump = 3f;
        [Tooltip("Max distance we are allowed to fall. Usually higher than m_MaxVerticalJump.")]
        public float m_MaxVerticalFall = 5f;
        public int m_Steps = 10;
        public LayerMask m_PhysicsMask = -1;
        //public NavMeshAreas m_NavMask = NavMeshAreas.All;
        //public AreaMask m_AreaMask = NavMeshAreas.All;
        public float m_AgentHeight = 1.5f;
        public float m_AgentRadius = 0.5f;
        public float MaxValidPathenght = 10;
        [Tooltip("Maximum degrees away from the normal pointing horizontally out of a navmesh edge. Larger values allow more awkward links, but may result in redundant or inappropriate links.")]
        [Range(0f, 60f)]
        public float m_MaxAngleFromEdgeNormal = 45f;
        [SerializeField] List<NavMeshLink> m_CreatedLinks = new List<NavMeshLink>();

        public bool Generated;

        public Dictionary<int, NavMeshLink> Invalidinks = new Dictionary<int, NavMeshLink>();
        public List<NavMeshLink> InvalidinksList;

        void Start()
        {
            //if(!Generated)
            //GenerateLinks(this);
        }

        //  [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFastPathLenght(NavMeshPath path)
        {
            float length = 0.0f;
            if ((path.status != NavMeshPathStatus.PathInvalid) && (path.corners.Length > 1))
            {
                for (int i = 1; i < path.corners.Length; i++)
                {
                    length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }
            }

            return length;
        }

        [HideInInspector] public NavMeshPath T_PlayerPath;
        public void GenerateLinks()
        {
            Physics.queriesHitBackfaces = true;
            try
            {
                T_PlayerPath = new NavMeshPath();
                var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
                var edge_list = CreateEdges(tri);
                float T_PlayerDist;
                foreach (var edge in edge_list)
                {

                    edge.ComputeDerivedData();
                }
                if (edge_list.Count() == 0)
                {
                    return;
                }





                RemoveLinks();
                m_CreatedLinks.Clear();
                var parent = GameObject.Find("Generated NavLinks");
                var InvLinksParent = GameObject.Find("Invalid NavLinks");
                if (parent == null) parent = new GameObject("Generated NavLinks");
                if (InvLinksParent == null) InvLinksParent = new GameObject("Invalid NavLinks");

                foreach (var edge in edge_list)
                {
                    var mid = edge.GetMidpoint();
                    var fwd = edge.m_Normal;
                    var link = CreateNavLink(parent.transform, edge, mid, fwd);
                    if (link != null)
                    {
                        m_CreatedLinks.Add(link);
                    }
                }

                float SqrDist = MaxValidPathenght * MaxValidPathenght;
                InvalidinksList.Clear();
                // foreach(NavMeshLink NML in m_CreatedLinks)NML.enabled=false;
                parent.gameObject.SetActive(false);
                //InvLinksParent.gameObject.SetActive(false);

                for (int it = 0; it < 10; it++)
                {
                    bool Removed = false;
                    Invalidinks.Clear();
                    NavMeshLink[] Links_array = m_CreatedLinks.ToArray();

                    for (int i = 0; i < Links_array.Length; i++)
                    {
                        if (Invalidinks.ContainsKey(i)) continue;
                        NavMeshLink l1 = Links_array[i];
                        Vector3 l1SP = l1.transform.TransformPoint(l1.startPoint);
                        Vector3 l1EP = l1.transform.TransformPoint(l1.endPoint);

                        if (NavMesh.CalculatePath(l1SP, l1EP, m_PhysicsMask, T_PlayerPath))
                        {
                            if (T_PlayerPath.status == NavMeshPathStatus.PathComplete)
                            {
                                T_PlayerDist = GetFastPathLenght(T_PlayerPath);
                                if (T_PlayerDist < MaxValidPathenght) { Invalidinks.Add(i, l1); Removed = true; continue; }
                            }
                        }

                        for (int j = i + 1; j < Links_array.Length; j++)
                        {
                            if (Invalidinks.ContainsKey(j)) continue;
                            NavMeshLink l2 = Links_array[j];
                            if (l2.bidirectional != l1.bidirectional) continue;
                            Vector3 l2SP = l2.transform.TransformPoint(l2.startPoint);
                            Vector3 l2EP = l2.transform.TransformPoint(l2.endPoint);

                            if (NavMesh.CalculatePath(l2SP, l2EP, m_PhysicsMask, T_PlayerPath))
                            {
                                if (T_PlayerPath.status == NavMeshPathStatus.PathComplete)
                                {
                                    T_PlayerDist = GetFastPathLenght(T_PlayerPath);
                                    if (T_PlayerDist < MaxValidPathenght) { Invalidinks.Add(j, l2); Removed = true; continue; }
                                }
                            }

                            if (FastDist(l1SP, l2SP) > 20 * 20 && FastDist(l1EP, l2EP) > 20 * 20) continue;

                            if (NavMesh.CalculatePath(l1SP, l2SP, m_PhysicsMask, T_PlayerPath))
                            {
                                if (T_PlayerPath.status == NavMeshPathStatus.PathComplete)
                                {
                                    T_PlayerDist = GetFastPathLenght(T_PlayerPath);
                                    if (T_PlayerDist > MaxValidPathenght) { continue; }
                                    //Debug.Log("Path Lenght : " + T_PlayerDist);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }

                            if (NavMesh.CalculatePath(l1EP, l2EP, m_PhysicsMask, T_PlayerPath))
                            {
                                if (T_PlayerPath.status == NavMeshPathStatus.PathComplete)
                                {
                                    T_PlayerDist = GetFastPathLenght(T_PlayerPath);
                                    if (T_PlayerDist > MaxValidPathenght) { continue; }
                                    // Debug.Log("Path Lenght : " + T_PlayerDist);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }

                            Invalidinks.TryAdd(l2.width > l1.width ? i : j, l2.width > l1.width ? l1 : l2);
                            Removed = true;
                        }
                    }
                    if (!Removed)
                    {
                        Debug.Log($"Finished After {it} iterations");
                        break;
                    }
                    foreach (NavMeshLink NML in Invalidinks.Values)
                    {
                        m_CreatedLinks.Remove(NML);
                        InvalidinksList.Add(NML);
                    }
                }
                foreach (NavMeshLink NML in InvalidinksList)
                {
                    //if(NML!=null)
                    //DestroyImmediate(NML.gameObject);
                    NML.transform.parent = InvLinksParent.transform;
                    NML.gameObject.SetActive(false);
                }
                parent.SetActive(true);
                Generated = true;
            }
            catch (System.Exception e)
            {
                throw e;
            }
            finally
            {
                Physics.queriesHitBackfaces = false;

            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastDist(Vector3 a, Vector3 b)
        {
            return (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y) + (b.z - a.z) * (b.z - a.z);
        }

        NavMeshLink CreateNavLink(Transform parent, NavEdge edge, Vector3 mid, Vector3 fwd)
        {
            RaycastHit phys_hit;
            RaycastHit ignored;
            UnityEngine.AI.NavMeshHit nav_hit;
            /*var ground_found = Color.Lerp(Color.red, Color.white, 0.75f);
            var ground_missing = Color.Lerp(Color.red, Color.white, 0.35f);
            var navmesh_found = Color.Lerp(Color.cyan, Color.white, 0.75f);
            var navmesh_missing = Color.Lerp(Color.red, Color.white, 0.65f);
            var traverse_clear = Color.green;
            var traverse_hit = Color.red;*/
            for (int i = 1; i < m_Steps + 1; ++i)
            {
                float scale = (float)i / (float)m_Steps;

                Vector3 top = mid + (fwd * m_MaxHorizontalJump * scale);
                Vector3 down=Vector3.zero;

                

                bool TDhit=true;
                bool DThit=false;
                bool NMhit;

                if (Physics.Raycast(top, Vector3.down * m_MaxVerticalFall, out phys_hit, Mathf.Infinity, m_PhysicsMask.value))
                {
                    down = phys_hit.point;
                    TDhit=true;
                }
                DThit = Physics.Linecast(down, top, m_PhysicsMask.value, QueryTriggerInteraction.Ignore);
                if(!TDhit)continue;
                if(DThit)continue;
                //Debug.Log("");
                //Debug.Log("Pass_0");
                //~ Debug.DrawLine(mid, top, hit ? ground_found : ground_missing, k_DrawDuration);
                //~ Debug.DrawLine(top, down, hit ? ground_found : ground_missing, k_DrawDuration);
                
                    //var max_distance = m_MaxVerticalFall - phys_hit.distance;
                    //Debug.Log("Pass_1 : " + phys_hit.distance);
                    NMhit = UnityEngine.AI.NavMesh.SamplePosition(phys_hit.point, out nav_hit, 3, m_PhysicsMask);
                    //if (hit) Debug.Log("Pass_2 : ");
                    // Only place downward links (to avoid back and forth double placement).
                    NMhit = NMhit && (nav_hit.position.y <= mid.y);
                    //if (hit) Debug.Log("Pass_3 : ");
                    // Only accept 90 wedge in front of normal (prevent links
                    // that other edges are already handling).
                    NMhit = NMhit && Vector3.Dot(nav_hit.position - mid, edge.m_Normal) > Mathf.Cos(m_MaxAngleFromEdgeNormal);
                    //if (hit) Debug.Log("Pass_4 : ");
                    bool is_original_edge = edge.IsPointOnEdge(nav_hit.position);
                    NMhit &= !is_original_edge; // don't count self
                    //if (hit) Debug.Log("Pass_5 : ");
                    //~ Debug.DrawLine(phys_hit.point, nav_hit.position, hit ? navmesh_found : navmesh_missing, k_DrawDuration);
                    if (NMhit)
                    {
                        //if (hit) Debug.Log("Pass_6 : ");
                        var height_offset = Vector3.up * m_AgentHeight;
                        var transit_start = mid + height_offset + (fwd * m_AgentRadius*1.1f);
                        var transit_end = nav_hit.position + height_offset;
                        // Raycast both ways to ensure we're not inside a collider.

                        NMhit =
                        Physics.Linecast(transit_start, transit_end, out ignored, m_PhysicsMask.value, QueryTriggerInteraction.Ignore)
                        ||
                        Physics.Linecast(transit_end, transit_start, out ignored, m_PhysicsMask.value, QueryTriggerInteraction.Ignore);
                        //~ Debug.DrawLine(transit_start, transit_end, hit ? traverse_clear : traverse_hit, k_DrawDuration);
                        if (NMhit)
                        {
                            // Agent can't jump through here.
                            continue;
                        }
                        //Debug.Log("Pass_7 : ");
                        var height_delta = mid.y - nav_hit.position.y;
                        var Distance = Vector3.Distance(new Vector3(mid.x, 0, mid.z), new Vector3(nav_hit.position.x, 0, nav_hit.position.z));
                        Debug.Assert(height_delta >= 0, "Not handling negative delta.");
                        Transform prefab = null;

                        if (height_delta < 0.4) continue;
                        //Debug.Log("Pass_8 : ");
                        if (height_delta < m_MaxVerticalJump) prefab = m_JumpLinkPrefab;
                        else
                        if (height_delta < m_MaxVerticalFall) prefab = m_FallLinkPrefab;
                        if (prefab == null) continue;
                        //Debug.Log("Pass_9 : ");
                        //var t = PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene) as Transform;
                        var t = Instantiate(prefab, parent.transform) as Transform;
                        //Debug.Assert(t != null, $"Failed to instantiate {prefab}");
                        t.SetParent(parent);
                        t.SetPositionAndRotation(mid, edge.m_Away);
                        var link = t.GetComponent<NavMeshLink>();

                        // Push endpoint out into the navmesh to ensure good
                        // connection. Necessary to prevent invalid links.
                        var inset = 0.05f;
                        link.startPoint = link.transform.InverseTransformPoint(mid - fwd * inset);
                        link.endPoint = link.transform.InverseTransformPoint(nav_hit.position) + (Vector3.forward * inset);
                        link.width = edge.m_Length;
                        link.UpdateLink();
                        //                        Debug.Log("Created NavLink", link);
                        //Undo.RegisterCompleteObjectUndo(link.gameObject, "Create NavMeshLink");

                        /*if (m_AttachDebugToLinks)
                        {
                            // Attach a component that has the information we
                            // used to decide how to create this navlink. Much
                            // easier to go back and inspect it like this than
                            // to try to examine the output as you generate
                            // navlinks. Mostly useful for debugging
                            // NavLinkGenerator.
                            var reason = link.gameObject.AddComponent<NavLinkCreationReason>();
                            reason.gen = gen;
                            reason.fwd = fwd;
                            reason.mid = mid;
                            reason.top = top;
                            reason.down = down;
                            reason.transit_start = transit_start;
                            reason.transit_end = transit_end;
                            reason.nav_hit_position = nav_hit.position;
                            reason.phys_hit_point = phys_hit.point;
                        }*/

                        //Debug.Log("Pass_all : ");
                        return link;
                    }
                
            }
            return null;
        }

        static IEnumerable<NavEdge> CreateEdges(UnityEngine.AI.NavMeshTriangulation tri)
        {
            // use HashSet to ignore duplicate edges.
            var edges = new HashSet<NavEdge>(new NavEdgeEqualityComparer());
            for (int i = 0; i < tri.indices.Length - 1; i += 3)
            {
                AddIfUniqueAndRemoveIfNot(edges, TriangleToEdge(tri, i, i + 1));
                AddIfUniqueAndRemoveIfNot(edges, TriangleToEdge(tri, i + 1, i + 2));
                AddIfUniqueAndRemoveIfNot(edges, TriangleToEdge(tri, i + 2, i));
            }
            return edges;
        }

        static NavEdge TriangleToEdge(UnityEngine.AI.NavMeshTriangulation tri, int start, int end)
        {
            var v1 = tri.vertices[tri.indices[start]];
            var v2 = tri.vertices[tri.indices[end]];
            return new NavEdge(v1, v2);
        }

        public void RemoveLinks()
        {
            var parent = GameObject.Find("Generated NavLinks");
            var InvLinksParent = GameObject.Find("Invalid NavLinks");
            if (parent != null) DestroyImmediate(parent);
            if (InvLinksParent != null) DestroyImmediate(InvLinksParent);
        }


        class NavEdge
        {
            public Vector3 m_StartPos;
            public Vector3 m_EndPos;
            public Vector3 m_MidPos;

            // Derived data
            public float m_Length;
            public Vector3 m_Normal;
            public Quaternion m_Away;

            public bool valid;

            public NavEdge(Vector3 start, Vector3 end)
            {
                m_StartPos = start;
                m_EndPos = end;
                m_MidPos = GetMidpoint();
            }

            public Vector3 GetMidpoint()
            {
                return Vector3.Lerp(m_StartPos, m_EndPos, 0.5f);
            }

            public void ComputeDerivedData()
            {
                m_Length = Vector3.Distance(m_StartPos, m_EndPos);
                var normal = Vector3.Cross(m_EndPos - m_StartPos, Vector3.up).normalized;

                // Point it outside the nav poly.
                UnityEngine.AI.NavMeshHit nav_hit;
                var mid = GetMidpoint();
                var end = mid - normal * 0.3f;
                bool hit = UnityEngine.AI.NavMesh.SamplePosition(end, out nav_hit, 0.2f, UnityEngine.AI.NavMesh.AllAreas);
                //~ Debug.DrawLine(mid, end, hit ? Color.red : Color.white);
                if (!hit)
                {
                    normal *= -1f;
                }
                m_Normal = normal;
                m_Away = Quaternion.LookRotation(normal);
            }
            public bool IsPointOnEdge(Vector3 point)
            {
                return DistanceSqToPointOnLine(m_StartPos, m_EndPos, point) < 0.001f;
            }
        }

        static float DistanceSqToPointOnLine(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            Vector3 pa = a - p;
            var mag = ab.magnitude;
            Vector3 c = ab * (Vector3.Dot(pa, ab) / (mag * mag));
            Vector3 d = pa - c;
            return Vector3.Dot(d, d);
        }

        // Using EqualityComparer on NavEdge didn't work, so use a comparer.
        class NavEdgeEqualityComparer : IEqualityComparer<NavEdge>
        {
            public bool Equals(NavEdge lhs, NavEdge rhs)
            {
                return
                    (lhs.m_StartPos == rhs.m_StartPos && lhs.m_EndPos == rhs.m_EndPos)
                    || (lhs.m_StartPos == rhs.m_EndPos && lhs.m_EndPos == rhs.m_StartPos);
            }

            public int GetHashCode(NavEdge e)
            {
                return e.m_StartPos.GetHashCode() ^ e.m_EndPos.GetHashCode();
            }
        }

#if NAVGEN_INCLUDE_TESTS
        [UnityEditor.MenuItem("Tools/Test/NavEdge Compare")]
#endif
        static void Test_NavEdge_Compare()
        {
            var cmp = new NavEdgeEqualityComparer();
            var edge = new NavEdge(new Vector3(10f, 20f, 30f), new Vector3(20f, 20f, 20f));
            var edge_identical = new NavEdge(edge.m_StartPos, edge.m_EndPos);
            var edge_reverse = new NavEdge(edge.m_EndPos, edge.m_StartPos);

            Debug.Assert(cmp.Equals(edge, edge), "compare to self.");
            Debug.Assert(cmp.Equals(edge, edge_identical), "compare to identical.");
            Debug.Assert(cmp.Equals(edge, edge_reverse), "compare to mirrored.");

            var edge_list = new HashSet<NavEdge>(cmp);
            edge_list.Add(edge);
            Debug.Assert(edge_list.Add(edge) == false, "Add failed to find duplicate");
            Debug.Assert(edge_list.Remove(edge) == true, "Remove failed to find edge");
            Debug.Assert(edge_list.Count == 0, "Must be empty now.");

            AddIfUniqueAndRemoveIfNot(edge_list, edge);
            Debug.Assert(edge_list.Count == 1, "AddIfUniqueAndRemoveIfNot should add edge to empty set.");
            AddIfUniqueAndRemoveIfNot(edge_list, edge_identical);
            Debug.Assert(edge_list.Count == 0, "AddIfUniqueAndRemoveIfNot should remove identical edge.");

            AddIfUniqueAndRemoveIfNot(edge_list, edge);
            Debug.Assert(edge_list.Count == 1, "AddIfUniqueAndRemoveIfNot failed to add edge");
            AddIfUniqueAndRemoveIfNot(edge_list, edge_reverse);
            Debug.Assert(edge_list.Count == 0, "AddIfUniqueAndRemoveIfNot failed to find edge");

            Debug.Log("Test complete: NavEdge");
        }

        // Don't want inner edges (which match another existing edge).
        static void AddIfUniqueAndRemoveIfNot(HashSet<NavEdge> set, NavEdge edge)
        {
            bool had_edge = set.Remove(edge);
            if (!had_edge)
            {
                set.Add(edge);
            }
        }

    }

}
