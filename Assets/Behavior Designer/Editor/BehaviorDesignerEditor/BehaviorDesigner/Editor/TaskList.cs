namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public class TaskList : ScriptableObject
    {
        private List<CategoryList> mCategoryList;
        private bool mFocusSearch;
        private Vector2 mScrollPosition = Vector2.zero;
        private string mSearchString = string.Empty;

        private void AddCategoryTasksToMenu(ref GenericMenu genericMenu, List<CategoryList> categoryList, System.Type selectedTaskType, string parentName, GenericMenu.MenuFunction2 menuFunction)
        {
            for (int i = 0; i < categoryList.Count; i++)
            {
                if (categoryList[i].Subcategories != null)
                {
                    this.AddCategoryTasksToMenu(ref genericMenu, categoryList[i].Subcategories, selectedTaskType, parentName, menuFunction);
                }
                if (categoryList[i].Tasks != null)
                {
                    for (int j = 0; j < categoryList[i].Tasks.Count; j++)
                    {
                        if (parentName.Equals(string.Empty))
                        {
                            genericMenu.AddItem(new GUIContent(string.Format("{0}/{1}", categoryList[i].Fullpath, categoryList[i].Tasks[j].Name.ToString())), categoryList[i].Tasks[j].Type.Equals(selectedTaskType), menuFunction, categoryList[i].Tasks[j].Type);
                        }
                        else
                        {
                            genericMenu.AddItem(new GUIContent(string.Format("{0}/{1}/{2}", parentName, categoryList[i].Fullpath, categoryList[i].Tasks[j].Name.ToString())), categoryList[i].Tasks[j].Type.Equals(selectedTaskType), menuFunction, categoryList[i].Tasks[j].Type);
                        }
                    }
                }
            }
        }

        public void AddConditionalTasksToMenu(ref GenericMenu genericMenu, System.Type selectedTaskType, string parentName, GenericMenu.MenuFunction2 menuFunction)
        {
            if (this.mCategoryList[2].Tasks != null)
            {
                for (int i = 0; i < this.mCategoryList[2].Tasks.Count; i++)
                {
                    if (parentName.Equals(string.Empty))
                    {
                        genericMenu.AddItem(new GUIContent(string.Format("{0}/{1}", this.mCategoryList[2].Fullpath, this.mCategoryList[2].Tasks[i].Name.ToString())), this.mCategoryList[2].Tasks[i].Type.Equals(selectedTaskType), menuFunction, this.mCategoryList[2].Tasks[i].Type);
                    }
                    else
                    {
                        genericMenu.AddItem(new GUIContent(string.Format("{0}/{1}/{2}", parentName, this.mCategoryList[0x16].Fullpath, this.mCategoryList[2].Tasks[i].Name.ToString())), this.mCategoryList[2].Tasks[i].Type.Equals(selectedTaskType), menuFunction, this.mCategoryList[2].Tasks[i].Type);
                    }
                }
            }
            this.AddCategoryTasksToMenu(ref genericMenu, this.mCategoryList[2].Subcategories, selectedTaskType, parentName, menuFunction);
        }

        public void AddTasksToMenu(ref GenericMenu genericMenu, System.Type selectedTaskType, string parentName, GenericMenu.MenuFunction2 menuFunction)
        {
            this.AddCategoryTasksToMenu(ref genericMenu, this.mCategoryList, selectedTaskType, parentName, menuFunction);
        }

        private void DrawCategory(BehaviorDesignerWindow window, CategoryList category)
        {
            if (category.Visible)
            {
                category.Expanded = EditorGUILayout.Foldout(category.Expanded, category.Name, BehaviorDesignerUtility.TaskFoldoutGUIStyle);
                this.SetExpanded(category.ID, category.Expanded);
                if (category.Expanded)
                {
                    EditorGUI.indentLevel++;
                    if (category.Tasks != null)
                    {
                        for (int i = 0; i < category.Tasks.Count; i++)
                        {
                            if (category.Tasks[i].Visible)
                            {
                                string name;
                                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                                GUILayout.Space((float) (EditorGUI.indentLevel * 0x10));
                                TaskNameAttribute[] attributeArray = null;
                                if ((attributeArray = category.Tasks[i].Type.GetCustomAttributes(typeof(TaskNameAttribute), false) as TaskNameAttribute[]).Length > 0)
                                {
                                    name = attributeArray[0].Name;
                                }
                                else
                                {
                                    name = category.Tasks[i].Name;
                                }
                                GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.MaxWidth((float) ((300 - (EditorGUI.indentLevel * 0x10)) - 0x18)) };
                                if (GUILayout.Button(name, EditorStyles.toolbarButton, options))
                                {
                                    window.AddTask(category.Tasks[i].Type, false);
                                }
                                GUILayout.Space(3f);
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    if (category.Subcategories != null)
                    {
                        this.DrawCategoryTaskList(window, category.Subcategories);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawCategoryTaskList(BehaviorDesignerWindow window, List<CategoryList> categoryList)
        {
            for (int i = 0; i < categoryList.Count; i++)
            {
                this.DrawCategory(window, categoryList[i]);
            }
        }

        public void DrawTaskList(BehaviorDesignerWindow window, bool enabled)
        {
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUI.SetNextControlName("Search");
            string str = GUILayout.TextField(this.mSearchString, GUI.skin.FindStyle("ToolbarSeachTextField"), new GUILayoutOption[0]);
            if (this.mFocusSearch)
            {
                GUI.FocusControl("Search");
                this.mFocusSearch = false;
            }
            if (!this.mSearchString.Equals(str))
            {
                this.mSearchString = str;
                this.Search(BehaviorDesignerUtility.SplitCamelCase(this.mSearchString).ToLower().Replace(" ", string.Empty), this.mCategoryList);
            }
            if (GUILayout.Button(string.Empty, !this.mSearchString.Equals(string.Empty) ? GUI.skin.FindStyle("ToolbarSeachCancelButton") : GUI.skin.FindStyle("ToolbarSeachCancelButtonEmpty"), new GUILayoutOption[0]))
            {
                this.mSearchString = string.Empty;
                this.Search(string.Empty, this.mCategoryList);
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
            BehaviorDesignerUtility.DrawContentSeperator(2);
            GUILayout.Space(4f);
            this.mScrollPosition = GUILayout.BeginScrollView(this.mScrollPosition, new GUILayoutOption[0]);
            GUI.enabled = enabled;
            if (this.mCategoryList.Count > 1)
            {
                this.DrawCategory(window, this.mCategoryList[1]);
            }
            if (this.mCategoryList.Count > 3)
            {
                this.DrawCategory(window, this.mCategoryList[3]);
            }
            if (this.mCategoryList.Count > 0)
            {
                this.DrawCategory(window, this.mCategoryList[0]);
            }
            if (this.mCategoryList.Count > 2)
            {
                this.DrawCategory(window, this.mCategoryList[2]);
            }
            GUI.enabled = true;
            GUILayout.EndScrollView();
        }

        public void FocusSearchField()
        {
            this.mFocusSearch = true;
        }

        public void Init()
        {
            this.mCategoryList = new List<CategoryList>();
            List<System.Type> list = new List<System.Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                System.Type[] types = assemblies[i].GetTypes();
                for (int k = 0; k < types.Length; k++)
                {
                    if ((!types[k].Equals(typeof(BehaviorReference)) && !types[k].IsAbstract) && ((types[k].IsSubclassOf(typeof(BehaviorDesigner.Runtime.Tasks.Action)) || types[k].IsSubclassOf(typeof(Composite))) || (types[k].IsSubclassOf(typeof(Conditional)) || types[k].IsSubclassOf(typeof(Decorator)))))
                    {
                        list.Add(types[k]);
                    }
                }
            }
            list.Sort(new AlphanumComparator<System.Type>());
            Dictionary<string, CategoryList> dictionary = new Dictionary<string, CategoryList>();
            string str = string.Empty;
            TaskCategoryAttribute[] attributeArray = null;
            int id = 0;
            for (int j = 0; j < list.Count; j++)
            {
                if (list[j].IsSubclassOf(typeof(BehaviorDesigner.Runtime.Tasks.Action)))
                {
                    str = "Actions";
                }
                else if (list[j].IsSubclassOf(typeof(Composite)))
                {
                    str = "Composites";
                }
                else if (list[j].IsSubclassOf(typeof(Conditional)))
                {
                    str = "Conditionals";
                }
                else
                {
                    str = "Decorators";
                }
                if ((attributeArray = list[j].GetCustomAttributes(typeof(TaskCategoryAttribute), false) as TaskCategoryAttribute[]).Length > 0)
                {
                    str = str + "/" + attributeArray[0].Category;
                }
                string key = string.Empty;
                CategoryList item = null;
                char[] separator = new char[] { '/' };
                string[] strArray = str.Split(separator);
                CategoryList list3 = null;
                for (int m = 0; m < strArray.Length; m++)
                {
                    if (m > 0)
                    {
                        key = key + "/";
                    }
                    key = key + strArray[m];
                    if (!dictionary.ContainsKey(key))
                    {
                        item = new CategoryList(strArray[m], key, this.PreviouslyExpanded(id), id++);
                        if (list3 == null)
                        {
                            this.mCategoryList.Add(item);
                        }
                        else
                        {
                            list3.addSubcategory(item);
                        }
                        dictionary.Add(key, item);
                    }
                    else
                    {
                        item = dictionary[key];
                    }
                    list3 = item;
                }
                dictionary[key].addTask(list[j]);
            }
            this.Search(BehaviorDesignerUtility.SplitCamelCase(this.mSearchString).ToLower().Replace(" ", string.Empty), this.mCategoryList);
        }

        private void MarkVisible(List<CategoryList> categoryList)
        {
            for (int i = 0; i < categoryList.Count; i++)
            {
                categoryList[i].Visible = true;
                if (categoryList[i].Subcategories != null)
                {
                    this.MarkVisible(categoryList[i].Subcategories);
                }
                if (categoryList[i].Tasks != null)
                {
                    for (int j = 0; j < categoryList[i].Tasks.Count; j++)
                    {
                        categoryList[i].Tasks[j].Visible = true;
                    }
                }
            }
        }

        public void OnEnable()
        {
            base.hideFlags = HideFlags.HideAndDontSave;
        }

        private bool PreviouslyExpanded(int id)
        {
            return EditorPrefs.GetBool("BehaviorDesignerTaskList" + id, true);
        }

        private bool Search(string searchString, List<CategoryList> categoryList)
        {
            bool flag = searchString.Equals(string.Empty);
            for (int i = 0; i < categoryList.Count; i++)
            {
                bool flag2 = false;
                categoryList[i].Visible = false;
                if ((categoryList[i].Subcategories != null) && this.Search(searchString, categoryList[i].Subcategories))
                {
                    categoryList[i].Visible = true;
                    flag = true;
                }
                if (BehaviorDesignerUtility.SplitCamelCase(categoryList[i].Name).ToLower().Replace(" ", string.Empty).Contains(searchString))
                {
                    flag = true;
                    flag2 = true;
                    categoryList[i].Visible = true;
                    if (categoryList[i].Subcategories != null)
                    {
                        this.MarkVisible(categoryList[i].Subcategories);
                    }
                }
                if (categoryList[i].Tasks != null)
                {
                    for (int j = 0; j < categoryList[i].Tasks.Count; j++)
                    {
                        categoryList[i].Tasks[j].Visible = searchString.Equals(string.Empty);
                        if (flag2 || categoryList[i].Tasks[j].Name.ToLower().Replace(" ", string.Empty).Contains(searchString))
                        {
                            categoryList[i].Tasks[j].Visible = true;
                            flag = true;
                            categoryList[i].Visible = true;
                        }
                    }
                }
            }
            return flag;
        }

        private void SetExpanded(int id, bool visible)
        {
            EditorPrefs.SetBool("BehaviorDesignerTaskList" + id, visible);
        }

        private class CategoryList
        {
            private bool mExpanded = true;
            private string mFullpath = string.Empty;
            private int mID;
            private string mName = string.Empty;
            private List<TaskList.CategoryList> mSubcategories;
            private List<TaskList.SearchableType> mTasks;
            private bool mVisible = true;

            public CategoryList(string name, string fullpath, bool expanded, int id)
            {
                this.mName = name;
                this.mFullpath = fullpath;
                this.mExpanded = expanded;
                this.mID = id;
            }

            public void addSubcategory(TaskList.CategoryList category)
            {
                if (this.mSubcategories == null)
                {
                    this.mSubcategories = new List<TaskList.CategoryList>();
                }
                this.mSubcategories.Add(category);
            }

            public void addTask(System.Type taskType)
            {
                if (this.mTasks == null)
                {
                    this.mTasks = new List<TaskList.SearchableType>();
                }
                this.mTasks.Add(new TaskList.SearchableType(taskType));
            }

            public bool Expanded
            {
                get
                {
                    return this.mExpanded;
                }
                set
                {
                    this.mExpanded = value;
                }
            }

            public string Fullpath
            {
                get
                {
                    return this.mFullpath;
                }
            }

            public int ID
            {
                get
                {
                    return this.mID;
                }
            }

            public string Name
            {
                get
                {
                    return this.mName;
                }
            }

            public List<TaskList.CategoryList> Subcategories
            {
                get
                {
                    return this.mSubcategories;
                }
            }

            public List<TaskList.SearchableType> Tasks
            {
                get
                {
                    return this.mTasks;
                }
            }

            public bool Visible
            {
                get
                {
                    return this.mVisible;
                }
                set
                {
                    this.mVisible = value;
                }
            }
        }

        private class SearchableType
        {
            private string mName;
            private System.Type mType;
            private bool mVisible = true;

            public SearchableType(System.Type type)
            {
                this.mType = type;
                this.mName = BehaviorDesignerUtility.SplitCamelCase(this.mType.Name);
            }

            public string Name
            {
                get
                {
                    return this.mName;
                }
            }

            public System.Type Type
            {
                get
                {
                    return this.mType;
                }
            }

            public bool Visible
            {
                get
                {
                    return this.mVisible;
                }
                set
                {
                    this.mVisible = value;
                }
            }
        }

        public enum TaskTypes
        {
            Action,
            Composite,
            Conditional,
            Decorator,
            Last
        }
    }
}

