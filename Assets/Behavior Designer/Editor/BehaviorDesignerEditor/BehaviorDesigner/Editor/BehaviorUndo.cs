namespace BehaviorDesigner.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public class BehaviorUndo
    {
        public static Component AddComponent(GameObject undoObject, System.Type type)
        {
            return Undo.AddComponent(undoObject, type);
        }

        public static void DestroyObject(UnityEngine.Object undoObject, bool registerScene)
        {
            Undo.DestroyObjectImmediate(undoObject);
        }

        public static void RegisterUndo(string undoName, UnityEngine.Object undoObject)
        {
            Undo.RecordObject(undoObject, undoName);
        }
    }
}

