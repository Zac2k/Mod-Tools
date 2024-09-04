#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static CWModUtility;

[CustomEditor(typeof(ModMapManager))]
public class ModMapManagerEditor : Editor
{
    SerializedProperty GeneralSettingsFoldout;
    SerializedProperty BotsSettingsFoldout;
    SerializedProperty ConquestSettingsFoldout;
    SerializedProperty PatrolSettingsFoldout;
    SerializedProperty SabotageSettingsFoldout;
    SerializedProperty ExportSettingsFoldout;


    SerializedProperty PlayableArea;
    SerializedProperty Team1;
    SerializedProperty Team2;
    SerializedProperty EnableConquest;
    SerializedProperty ConquestRadius;
    SerializedProperty RespawnRadius;
    SerializedProperty ConquestOnlyObjects;
    SerializedProperty EnablePatrol;
    SerializedProperty PatrolOnlyObjects;

    SerializedProperty EnableSabotage;
    SerializedProperty SabotageOnlyObjects;
    SerializedProperty platforms;
    SerializedProperty buildTarget;
    SerializedProperty Info;
    SerializedProperty CompressMeshUVs;
    SerializedProperty CompressMeshVertices;
    SerializedProperty ConvertShaders;
    SerializedProperty PackTextures;
    SerializedProperty AtlasSize;
    SerializedProperty MaxTextureSize;
    SerializedProperty SetStageVertexLimit;
    SerializedProperty VertexLimit;
    SerializedProperty mapPath;
    Texture Splash;



