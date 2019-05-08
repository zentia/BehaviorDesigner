namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;

    public class AlphanumComparator<T> : IComparer<T>
    {
        public int Compare(T x, T y)
        {
            string str = string.Empty;
            if (x.GetType().IsSubclassOf(typeof(Type)))
            {
                Type t = x as Type;
                str = this.TypePrefix(t) + "/";
                TaskCategoryAttribute[] attributeArray = null;
                if ((attributeArray = t.GetCustomAttributes(typeof(TaskCategoryAttribute), false) as TaskCategoryAttribute[]).Length > 0)
                {
                    str = str + attributeArray[0].Category + "/";
                }
                TaskNameAttribute[] attributeArray2 = null;
                if ((attributeArray2 = t.GetCustomAttributes(typeof(TaskNameAttribute), false) as TaskNameAttribute[]).Length > 0)
                {
                    str = str + attributeArray2[0].Name;
                }
                else
                {
                    str = str + BehaviorDesignerUtility.SplitCamelCase(t.Name.ToString());
                }
            }
            else if (x.GetType().IsSubclassOf(typeof(SharedVariable)))
            {
                string name = x.GetType().Name;
                if ((name.Length > 6) && name.Substring(0, 6).Equals("Shared"))
                {
                    name = name.Substring(6, name.Length - 6);
                }
                str = BehaviorDesignerUtility.SplitCamelCase(name);
            }
            else
            {
                str = BehaviorDesignerUtility.SplitCamelCase(x.ToString());
            }
            if (str == null)
            {
                return 0;
            }
            string str3 = string.Empty;
            if (y.GetType().IsSubclassOf(typeof(Type)))
            {
                Type type2 = y as Type;
                str3 = this.TypePrefix(type2) + "/";
                TaskCategoryAttribute[] attributeArray3 = null;
                if ((attributeArray3 = type2.GetCustomAttributes(typeof(TaskCategoryAttribute), false) as TaskCategoryAttribute[]).Length > 0)
                {
                    str3 = str3 + attributeArray3[0].Category + "/";
                }
                TaskNameAttribute[] attributeArray4 = null;
                if ((attributeArray4 = type2.GetCustomAttributes(typeof(TaskNameAttribute), false) as TaskNameAttribute[]).Length > 0)
                {
                    str3 = str3 + attributeArray4[0].Name;
                }
                else
                {
                    str3 = str3 + BehaviorDesignerUtility.SplitCamelCase(type2.Name.ToString());
                }
            }
            else if (y.GetType().IsSubclassOf(typeof(SharedVariable)))
            {
                string s = y.GetType().Name;
                if ((s.Length > 6) && s.Substring(0, 6).Equals("Shared"))
                {
                    s = s.Substring(6, s.Length - 6);
                }
                str3 = BehaviorDesignerUtility.SplitCamelCase(s);
            }
            else
            {
                str3 = BehaviorDesignerUtility.SplitCamelCase(y.ToString());
            }
            if (str3 == null)
            {
                return 0;
            }
            int length = str.Length;
            int num2 = str3.Length;
            int num3 = 0;
            for (int i = 0; (num3 < length) && (i < num2); i++)
            {
                int num5 = 0;
                if (char.IsDigit(str[num3]) && char.IsDigit(str[i]))
                {
                    string str5 = string.Empty;
                    while ((num3 < length) && char.IsDigit(str[num3]))
                    {
                        str5 = str5 + str[num3];
                        num3++;
                    }
                    string str6 = string.Empty;
                    while ((i < num2) && char.IsDigit(str3[i]))
                    {
                        str6 = str6 + str3[i];
                        i++;
                    }
                    int result = 0;
                    int.TryParse(str5, out result);
                    int num7 = 0;
                    int.TryParse(str6, out num7);
                    num5 = result.CompareTo(num7);
                }
                else
                {
                    num5 = str[num3].CompareTo(str3[i]);
                }
                if (num5 != 0)
                {
                    return num5;
                }
                num3++;
            }
            return (length - num2);
        }

        private string TypePrefix(Type t)
        {
            if (t.IsSubclassOf(typeof(BehaviorDesigner.Runtime.Tasks.Action)))
            {
                return "Action";
            }
            if (t.IsSubclassOf(typeof(Composite)))
            {
                return "Composite";
            }
            if (t.IsSubclassOf(typeof(Conditional)))
            {
                return "Conditional";
            }
            return "Decorator";
        }
    }
}

