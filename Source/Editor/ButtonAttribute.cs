// ButtonAttribute.cs
using System;
using UnityEditor;
using UnityEngine;

public class ButtonAttribute : Attribute
{
    public readonly string methodName;

    public ButtonAttribute(string methodName)
    {
        this.methodName = methodName;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MonoBehaviour), true)]
public class ButtonAttributeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var mono = target as MonoBehaviour;
        if (mono == null)
            return;

        var methods = mono.GetType()
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(ButtonAttribute), true);
            foreach (var attribute in attributes)
            {
                if (GUILayout.Button(((ButtonAttribute)attribute).methodName))
                {
                    method.Invoke(mono, null);
                }
            }
        }
    }
}
#endif