    static Color PlayableAreaColor = new Color(.5f, .5f, 1, .4f);
    static Color PlayableAreaEditColor = new Color(0f, 0, 1, 1);
    bool EditingPlayableArea;
    BoxBoundsHandle PlayableAreaHandle = new BoxBoundsHandle();
    string Root;
    void OnEnable()
    {
        GeneralSettingsFoldout = serializedObject.FindProperty("GeneralSettingsFoldout");
        BotsSettingsFoldout = serializedObject.FindProperty("BotsSettingsFoldout");
        ConquestSettingsFoldout = serializedObject.FindProperty("ConquestSettingsFoldout");
        PatrolSettingsFoldout = serializedObject.FindProperty("PatrolSettingsFoldout");
        SabotageSettingsFoldout = serializedObject.FindProperty("SabotageSettingsFoldout");
        ExportSettingsFoldout = serializedObject.FindProperty("ExportSettingsFoldout");

        PlayableArea = serializedObject.FindProperty("PlayableArea");
        Team1 = serializedObject.FindProperty("Team1");
        Team1.isExpanded = true;
        Team2 = serializedObject.FindProperty("Team2");
        Team2.isExpanded = true;
        ConquestRadius = serializedObject.FindProperty("ConquestRadius");
        EnableConquest = serializedObject.FindProperty("EnableConquest");
        RespawnRadius = serializedObject.FindProperty("RespawnRadius");
        ConquestOnlyObjects = serializedObject.FindProperty("ConquestOnlyObjects");
        PatrolOnlyObjects = serializedObject.FindProperty("PatrolOnlyObjects");
        EnablePatrol = serializedObject.FindProperty("EnablePatrol");
        SabotageOnlyObjects = serializedObject.FindProperty("SabotageOnlyObjects");
        EnableSabotage = serializedObject.FindProperty("EnableSabotage");
        mapPath = serializedObject.FindProperty("MapPath");

        Info = serializedObject.FindProperty("Info");
        platforms = serializedObject.FindProperty("platforms");
        buildTarget = serializedObject.FindProperty("buildTarget");
        CompressMeshUVs = serializedObject.FindProperty("CompressMeshUVs");
        CompressMeshVertices = serializedObject.FindProperty("CompressMeshVertices");
        ConvertShaders = serializedObject.FindProperty("ConvertShaders");
        PackTextures = serializedObject.FindProperty("PackTextures");
        AtlasSize = serializedObject.FindProperty("AtlasSize");
        MaxTextureSize = serializedObject.FindProperty("MaxTextureSize");
        SetStageVertexLimit = serializedObject.FindProperty("SetStageVertexLimit");
        VertexLimit = serializedObject.FindProperty("VertexLimit");


        Root = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(target as ModMapManager)).Replace("ModMapManager.cs", "");


    }

    ModMapManager MMM;
    public override void OnInspectorGUI()
    {
        if (!MMM) MMM = (ModMapManager)(target);
        GUILayout.Space(5);
        float space = 0;
        DrawSpacer(0, 5, space);

        GUI.skin.button.fontStyle = FontStyle.Bold;

        DrawSplash();

        GUI.backgroundColor = new Color(0.35f, 1, 0.35f);
        GUI.skin.button.fontStyle = FontStyle.Bold;
        if (GUILayout.Button("Open Documentation"))
        {
            Application.OpenURL("");
        }
        GUI.skin.button.fontStyle = FontStyle.Normal;
        GUI.backgroundColor = Color.white;
        DrawSpacer(space, 5, space);

        serializedObject.Update();

        DrawGeneralSettings(Color.black);
        DrawSpacer(space, 5, space);
        DrawBotsSettings(Color.black);
        DrawSpacer(space, 5, space);
        DrawConquestSettings(Color.black);
        DrawSpacer(space, 5, space);
        DrawPatrolSettings(Color.black);
        DrawSpacer(space, 5, space);
        DrawSabotageSettings(Color.black);
        DrawSpacer(space, 5, space);
        DrawExportSettings(Color.black);



        EditorUtility.SetDirty(MMM);
        serializedObject.ApplyModifiedProperties();

    }

    public void DrawSplash()
    {

        if (!Splash)
        {
            Splash = AssetDatabase.LoadAssetAtPath(Root + "Icons/Splash.png", typeof(Texture)) as Texture;
        }
        Rect lastRect = GUILayoutUtility.GetLastRect();

        Rect rect = new Rect(0, lastRect.y + 4, 256, 128);
        rect.width = Screen.width - 64;
        rect.height = rect.width / 2;
        rect.x = (Screen.width / 2f) - (rect.width / 2f);

        if (GUI.Button(rect, Splash, GUIStyle.none))
        {
            Application.OpenURL("https://discord.gg/jwMhHzjtrB");
        }

        GUILayout.Space(rect.height + 8);
        DrawSpacer(0, 5, 0);
    }

    void DrawGeneralSettings(Color color)
    {
        DrawHeader(GeneralSettingsFoldout, new GUIContent("General Settings", "This Are General Settings Shared With All GameModes"), color);

        if (!GeneralSettingsFoldout.boolValue) { EditorGUILayout.EndVertical(); return; }

        EditorGUI.indentLevel++;

        GUI.changed = false;
        ModMapManager MMM = (ModMapManager)target;
        if (DrawEditButton("Edit Area", EditorGUIUtility.IconContent("LightProbeProxyVolume Gizmo"), EditingPlayableArea ? Color.gray : Color.white)) { if (!EditingPlayableArea) { StopEdit(); EditingPlayableArea = true; } else StopEdit(); if (EditingPlayableArea) FrameBounds(MMM.PlayableArea); }
        //  EditMode.DoEditModeInspectorModeButton(EditMode.SceneViewEditMode.Collider, "Edit Area",EditorGUIUtility.IconContent("LightProbeProxyVolume Gizmo"), GetPlayableBounds, this);
        //EditorGUILayout.PropertyField(PlayableArea);
        // Use EditorGUI instead of GUILayout for button clicks
        if (DrawEditButton("Edit SpwanPoints", EditorGUIUtility.IconContent("EditCollider"), (MMM.EditingSpawnPoints ? Color.gray : Color.white))) { if (!MMM.EditingSpawnPoints) { StopEdit(); MMM.EditingSpawnPoints = true; } else StopEdit(); if (MMM.EditingSpawnPoints) FrameBounds(MMM.SpawnPoints); }
        if (DrawEditButton("Edit WeaponsSpawnPoints", EditorGUIUtility.IconContent("EditCollider"), (MMM.EditingWeaponSpawnPoints ? Color.gray : Color.white))) { if (!MMM.EditingWeaponSpawnPoints) { StopEdit(); MMM.EditingWeaponSpawnPoints = true; } else StopEdit(); if (MMM.EditingWeaponSpawnPoints) FrameBounds(MMM.WeaponSpawnPoints); }


        EditorGUILayout.Space(20);
        EditorGUILayout.PropertyField(Team1, false);
        if (Team1.isExpanded)
        {
            if (DrawEditButton("Set Team1 Base", EditorGUIUtility.IconContent("d_MoveTool on"), (MMM.Team1.EditingPosition ? Color.gray : Color.white) + new Color(0.25f, 0, 0))) { if (!MMM.Team1.EditingPosition) { StopEdit(); MMM.Team1.EditingPosition = true; } else StopEdit(); if (MMM.Team1.EditingPosition) FrameBounds(new Vector3[] { MMM.Team1.BasePosition }); }
            if (DrawEditButton("Set Team1 Flag", EditorGUIUtility.IconContent("d_MoveTool on"), (MMM.Team1.EditingFlagPositions ? Color.gray : Color.white) + new Color(0.25f, 0, 0))) { if (!MMM.Team1.EditingFlagPositions) { StopEdit(); MMM.Team1.EditingFlagPositions = true; } else StopEdit(); if (MMM.Team1.EditingFlagPositions) FrameBounds(new Vector3[] { MMM.Team1.FlagPosition }); }
        }

        EditorGUILayout.PropertyField(Team2, false);
        if (Team2.isExpanded)
        {
            if (DrawEditButton("Set Team2 Base", EditorGUIUtility.IconContent("d_MoveTool on"), (MMM.Team2.EditingPosition ? Color.gray : Color.white) + new Color(0, 0, 0.25f))) { if (!MMM.Team2.EditingPosition) { StopEdit(); MMM.Team2.EditingPosition = true; } else StopEdit(); if (MMM.Team2.EditingPosition) FrameBounds(new Vector3[] { MMM.Team2.BasePosition }); }
            if (DrawEditButton("Set Team2 Flag", EditorGUIUtility.IconContent("d_MoveTool on"), (MMM.Team2.EditingFlagPositions ? Color.gray : Color.white) + new Color(0, 0, 0.25f))) { if (!MMM.Team2.EditingFlagPositions) { StopEdit(); MMM.Team2.EditingFlagPositions = true; } else StopEdit(); if (MMM.Team2.EditingFlagPositions) FrameBounds(new Vector3[] { MMM.Team2.FlagPosition }); }
        }




        // EditorGUILayout.PropertyField(platforms);
        //EditorGUILayout.PropertyField(mapPath);

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    void DrawBotsSettings(Color color)
    {
        DrawHeader(BotsSettingsFoldout, new GUIContent("Bots Settings", "This Are Setting That Affect Bots Behaviour"), color);
        if (!BotsSettingsFoldout.boolValue) { EditorGUILayout.EndVertical(); return; }
        EditorGUI.indentLevel++;
        if (DrawEditButton("Edit BotsReferencePoints", EditorGUIUtility.IconContent("EditCollider"), (MMM.EditingBotsReferencePoints ? Color.gray : Color.white))) { if (!MMM.EditingBotsReferencePoints) { StopEdit(); MMM.EditingBotsReferencePoints = true; } else StopEdit(); if (MMM.EditingBotsReferencePoints) FrameBounds(MMM.BotsReferencePoints); }

        EditorGUI.indentLevel--;

        EditorGUILayout.EndVertical();
    }

    void DrawConquestSettings(Color color)
    {
        DrawHeader(ConquestSettingsFoldout, new GUIContent("Conquest Settings", "This Are Settings For Conquest GameMode"), color);
        if (!ConquestSettingsFoldout.boolValue) { EditorGUILayout.EndVertical(); return; }

        EditorGUILayout.PropertyField(EnableConquest);
        if (!MMM.EnableConquest) { EditorGUILayout.EndVertical(); return; }

        EditorGUILayout.Space(20);

        EditorGUILayout.PropertyField(ConquestRadius);
        EditorGUILayout.PropertyField(RespawnRadius);
        EditorGUILayout.PropertyField(ConquestOnlyObjects);

        EditorGUILayout.Space(20);

        foreach (ModMapManager.ConquestPoint CP in MMM.ConquestPoints.ToArray())
        {

            //if(DrawEditButton(,,))

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Objective " + Letters[MMM.ConquestPoints.IndexOf(CP)]);
            EditorGUI.indentLevel++;
            Color TC = GUI.color;

            Rect rect2 = new Rect(EditorGUILayout.GetControlRect().position, new Vector2(20, 6));
            rect2.x = Screen.width - (rect2.width / 2) - 40;
            if (GUI.Button(rect2, "")) { MMM.ConquestPoints.Remove(CP); break; }

            Rect rect = new Rect(EditorGUILayout.GetControlRect().position, new Vector2(35, 30));
            rect.x = (Screen.width / 2) - (rect.width / 2);
            GUI.color = (CP.Editing ? Color.gray : Color.white) + new Color(0, 0, 0.25f);
            bool value = (GUI.Button(rect, EditorGUIUtility.IconContent("d_MoveTool on")));

            GUI.color = TC;
            EditorGUI.indentLevel--;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space((rect.size.y / 2) + 5);

            if (value) { if (!CP.Editing) { StopEdit(); CP.Editing = true; } else StopEdit(); if (CP.Editing) FrameBounds(new Vector3[] { CP.Position }); }

            EditorGUILayout.Space(10);
        }


        if (DrawEditButton("Add", EditorGUIUtility.IconContent("d_ol_plus"), (Color.white)))
        {
            MMM.ConquestPoints.Add(new ModMapManager.ConquestPoint());
        }


        EditorGUILayout.EndVertical();
    }


    void DrawPatrolSettings(Color color)
    {
        DrawHeader(PatrolSettingsFoldout, new GUIContent("Patrol Settings", "This Are Settings For Patrol GameMode"), color);
        if (!PatrolSettingsFoldout.boolValue) { EditorGUILayout.EndVertical(); return; }

        EditorGUILayout.PropertyField(EnablePatrol);
        if (!MMM.EnablePatrol) { EditorGUILayout.EndVertical(); return; }

        EditorGUILayout.Space(20);

        EditorGUILayout.PropertyField(PatrolOnlyObjects);

        EditorGUILayout.Space(20);

        foreach (ModMapManager.PatrolPoint PP in MMM.PatrolPoints.ToArray())
        {

            //if(DrawEditButton(,,))

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Objective " + MMM.PatrolPoints.IndexOf(PP));
            EditorGUI.indentLevel++;
            Color TC = GUI.color;

            Rect rect2 = new Rect(EditorGUILayout.GetControlRect().position, new Vector2(20, 6));
            rect2.x = Screen.width - (rect2.width / 2) - 40;
            if (GUI.Button(rect2, "")) { MMM.PatrolPoints.Remove(PP); break; }

            Rect rect = new Rect(EditorGUILayout.GetControlRect().position, new Vector2(35, 30));
            rect.x = (Screen.width / 2) - (rect.width / 2);
            GUI.color = (PP.Editing ? Color.gray : Color.white) + new Color(0, 0, 0.25f);
            bool value = (GUI.Button(rect, EditorGUIUtility.IconContent("EditCollider")));

            GUI.color = TC;
            EditorGUI.indentLevel--;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space((rect.size.y / 2) + 5);

            if (value) { if (!PP.Editing) { StopEdit(); PP.Editing = true; } else StopEdit(); if (PP.Editing) FrameBounds(PP.Points); }

            EditorGUILayout.Space(10);
        }


        if (DrawEditButton("Add", EditorGUIUtility.IconContent("d_ol_plus"), (Color.white)))
        {
            MMM.PatrolPoints.Add(new ModMapManager.PatrolPoint());
        }


        EditorGUILayout.EndVertical();
    }

    void DrawSabotageSettings(Color color)
    {
        DrawHeader(SabotageSettingsFoldout, new GUIContent("Sabotage Settings", "This Are Settings For Sabotage GameMode"), color);
        if (!SabotageSettingsFoldout.boolValue) { EditorGUILayout.EndVertical(); return; }

        EditorGUILayout.PropertyField(EnableSabotage);
        if (!MMM.EnableSabotage) { EditorGUILayout.EndVertical(); return; }

        EditorGUILayout.Space(20);

        EditorGUILayout.PropertyField(SabotageOnlyObjects);

        EditorGUILayout.Space(20);

        foreach (ModMapManager.BombPoint BP in MMM.BombPoints.ToArray())
        {

            //if(DrawEditButton(,,))

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bomb Objective " + Letters[MMM.BombPoints.IndexOf(BP)]);
            EditorGUI.indentLevel++;
            Color TC = GUI.color;

            Rect rect2 = new Rect(EditorGUILayout.GetControlRect().position, new Vector2(20, 6));
            rect2.x = Screen.width - (rect2.width / 2) - 40;
            if (GUI.Button(rect2, "")) { MMM.BombPoints.Remove(BP); break; }

            Rect rect = new Rect(EditorGUILayout.GetControlRect().position, new Vector2(35, 30));
            rect.x = (Screen.width / 2) - (rect.width / 2);
            GUI.color = (BP.Editing ? Color.gray : Color.white) + new Color(0, 0, 0.25f);
            bool value = (GUI.Button(rect, EditorGUIUtility.IconContent("d_MoveTool on")));

            GUI.color = TC;
            EditorGUI.indentLevel--;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space((rect.size.y / 2) + 5);

            if (value) { if (!BP.Editing) { StopEdit(); BP.Editing = true; } else StopEdit(); if (BP.Editing) FrameBounds(new Vector3[] { BP.Position }); }

            EditorGUILayout.Space(10);
        }


        if (DrawEditButton("Add", EditorGUIUtility.IconContent("d_ol_plus"), (Color.white)))
        {
            MMM.BombPoints.Add(new ModMapManager.BombPoint());
        }


        EditorGUILayout.EndVertical();
    }

    void DrawExportSettings(Color color)
    {
        DrawHeader(ExportSettingsFoldout, new GUIContent("Export Settings", "This Are Settings For Exporting The Map"), color);
        if (!ExportSettingsFoldout.boolValue) { EditorGUILayout.EndVertical(); return; }
        EditorGUILayout.PropertyField(buildTarget);
        EditorGUILayout.PropertyField(Info);
        EditorGUILayout.PropertyField(platforms);
        EditorGUILayout.PropertyField(CompressMeshUVs);
        EditorGUILayout.PropertyField(CompressMeshVertices);
        EditorGUILayout.PropertyField(ConvertShaders);
        if (MMM.ConvertShaders)
        {
            EditorGUILayout.PropertyField(PackTextures);
            if (MMM.PackTextures)
            {
                EditorGUILayout.PropertyField(AtlasSize);
                EditorGUILayout.PropertyField(MaxTextureSize);
            }

            EditorGUILayout.PropertyField(SetStageVertexLimit);
            if (MMM.SetStageVertexLimit)
            {
                EditorGUILayout.PropertyField(VertexLimit);
            }
        }

        EditorGUILayout.Space(20);
        if (GUILayout.Button("Build Map")) { MMM.SaveMap(); }
        if (GUILayout.Button("Load Map")) { MMM.LoadMap(); }


        EditorGUILayout.EndVertical();
    }

    private void OnDisable()
    {
        UnityEditor.Tools.hidden = false;
    }

    void OnSceneGUI()
    {


        UnityEditor.Tool currentTool = UnityEditor.Tools.current;
        if (currentTool == UnityEditor.Tool.Rotate || currentTool == UnityEditor.Tool.Move || currentTool == UnityEditor.Tool.Scale) UnityEditor.Tools.hidden = true;
        else { UnityEditor.Tools.hidden = false; return; }

        if (!MMM) MMM = (ModMapManager)target;
        if (MMM.GeneralSettingsFoldout)
        {
            if (!EditingPlayableArea)
            {
                Handles.color = PlayableAreaColor;
                Handles.DrawWireCube(MMM.PlayableArea.center, MMM.PlayableArea.size);
            }
            else
                using (new Handles.DrawingScope(PlayableAreaEditColor))
                {
                    PlayableAreaHandle.center = MMM.PlayableArea.center;
                    PlayableAreaHandle.size = MMM.PlayableArea.size;

                    EditorGUI.BeginChangeCheck();
                    PlayableAreaHandle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(MMM, "Modified ModMapManager");
                        MMM.PlayableArea.center = PlayableAreaHandle.center;
                        MMM.PlayableArea.size = PlayableAreaHandle.size;
                        EditorUtility.SetDirty(target);
                    }
                }
            // Draw gizmos for existing  points
            Handles.color = Color.green - new Color(0, 0, 0, 0.5f);
            if (MMM.EditingSpawnPoints)
            {
                Handles.color += new Color(0, 0, 0, 0.5f);
                EditPoints(ref MMM.SpawnPoints);
            }
            foreach (Vector3 Point in MMM.SpawnPoints)
            {
                Handles.DrawWireCube(Point, Vector3.one * 1);
            }

            Handles.color = Color.cyan - new Color(0, 0, 0, 0.5f);
            if (MMM.EditingWeaponSpawnPoints)
            {
                Handles.color += new Color(0, 0, 0, 0.5f);
                EditPoints(ref MMM.WeaponSpawnPoints);
            }
            foreach (Vector3 Point in MMM.WeaponSpawnPoints)
            {
                DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/Mp5LP.mesh", typeof(Mesh)) as Mesh, Point, 5);
            }

            if (Team1.isExpanded)
            {
                Handles.color = Color.red;

                if (MMM.Team1.EditingPosition) GetLeftClickedPoint(ref MMM.Team1.BasePosition);//Handles.DoPositionHandle(MMM.Team1.BasePosition,Quaternion.identity);
                DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/X.mesh", typeof(Mesh)) as Mesh, MMM.Team1.BasePosition, 3);

                if (MMM.Team1.EditingFlagPositions) GetLeftClickedPoint(ref MMM.Team1.FlagPosition);//Handles.DoPositionHandle(MMM.Team1.BasePosition,Quaternion.identity);
                DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/Flag.mesh", typeof(Mesh)) as Mesh, MMM.Team1.FlagPosition, 1.5f);
            }

            if (Team2.isExpanded)
            {
                Handles.color = Color.blue;

                if (MMM.Team2.EditingPosition) GetLeftClickedPoint(ref MMM.Team2.BasePosition);//Handles.DoPositionHandle(MMM.Team1.BasePosition,Quaternion.identity);
                DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/X.mesh", typeof(Mesh)) as Mesh, MMM.Team2.BasePosition, 3);

                if (MMM.Team2.EditingFlagPositions) GetLeftClickedPoint(ref MMM.Team2.FlagPosition);//Handles.DoPositionHandle(MMM.Team1.BasePosition,Quaternion.identity);
                DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/Flag.mesh", typeof(Mesh)) as Mesh, MMM.Team2.FlagPosition, 1.5f);
            }
        }
        if (MMM.BotsSettingsFoldout)
        {
            Handles.color = Color.yellow - new Color(0, 0, 0, 0.5f);
            if (MMM.EditingBotsReferencePoints)
            {
                Handles.color += new Color(0, 0, 0, 0.5f);
                EditPoints(ref MMM.BotsReferencePoints);
            }
            foreach (Vector3 Point in MMM.BotsReferencePoints)
            {
                Handles.DrawWireCube(Point, Vector3.one * 1f);
            }
        }

        if (MMM.ConquestSettingsFoldout && MMM.EnableConquest)
        {
            Handles.color = Color.white;
            foreach (Transform T in MMM.transform.GetChild(1))
            {
                bool destroy = true;
                foreach (ModMapManager.ConquestPoint CP in MMM.ConquestPoints) if (CP.Obj == T.gameObject) destroy = false;
                if (destroy) DestroyImmediate(T.gameObject);
            }

            foreach (ModMapManager.ConquestPoint CP in MMM.ConquestPoints)
            {
                if (CP.Obj == null) CP.Obj = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath(Root + "Prefabs/Conquest_Point_PH.prefab", typeof(GameObject)), MMM.transform.GetChild(1));
                CP.Obj.transform.GetChild(0).GetComponent<TextMesh>().text = Letters[MMM.ConquestPoints.IndexOf(CP)];
                CP.Obj.transform.localScale = Vector3.one * MMM.ConquestRadius;
                if (CP.Editing) GetLeftClickedPoint(ref CP.Position);//CP.Position=Handles.DoPositionHandle(CP.Position,Quaternion.identity);
                CP.Obj.transform.position = CP.Position;
                Handles.DrawWireDisc(CP.Position, Vector3.up, MMM.RespawnRadius);
            }
        }

        if (MMM.PatrolSettingsFoldout && MMM.EnablePatrol)
        {

            foreach (ModMapManager.PatrolPoint PP in MMM.PatrolPoints)
            {
                foreach (Vector3 V3 in PP.Points)
                {
                    Handles.color = Color.red;
                    DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/PatrolMarker.mesh", typeof(Mesh)) as Mesh, V3);
                    Handles.color = Color.cyan;
                    DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/PatrolMarker.mesh", typeof(Mesh)) as Mesh, V3 + new Vector3(0, 0.37f, 0));

                }
                Handles.color = Color.white;

                if (PP.Editing)
                {
                    EditPoints(ref PP.Points);
                    Vector3[] WrappedPP = PP.Wrapped;
                    System.Array.Resize(ref WrappedPP, WrappedPP.Length + 1);
                    WrappedPP[WrappedPP.Length - 1] = WrappedPP[0];
                    Handles.DrawPolyLine(WrappedPP);
                }

            }
        }

        if (MMM.SabotageSettingsFoldout && MMM.EnableSabotage)
        {

            foreach (ModMapManager.BombPoint BP in MMM.BombPoints)
            {
                Handles.color = Color.cyan;
                DrawMesh(AssetDatabase.LoadAssetAtPath(Root + "Meshes/Bomb.mesh", typeof(Mesh)) as Mesh, BP.Position);

                Handles.color = Color.white;

                if (BP.Editing) GetLeftClickedPoint(ref BP.Position);//BP.Position=Handles.DoPositionHandle(BP.Position,Quaternion.identity);

            }
        }

    }

    public void DrawMesh(Mesh mesh, Vector3 pos, float size = 1)
    {
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        List<Vector3[]> Faces = new List<Vector3[]>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            Handles.DrawPolyLine(
                new Vector3[]{
                        (verts[tris[i]]*size)+pos,
                        (verts[tris[i+1]]*size)+pos,
                        (verts[tris[i+2]]*size)+pos,
                        (verts[tris[i]]*size)+pos
                    }
            );
        }
    }


    public void EditPoints(ref Vector3[] Points)
    {
        // Disable the default control behavior to prevent object selection
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event currentEvent = Event.current;

        // Ensure the ray is cast only on MouseUp event

        bool mouseDragged = currentEvent.type == EventType.MouseDown;
        if (currentEvent.type == EventType.MouseUp)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;

            // Define a layer mask to consider only the default layer
            int layerMask = 1 << LayerMask.NameToLayer("Default");

            // Raycast with the specified layer mask
            if (Physics.Raycast(ray, out hit, float.MaxValue, layerMask))
            {

                switch (currentEvent.button)
                {
                    case 0: // Left mouse button click
                        Undo.RecordObject(MMM, "Add  Point");

                        // Add the  point to the array
                        System.Array.Resize(ref Points, Points.Length + 1);
                        Points[Points.Length - 1] = hit.point;
                        EditorUtility.SetDirty(MMM);

                        // Consume the event to prevent selection
                        currentEvent.Use();
                        break;

                    case 1: // Right mouse button click
                        if (!currentEvent.alt && !currentEvent.control && !currentEvent.shift)
                        {
                            // Ensure the mouse wasn't dragged before removing
                            if (!mouseDragged)
                            {
                                Undo.RecordObject(MMM, "Remove  Point");

                                // Find the closest  point and remove it
                                float minDistance = float.MaxValue;
                                int indexToRemove = -1;

                                for (int i = 0; i < Points.Length; i++)
                                {
                                    float distance = Vector3.Distance(hit.point, Points[i]);
                                    if (distance < minDistance && distance < 1.0f) // Adjust the threshold as needed
                                    {
                                        minDistance = distance;
                                        indexToRemove = i;
                                    }
                                }

                                if (indexToRemove != -1)
                                {
                                    // Remove the closest  point from the array
                                    // Remove the  point at the specified index
                                    List<Vector3> PointsList = new List<Vector3>(Points);
                                    PointsList.RemoveAt(indexToRemove);
                                    Points = PointsList.ToArray();
                                    EditorUtility.SetDirty(MMM);
                                }
                            }
                        }
                        break;
                }

                mouseDragged = false;

            }


        }
    }


    public void GetLeftClickedPoint(ref Vector3 Point)
    {
        // Disable the default control behavior to prevent object selection
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event currentEvent = Event.current;
        // Ensure the ray is cast only on MouseUp event

        if (currentEvent.type == EventType.MouseUp)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;

            // Define a layer mask to consider only the default layer
            int layerMask = 1 << LayerMask.NameToLayer("Default");

            // Raycast with the specified layer mask
            if (Physics.Raycast(ray, out hit, float.MaxValue, layerMask))
            {

                switch (currentEvent.button)
                {
                    case 0: // Left mouse button click
                        Point = hit.point;

                        // Consume the event to prevent selection
                        currentEvent.Use();
                        break;
                }


            }


        }

    }


    public bool DrawEditButton(string text, GUIContent content, Color color)
    {
        Color TC = GUI.color;

        EditorGUILayout.BeginHorizontal();
        EditorGUI.indentLevel++;
        Rect rect = new Rect(EditorGUILayout.GetControlRect().position, new Vector2(35, 30));
        rect.x = (Screen.width / 2) - (rect.width / 2);
        GUI.color = color;
        bool value = (GUI.Button(rect, content));
        GUI.color = TC;
        EditorGUILayout.LabelField(text);
        EditorGUI.indentLevel--;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space((rect.size.y / 2) + 5);

        //EditorUtility.SetDirty(target);

        return value;

    }


    public static void DrawHeader(SerializedProperty foldout, GUIContent guiContent, Color color)
    {
        GUI.color = color;
        EditorGUILayout.BeginVertical("Box");
        GUI.color = Color.white;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(15);
        LabelWidthUnderline(guiContent, 14, true, foldout.boolValue);
        Rect rect = GUILayoutUtility.GetLastRect();
        rect.x = 20;
        rect.y += 3;
        rect.width = 20;
        rect.height = 20;
        foldout.boolValue = EditorGUI.Foldout(rect, foldout.boolValue, GUIContent.none);

        EditorGUILayout.EndHorizontal();
        if (foldout.boolValue) GUILayout.Space(4);
    }



    static public void DrawSpacer(float spaceBegin = 5, float height = 5, float spaceEnd = 5)
    {
        GUILayout.Space(spaceBegin - 1);
        EditorGUILayout.BeginHorizontal();
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
        GUILayout.Button(string.Empty, GUILayout.Height(height));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(spaceEnd - 1);

        GUI.color = Color.white;
    }
    static public void LabelWidthUnderline(GUIContent guiContent, int fontSize, bool boldLabel = true, bool drawUnderline = true)
    {
        int fontSizeOld = EditorStyles.label.fontSize;
        EditorStyles.boldLabel.fontSize = fontSize;
        EditorGUILayout.LabelField(guiContent, boldLabel ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Height(fontSize + 6));
        EditorStyles.boldLabel.fontSize = fontSizeOld;
        if (drawUnderline) DrawUnderLine();
        GUILayout.Space(5);
    }

    static public void DrawUnderLine(float offsetY = 0)
    {
        Rect rect = GUILayoutUtility.GetLastRect();
        if (EditorGUIUtility.isProSkin) GUI.color = Color.grey; else GUI.color = Color.black;
        GUI.DrawTexture(new Rect(rect.x, rect.yMax + offsetY, rect.width, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void FrameBounds(Vector3[] positions)
    {
        if (positions.Length == 0) return;
        if (positions.Length == 1 && positions[0] == Vector3.zero) return;

        Bounds bounds = new Bounds(positions[0], Vector3.zero);

        foreach (Vector3 position in positions)
        {
            bounds.Encapsulate(position);
        }

        FrameBounds(bounds);
    }

    void FrameBounds(Bounds bounds)
    {
        SceneView.lastActiveSceneView.Frame(bounds, false);
    }

    void StopEdit()
    {
        EditingPlayableArea = false;
        MMM.EditingWeaponSpawnPoints = false;
        MMM.EditingBotsReferencePoints = false;
        MMM.EditingSpawnPoints = false;
        MMM.Team1.EditingFlagPositions = false;
        MMM.Team1.EditingPosition = false;
        MMM.Team2.EditingFlagPositions = false;
        MMM.Team2.EditingPosition = false;

        foreach (ModMapManager.ConquestPoint CP in MMM.ConquestPoints) { CP.Editing = false; }
        foreach (ModMapManager.PatrolPoint PP in MMM.PatrolPoints) { PP.Editing = false; }
        foreach (ModMapManager.BombPoint BP in MMM.BombPoints) { BP.Editing = false; }
    }
}
#endif
