namespace BehaviorDesigner.Runtime
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    public class TaskUtility
    {
        private static List<string> loadedAssemblies = null;
        [NonSerialized]
        private static Dictionary<string, System.Type> typeDictionary = new Dictionary<string, System.Type>();

        public static bool CompareType(System.Type t, string typeName)
        {
            System.Type o = System.Type.GetType(typeName + ", Assembly-CSharp");
            if (o == null)
            {
                o = System.Type.GetType(typeName + ", Assembly-CSharp-firstpass");
            }
            return t.Equals(o);
        }

        public static object CreateInstance(System.Type t)
        {
            if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                t = Nullable.GetUnderlyingType(t);
            }
            return Activator.CreateInstance(t, true);
        }

        public static FieldInfo[] GetAllFields(System.Type t)
        {
            List<FieldInfo> fieldList = new List<FieldInfo>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            GetFields(t, ref fieldList, (int) flags);
            return fieldList.ToArray();
        }

        private static void GetFields(System.Type t, ref List<FieldInfo> fieldList, int flags)
        {
            if (((t != null) && !t.Equals(typeof(ParentTask))) && (!t.Equals(typeof(Task)) && !t.Equals(typeof(SharedVariable))))
            {
                FieldInfo[] fields = t.GetFields((BindingFlags) flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    fieldList.Add(fields[i]);
                }
                GetFields(t.BaseType, ref fieldList, flags);
            }
        }

        public static FieldInfo[] GetPublicFields(System.Type t)
        {
            List<FieldInfo> fieldList = new List<FieldInfo>();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            GetFields(t, ref fieldList, (int) flags);
            return fieldList.ToArray();
        }

        public static System.Type GetTypeWithinAssembly(string typeName)
        {
            if (typeDictionary.ContainsKey(typeName))
            {
                return typeDictionary[typeName];
            }
            System.Type type = System.Type.GetType(typeName);
            if (type == null)
            {
                if (loadedAssemblies == null)
                {
                    loadedAssemblies = new List<string>();
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    for (int j = 0; j < assemblies.Length; j++)
                    {
                        loadedAssemblies.Add(assemblies[j].FullName);
                    }
                }
                for (int i = 0; i < loadedAssemblies.Count; i++)
                {
                    type = System.Type.GetType(typeName + "," + loadedAssemblies[i]);
                    if (type != null)
                    {
                        break;
                    }
                }
            }
            if (type != null)
            {
                typeDictionary.Add(typeName, type);
            }
            return type;
        }

        public static bool HasAttribute(FieldInfo field, System.Type attribute)
        {
            if (field == null)
            {
                return false;
            }
            return (field.GetCustomAttributes(attribute, false).Length > 0);
        }

        public static System.Type SharedVariableToConcreteType(System.Type sharedVariableType)
        {
            if (sharedVariableType.Equals(GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedInt")))
            {
                return typeof(int);
            }
            if (sharedVariableType.Equals(GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedFloat")))
            {
                return typeof(float);
            }
            if (sharedVariableType.Equals(GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedBool")))
            {
                return typeof(bool);
            }
            if (sharedVariableType.Equals(GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedString")))
            {
                return typeof(string);
            }
            if (sharedVariableType.Equals(GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedVector2")))
            {
                return typeof(Vector2);
            }
            if (sharedVariableType.Equals(GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedVector3")))
            {
                return typeof(Vector3);
            }
            if (sharedVariableType.Equals(GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedGameObject")))
            {
                return typeof(GameObject);
            }
            return null;
        }
    }
}

