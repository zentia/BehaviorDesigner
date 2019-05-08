using BehaviorDesigner.Editor;
using BehaviorDesigner.Runtime;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomObjectDrawer(typeof(NamedVariable))]
public class SharedNamedVariableDrawer : ObjectDrawer
{
    private static string[] variableNames;

    public override void OnGUI(GUIContent label)
    {
        NamedVariable variable = base.value as NamedVariable;
        EditorGUILayout.BeginVertical(new GUILayoutOption[0]);
        if (FieldInspector.DrawFoldout(variable.GetHashCode(), label))
        {
            EditorGUI.indentLevel++;
            if (variableNames == null)
            {
                List<System.Type> list = VariableInspector.FindAllSharedVariableTypes(true);
                variableNames = new string[list.Count];
                for (int j = 0; j < list.Count; j++)
                {
                    variableNames[j] = list[j].Name.Remove(0, 6);
                }
            }
            int selectedIndex = 0;
            string str = variable.type.Remove(0, 6);
            for (int i = 0; i < variableNames.Length; i++)
            {
                if (variableNames[i].Equals(str))
                {
                    selectedIndex = i;
                    break;
                }
            }
            variable.name = EditorGUILayout.TextField("Name", variable.name, new GUILayoutOption[0]);
            int num4 = EditorGUILayout.Popup("Type", selectedIndex, variableNames, BehaviorDesignerUtility.SharedVariableToolbarPopup, new GUILayoutOption[0]);
            System.Type type = VariableInspector.FindAllSharedVariableTypes(1)[num4];
            if (num4 != selectedIndex)
            {
                selectedIndex = num4;
                variable.value = Activator.CreateInstance(type) as SharedVariable;
            }
            GUILayout.Space(3f);
            variable.type = "Shared" + variableNames[selectedIndex];
            variable.value = FieldInspector.DrawSharedVariable(null, new GUIContent("Value"), null, type, variable.value);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }
}

