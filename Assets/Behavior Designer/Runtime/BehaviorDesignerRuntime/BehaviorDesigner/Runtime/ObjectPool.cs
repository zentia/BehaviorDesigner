namespace BehaviorDesigner.Runtime
{
    using System;
    using System.Collections.Generic;

    public static class ObjectPool
    {
        private static Dictionary<Type, object> poolDictionary = new Dictionary<Type, object>();

        public static T Get<T>()
        {
            if (poolDictionary.ContainsKey(typeof(T)))
            {
                List<T> list = poolDictionary[typeof(T)] as List<T>;
                if (list.Count > 0)
                {
                    T local = list[0];
                    list.RemoveAt(0);
                    return local;
                }
            }
            return (T) TaskUtility.CreateInstance(typeof(T));
        }

        public static void Return<T>(T obj)
        {
            if (obj != null)
            {
                if (poolDictionary.ContainsKey(typeof(T)))
                {
                    (poolDictionary[typeof(T)] as List<T>).Add(obj);
                }
                else
                {
                    List<T> list2 = new List<T> {
                        obj
                    };
                    poolDictionary.Add(typeof(T), list2);
                }
            }
        }
    }
}

