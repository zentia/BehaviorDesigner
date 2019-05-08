namespace BehaviorDesigner.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public class AssetCreator : EditorWindow
    {
        private string m_AssetName;
        private AssetClassType m_classType;
        private bool m_CSharp = true;

        private static string ActionTaskContents(string name, bool cSharp)
        {
            if (cSharp)
            {
                return ("using UnityEngine;\nusing BehaviorDesigner.Runtime;\nusing BehaviorDesigner.Runtime.Tasks;\n\npublic class " + name + " : Action\n{\n\tpublic override void OnStart()\n\t{\n\t\t\n\t}\n\n\tpublic override TaskStatus OnUpdate()\n\t{\n\t\treturn TaskStatus.Success;\n\t}\n}");
            }
            return ("#pragma strict\n\nclass " + name + " extends BehaviorDesigner.Runtime.Tasks.Action\n{\n\tfunction OnStart()\n\t{\n\t\t\n\t}\n\n\tfunction OnUpdate()\n\t{\n\t\treturn BehaviorDesigner.Runtime.Tasks.TaskStatus.Success;\n\t}\n}");
        }

        private static string ConditionalTaskContents(string name, bool cSharp)
        {
            if (cSharp)
            {
                return ("using UnityEngine;\nusing BehaviorDesigner.Runtime;\nusing BehaviorDesigner.Runtime.Tasks;\n\npublic class " + name + " : Conditional\n{\n\tpublic override TaskStatus OnUpdate()\n\t{\n\t\treturn TaskStatus.Success;\n\t}\n}");
            }
            return ("#pragma strict\n\nclass " + name + " extends BehaviorDesigner.Runtime.Tasks.Conditional\n{\n\tfunction OnUpdate()\n\t{\n\t\treturn BehaviorDesigner.Runtime.Tasks.TaskStatus.Success;\n\t}\n}");
        }

        public static void CreateAsset(System.Type type, string name)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (assetPath == string.Empty)
            {
                assetPath = "Assets";
            }
            else if (Path.GetExtension(assetPath) != string.Empty)
            {
                assetPath = assetPath.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), string.Empty);
            }
            string path = AssetDatabase.GenerateUniqueAssetPath(assetPath + "/" + name + ".asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        private static void CreateScript(string name, AssetClassType classType, bool cSharp)
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (assetPath == string.Empty)
            {
                assetPath = "Assets";
            }
            else if (Path.GetExtension(assetPath) != string.Empty)
            {
                assetPath = assetPath.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), string.Empty);
            }
            string path = AssetDatabase.GenerateUniqueAssetPath(assetPath + "/" + name + (!cSharp ? ".js" : ".cs"));
            StreamWriter writer = new StreamWriter(path, false);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string str4 = string.Empty;
            switch (classType)
            {
                case AssetClassType.Action:
                    str4 = ActionTaskContents(fileNameWithoutExtension, cSharp);
                    break;

                case AssetClassType.Conditional:
                    str4 = ConditionalTaskContents(fileNameWithoutExtension, cSharp);
                    break;

                case AssetClassType.SharedVariable:
                    str4 = SharedVariableContents(fileNameWithoutExtension);
                    break;
            }
            writer.Write(str4);
            writer.Close();
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            this.m_AssetName = EditorGUILayout.TextField("Name", this.m_AssetName, new GUILayoutOption[0]);
            EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
            if (GUILayout.Button("OK", new GUILayoutOption[0]))
            {
                CreateScript(this.m_AssetName, this.m_classType, this.m_CSharp);
                base.Close();
            }
            if (GUILayout.Button("Cancel", new GUILayoutOption[0]))
            {
                base.Close();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string SharedVariableContents(string name)
        {
            string str = name.Remove(0, 6);
            string[] textArray1 = new string[] { "using UnityEngine;\nusing BehaviorDesigner.Runtime;\n\n[System.Serializable]\npublic class ", str, "\n{\n\n}\n\n[System.Serializable]\npublic class ", name, " : SharedVariable<", str, ">\n{\n\tpublic override string ToString() { return mValue == null ? \"null\" : mValue.ToString(); }\n\tpublic static implicit operator ", name, "(", str, " value) { return new ", name, " { mValue = value }; }\n}" };
            return string.Concat(textArray1);
        }

        public static void ShowWindow(AssetClassType classType, bool cSharp)
        {
            AssetCreator window = EditorWindow.GetWindow<AssetCreator>(true, "Asset Name");
            Vector2 vector = new Vector2(300f, 55f);
            window.maxSize = vector;
            window.minSize = vector;
            window.ClassType = classType;
            window.CSharp = cSharp;
        }

        private AssetClassType ClassType
        {
            set
            {
                this.m_classType = value;
                switch (this.m_classType)
                {
                    case AssetClassType.Action:
                        this.m_AssetName = "NewAction";
                        break;

                    case AssetClassType.Conditional:
                        this.m_AssetName = "NewConditional";
                        break;

                    case AssetClassType.SharedVariable:
                        this.m_AssetName = "SharedNewVariable";
                        break;
                }
            }
        }

        private bool CSharp
        {
            set
            {
                this.m_CSharp = value;
            }
        }

        public enum AssetClassType
        {
            Action,
            Conditional,
            SharedVariable
        }
    }
}

