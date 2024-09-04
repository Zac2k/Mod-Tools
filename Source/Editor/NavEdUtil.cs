using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine;

using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace idbrii.navgen
{
    static class NavEdUtil
    {

        public static Object[] GetAllInActiveScene<T>() where T : Component
        {
#if UNITY_EDITOR
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(comp => comp.gameObject.scene == scene)
                .Select(comp => comp as Object)
                .ToArray();
#else
                return null;
#endif
        }

    }
}
