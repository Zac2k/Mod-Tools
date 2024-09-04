using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR

[CustomEditor(typeof(PointsManager))]
public class PointsEditor : Editor
{
    public bool mouseDragged=false;
private void OnSceneGUI()
{
    // Disable the default control behavior to prevent object selection
    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

    Event currentEvent = Event.current;

    // Ensure the ray is cast only on MouseUp event

    if (currentEvent.type == EventType.MouseDown)
    {
        mouseDragged=IsMouseDragged();
    }
    if (currentEvent.type == EventType.MouseUp)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        RaycastHit hit;

        // Define a layer mask to consider only the default layer
        int layerMask = 1 << LayerMask.NameToLayer("Default");

        // Raycast with the specified layer mask
        if (Physics.Raycast(ray, out hit, float.MaxValue, layerMask))
        {
            PointsManager PointsManager = (PointsManager)target;

            switch (currentEvent.button)
            {
                case 0: // Left mouse button click
                    Undo.RecordObject(PointsManager, "Add  Point");

                    // Add the  point to the array
                    PointsManager.AddPoint(hit.point);
                    EditorUtility.SetDirty(PointsManager);

                    // Consume the event to prevent selection
                    currentEvent.Use();
                    break;

                case 1: // Right mouse button click
                    if (!currentEvent.alt && !currentEvent.control && !currentEvent.shift)
                    {
                        // Ensure the mouse wasn't dragged before removing
                        if (!mouseDragged)
                        {
                            Undo.RecordObject(PointsManager, "Remove  Point");

                            // Find the closest  point and remove it
                            float minDistance = float.MaxValue;
                            int indexToRemove = -1;

                            for (int i = 0; i < PointsManager.Points.Length; i++)
                            {
                                float distance = Vector3.Distance(hit.point,PointsManager.PointSpace==Space.Self?PointsManager.transform.TransformPoint(PointsManager.Points[i]) : PointsManager.Points[i]);
                                if (distance < minDistance && distance < 1.0f) // Adjust the threshold as needed
                                {
                                    minDistance = distance;
                                    indexToRemove = i;
                                }
                            }

                            if (indexToRemove != -1)
                            {
                                // Remove the closest  point from the array
                                PointsManager.RemovePoint(indexToRemove);
                                EditorUtility.SetDirty(PointsManager);
                            }
                        }
                    }
                    break;
            }
        }

        mouseDragged=false;

    }

    // Draw gizmos for existing  points
   PointsManager manager = (PointsManager)target;
        if (manager != null && manager.Points != null)
        {
            foreach (Vector3 Point in manager.Points)
            {
                Handles.color = manager.GizmozColor;
                Handles.DrawWireCube(manager.PointSpace==Space.Self?manager.transform.TransformPoint(Point):Point, Vector3.one * manager.GizmozSize*(manager.PointSpace==Space.Self?manager.transform.lossyScale.magnitude/2:1));
            }
        }


}

  public override void OnInspectorGUI() {
   PointsManager manager = (PointsManager)target;
            //   Handles.BeginGUI();
     DrawDefaultInspector();
       // GUILayout.BeginArea(new Rect(10, 10, 100, 50));
    if(GUILayout.Button("Trim Points Below TrimHeight")){manager.TrimPoints();}
    if(GUILayout.Button("Fill Points With FillPrefab")){manager.FillPointsWithPrefab();}
    if(GUILayout.Button("Toggle Space")){
        manager.PointSpace=manager.PointSpace==Space.Self?manager.PointSpace=Space.World:manager.PointSpace=Space.Self;
        for(int i=0; i<manager.Points.Length; i++){
            manager.Points[i]=manager.PointSpace==Space.Self?manager.transform.InverseTransformPoint( manager.Points[i]):manager.transform.TransformPoint( manager.Points[i]);
        }
        }

       /* if(GUILayout.Button("Wrap")){
            // Call the TrimPoints method when the button is clicked
            WrapPoints(manager); // You can replace 0.0f with the desired trim height
    }*/
       // GUILayout.EndArea();
      //  Handles.EndGUI();

  }

private bool IsMouseDragged()
{
    return Event.current.delta.magnitude > 0.5f;
}











}
#endif