using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class PointsManager : MonoBehaviour
{
    public enum PointType{
        Weapon = 0
    }

    public PointType type;
    public Space PointSpace = Space.Self;
    public Color GizmozColor = Color.green;
    [Range(0.25f,5)]public float GizmozSize = 0.5f;
    public float TrimHeight;
    public Vector3[] Points=new Vector3[0];
    public GameObject FillPrefab;
    public List<GameObject> FilledPrefabs=new List<GameObject>();

#if UNITY_EDITOR
    public void AddPoint(Vector3 point)
    {
        // Add a new  point to the array
        System.Array.Resize(ref Points, Points.Length + 1);
        Points[Points.Length - 1] = (PointSpace==Space.Self?transform.InverseTransformPoint(point) :point);
    }

    public void RemovePoint(int index)
{
    // Remove the  point at the specified index
    List<Vector3> PointsList = new List<Vector3>(Points);
    PointsList.RemoveAt(index);
    Points = PointsList.ToArray();
}

    public void FillPointsWithPrefab()
{
foreach(Vector3 V3 in Points){
   GameObject GO = Instantiate(FillPrefab,transform);
   Undo.RegisterCreatedObjectUndo(GO,$"Cloned {FillPrefab.name}");
   GO.transform.position = PointSpace==Space.Self?transform.TransformPoint(V3) :V3;
    FilledPrefabs.Add(GO);
}
}

    // Method to trim  points below a specified height
    public void TrimPoints()
    {
        Undo.RecordObject(this, "Trim  Points");

        // Filter out  points below the trim height
        Points = Points.Where(point => point.y >= TrimHeight).ToArray();

    }


#endif

}
