namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using UnityEditor;
    using UnityEngine;

    public class BehaviorDesignerPreferences : Editor
    {
        private static string[] prefString;
        private static string[] serializationString = new string[] { "Binary", "JSON" };

        private static void DrawBoolPref(BDPreferences pref, string text, PreferenceChangeHandler callback)
        {
            bool @bool = GetBool(pref);
            bool flag2 = GUILayout.Toggle(@bool, text, new GUILayoutOption[0]);
            if (flag2 != @bool)
            {
                SetBool(pref, flag2);
                callback(pref, flag2);
                if (((pref == BDPreferences.EditablePrefabInstances) && flag2) && GetBool(BDPreferences.BinarySerialization))
                {
                    SetBool(BDPreferences.BinarySerialization, false);
                    callback(BDPreferences.BinarySerialization, false);
                }
                else if (((pref == BDPreferences.BinarySerialization) && flag2) && GetBool(BDPreferences.EditablePrefabInstances))
                {
                    SetBool(BDPreferences.EditablePrefabInstances, false);
                    callback(BDPreferences.EditablePrefabInstances, false);
                }
            }
        }

        public static void DrawPreferencesPane(PreferenceChangeHandler callback)
        {
            DrawBoolPref(BDPreferences.ShowWelcomeScreen, "Show welcome screen", callback);
            DrawBoolPref(BDPreferences.ShowSceneIcon, "Show Behavior Designer icon in the scene", callback);
            DrawBoolPref(BDPreferences.ShowHierarchyIcon, "Show Behavior Designer icon in the hierarchy window", callback);
            DrawBoolPref(BDPreferences.OpenInspectorOnTaskSelection, "Open inspector on single task selection", callback);
            DrawBoolPref(BDPreferences.OpenInspectorOnTaskDoubleClick, "Open inspector on task double click", callback);
            DrawBoolPref(BDPreferences.FadeNodes, "Fade tasks after they are done running", callback);
            DrawBoolPref(BDPreferences.EditablePrefabInstances, "Allow edit of prefab instances", callback);
            DrawBoolPref(BDPreferences.PropertiesPanelOnLeft, "Position properties panel on the left", callback);
            DrawBoolPref(BDPreferences.MouseWhellScrolls, "Mouse wheel scrolls graph view", callback);
            DrawBoolPref(BDPreferences.FoldoutFields, "Grouped fields start visible", callback);
            DrawBoolPref(BDPreferences.CompactMode, "Compact mode", callback);
            DrawBoolPref(BDPreferences.SnapToGrid, "Snap to grid", callback);
            DrawBoolPref(BDPreferences.ShowTaskDescription, "Show selected task description", callback);
            DrawBoolPref(BDPreferences.ErrorChecking, "Realtime error checking", callback);
            DrawBoolPref(BDPreferences.UpdateCheck, "Check for updates", callback);
            DrawBoolPref(BDPreferences.AddGameGUIComponent, "Add Game GUI Component", callback);
            bool @bool = GetBool(BDPreferences.BinarySerialization);
            if (EditorGUILayout.Popup("Serialization", !@bool ? 1 : 0, serializationString, new GUILayoutOption[0]) != (!@bool ? 1 : 0))
            {
                SetBool(BDPreferences.BinarySerialization, !@bool);
                callback(BDPreferences.BinarySerialization, !@bool);
            }
            int @int = GetInt(BDPreferences.GizmosViewMode);
            int num2 = (int) ((Behavior.GizmoViewMode) EditorGUILayout.EnumPopup("Gizmos View Mode", (Behavior.GizmoViewMode) @int, new GUILayoutOption[0]));
            if (num2 != @int)
            {
                SetInt(BDPreferences.GizmosViewMode, num2);
                callback(BDPreferences.GizmosViewMode, num2);
            }
            if (GUILayout.Button("Restore to Defaults", EditorStyles.miniButtonMid, new GUILayoutOption[0]))
            {
                ResetPrefs();
            }
        }

        public static bool GetBool(BDPreferences pref)
        {
            return EditorPrefs.GetBool(PrefString[(int) pref]);
        }

        public static int GetInt(BDPreferences pref)
        {
            return EditorPrefs.GetInt(PrefString[(int) pref]);
        }

        public static void InitPrefernces()
        {
            if (!EditorPrefs.HasKey(PrefString[0]))
            {
                SetBool(BDPreferences.ShowWelcomeScreen, true);
            }
            if (!EditorPrefs.HasKey(PrefString[1]))
            {
                SetBool(BDPreferences.ShowSceneIcon, true);
            }
            if (!EditorPrefs.HasKey(PrefString[2]))
            {
                SetBool(BDPreferences.ShowHierarchyIcon, true);
            }
            if (!EditorPrefs.HasKey(PrefString[3]))
            {
                SetBool(BDPreferences.OpenInspectorOnTaskSelection, false);
            }
            if (!EditorPrefs.HasKey(PrefString[3]))
            {
                SetBool(BDPreferences.OpenInspectorOnTaskSelection, false);
            }
            if (!EditorPrefs.HasKey(PrefString[5]))
            {
                SetBool(BDPreferences.FadeNodes, true);
            }
            if (!EditorPrefs.HasKey(PrefString[6]))
            {
                SetBool(BDPreferences.EditablePrefabInstances, false);
            }
            if (!EditorPrefs.HasKey(PrefString[7]))
            {
                SetBool(BDPreferences.PropertiesPanelOnLeft, true);
            }
            if (!EditorPrefs.HasKey(PrefString[8]))
            {
                SetBool(BDPreferences.MouseWhellScrolls, false);
            }
            if (!EditorPrefs.HasKey(PrefString[9]))
            {
                SetBool(BDPreferences.FoldoutFields, true);
            }
            if (!EditorPrefs.HasKey(PrefString[10]))
            {
                SetBool(BDPreferences.CompactMode, false);
            }
            if (!EditorPrefs.HasKey(PrefString[11]))
            {
                SetBool(BDPreferences.SnapToGrid, true);
            }
            if (!EditorPrefs.HasKey(PrefString[12]))
            {
                SetBool(BDPreferences.ShowTaskDescription, true);
            }
            if (!EditorPrefs.HasKey(PrefString[13]))
            {
                SetBool(BDPreferences.BinarySerialization, true);
            }
            if (!EditorPrefs.HasKey(PrefString[14]))
            {
                SetBool(BDPreferences.ErrorChecking, true);
            }
            if (!EditorPrefs.HasKey(PrefString[15]))
            {
                SetBool(BDPreferences.UpdateCheck, true);
            }
            if (!EditorPrefs.HasKey(PrefString[0x10]))
            {
                SetBool(BDPreferences.AddGameGUIComponent, false);
            }
            if (!EditorPrefs.HasKey(PrefString[0x11]))
            {
                SetInt(BDPreferences.GizmosViewMode, 2);
            }
            if (GetBool(BDPreferences.EditablePrefabInstances) && GetBool(BDPreferences.BinarySerialization))
            {
                SetBool(BDPreferences.BinarySerialization, false);
            }
        }

        private static void InitPrefString()
        {
            prefString = new string[0x12];
            for (int i = 0; i < prefString.Length; i++)
            {
                prefString[i] = string.Format("BehaviorDesigner{0}", (BDPreferences) i);
            }
        }

        private static void ResetPrefs()
        {
            SetBool(BDPreferences.ShowWelcomeScreen, true);
            SetBool(BDPreferences.ShowSceneIcon, true);
            SetBool(BDPreferences.ShowHierarchyIcon, true);
            SetBool(BDPreferences.OpenInspectorOnTaskSelection, false);
            SetBool(BDPreferences.OpenInspectorOnTaskDoubleClick, false);
            SetBool(BDPreferences.FadeNodes, true);
            SetBool(BDPreferences.EditablePrefabInstances, false);
            SetBool(BDPreferences.PropertiesPanelOnLeft, true);
            SetBool(BDPreferences.MouseWhellScrolls, false);
            SetBool(BDPreferences.FoldoutFields, true);
            SetBool(BDPreferences.CompactMode, false);
            SetBool(BDPreferences.SnapToGrid, true);
            SetBool(BDPreferences.ShowTaskDescription, true);
            SetBool(BDPreferences.BinarySerialization, true);
            SetBool(BDPreferences.ErrorChecking, true);
            SetBool(BDPreferences.UpdateCheck, true);
            SetBool(BDPreferences.AddGameGUIComponent, false);
            SetInt(BDPreferences.GizmosViewMode, 2);
        }

        public static void SetBool(BDPreferences pref, bool value)
        {
            EditorPrefs.SetBool(PrefString[(int) pref], value);
        }

        public static void SetInt(BDPreferences pref, int value)
        {
            EditorPrefs.SetInt(PrefString[(int) pref], value);
        }

        private static string[] PrefString
        {
            get
            {
                if (prefString == null)
                {
                    InitPrefString();
                }
                return prefString;
            }
        }
    }
}

