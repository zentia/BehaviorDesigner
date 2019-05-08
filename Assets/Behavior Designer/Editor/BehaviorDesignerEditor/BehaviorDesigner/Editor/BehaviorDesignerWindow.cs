namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using UnityEditor;
    using UnityEngine;

    public class BehaviorDesignerWindow : EditorWindow
    {
        [SerializeField]
        public static BehaviorDesignerWindow instance;
        private int mActiveBehaviorID = -1;
        private BehaviorSource mActiveBehaviorSource;
        [SerializeField]
        private UnityEngine.Object mActiveObject;
        private BehaviorManager mBehaviorManager;
        [SerializeField]
        private List<UnityEngine.Object> mBehaviorSourceHistory = new List<UnityEngine.Object>();
        [SerializeField]
        private int mBehaviorSourceHistoryIndex = -1;
        private int mBehaviorToolbarSelection = 1;
        private string[] mBehaviorToolbarStrings = new string[] { "Behavior", "Tasks", "Variables", "Inspector" };
        [SerializeField]
        private GenericMenu mBreadcrumbBehaviorMenu;
        [SerializeField]
        private GenericMenu mBreadcrumbGameObjectBehaviorMenu;
        [SerializeField]
        private GenericMenu mBreadcrumbGameObjectMenu;
        private bool mCommandDown;
        private List<TaskSerializer> mCopiedTasks;
        private Vector2 mCurrentMousePosition = Vector2.zero;
        private Rect mDebugToolBarRect;
        private Vector2 mDragDelta = Vector2.zero;
        private bool mEditorAtBreakpoint;
        [SerializeField]
        private List<BehaviorDesigner.Editor.ErrorDetails> mErrorDetails;
        private Rect mFileToolBarRect;
        [SerializeField]
        private GraphDesigner mGraphDesigner = ScriptableObject.CreateInstance<GraphDesigner>();
        private Vector2 mGraphOffset = Vector2.zero;
        private Rect mGraphRect;
        private Vector2 mGraphScrollPosition = new Vector2(-1f, -1f);
        private Rect mGraphScrollRect;
        private Vector2 mGraphScrollSize = new Vector2(20000f, 20000f);
        private string mGraphStatus = string.Empty;
        private float mGraphZoom = 1f;
        private Material mGridMaterial;
        private int mGUITickCount;
        private bool mIsDragging;
        [SerializeField]
        private bool mIsPlaying;
        private bool mIsSelecting;
        private bool mKeepTasksSelected;
        private DateTime mLastUpdateCheck = DateTime.MinValue;
        private string mLatestVersion;
        private bool mLoadedFromInspector;
        private bool mLockActiveGameObject;
        private bool mNodeClicked;
        private Dictionary<NodeDesigner, Task> mNodeDesignerTaskMap;
        private Rect mPreferencesPaneRect;
        private UnityEngine.Object mPrevActiveObject;
        private Vector2 mPrevEntryPosition;
        private float mPrevScreenHeight = -1f;
        private float mPrevScreenWidth = -1f;
        private bool mPropertiesPanelOnLeft = true;
        private Rect mPropertyBoxRect;
        private Rect mPropertyToolbarRect;
        [SerializeField]
        private GenericMenu mReferencedBehaviorsMenu;
        private GenericMenu mRightClickMenu;
        private Vector2 mScreenshotGraphOffset;
        private Rect mScreenshotGraphSize;
        private string mScreenshotPath;
        private Vector2 mScreenshotStartGraphOffset;
        private float mScreenshotStartGraphZoom;
        private Texture2D mScreenshotTexture;
        private Rect mSelectionArea;
        private Vector2 mSelectStartPosition = Vector2.zero;
        private bool mShowPrefPane;
        private bool mShowRightClickMenu;
        private bool mSizesInitialized;
        private bool mStepApplication;
        private bool mTakingScreenshot;
        private TaskInspector mTaskInspector;
        private TaskList mTaskList;
        private WWW mUpdateCheckRequest;
        private bool mUpdateNodeTaskMap;
        private VariableInspector mVariableInspector;

        private void AddBehavior()
        {
            if (!EditorApplication.isPlaying && (Selection.activeGameObject != null))
            {
                GameObject activeGameObject = Selection.activeGameObject;
                this.mActiveObject = Selection.activeObject;
                this.mGraphDesigner = ScriptableObject.CreateInstance<GraphDesigner>();
                System.Type typeWithinAssembly = System.Type.GetType("BehaviorDesigner.Runtime.BehaviorTree, Assembly-CSharp");
                if (typeWithinAssembly == null)
                {
                    typeWithinAssembly = System.Type.GetType("BehaviorDesigner.Runtime.BehaviorTree, Assembly-CSharp-firstpass");
                }
                Behavior behavior = BehaviorUndo.AddComponent(activeGameObject, typeWithinAssembly) as Behavior;
                Behavior[] components = activeGameObject.GetComponents<Behavior>();
                HashSet<string> set = new HashSet<string>();
                string item = string.Empty;
                for (int i = 0; i < components.Length; i++)
                {
                    item = components[i].GetBehaviorSource().behaviorName;
                    for (int j = 2; set.Contains(item); j++)
                    {
                        item = string.Format("{0} {1}", components[i].GetBehaviorSource().behaviorName, j);
                    }
                    components[i].GetBehaviorSource().behaviorName = item;
                    set.Add(components[i].GetBehaviorSource().behaviorName);
                }
                this.LoadBehavior(behavior.GetBehaviorSource(), false);
                base.Repaint();
                if (BehaviorDesignerPreferences.GetBool(BDPreferences.AddGameGUIComponent))
                {
                    typeWithinAssembly = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.BehaviorGameGUI");
                    BehaviorUndo.AddComponent(activeGameObject, typeWithinAssembly);
                }
            }
        }

        public NodeDesigner AddTask(System.Type type, bool useMousePosition)
        {
            if (((this.mActiveObject is GameObject) || (this.mActiveObject is ExternalBehavior)) && !EditorApplication.isPlaying)
            {
                Vector2 mousePosition = new Vector2(this.mGraphRect.width / (2f * this.mGraphZoom), 150f);
                if (useMousePosition)
                {
                    this.GetMousePositionInGraph(out mousePosition);
                }
                mousePosition -= this.mGraphOffset;
                GameObject mActiveObject = this.mActiveObject as GameObject;
                if ((mActiveObject != null) && (mActiveObject.GetComponent<Behavior>() == null))
                {
                    this.AddBehavior();
                }
                BehaviorUndo.RegisterUndo("Add", this.mActiveBehaviorSource.Owner.GetObject());
                NodeDesigner designer = this.mGraphDesigner.AddNode(this.mActiveBehaviorSource, type, mousePosition);
                if (designer != null)
                {
                    this.SaveBehavior();
                    return designer;
                }
            }
            return null;
        }

        private void AddTaskCallback(object obj)
        {
            this.AddTask((System.Type) obj, true);
        }

        private void BehaviorSelectionCallback(object obj)
        {
            BehaviorSource behaviorSource = obj as BehaviorSource;
            if (behaviorSource.Owner is Behavior)
            {
                this.mActiveObject = (behaviorSource.Owner as Behavior).gameObject;
            }
            else
            {
                this.mActiveObject = behaviorSource.Owner as ExternalBehavior;
            }
            Selection.activeObject = this.mActiveObject;
            this.LoadBehavior(behaviorSource, false);
            this.UpdateGraphStatus();
            if (EditorApplication.isPaused)
            {
                this.mUpdateNodeTaskMap = true;
                this.UpdateNodeTaskMap();
            }
        }

        private BehaviorSource BehaviorSourceFromIBehaviorHistory(IBehavior behavior)
        {
            if (behavior != null)
            {
                if (!(behavior.GetObject() is GameObject))
                {
                    return behavior.GetBehaviorSource();
                }
                Behavior[] components = (behavior.GetObject() as GameObject).GetComponents<Behavior>();
                for (int i = 0; i < components.Count<Behavior>(); i++)
                {
                    if (components[i].GetBehaviorSource().BehaviorID == behavior.GetBehaviorSource().BehaviorID)
                    {
                        return components[i].GetBehaviorSource();
                    }
                }
            }
            return null;
        }

        private void BuildBreadcrumbMenus(BreadcrumbMenuType menuType)
        {
            Dictionary<BehaviorSource, string> dictionary = new Dictionary<BehaviorSource, string>();
            Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
            HashSet<UnityEngine.Object> set = new HashSet<UnityEngine.Object>();
            List<BehaviorSource> list = new List<BehaviorSource>();
            Behavior[] behaviorArray = Resources.FindObjectsOfTypeAll(typeof(Behavior)) as Behavior[];
            for (int i = behaviorArray.Length - 1; i > -1; i--)
            {
                BehaviorSource behaviorSource = behaviorArray[i].GetBehaviorSource();
                if (behaviorSource.Owner == null)
                {
                    behaviorSource.Owner = behaviorArray[i];
                }
                list.Add(behaviorSource);
            }
            ExternalBehavior[] behaviorArray2 = Resources.FindObjectsOfTypeAll(typeof(ExternalBehavior)) as ExternalBehavior[];
            for (int j = behaviorArray2.Length - 1; j > -1; j--)
            {
                BehaviorSource item = behaviorArray2[j].GetBehaviorSource();
                if (item.Owner == null)
                {
                    item.Owner = behaviorArray2[j];
                }
                list.Add(item);
            }
            list.Sort(new AlphanumComparator<BehaviorSource>());
            for (int k = 0; k < list.Count; k++)
            {
                UnityEngine.Object assetObject = list[k].Owner.GetObject();
                if (menuType == BreadcrumbMenuType.Behavior)
                {
                    if (assetObject is Behavior)
                    {
                        if ((assetObject as Behavior).gameObject.Equals(this.mActiveObject))
                        {
                            goto Label_014E;
                        }
                        continue;
                    }
                    if (!(assetObject as ExternalBehavior).Equals(this.mActiveObject))
                    {
                        continue;
                    }
                }
            Label_014E:
                if ((menuType == BreadcrumbMenuType.GameObject) && (assetObject is Behavior))
                {
                    if (set.Contains((assetObject as Behavior).gameObject))
                    {
                        continue;
                    }
                    set.Add((assetObject as Behavior).gameObject);
                }
                string key = string.Empty;
                if (assetObject is Behavior)
                {
                    switch (menuType)
                    {
                        case BreadcrumbMenuType.GameObjectBehavior:
                            key = list[k].ToString();
                            break;

                        case BreadcrumbMenuType.GameObject:
                            key = (assetObject as Behavior).gameObject.name;
                            break;

                        case BreadcrumbMenuType.Behavior:
                            key = list[k].behaviorName;
                            break;
                    }
                    if (!AssetDatabase.GetAssetPath(assetObject).Equals(string.Empty))
                    {
                        key = key + " (prefab)";
                    }
                }
                else
                {
                    key = list[k].ToString() + " (external)";
                }
                int num4 = 0;
                if (dictionary2.TryGetValue(key, out num4))
                {
                    dictionary2[key] = ++num4;
                    key = key + string.Format(" ({0})", num4 + 1);
                }
                else
                {
                    dictionary2.Add(key, 0);
                }
                dictionary.Add(list[k], key);
            }
            switch (menuType)
            {
                case BreadcrumbMenuType.GameObjectBehavior:
                    this.mBreadcrumbGameObjectBehaviorMenu = new GenericMenu();
                    break;

                case BreadcrumbMenuType.GameObject:
                    this.mBreadcrumbGameObjectMenu = new GenericMenu();
                    break;

                case BreadcrumbMenuType.Behavior:
                    this.mBreadcrumbBehaviorMenu = new GenericMenu();
                    break;
            }
            foreach (KeyValuePair<BehaviorSource, string> pair in dictionary)
            {
                bool flag;
                switch (menuType)
                {
                    case BreadcrumbMenuType.GameObjectBehavior:
                    {
                        this.mBreadcrumbGameObjectBehaviorMenu.AddItem(new GUIContent(pair.Value), pair.Key.Equals(this.mActiveBehaviorSource), new GenericMenu.MenuFunction2(this.BehaviorSelectionCallback), pair.Key);
                        continue;
                    }
                    case BreadcrumbMenuType.GameObject:
                        flag = false;
                        if (!(pair.Key.Owner.GetObject() is ExternalBehavior))
                        {
                            break;
                        }
                        flag = (pair.Key.Owner.GetObject() as ExternalBehavior).GetObject().Equals(this.mActiveObject);
                        goto Label_03DE;

                    case BreadcrumbMenuType.Behavior:
                    {
                        this.mBreadcrumbBehaviorMenu.AddItem(new GUIContent(pair.Value), pair.Key.Equals(this.mActiveBehaviorSource), new GenericMenu.MenuFunction2(this.BehaviorSelectionCallback), pair.Key);
                        continue;
                    }
                    default:
                    {
                        continue;
                    }
                }
                flag = (pair.Key.Owner.GetObject() as Behavior).gameObject.Equals(this.mActiveObject);
            Label_03DE:
                this.mBreadcrumbGameObjectMenu.AddItem(new GUIContent(pair.Value), flag, new GenericMenu.MenuFunction2(this.BehaviorSelectionCallback), pair.Key);
            }
        }

        private void BuildRightClickMenu(NodeDesigner clickedNode)
        {
            if (this.mActiveObject != null)
            {
                this.mRightClickMenu = new GenericMenu();
                if (((clickedNode == null) && !EditorApplication.isPlaying) && !this.ViewOnlyMode(true))
                {
                    this.mTaskList.AddTasksToMenu(ref this.mRightClickMenu, null, "Add Task", new GenericMenu.MenuFunction2(this.AddTaskCallback));
                    if ((this.mCopiedTasks != null) && (this.mCopiedTasks.Count > 0))
                    {
                        this.mRightClickMenu.AddItem(new GUIContent("Paste Tasks"), false, new GenericMenu.MenuFunction(this.PasteNodes));
                    }
                    else
                    {
                        this.mRightClickMenu.AddDisabledItem(new GUIContent("Paste Tasks"));
                    }
                }
                if ((clickedNode != null) && !clickedNode.IsEntryDisplay)
                {
                    if (this.mGraphDesigner.SelectedNodes.Count == 1)
                    {
                        this.mRightClickMenu.AddItem(new GUIContent("Edit Script"), false, new GenericMenu.MenuFunction2(this.OpenInFileEditor), clickedNode);
                        this.mRightClickMenu.AddItem(new GUIContent("Locate Script"), false, new GenericMenu.MenuFunction2(this.SelectInProject), clickedNode);
                        if (!this.ViewOnlyMode(true))
                        {
                            this.mRightClickMenu.AddItem(new GUIContent(!clickedNode.Task.NodeData.Disabled ? "Disable" : "Enable"), false, new GenericMenu.MenuFunction2(this.ToggleEnableState), clickedNode);
                            if (clickedNode.IsParent)
                            {
                                this.mRightClickMenu.AddItem(new GUIContent(!clickedNode.Task.NodeData.Collapsed ? "Collapse" : "Expand"), false, new GenericMenu.MenuFunction2(this.ToggleCollapseState), clickedNode);
                            }
                            this.mRightClickMenu.AddItem(new GUIContent(!clickedNode.Task.NodeData.IsBreakpoint ? "Set Breakpoint" : "Remove Breakpoint"), false, new GenericMenu.MenuFunction2(this.ToggleBreakpoint), clickedNode);
                            this.mTaskList.AddTasksToMenu(ref this.mRightClickMenu, this.mGraphDesigner.SelectedNodes[0].Task.GetType(), "Replace", new GenericMenu.MenuFunction2(this.ReplaceTaskCallback));
                        }
                    }
                    if (!EditorApplication.isPlaying && !this.ViewOnlyMode(true))
                    {
                        this.mRightClickMenu.AddItem(new GUIContent(string.Format("Copy Task{0}", (this.mGraphDesigner.SelectedNodes.Count <= 1) ? string.Empty : "s")), false, new GenericMenu.MenuFunction(this.CopyNodes));
                        if ((this.mCopiedTasks != null) && (this.mCopiedTasks.Count > 0))
                        {
                            this.mRightClickMenu.AddItem(new GUIContent(string.Format("Paste Task{0}", (this.mCopiedTasks.Count <= 1) ? string.Empty : "s")), false, new GenericMenu.MenuFunction(this.PasteNodes));
                        }
                        else
                        {
                            this.mRightClickMenu.AddDisabledItem(new GUIContent("Paste Tasks"));
                        }
                        this.mRightClickMenu.AddItem(new GUIContent(string.Format("Delete Task{0}", (this.mGraphDesigner.SelectedNodes.Count <= 1) ? string.Empty : "s")), false, new GenericMenu.MenuFunction(this.DeleteNodes));
                    }
                }
                if (!EditorApplication.isPlaying && (this.mActiveObject is GameObject))
                {
                    if ((clickedNode != null) && !clickedNode.IsEntryDisplay)
                    {
                        this.mRightClickMenu.AddSeparator(string.Empty);
                    }
                    this.mRightClickMenu.AddItem(new GUIContent("Add Behavior Tree"), false, new GenericMenu.MenuFunction(this.AddBehavior));
                    if (this.mActiveBehaviorSource != null)
                    {
                        this.mRightClickMenu.AddItem(new GUIContent("Remove Behavior Tree"), false, new GenericMenu.MenuFunction(this.RemoveBehavior));
                        this.mRightClickMenu.AddItem(new GUIContent("Save As External Behavior Tree"), false, new GenericMenu.MenuFunction(this.SaveAsAsset));
                    }
                }
            }
        }

        private bool CheckForAutoScroll()
        {
            Vector2 vector;
            if (!this.GetMousePositionInGraph(out vector))
            {
                return false;
            }
            if (this.mGraphScrollRect.Contains(this.mCurrentMousePosition))
            {
                return false;
            }
            if ((!this.mIsDragging && !this.mIsSelecting) && (this.mGraphDesigner.ActiveNodeConnection == null))
            {
                return false;
            }
            Vector2 zero = Vector2.zero;
            if (this.mCurrentMousePosition.y < (this.mGraphScrollRect.yMin + 15f))
            {
                zero.y = 3f;
            }
            else if (this.mCurrentMousePosition.y > (this.mGraphScrollRect.yMax - 15f))
            {
                zero.y = -3f;
            }
            if (this.mCurrentMousePosition.x < (this.mGraphScrollRect.xMin + 15f))
            {
                zero.x = 3f;
            }
            else if (this.mCurrentMousePosition.x > (this.mGraphScrollRect.xMax - 15f))
            {
                zero.x = -3f;
            }
            this.ScrollGraph(zero);
            if (this.mIsDragging)
            {
                this.mGraphDesigner.DragSelectedNodes((Vector2) (-zero / this.mGraphZoom), Event.current.modifiers != EventModifiers.Alt);
            }
            if (this.mIsSelecting)
            {
                this.mSelectStartPosition += (Vector2) (zero / this.mGraphZoom);
            }
            return true;
        }

        private void CheckForErrors()
        {
            if (this.mErrorDetails != null)
            {
                for (int i = 0; i < this.mErrorDetails.Count; i++)
                {
                    if (this.mErrorDetails[i].NodeDesigner != null)
                    {
                        this.mErrorDetails[i].NodeDesigner.HasError = false;
                    }
                }
            }
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.ErrorChecking))
            {
                this.mErrorDetails = ErrorCheck.CheckForErrors(this.mActiveBehaviorSource);
                if (this.mErrorDetails != null)
                {
                    for (int j = 0; j < this.mErrorDetails.Count; j++)
                    {
                        this.mErrorDetails[j].NodeDesigner.HasError = true;
                    }
                }
            }
            else
            {
                this.mErrorDetails = null;
            }
            if (ErrorWindow.instance != null)
            {
                ErrorWindow.instance.ErrorDetails = this.mErrorDetails;
                ErrorWindow.instance.Repaint();
            }
        }

        private void ClearBreadcrumbMenu()
        {
            this.mBreadcrumbGameObjectBehaviorMenu = null;
            this.mBreadcrumbGameObjectMenu = null;
            this.mBreadcrumbBehaviorMenu = null;
        }

        public void ClearGraph()
        {
            this.mGraphDesigner.Clear(true);
            this.mActiveBehaviorSource = null;
            this.CheckForErrors();
            this.UpdateGraphStatus();
            base.Repaint();
        }

        private void CopyNodes()
        {
            this.mCopiedTasks = this.mGraphDesigner.Copy(this.mGraphOffset, this.mGraphZoom);
        }

        private void CutNodes()
        {
            this.mCopiedTasks = this.mGraphDesigner.Copy(this.mGraphOffset, this.mGraphZoom);
            if ((this.mCopiedTasks != null) && (this.mCopiedTasks.Count > 0))
            {
                BehaviorUndo.RegisterUndo("Cut", this.mActiveBehaviorSource.Owner.GetObject());
            }
            this.mGraphDesigner.Delete(this.mActiveBehaviorSource);
            this.SaveBehavior();
        }

        private void DeleteNodes()
        {
            if (!this.ViewOnlyMode(true))
            {
                this.mGraphDesigner.Delete(this.mActiveBehaviorSource);
                this.SaveBehavior();
            }
        }

        private void DisableReferenceTasks()
        {
            if (this.IsReferencingTasks())
            {
                this.ToggleReferenceTasks();
            }
        }

        private bool Draw()
        {
            bool flag = false;
            Color color = GUI.color;
            Color backgroundColor = GUI.backgroundColor;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            this.DrawFileToolbar();
            this.DrawDebugToolbar();
            this.DrawPropertiesBox();
            if (this.DrawGraphArea())
            {
                flag = true;
            }
            this.DrawPreferencesPane();
            if (this.mTakingScreenshot)
            {
                GUI.DrawTexture(new Rect(0f, 0f, (float) Screen.width, (float) Screen.height), BehaviorDesignerUtility.ScreenshotBackgroundTexture, ScaleMode.StretchToFill, false);
            }
            GUI.color = color;
            GUI.backgroundColor = backgroundColor;
            return flag;
        }

        private void DrawDebugToolbar()
        {
            GUILayout.BeginArea(this.mDebugToolBarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(40f) };
            if (GUILayout.Button(BehaviorDesignerUtility.PlayTexture, !EditorApplication.isPlaying ? EditorStyles.toolbarButton : BehaviorDesignerUtility.ToolbarButtonSelectionGUIStyle, options))
            {
                EditorApplication.isPlaying = !EditorApplication.isPlaying;
            }
            GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(40f) };
            if (GUILayout.Button(BehaviorDesignerUtility.PauseTexture, !EditorApplication.isPaused ? EditorStyles.toolbarButton : BehaviorDesignerUtility.ToolbarButtonSelectionGUIStyle, optionArray2))
            {
                EditorApplication.isPaused = !EditorApplication.isPaused;
            }
            GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(40f) };
            if (GUILayout.Button(BehaviorDesignerUtility.StepTexture, EditorStyles.toolbarButton, optionArray3) && EditorApplication.isPlaying)
            {
                this.mStepApplication = true;
            }
            if ((this.mErrorDetails != null) && (this.mErrorDetails.Count > 0))
            {
                GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(85f) };
                if (GUILayout.Button(new GUIContent(this.mErrorDetails.Count + " Error" + ((this.mErrorDetails.Count <= 1) ? string.Empty : "s"), BehaviorDesignerUtility.SmallErrorIconTexture), BehaviorDesignerUtility.ToolbarButtonLeftAlignGUIStyle, optionArray4))
                {
                    ErrorWindow.ShowWindow();
                }
            }
            GUILayout.FlexibleSpace();
            if ("1.5.5".ToString().CompareTo(this.LatestVersion) < 0)
            {
                GUILayout.Label("Behavior Designer " + this.LatestVersion + " is now available.", BehaviorDesignerUtility.ToolbarLabelGUIStyle, new GUILayoutOption[0]);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawFileToolbar()
        {
            GUILayout.BeginArea(this.mFileToolBarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            if (GUILayout.Button(BehaviorDesignerUtility.HistoryBackwardTexture, EditorStyles.toolbarButton, new GUILayoutOption[0]) && ((this.mBehaviorSourceHistoryIndex > 0) || ((this.mActiveBehaviorSource == null) && (this.mBehaviorSourceHistoryIndex == 0))))
            {
                BehaviorSource behaviorSource = null;
                if (this.mActiveBehaviorSource == null)
                {
                    this.mBehaviorSourceHistoryIndex++;
                }
                while (((behaviorSource == null) && (this.mBehaviorSourceHistory.Count > 0)) && (this.mBehaviorSourceHistoryIndex > 0))
                {
                    this.mBehaviorSourceHistoryIndex--;
                    behaviorSource = this.BehaviorSourceFromIBehaviorHistory(this.mBehaviorSourceHistory[this.mBehaviorSourceHistoryIndex] as IBehavior);
                    if (((behaviorSource == null) || (behaviorSource.Owner == null)) || (behaviorSource.Owner.GetObject() == null))
                    {
                        this.mBehaviorSourceHistory.RemoveAt(this.mBehaviorSourceHistoryIndex);
                        behaviorSource = null;
                    }
                }
                if (behaviorSource != null)
                {
                    this.LoadBehavior(behaviorSource, false);
                }
            }
            if (GUILayout.Button(BehaviorDesignerUtility.HistoryForwardTexture, EditorStyles.toolbarButton, new GUILayoutOption[0]))
            {
                BehaviorSource source2 = null;
                if (this.mBehaviorSourceHistoryIndex < (this.mBehaviorSourceHistory.Count - 1))
                {
                    this.mBehaviorSourceHistoryIndex++;
                    while (((source2 == null) && (this.mBehaviorSourceHistoryIndex < this.mBehaviorSourceHistory.Count)) && (this.mBehaviorSourceHistoryIndex > 0))
                    {
                        source2 = this.BehaviorSourceFromIBehaviorHistory(this.mBehaviorSourceHistory[this.mBehaviorSourceHistoryIndex] as IBehavior);
                        if (((source2 == null) || (source2.Owner == null)) || (source2.Owner.GetObject() == null))
                        {
                            this.mBehaviorSourceHistory.RemoveAt(this.mBehaviorSourceHistoryIndex);
                            source2 = null;
                        }
                    }
                }
                if (source2 != null)
                {
                    this.LoadBehavior(source2, false);
                }
            }
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(22f) };
            if (GUILayout.Button("...", EditorStyles.toolbarButton, options))
            {
                this.BuildBreadcrumbMenus(BreadcrumbMenuType.GameObjectBehavior);
                this.mBreadcrumbGameObjectBehaviorMenu.ShowAsContext();
            }
            string text = (!(this.mActiveObject is GameObject) && !(this.mActiveObject is ExternalBehavior)) ? "(None Selected)" : this.mActiveObject.name;
            GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(140f) };
            if (GUILayout.Button(text, EditorStyles.toolbarPopup, optionArray2))
            {
                this.BuildBreadcrumbMenus(BreadcrumbMenuType.GameObject);
                this.mBreadcrumbGameObjectMenu.ShowAsContext();
            }
            string str2 = (this.mActiveBehaviorSource == null) ? "(None Selected)" : this.mActiveBehaviorSource.behaviorName;
            GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(140f) };
            if (GUILayout.Button(str2, EditorStyles.toolbarPopup, optionArray3) && (this.mActiveBehaviorSource != null))
            {
                this.BuildBreadcrumbMenus(BreadcrumbMenuType.Behavior);
                this.mBreadcrumbBehaviorMenu.ShowAsContext();
            }
            GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(140f) };
            if (GUILayout.Button("Referenced Behaviors", EditorStyles.toolbarPopup, optionArray4) && (this.mActiveBehaviorSource != null))
            {
                List<BehaviorSource> list = this.mGraphDesigner.FindReferencedBehaviors();
                if (list.Count > 0)
                {
                    list.Sort(new AlphanumComparator<BehaviorSource>());
                    this.mReferencedBehaviorsMenu = new GenericMenu();
                    for (int i = 0; i < list.Count; i++)
                    {
                        this.mReferencedBehaviorsMenu.AddItem(new GUIContent(list[i].ToString()), false, new GenericMenu.MenuFunction2(this.BehaviorSelectionCallback), list[i]);
                    }
                    this.mReferencedBehaviorsMenu.ShowAsContext();
                }
            }
            GUILayoutOption[] optionArray5 = new GUILayoutOption[] { GUILayout.Width(22f) };
            if (GUILayout.Button("-", EditorStyles.toolbarButton, optionArray5))
            {
                if (this.mActiveBehaviorSource != null)
                {
                    this.RemoveBehavior();
                }
                else
                {
                    EditorUtility.DisplayDialog("Unable to Remove Behavior Tree", "No behavior tree selected.", "OK");
                }
            }
            GUILayoutOption[] optionArray6 = new GUILayoutOption[] { GUILayout.Width(22f) };
            if (GUILayout.Button("+", EditorStyles.toolbarButton, optionArray6))
            {
                if (this.mActiveObject != null)
                {
                    this.AddBehavior();
                }
                else
                {
                    EditorUtility.DisplayDialog("Unable to Add Behavior Tree", "No GameObject is selected.", "OK");
                }
            }
            GUILayoutOption[] optionArray7 = new GUILayoutOption[] { GUILayout.Width(42f) };
            if (GUILayout.Button("Lock", !this.mLockActiveGameObject ? EditorStyles.toolbarButton : BehaviorDesignerUtility.ToolbarButtonSelectionGUIStyle, optionArray7))
            {
                if (this.mActiveObject != null)
                {
                    this.mLockActiveGameObject = !this.mLockActiveGameObject;
                    if (!this.mLockActiveGameObject)
                    {
                        this.UpdateTree(false);
                    }
                }
                else if (this.mLockActiveGameObject)
                {
                    this.mLockActiveGameObject = false;
                }
                else
                {
                    EditorUtility.DisplayDialog("Unable to Lock GameObject", "No GameObject is selected.", "OK");
                }
            }
            GUILayoutOption[] optionArray8 = new GUILayoutOption[] { GUILayout.Width(46f) };
            if (GUILayout.Button("Export", EditorStyles.toolbarButton, optionArray8))
            {
                if (this.mActiveBehaviorSource != null)
                {
                    if (this.mActiveBehaviorSource.Owner.GetObject() is Behavior)
                    {
                        this.SaveAsAsset();
                    }
                    else
                    {
                        this.SaveAsPrefab();
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Unable to Save Behavior Tree", "Select a behavior tree from within the scene.", "OK");
                }
            }
            GUILayoutOption[] optionArray9 = new GUILayoutOption[] { GUILayout.Width(96f) };
            if (GUILayout.Button("Take Screenshot", EditorStyles.toolbarButton, optionArray9))
            {
                if (this.mActiveBehaviorSource != null)
                {
                    this.TakeScreenshot();
                }
                else
                {
                    EditorUtility.DisplayDialog("Unable to Take Screenshot", "Select a behavior tree from within the scene.", "OK");
                }
            }
            GUILayout.FlexibleSpace();
            GUILayoutOption[] optionArray10 = new GUILayoutOption[] { GUILayout.Width(80f) };
            if (GUILayout.Button("Preferences", !this.mShowPrefPane ? EditorStyles.toolbarButton : BehaviorDesignerUtility.ToolbarButtonSelectionGUIStyle, optionArray10))
            {
                this.mShowPrefPane = !this.mShowPrefPane;
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private bool DrawGraphArea()
        {
            Vector2 vector2;
            if ((Event.current.type != EventType.ScrollWheel) && !this.mTakingScreenshot)
            {
                Vector2 vector = GUI.BeginScrollView(new Rect(this.mGraphRect.x, this.mGraphRect.y, this.mGraphRect.width + 15f, this.mGraphRect.height + 15f), this.mGraphScrollPosition, new Rect(0f, 0f, this.mGraphScrollSize.x, this.mGraphScrollSize.y), true, true);
                if (((vector != this.mGraphScrollPosition) && (Event.current.type != EventType.DragUpdated)) && (Event.current.type != EventType.Ignore))
                {
                    this.mGraphOffset -= (Vector2) ((vector - this.mGraphScrollPosition) / this.mGraphZoom);
                    this.mGraphScrollPosition = vector;
                    this.mGraphDesigner.GraphDirty();
                }
                GUI.EndScrollView();
            }
            GUI.Box(this.mGraphRect, string.Empty, BehaviorDesignerUtility.GraphBackgroundGUIStyle);
            this.DrawGrid();
            EditorZoomArea.Begin(this.mGraphRect, this.mGraphZoom);
            if (!this.GetMousePositionInGraph(out vector2))
            {
                vector2 = new Vector2(-1f, -1f);
            }
            bool flag = false;
            if ((this.mGraphDesigner != null) && this.mGraphDesigner.DrawNodes(vector2, this.mGraphOffset))
            {
                flag = true;
            }
            if (this.mTakingScreenshot && (Event.current.type == EventType.Repaint))
            {
                this.RenderScreenshotTile();
            }
            if (this.mIsSelecting)
            {
                GUI.Box(this.GetSelectionArea(), string.Empty, BehaviorDesignerUtility.SelectionGUIStyle);
            }
            EditorZoomArea.End();
            this.DrawGraphStatus();
            this.DrawSelectedTaskDescription();
            return flag;
        }

        private void DrawGraphStatus()
        {
            if (!this.mGraphStatus.Equals(string.Empty))
            {
                GUI.Label(new Rect(this.mGraphRect.x + 5f, this.mGraphRect.y + 5f, this.mGraphRect.width, 30f), this.mGraphStatus, BehaviorDesignerUtility.GraphStatusGUIStyle);
            }
        }

        private void DrawGrid()
        {
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.SnapToGrid))
            {
                this.mGridMaterial.SetPass(!EditorGUIUtility.isProSkin ? 1 : 0);
                GL.PushMatrix();
                GL.Begin(1);
                this.DrawGridLines(10f * this.mGraphZoom, new Vector2((this.mGraphOffset.x % 10f) * this.mGraphZoom, (this.mGraphOffset.y % 10f) * this.mGraphZoom));
                GL.End();
                GL.PopMatrix();
                this.mGridMaterial.SetPass(!EditorGUIUtility.isProSkin ? 3 : 2);
                GL.PushMatrix();
                GL.Begin(1);
                this.DrawGridLines(50f * this.mGraphZoom, new Vector2((this.mGraphOffset.x % 50f) * this.mGraphZoom, (this.mGraphOffset.y % 50f) * this.mGraphZoom));
                GL.End();
                GL.PopMatrix();
            }
        }

        private void DrawGridLines(float gridSize, Vector2 offset)
        {
            float num = this.mGraphRect.x + offset.x;
            if (offset.x < 0f)
            {
                num += gridSize;
            }
            for (float i = num; i < (this.mGraphRect.x + this.mGraphRect.width); i += gridSize)
            {
                this.DrawLine(new Vector2(i, this.mGraphRect.y), new Vector2(i, this.mGraphRect.y + this.mGraphRect.height));
            }
            float num3 = this.mGraphRect.y + offset.y;
            if (offset.y < 0f)
            {
                num3 += gridSize;
            }
            for (float j = num3; j < (this.mGraphRect.y + this.mGraphRect.height); j += gridSize)
            {
                this.DrawLine(new Vector2(this.mGraphRect.x, j), new Vector2(this.mGraphRect.x + this.mGraphRect.width, j));
            }
        }

        private void DrawLine(Vector2 p1, Vector2 p2)
        {
            GL.Vertex((Vector3) p1);
            GL.Vertex((Vector3) p2);
        }

        private void DrawPreferencesPane()
        {
            if (this.mShowPrefPane)
            {
                GUILayout.BeginArea(this.mPreferencesPaneRect, BehaviorDesignerUtility.PreferencesPaneGUIStyle);
                BehaviorDesignerPreferences.DrawPreferencesPane(new PreferenceChangeHandler(this.OnPreferenceChange));
                GUILayout.EndArea();
            }
        }

        private void DrawPropertiesBox()
        {
            GUILayout.BeginArea(this.mPropertyToolbarRect, EditorStyles.toolbar);
            int mBehaviorToolbarSelection = this.mBehaviorToolbarSelection;
            this.mBehaviorToolbarSelection = GUILayout.Toolbar(this.mBehaviorToolbarSelection, this.mBehaviorToolbarStrings, EditorStyles.toolbarButton, new GUILayoutOption[0]);
            GUILayout.EndArea();
            GUILayout.BeginArea(this.mPropertyBoxRect, BehaviorDesignerUtility.PropertyBoxGUIStyle);
            if (this.mBehaviorToolbarSelection == 0)
            {
                if (this.mActiveBehaviorSource != null)
                {
                    GUILayout.Space(3f);
                    if (this.mActiveBehaviorSource.Owner is Behavior)
                    {
                        bool externalModification = false;
                        bool showOptions = false;
                        if (BehaviorInspector.DrawInspectorGUI(this.mActiveBehaviorSource.Owner as Behavior, new SerializedObject(this.mActiveBehaviorSource.Owner as Behavior), false, ref externalModification, ref showOptions, ref showOptions))
                        {
                            EditorUtility.SetDirty(this.mActiveBehaviorSource.Owner.GetObject());
                            if (externalModification)
                            {
                                this.LoadBehavior(this.mActiveBehaviorSource, false, false);
                            }
                        }
                    }
                    else
                    {
                        bool showVariables = false;
                        ExternalBehaviorInspector.DrawInspectorGUI(this.mActiveBehaviorSource, false, ref showVariables);
                    }
                }
                else
                {
                    GUILayout.Space(5f);
                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(285f) };
                    GUILayout.Label("No behavior tree selected. Create a new behavior tree or select one from the hierarchy.", BehaviorDesignerUtility.LabelWrapGUIStyle, options);
                }
            }
            else if (this.mBehaviorToolbarSelection == 1)
            {
                this.mTaskList.DrawTaskList(this, !this.ViewOnlyMode(true) && !EditorApplication.isCompiling);
                if (mBehaviorToolbarSelection != 1)
                {
                    this.mTaskList.FocusSearchField();
                }
            }
            else if (this.mBehaviorToolbarSelection == 2)
            {
                if (this.mActiveBehaviorSource != null)
                {
                    if (this.mVariableInspector.DrawVariables(this.mActiveBehaviorSource, !EditorApplication.isCompiling))
                    {
                        this.SaveBehavior();
                    }
                    if (mBehaviorToolbarSelection != 2)
                    {
                        this.mVariableInspector.FocusNameField();
                    }
                }
                else
                {
                    GUILayout.Space(5f);
                    GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(285f) };
                    GUILayout.Label("No behavior tree selected. Create a new behavior tree or select one from the hierarchy.", BehaviorDesignerUtility.LabelWrapGUIStyle, optionArray2);
                }
            }
            else if (this.mBehaviorToolbarSelection == 3)
            {
                if ((this.mGraphDesigner.SelectedNodes.Count == 1) && !this.mGraphDesigner.SelectedNodes[0].IsEntryDisplay)
                {
                    Task task = this.mGraphDesigner.SelectedNodes[0].Task;
                    if ((this.mNodeDesignerTaskMap != null) && (this.mNodeDesignerTaskMap.Count > 0))
                    {
                        NodeDesigner nodeDesigner = this.mGraphDesigner.SelectedNodes[0].Task.NodeData.NodeDesigner as NodeDesigner;
                        if ((nodeDesigner != null) && this.mNodeDesignerTaskMap.ContainsKey(nodeDesigner))
                        {
                            task = this.mNodeDesignerTaskMap[nodeDesigner];
                        }
                    }
                    if (this.mTaskInspector.DrawTaskInspector(this.mActiveBehaviorSource, this.mTaskList, task, !this.ViewOnlyMode(true) && !EditorApplication.isCompiling) && !Application.isPlaying)
                    {
                        this.SaveBehavior();
                    }
                }
                else
                {
                    GUILayout.Space(5f);
                    if (this.mGraphDesigner.SelectedNodes.Count > 1)
                    {
                        GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(285f) };
                        GUILayout.Label("Only one task can be selected at a time to\n view its properties.", BehaviorDesignerUtility.LabelWrapGUIStyle, optionArray3);
                    }
                    else
                    {
                        GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(285f) };
                        GUILayout.Label("Select a task from the tree to\nview its properties.", BehaviorDesignerUtility.LabelWrapGUIStyle, optionArray4);
                    }
                }
            }
            GUILayout.EndArea();
        }

        private void DrawSelectedTaskDescription()
        {
            TaskDescriptionAttribute[] attributeArray;
            if ((BehaviorDesignerPreferences.GetBool(BDPreferences.ShowTaskDescription) && (this.mGraphDesigner.SelectedNodes.Count == 1)) && ((attributeArray = this.mGraphDesigner.SelectedNodes[0].Task.GetType().GetCustomAttributes(typeof(TaskDescriptionAttribute), false) as TaskDescriptionAttribute[]).Length > 0))
            {
                float num;
                float num2;
                BehaviorDesignerUtility.TaskCommentGUIStyle.CalcMinMaxWidth(new GUIContent(attributeArray[0].Description), out num, out num2);
                float width = Mathf.Min((float) 400f, (float) (num2 + 20f));
                float height = Mathf.Min(300f, BehaviorDesignerUtility.TaskCommentGUIStyle.CalcHeight(new GUIContent(attributeArray[0].Description), width)) + 3f;
                GUI.Box(new Rect(this.mGraphRect.x + 5f, (this.mGraphRect.yMax - height) - 5f, width, height), string.Empty, BehaviorDesignerUtility.TaskDescriptionGUIStyle);
                GUI.Box(new Rect(this.mGraphRect.x + 2f, (this.mGraphRect.yMax - height) - 5f, width, height), attributeArray[0].Description, BehaviorDesignerUtility.TaskCommentGUIStyle);
            }
        }

        private void DuplicateNodes()
        {
            List<TaskSerializer> copiedTasks = this.mGraphDesigner.Copy(this.mGraphOffset, this.mGraphZoom);
            if ((copiedTasks != null) && (copiedTasks.Count > 0))
            {
                BehaviorUndo.RegisterUndo("Duplicate", this.mActiveBehaviorSource.Owner.GetObject());
            }
            this.mGraphDesigner.Paste(this.mActiveBehaviorSource, copiedTasks, this.mGraphOffset, this.mGraphZoom);
            this.SaveBehavior();
        }

        private bool GetMousePositionInGraph(out Vector2 mousePosition)
        {
            mousePosition = this.mCurrentMousePosition;
            if (!this.mGraphRect.Contains(mousePosition))
            {
                return false;
            }
            if (this.mShowPrefPane && this.mPreferencesPaneRect.Contains(mousePosition))
            {
                return false;
            }
            mousePosition -= new Vector2(this.mGraphRect.xMin, this.mGraphRect.yMin);
            mousePosition = (Vector2) (mousePosition / this.mGraphZoom);
            return true;
        }

        private bool GetMousePositionInPropertiesPane(out Vector2 mousePosition)
        {
            mousePosition = this.mCurrentMousePosition;
            if (!this.mPropertyBoxRect.Contains(mousePosition))
            {
                return false;
            }
            mousePosition.x -= this.mPropertyBoxRect.xMin;
            mousePosition.y -= this.mPropertyBoxRect.yMin;
            return true;
        }

        private Rect GetSelectionArea()
        {
            Vector2 vector;
            if (this.GetMousePositionInGraph(out vector))
            {
                float x = (this.mSelectStartPosition.x >= vector.x) ? vector.x : this.mSelectStartPosition.x;
                float num2 = (this.mSelectStartPosition.x <= vector.x) ? vector.x : this.mSelectStartPosition.x;
                float y = (this.mSelectStartPosition.y >= vector.y) ? vector.y : this.mSelectStartPosition.y;
                float num4 = (this.mSelectStartPosition.y <= vector.y) ? vector.y : this.mSelectStartPosition.y;
                this.mSelectionArea = new Rect(x, y, num2 - x, num4 - y);
            }
            return this.mSelectionArea;
        }

        private void HandleEvents()
        {
            if (!this.mTakingScreenshot)
            {
                if ((Event.current.type != EventType.MouseUp) && this.CheckForAutoScroll())
                {
                    base.Repaint();
                }
                else if ((Event.current.type != EventType.Repaint) && (Event.current.type != EventType.Layout))
                {
                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                            Vector2 vector;
                            if ((Event.current.button != 0) || (Event.current.modifiers == EventModifiers.Control))
                            {
                                if (((Event.current.button == 1) || ((Event.current.modifiers == EventModifiers.Control) && (Event.current.button == 0))) && this.RightMouseDown())
                                {
                                    Event.current.Use();
                                }
                                break;
                            }
                            if (!this.GetMousePositionInGraph(out vector))
                            {
                                if ((this.GetMousePositionInPropertiesPane(out vector) && (this.mBehaviorToolbarSelection == 2)) && this.mVariableInspector.LeftMouseDown(this.mActiveBehaviorSource, this.mActiveBehaviorSource, vector))
                                {
                                    Event.current.Use();
                                    base.Repaint();
                                }
                                break;
                            }
                            if (this.LeftMouseDown(Event.current.clickCount, vector))
                            {
                                Event.current.Use();
                            }
                            break;

                        case EventType.MouseUp:
                            if ((Event.current.button != 0) || (Event.current.modifiers == EventModifiers.Control))
                            {
                                if (((Event.current.button == 1) || ((Event.current.modifiers == EventModifiers.Control) && (Event.current.button == 0))) && this.mShowRightClickMenu)
                                {
                                    this.mShowRightClickMenu = false;
                                    this.mRightClickMenu.ShowAsContext();
                                    Event.current.Use();
                                }
                                break;
                            }
                            if (this.LeftMouseRelease())
                            {
                                Event.current.Use();
                            }
                            break;

                        case EventType.MouseMove:
                            if (this.MouseMove())
                            {
                                Event.current.Use();
                            }
                            break;

                        case EventType.MouseDrag:
                            if (Event.current.button != 0)
                            {
                                if ((Event.current.button == 2) && this.MousePan())
                                {
                                    Event.current.Use();
                                }
                                break;
                            }
                            if (!this.LeftMouseDragged())
                            {
                                if ((Event.current.modifiers == EventModifiers.Alt) && this.MousePan())
                                {
                                    Event.current.Use();
                                }
                                break;
                            }
                            Event.current.Use();
                            break;

                        case EventType.KeyDown:
                            if ((Event.current.keyCode == KeyCode.LeftCommand) || (Event.current.keyCode == KeyCode.RightCommand))
                            {
                                this.mCommandDown = true;
                            }
                            break;

                        case EventType.KeyUp:
                            if (((Event.current.keyCode != KeyCode.Delete) && (Event.current.keyCode != KeyCode.Backspace)) && !Event.current.commandName.Equals("Delete"))
                            {
                                if ((Event.current.keyCode == KeyCode.Return) || (Event.current.keyCode == KeyCode.KeypadEnter))
                                {
                                    if (this.mVariableInspector.HasFocus())
                                    {
                                        if (this.mVariableInspector.ClearFocus(true, this.mActiveBehaviorSource))
                                        {
                                            this.SaveBehavior();
                                        }
                                        base.Repaint();
                                    }
                                    else
                                    {
                                        this.DisableReferenceTasks();
                                    }
                                    Event.current.Use();
                                }
                                else if (Event.current.keyCode == KeyCode.Escape)
                                {
                                    this.DisableReferenceTasks();
                                }
                                else if ((Event.current.keyCode == KeyCode.LeftCommand) || (Event.current.keyCode == KeyCode.RightCommand))
                                {
                                    this.mCommandDown = false;
                                }
                                break;
                            }
                            if (!this.PropertiesInspectorHasFocus() && !EditorApplication.isPlaying)
                            {
                                this.DeleteNodes();
                                Event.current.Use();
                                break;
                            }
                            return;

                        case EventType.ScrollWheel:
                            if (!BehaviorDesignerPreferences.GetBool(BDPreferences.MouseWhellScrolls) || this.mCommandDown)
                            {
                                if (this.MouseZoom())
                                {
                                    Event.current.Use();
                                }
                                break;
                            }
                            this.MousePan();
                            break;

                        case EventType.ValidateCommand:
                            if (!EditorApplication.isPlaying)
                            {
                                if ((Event.current.commandName.Equals("Copy") || Event.current.commandName.Equals("Paste")) || ((Event.current.commandName.Equals("Cut") || Event.current.commandName.Equals("SelectAll")) || Event.current.commandName.Equals("Duplicate")))
                                {
                                    if ((this.PropertiesInspectorHasFocus() || EditorApplication.isPlaying) || this.ViewOnlyMode(true))
                                    {
                                        return;
                                    }
                                    Event.current.Use();
                                }
                                break;
                            }
                            return;

                        case EventType.ExecuteCommand:
                            if ((!this.PropertiesInspectorHasFocus() && !EditorApplication.isPlaying) && !this.ViewOnlyMode(true))
                            {
                                if (Event.current.commandName.Equals("Copy"))
                                {
                                    this.CopyNodes();
                                    Event.current.Use();
                                }
                                else if (Event.current.commandName.Equals("Paste"))
                                {
                                    this.PasteNodes();
                                    Event.current.Use();
                                }
                                else if (Event.current.commandName.Equals("Cut"))
                                {
                                    this.CutNodes();
                                    Event.current.Use();
                                }
                                else if (Event.current.commandName.Equals("SelectAll"))
                                {
                                    this.mGraphDesigner.SelectAll();
                                    Event.current.Use();
                                }
                                else if (Event.current.commandName.Equals("Duplicate"))
                                {
                                    this.DuplicateNodes();
                                    Event.current.Use();
                                }
                                break;
                            }
                            return;
                    }
                }
            }
        }

        public void IdentifyNode(NodeDesigner nodeDesigner)
        {
            this.mGraphDesigner.IdentifyNode(nodeDesigner);
        }

        private int IndexForBehavior(IBehavior behavior)
        {
            if (!(behavior.GetObject() is Behavior))
            {
                return 0;
            }
            Behavior[] components = (behavior.GetObject() as Behavior).gameObject.GetComponents<Behavior>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].Equals(behavior))
                {
                    return i;
                }
            }
            return -1;
        }

        private void Init()
        {
            if (this.mTaskList == null)
            {
                this.mTaskList = ScriptableObject.CreateInstance<TaskList>();
            }
            if (this.mVariableInspector == null)
            {
                this.mVariableInspector = ScriptableObject.CreateInstance<VariableInspector>();
            }
            this.mTaskList.Init();
            FieldInspector.Init();
            this.ClearBreadcrumbMenu();
        }

        public bool IsReferencingField(System.Reflection.FieldInfo fieldInfo)
        {
            return fieldInfo.Equals(this.mTaskInspector.ActiveReferenceTaskFieldInfo);
        }

        public bool IsReferencingTasks()
        {
            return (this.mTaskInspector.ActiveReferenceTask != null);
        }

        private bool LeftMouseDown(int clickCount, Vector2 mousePosition)
        {
            if (this.PropertiesInspectorHasFocus())
            {
                this.mTaskInspector.ClearFocus();
                this.mVariableInspector.ClearFocus(false, null);
                base.Repaint();
            }
            NodeDesigner nodeDesigner = this.mGraphDesigner.NodeAt(mousePosition, this.mGraphOffset);
            if (Event.current.modifiers == EventModifiers.Alt)
            {
                this.mNodeClicked = this.mGraphDesigner.IsSelected(nodeDesigner);
                return false;
            }
            if (this.IsReferencingTasks())
            {
                if (nodeDesigner == null)
                {
                    this.DisableReferenceTasks();
                }
                else
                {
                    this.ReferenceTask(nodeDesigner);
                }
                return true;
            }
            if (nodeDesigner != null)
            {
                if ((this.mGraphDesigner.HoverNode != null) && !nodeDesigner.Equals(this.mGraphDesigner.HoverNode))
                {
                    this.mGraphDesigner.ClearHover();
                    this.mGraphDesigner.Hover(nodeDesigner);
                }
                NodeConnection connection = null;
                if (!this.ViewOnlyMode(true) && ((connection = nodeDesigner.NodeConnectionRectContains(mousePosition, this.mGraphOffset)) != null))
                {
                    if (this.mGraphDesigner.NodeCanOriginateConnection(nodeDesigner, connection))
                    {
                        this.mGraphDesigner.ActiveNodeConnection = connection;
                    }
                    return true;
                }
                if (nodeDesigner.Contains(mousePosition, this.mGraphOffset, false))
                {
                    this.mKeepTasksSelected = false;
                    if (this.mGraphDesigner.IsSelected(nodeDesigner))
                    {
                        if (Event.current.modifiers == EventModifiers.Control)
                        {
                            this.mKeepTasksSelected = true;
                            this.mGraphDesigner.Deselect(nodeDesigner);
                        }
                        else if (Event.current.modifiers == EventModifiers.Shift)
                        {
                            nodeDesigner.Task.NodeData.Collapsed = !nodeDesigner.Task.NodeData.Collapsed;
                            this.mGraphDesigner.DeselectWithParent(nodeDesigner);
                        }
                        else if (clickCount == 2)
                        {
                            if ((this.mBehaviorToolbarSelection != 3) && BehaviorDesignerPreferences.GetBool(BDPreferences.OpenInspectorOnTaskDoubleClick))
                            {
                                this.mBehaviorToolbarSelection = 3;
                            }
                            else if (nodeDesigner.Task is BehaviorReference)
                            {
                                BehaviorReference task = nodeDesigner.Task as BehaviorReference;
                                if ((task.externalBehaviors != null) && (task.externalBehaviors[0] != null))
                                {
                                    Selection.activeObject = task.externalBehaviors[0];
                                    if (this.mLockActiveGameObject)
                                    {
                                        this.LoadBehavior(task.externalBehaviors[0].GetBehaviorSource(), false);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if ((Event.current.modifiers != EventModifiers.Shift) && (Event.current.modifiers != EventModifiers.Control))
                        {
                            this.mGraphDesigner.ClearNodeSelection();
                            this.mGraphDesigner.ClearConnectionSelection();
                            if (BehaviorDesignerPreferences.GetBool(BDPreferences.OpenInspectorOnTaskSelection))
                            {
                                this.mBehaviorToolbarSelection = 3;
                            }
                        }
                        else
                        {
                            this.mKeepTasksSelected = true;
                        }
                        this.mGraphDesigner.Select(nodeDesigner);
                    }
                    this.mNodeClicked = this.mGraphDesigner.IsSelected(nodeDesigner);
                    return true;
                }
            }
            if (this.mGraphDesigner.HoverNode != null)
            {
                bool collapsedButtonClicked = false;
                if (this.mGraphDesigner.HoverNode.HoverBarButtonClick(mousePosition, this.mGraphOffset, ref collapsedButtonClicked))
                {
                    this.SaveBehavior();
                    if (collapsedButtonClicked && this.mGraphDesigner.HoverNode.Task.NodeData.Collapsed)
                    {
                        this.mGraphDesigner.DeselectWithParent(this.mGraphDesigner.HoverNode);
                    }
                    return true;
                }
            }
            List<NodeConnection> nodeConnections = new List<NodeConnection>();
            this.mGraphDesigner.NodeConnectionsAt(mousePosition, this.mGraphOffset, ref nodeConnections);
            if (nodeConnections.Count > 0)
            {
                if ((Event.current.modifiers != EventModifiers.Shift) && (Event.current.modifiers != EventModifiers.Control))
                {
                    this.mGraphDesigner.ClearNodeSelection();
                    this.mGraphDesigner.ClearConnectionSelection();
                }
                for (int i = 0; i < nodeConnections.Count; i++)
                {
                    if (this.mGraphDesigner.IsSelected(nodeConnections[i]))
                    {
                        if (Event.current.modifiers == EventModifiers.Control)
                        {
                            this.mGraphDesigner.Deselect(nodeConnections[i]);
                        }
                    }
                    else
                    {
                        this.mGraphDesigner.Select(nodeConnections[i]);
                    }
                }
                return true;
            }
            if (Event.current.modifiers != EventModifiers.Shift)
            {
                this.mGraphDesigner.ClearNodeSelection();
                this.mGraphDesigner.ClearConnectionSelection();
            }
            this.mSelectStartPosition = mousePosition;
            this.mIsSelecting = true;
            this.mIsDragging = false;
            this.mDragDelta = Vector2.zero;
            this.mNodeClicked = false;
            return true;
        }

        private bool LeftMouseDragged()
        {
            Vector2 vector;
            if (!this.GetMousePositionInGraph(out vector))
            {
                return false;
            }
            if (Event.current.modifiers != EventModifiers.Alt)
            {
                if (this.IsReferencingTasks())
                {
                    return true;
                }
                if (this.mIsSelecting)
                {
                    this.mGraphDesigner.ClearNodeSelection();
                    List<NodeDesigner> list = this.mGraphDesigner.NodesAt(this.GetSelectionArea(), this.mGraphOffset);
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            this.mGraphDesigner.Select(list[i]);
                        }
                    }
                    return true;
                }
                if (this.mGraphDesigner.ActiveNodeConnection != null)
                {
                    return true;
                }
            }
            if (!this.mNodeClicked || this.ViewOnlyMode(true))
            {
                return false;
            }
            Vector2 zero = Vector2.zero;
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.SnapToGrid))
            {
                this.mDragDelta += Event.current.delta;
                if (Mathf.Abs(this.mDragDelta.x) > 10f)
                {
                    float num2 = Mathf.Abs(this.mDragDelta.x) % 10f;
                    zero.x = (Mathf.Abs(this.mDragDelta.x) - num2) * Mathf.Sign(this.mDragDelta.x);
                    this.mDragDelta.x = num2 * Mathf.Sign(this.mDragDelta.x);
                }
                if (Mathf.Abs(this.mDragDelta.y) > 10f)
                {
                    float num3 = Mathf.Abs(this.mDragDelta.y) % 10f;
                    zero.y = (Mathf.Abs(this.mDragDelta.y) - num3) * Mathf.Sign(this.mDragDelta.y);
                    this.mDragDelta.y = num3 * Mathf.Sign(this.mDragDelta.y);
                }
            }
            else
            {
                zero = Event.current.delta;
            }
            bool flag = this.mGraphDesigner.DragSelectedNodes((Vector2) (zero / this.mGraphZoom), Event.current.modifiers != EventModifiers.Alt);
            if (flag)
            {
                this.mKeepTasksSelected = true;
            }
            this.mIsDragging = true;
            return flag;
        }

        private bool LeftMouseRelease()
        {
            Vector2 vector3;
            this.mNodeClicked = false;
            if (this.IsReferencingTasks())
            {
                Vector2 vector;
                if (!this.mTaskInspector.IsActiveTaskArray() && !this.mTaskInspector.IsActiveTaskNull())
                {
                    this.DisableReferenceTasks();
                    base.Repaint();
                }
                if (!this.GetMousePositionInGraph(out vector))
                {
                    this.mGraphDesigner.ActiveNodeConnection = null;
                    return false;
                }
                return true;
            }
            if (this.mIsSelecting)
            {
                this.mIsSelecting = false;
                return true;
            }
            if (this.mIsDragging)
            {
                BehaviorUndo.RegisterUndo("Drag", this.mActiveBehaviorSource.Owner.GetObject());
                this.SaveBehavior();
                this.mIsDragging = false;
                this.mDragDelta = Vector3.zero;
                return true;
            }
            if (this.mGraphDesigner.ActiveNodeConnection != null)
            {
                Vector2 vector2;
                if (!this.GetMousePositionInGraph(out vector2))
                {
                    this.mGraphDesigner.ActiveNodeConnection = null;
                    return false;
                }
                NodeDesigner designer = this.mGraphDesigner.NodeAt(vector2, this.mGraphOffset);
                if (((designer != null) && !designer.Equals(this.mGraphDesigner.ActiveNodeConnection.OriginatingNodeDesigner)) && this.mGraphDesigner.NodeCanAcceptConnection(designer, this.mGraphDesigner.ActiveNodeConnection))
                {
                    this.mGraphDesigner.ConnectNodes(this.mActiveBehaviorSource, designer);
                    BehaviorUndo.RegisterUndo("Task Connection", this.mActiveBehaviorSource.Owner.GetObject());
                    this.SaveBehavior();
                }
                else
                {
                    this.mGraphDesigner.ActiveNodeConnection = null;
                }
                return true;
            }
            if ((Event.current.modifiers == EventModifiers.Shift) || this.mKeepTasksSelected)
            {
                return false;
            }
            if (!this.GetMousePositionInGraph(out vector3))
            {
                return false;
            }
            NodeDesigner nodeDesigner = this.mGraphDesigner.NodeAt(vector3, this.mGraphOffset);
            if ((nodeDesigner != null) && !this.mGraphDesigner.IsSelected(nodeDesigner))
            {
                this.mGraphDesigner.DeselectAllExcept(nodeDesigner);
            }
            return true;
        }

        public void LoadBehavior(BehaviorSource behaviorSource, bool loadPrevBehavior)
        {
            this.LoadBehavior(behaviorSource, loadPrevBehavior, false);
        }

        public void LoadBehavior(BehaviorSource behaviorSource, bool loadPrevBehavior, bool inspectorLoad)
        {
            if (((behaviorSource != null) && !object.ReferenceEquals(behaviorSource.Owner, null)) && !behaviorSource.Owner.Equals(null))
            {
                if (inspectorLoad && !this.mSizesInitialized)
                {
                    this.mActiveBehaviorID = behaviorSource.Owner.GetInstanceID();
                    this.mPrevActiveObject = Selection.activeObject;
                    this.mLoadedFromInspector = true;
                }
                else if (this.mSizesInitialized)
                {
                    if (!loadPrevBehavior)
                    {
                        this.DisableReferenceTasks();
                        this.mVariableInspector.ResetSelectedVariableIndex();
                    }
                    this.mActiveBehaviorSource = behaviorSource;
                    this.mActiveBehaviorSource.BehaviorID = this.mActiveBehaviorSource.Owner.GetInstanceID();
                    this.mActiveBehaviorID = this.mActiveBehaviorSource.BehaviorID;
                    this.mPrevActiveObject = Selection.activeObject;
                    if (((this.mBehaviorSourceHistory.Count == 0) || (this.mBehaviorSourceHistoryIndex >= this.mBehaviorSourceHistory.Count)) || ((this.mBehaviorSourceHistory[this.mBehaviorSourceHistoryIndex] == null) || (((this.mBehaviorSourceHistory[this.mBehaviorSourceHistoryIndex] as IBehavior).GetBehaviorSource() != null) && !this.mActiveBehaviorSource.BehaviorID.Equals((this.mBehaviorSourceHistory[this.mBehaviorSourceHistoryIndex] as IBehavior).GetBehaviorSource().BehaviorID))))
                    {
                        for (int i = this.mBehaviorSourceHistory.Count - 1; i > this.mBehaviorSourceHistoryIndex; i--)
                        {
                            this.mBehaviorSourceHistory.RemoveAt(i);
                        }
                        this.mBehaviorSourceHistory.Add(this.mActiveBehaviorSource.Owner.GetObject());
                        this.mBehaviorSourceHistoryIndex++;
                    }
                    Vector2 nodePosition = new Vector2(this.mGraphRect.width / (2f * this.mGraphZoom), 150f);
                    nodePosition -= this.mGraphOffset;
                    Vector2 zero = Vector2.zero;
                    if (loadPrevBehavior)
                    {
                        zero = this.mPrevEntryPosition;
                    }
                    if (this.mGraphDesigner.Load(this.mActiveBehaviorSource, loadPrevBehavior && !this.mLoadedFromInspector, nodePosition) && this.mGraphDesigner.HasEntryNode())
                    {
                        this.mGraphDesigner.SetRootNodesOffset(zero);
                        this.SaveBehavior();
                        if (!loadPrevBehavior || this.mLoadedFromInspector)
                        {
                            this.mGraphOffset = new Vector2((this.mGraphRect.width / (2f * this.mGraphZoom)) - 50f, 50f);
                            this.mGraphScrollPosition = (Vector2) (((this.mGraphScrollSize - new Vector2(this.mGraphRect.width, this.mGraphRect.height)) / 2f) - (2f * new Vector2(15f, 15f)));
                        }
                    }
                    this.mLoadedFromInspector = false;
                    if (this.mActiveBehaviorSource.Owner is Behavior)
                    {
                        this.mActiveObject = (this.mActiveBehaviorSource.Owner as Behavior).gameObject;
                    }
                    else
                    {
                        this.mActiveObject = this.mActiveBehaviorSource.Owner as ExternalBehavior;
                    }
                    Selection.activeObject = this.mActiveObject;
                    if (EditorApplication.isPlaying && (this.mActiveBehaviorSource != null))
                    {
                        this.mRightClickMenu = null;
                        this.mUpdateNodeTaskMap = true;
                        this.UpdateNodeTaskMap();
                    }
                    this.CheckForErrors();
                    this.UpdateGraphStatus();
                    this.ClearBreadcrumbMenu();
                    base.Repaint();
                }
            }
        }

        private bool MouseMove()
        {
            Vector2 vector;
            if (!this.GetMousePositionInGraph(out vector))
            {
                return false;
            }
            NodeDesigner designer = this.mGraphDesigner.NodeAt(vector, this.mGraphOffset);
            if ((this.mGraphDesigner.HoverNode != null) && (((designer != null) && !this.mGraphDesigner.HoverNode.Equals(designer)) || !this.mGraphDesigner.HoverNode.HoverBarAreaContains(vector, this.mGraphOffset)))
            {
                this.mGraphDesigner.ClearHover();
                base.Repaint();
            }
            if (((designer != null) && !designer.IsEntryDisplay) && !this.ViewOnlyMode(true))
            {
                this.mGraphDesigner.Hover(designer);
            }
            return (this.mGraphDesigner.HoverNode != null);
        }

        private bool MousePan()
        {
            Vector2 vector;
            if (!this.GetMousePositionInGraph(out vector))
            {
                return false;
            }
            Vector2 delta = Event.current.delta;
            if (Event.current.type == EventType.ScrollWheel)
            {
                delta = (Vector2) (delta * -1.5f);
                if (Event.current.modifiers == EventModifiers.Control)
                {
                    delta.x = delta.y;
                    delta.y = 0f;
                }
            }
            this.ScrollGraph(delta);
            return true;
        }

        private bool MouseZoom()
        {
            Vector2 vector;
            Vector2 vector2;
            if (!this.GetMousePositionInGraph(out vector))
            {
                return false;
            }
            float num = -Event.current.delta.y / 150f;
            this.mGraphZoom += num;
            this.mGraphZoom = Mathf.Clamp(this.mGraphZoom, 0.4f, 1f);
            this.GetMousePositionInGraph(out vector2);
            this.mGraphOffset += vector2 - vector;
            this.mGraphScrollPosition += vector2 - vector;
            this.mGraphDesigner.GraphDirty();
            return true;
        }

        public void OnEnable()
        {
            this.mIsPlaying = EditorApplication.isPlaying;
            this.mSizesInitialized = false;
            base.Repaint();
            if (this.mGraphDesigner == null)
            {
                this.mGraphDesigner = ScriptableObject.CreateInstance<GraphDesigner>();
            }
            if (this.mTaskInspector == null)
            {
                this.mTaskInspector = ScriptableObject.CreateInstance<TaskInspector>();
            }
            if (this.mGridMaterial == null)
            {
                this.mGridMaterial = new Material(Shader.Find("Hidden/Behavior Designer/Grid"));
                this.mGridMaterial.hideFlags = HideFlags.HideAndDontSave;
                this.mGridMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            EditorApplication.projectWindowChanged = (EditorApplication.CallbackFunction) Delegate.Combine(EditorApplication.projectWindowChanged, new EditorApplication.CallbackFunction(this.OnProjectWindowChange));
            EditorApplication.playmodeStateChanged = (EditorApplication.CallbackFunction) Delegate.Combine(EditorApplication.playmodeStateChanged, new EditorApplication.CallbackFunction(this.OnPlaymodeStateChange));
            Undo.undoRedoPerformed = (Undo.UndoRedoCallback) Delegate.Combine(Undo.undoRedoPerformed, new Undo.UndoRedoCallback(this.OnUndoRedo));
            this.Init();
            this.SetBehaviorManager();
        }

        public void OnFocus()
        {
            instance = this;
            base.wantsMouseMove = true;
            this.Init();
            if (!this.mLockActiveGameObject)
            {
                this.mActiveObject = Selection.activeObject;
            }
            this.ReloadPreviousBehavior();
            this.UpdateGraphStatus();
        }

        public void OnGUI()
        {
            GUI.enabled = !EditorApplication.isCompiling;
            this.mCurrentMousePosition = Event.current.mousePosition;
            this.SetupSizes();
            if (!this.mSizesInitialized)
            {
                this.mSizesInitialized = true;
                if (!this.mLockActiveGameObject || (this.mActiveObject == null))
                {
                    this.UpdateTree(true);
                }
                else if (!(this.mActiveObject is GameObject))
                {
                    if (this.mActiveObject is ExternalBehavior)
                    {
                        ExternalBehavior mActiveObject = this.mActiveObject as ExternalBehavior;
                        BehaviorSource behaviorSource = mActiveObject.BehaviorSource;
                        if (mActiveObject.BehaviorSource.Owner == null)
                        {
                            mActiveObject.BehaviorSource.Owner = mActiveObject;
                        }
                        this.LoadBehavior(behaviorSource, true, false);
                    }
                }
                else
                {
                    BehaviorSource source = null;
                    Behavior[] components = (this.mActiveObject as GameObject).GetComponents<Behavior>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i].GetInstanceID() == this.mActiveBehaviorSource.BehaviorID)
                        {
                            source = components[i].GetBehaviorSource();
                            break;
                        }
                    }
                    this.LoadBehavior(source, true, false);
                }
            }
            if (this.Draw() && (this.mGUITickCount > 1))
            {
                base.Repaint();
                this.mGUITickCount = 0;
            }
            this.HandleEvents();
            this.mGUITickCount++;
            GUI.enabled = true;
        }

        public void OnInspectorUpdate()
        {
            if (this.mStepApplication)
            {
                EditorApplication.Step();
                this.mStepApplication = false;
            }
            if ((EditorApplication.isPlaying && !EditorApplication.isPaused) && ((this.mActiveBehaviorSource != null) && (this.mBehaviorManager != null)))
            {
                if (this.mUpdateNodeTaskMap)
                {
                    this.UpdateNodeTaskMap();
                }
                if (this.mBehaviorManager.AtBreakpoint)
                {
                    this.mBehaviorManager.AtBreakpoint = false;
                }
                base.Repaint();
            }
            if (Application.isPlaying && (this.mBehaviorManager == null))
            {
                this.SetBehaviorManager();
            }
            if ((this.mBehaviorManager != null) && this.mBehaviorManager.Dirty)
            {
                if (this.mActiveBehaviorSource != null)
                {
                    this.LoadBehavior(this.mActiveBehaviorSource, true, false);
                }
                this.mBehaviorManager.Dirty = false;
            }
            if (!EditorApplication.isPlaying && this.mIsPlaying)
            {
                this.ReloadPreviousBehavior();
            }
            this.mIsPlaying = EditorApplication.isPlaying;
            this.UpdateGraphStatus();
            this.UpdateCheck();
        }

        public void OnPlaymodeStateChange()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                if (this.mBehaviorManager == null)
                {
                    this.SetBehaviorManager();
                    if (this.mBehaviorManager == null)
                    {
                        return;
                    }
                }
                if (this.mBehaviorManager.AtBreakpoint && this.mEditorAtBreakpoint)
                {
                    this.mEditorAtBreakpoint = false;
                    this.mBehaviorManager.AtBreakpoint = false;
                }
            }
            else if (EditorApplication.isPlaying && EditorApplication.isPaused)
            {
                if ((this.mBehaviorManager != null) && this.mBehaviorManager.AtBreakpoint)
                {
                    if (!this.mEditorAtBreakpoint)
                    {
                        this.mEditorAtBreakpoint = true;
                    }
                    else
                    {
                        this.mEditorAtBreakpoint = false;
                        this.mBehaviorManager.AtBreakpoint = false;
                    }
                }
            }
            else if (!EditorApplication.isPlaying)
            {
                this.mBehaviorManager = null;
            }
        }

        private void OnPreferenceChange(BDPreferences pref, object value)
        {
            switch (pref)
            {
                case BDPreferences.CompactMode:
                    this.mGraphDesigner.GraphDirty();
                    return;

                case BDPreferences.BinarySerialization:
                    this.SaveBehavior();
                    return;

                case BDPreferences.ErrorChecking:
                    this.CheckForErrors();
                    return;

                case BDPreferences.GizmosViewMode:
                case BDPreferences.ShowSceneIcon:
                    GizmoManager.UpdateAllGizmos();
                    return;
            }
        }

        public void OnProjectWindowChange()
        {
            this.ReloadPreviousBehavior();
            this.ClearBreadcrumbMenu();
        }

        public void OnSelectionChange()
        {
            if (!this.mLockActiveGameObject)
            {
                this.UpdateTree(false);
            }
            else
            {
                this.ReloadPreviousBehavior();
            }
            this.UpdateGraphStatus();
        }

        public void OnTaskBreakpoint()
        {
            EditorApplication.isPaused = true;
            base.Repaint();
        }

        private void OnUndoRedo()
        {
            if (this.mActiveBehaviorSource != null)
            {
                this.LoadBehavior(this.mActiveBehaviorSource, true, false);
            }
        }

        private void OpenInFileEditor(object obj)
        {
            NodeDesigner designer = obj as NodeDesigner;
            TaskInspector.OpenInFileEditor(designer.Task);
        }

        private void PasteNodes()
        {
            if ((this.mActiveObject != null) && !EditorApplication.isPlaying)
            {
                GameObject mActiveObject = this.mActiveObject as GameObject;
                if ((mActiveObject != null) && (mActiveObject.GetComponent<Behavior>() == null))
                {
                    this.AddBehavior();
                }
                if ((this.mCopiedTasks != null) && (this.mCopiedTasks.Count > 0))
                {
                    BehaviorUndo.RegisterUndo("Paste", this.mActiveBehaviorSource.Owner.GetObject());
                }
                this.mGraphDesigner.Paste(this.mActiveBehaviorSource, this.mCopiedTasks, this.mGraphOffset, this.mGraphZoom);
                this.SaveBehavior();
            }
        }

        private bool PropertiesInspectorHasFocus()
        {
            return (this.mTaskInspector.HasFocus() || this.mVariableInspector.HasFocus());
        }

        private void ReferenceTask(NodeDesigner nodeDesigner)
        {
            if ((nodeDesigner != null) && this.mTaskInspector.ReferenceTasks(nodeDesigner.Task))
            {
                this.SaveBehavior();
            }
        }

        private void ReloadPreviousBehavior()
        {
            if (this.mActiveObject == null)
            {
                if (this.mGraphDesigner != null)
                {
                    this.ClearGraph();
                    base.Repaint();
                }
            }
            else if (!(this.mActiveObject is GameObject))
            {
                if (this.mActiveObject is ExternalBehavior)
                {
                    ExternalBehavior mActiveObject = this.mActiveObject as ExternalBehavior;
                    BehaviorSource behaviorSource = mActiveObject.BehaviorSource;
                    if (mActiveObject.BehaviorSource.Owner == null)
                    {
                        mActiveObject.BehaviorSource.Owner = mActiveObject;
                    }
                    this.LoadBehavior(behaviorSource, true, false);
                }
                else if (this.mGraphDesigner != null)
                {
                    this.mActiveObject = null;
                    this.ClearGraph();
                }
            }
            else
            {
                GameObject obj2 = this.mActiveObject as GameObject;
                int index = -1;
                Behavior[] components = obj2.GetComponents<Behavior>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i].GetInstanceID() == this.mActiveBehaviorID)
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    this.LoadBehavior(components[index].GetBehaviorSource(), true, false);
                }
                else if (components.Count<Behavior>() > 0)
                {
                    this.LoadBehavior(components[0].GetBehaviorSource(), true, false);
                }
                else if (this.mGraphDesigner != null)
                {
                    this.ClearGraph();
                }
            }
        }

        private void RemoveBehavior()
        {
            if (!EditorApplication.isPlaying && ((this.mActiveObject is GameObject) && ((this.mActiveBehaviorSource.EntryTask == null) || ((this.mActiveBehaviorSource.EntryTask != null) && EditorUtility.DisplayDialog("Remove Behavior Tree", "Are you sure you want to remove this behavior tree?", "Yes", "No")))))
            {
                GameObject mActiveObject = this.mActiveObject as GameObject;
                int index = this.IndexForBehavior(this.mActiveBehaviorSource.Owner);
                BehaviorUndo.DestroyObject(this.mActiveBehaviorSource.Owner.GetObject(), true);
                index--;
                if ((index == -1) && (mActiveObject.GetComponents<Behavior>().Length > 0))
                {
                    index = 0;
                }
                if (index > -1)
                {
                    this.LoadBehavior(mActiveObject.GetComponents<Behavior>()[index].GetBehaviorSource(), true);
                }
                else
                {
                    this.ClearGraph();
                }
                this.ClearBreadcrumbMenu();
                base.Repaint();
            }
        }

        public void RemoveSharedVariableReferences(SharedVariable sharedVariable)
        {
            if (this.mGraphDesigner.RemoveSharedVariableReferences(sharedVariable))
            {
                this.SaveBehavior();
                base.Repaint();
            }
        }

        private void RenderScreenshotTile()
        {
            float width = Mathf.Min(this.mGraphRect.width, this.mScreenshotGraphSize.width - (this.mGraphOffset.x - this.mScreenshotGraphOffset.x));
            float height = Mathf.Min(this.mGraphRect.height, this.mScreenshotGraphSize.height + (this.mGraphOffset.y - this.mScreenshotGraphOffset.y));
            Rect source = new Rect(this.mGraphRect.x, ((39f + this.mGraphRect.height) - height) - 7f, width, height);
            this.mScreenshotTexture.ReadPixels(source, -((int) (this.mGraphOffset.x - this.mScreenshotGraphOffset.x)), (int) ((this.mScreenshotGraphSize.height - height) + (this.mGraphOffset.y - this.mScreenshotGraphOffset.y)));
            this.mScreenshotTexture.Apply(false);
            if (((this.mScreenshotGraphSize.xMin + width) - (this.mGraphOffset.x - this.mScreenshotGraphOffset.x)) < this.mScreenshotGraphSize.xMax)
            {
                this.mGraphOffset.x -= width - 1f;
                this.mGraphDesigner.GraphDirty();
                base.Repaint();
            }
            else if (((this.mScreenshotGraphSize.yMin + height) - (this.mGraphOffset.y - this.mScreenshotGraphOffset.y)) < this.mScreenshotGraphSize.yMax)
            {
                this.mGraphOffset.y -= height - 1f;
                this.mGraphOffset.x = this.mScreenshotGraphOffset.x;
                this.mGraphDesigner.GraphDirty();
                base.Repaint();
            }
            else
            {
                this.SaveScreenshot();
            }
        }

        private void ReplaceTaskCallback(object obj)
        {
            System.Type o = (System.Type) obj;
            if (((this.mGraphDesigner.SelectedNodes.Count == 1) && !this.mGraphDesigner.SelectedNodes[0].Task.GetType().Equals(o)) && this.mGraphDesigner.ReplaceSelectedNode(this.mActiveBehaviorSource, o))
            {
                this.SaveBehavior();
            }
        }

        private bool RightMouseDown()
        {
            Vector2 vector;
            if (this.IsReferencingTasks())
            {
                this.DisableReferenceTasks();
                return false;
            }
            if (!this.GetMousePositionInGraph(out vector))
            {
                return false;
            }
            NodeDesigner nodeDesigner = this.mGraphDesigner.NodeAt(vector, this.mGraphOffset);
            if ((nodeDesigner == null) || !this.mGraphDesigner.IsSelected(nodeDesigner))
            {
                this.mGraphDesigner.ClearNodeSelection();
                this.mGraphDesigner.ClearConnectionSelection();
                if (nodeDesigner != null)
                {
                    this.mGraphDesigner.Select(nodeDesigner);
                }
            }
            if (this.mGraphDesigner.HoverNode != null)
            {
                this.mGraphDesigner.ClearHover();
            }
            this.BuildRightClickMenu(nodeDesigner);
            this.mShowRightClickMenu = true;
            return true;
        }

        private void SaveAsAsset()
        {
            if (this.mActiveBehaviorSource != null)
            {
                string path = EditorUtility.SaveFilePanel("Save Behavior Tree", "Assets", this.mActiveBehaviorSource.behaviorName + ".asset", "asset");
                if ((path.Length != 0) && (Application.dataPath.Length < path.Length))
                {
                    System.Type type = System.Type.GetType("BehaviorDesigner.Runtime.ExternalBehaviorTree, Assembly-CSharp");
                    if (type == null)
                    {
                        type = System.Type.GetType("BehaviorDesigner.Runtime.ExternalBehaviorTree, Assembly-CSharp-firstpass");
                    }
                    if (BehaviorDesignerPreferences.GetBool(BDPreferences.BinarySerialization))
                    {
                        BinarySerialization.Save(this.mActiveBehaviorSource);
                    }
                    else
                    {
                        SerializeJSON.Save(this.mActiveBehaviorSource);
                    }
                    ExternalBehavior owner = ScriptableObject.CreateInstance(type) as ExternalBehavior;
                    BehaviorSource behaviorSource = new BehaviorSource(owner) {
                        behaviorName = this.mActiveBehaviorSource.behaviorName,
                        behaviorDescription = this.mActiveBehaviorSource.behaviorDescription,
                        TaskData = this.mActiveBehaviorSource.TaskData
                    };
                    owner.SetBehaviorSource(behaviorSource);
                    path = string.Format("Assets/{0}", path.Substring(Application.dataPath.Length + 1));
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(owner, path);
                    AssetDatabase.ImportAsset(path);
                    Selection.activeObject = owner;
                }
                else if (Path.GetExtension(path).Equals(".asset"))
                {
                    Debug.LogError("Error: Unable to save external behavior tree. The save location must be within the Asset directory.");
                }
            }
        }

        private void SaveAsPrefab()
        {
            if (this.mActiveBehaviorSource != null)
            {
                string path = EditorUtility.SaveFilePanel("Save Behavior Tree", "Assets", this.mActiveBehaviorSource.behaviorName + ".prefab", "prefab");
                if ((path.Length != 0) && (Application.dataPath.Length < path.Length))
                {
                    GameObject go = new GameObject();
                    System.Type componentType = System.Type.GetType("BehaviorDesigner.Runtime.BehaviorTree, Assembly-CSharp");
                    if (componentType == null)
                    {
                        componentType = System.Type.GetType("BehaviorDesigner.Runtime.BehaviorTree, Assembly-CSharp-firstpass");
                    }
                    Behavior owner = go.AddComponent(componentType) as Behavior;
                    BehaviorSource behaviorSource = new BehaviorSource(owner) {
                        behaviorName = this.mActiveBehaviorSource.behaviorName,
                        behaviorDescription = this.mActiveBehaviorSource.behaviorDescription,
                        TaskData = this.mActiveBehaviorSource.TaskData
                    };
                    owner.SetBehaviorSource(behaviorSource);
                    path = string.Format("Assets/{0}", path.Substring(Application.dataPath.Length + 1));
                    AssetDatabase.DeleteAsset(path);
                    GameObject obj3 = PrefabUtility.CreatePrefab(path, go);
                    UnityEngine.Object.DestroyImmediate(go, true);
                    AssetDatabase.ImportAsset(path);
                    Selection.activeObject = obj3;
                }
                else if (Path.GetExtension(path).Equals(".prefab"))
                {
                    Debug.LogError("Error: Unable to save prefab. The save location must be within the Asset directory.");
                }
            }
        }

        public void SaveBehavior()
        {
            if (((this.mActiveBehaviorSource != null) && !this.ViewOnlyMode(true)) && !Application.isPlaying)
            {
                this.mGraphDesigner.Save(this.mActiveBehaviorSource);
                this.CheckForErrors();
                if (this.mGraphDesigner.HasEntryNode())
                {
                    this.mPrevEntryPosition = this.mGraphDesigner.EntryNodePosition();
                }
            }
        }

        private void SaveScreenshot()
        {
            byte[] bytes = ImageConversion.EncodeToPNG(this.mScreenshotTexture);
            UnityEngine.Object.DestroyImmediate(this.mScreenshotTexture, true);
            File.WriteAllBytes(this.mScreenshotPath, bytes);
            AssetDatabase.ImportAsset(string.Format("Assets/{0}", this.mScreenshotPath.Substring(Application.dataPath.Length + 1)));
            this.mTakingScreenshot = false;
            this.mGraphZoom = this.mScreenshotStartGraphZoom;
            this.mGraphOffset = this.mScreenshotStartGraphOffset;
            this.mGraphDesigner.GraphDirty();
            base.Repaint();
        }

        private void ScrollGraph(Vector2 amount)
        {
            this.mGraphOffset += (Vector2) (amount / this.mGraphZoom);
            this.mGraphScrollPosition -= amount;
            this.mGraphDesigner.GraphDirty();
            base.Repaint();
        }

        private void SelectInProject(object obj)
        {
            NodeDesigner designer = obj as NodeDesigner;
            TaskInspector.SelectInProject(designer.Task);
        }

        private void SetBehaviorManager()
        {
            this.mBehaviorManager = BehaviorManager.instance;
            if (this.mBehaviorManager != null)
            {
                this.mBehaviorManager.OnTaskBreakpoint = (BehaviorManager.BehaviorManagerHandler) Delegate.Combine(this.mBehaviorManager.OnTaskBreakpoint, new BehaviorManager.BehaviorManagerHandler(this.OnTaskBreakpoint));
                this.mUpdateNodeTaskMap = true;
            }
        }

        private void SetupSizes()
        {
            if (((this.mPrevScreenWidth != Screen.width) || (this.mPrevScreenHeight != Screen.height)) || (this.mPropertiesPanelOnLeft != BehaviorDesignerPreferences.GetBool(BDPreferences.PropertiesPanelOnLeft)))
            {
                if (BehaviorDesignerPreferences.GetBool(BDPreferences.PropertiesPanelOnLeft))
                {
                    this.mFileToolBarRect = new Rect(300f, 0f, (float) (Screen.width - 300), 18f);
                    this.mPropertyToolbarRect = new Rect(0f, 0f, 300f, 18f);
                    this.mPropertyBoxRect = new Rect(0f, this.mPropertyToolbarRect.height, 300f, (Screen.height - this.mPropertyToolbarRect.height) - 21f);
                    this.mGraphRect = new Rect(300f, 18f, (float) ((Screen.width - 300) - 15), (float) (((Screen.height - 0x24) - 0x15) - 15));
                    this.mPreferencesPaneRect = new Rect((300f + this.mGraphRect.width) - 290f, (float) (0x12 + (!EditorGUIUtility.isProSkin ? 2 : 1)), 290f, 348f);
                }
                else
                {
                    this.mFileToolBarRect = new Rect(0f, 0f, (float) (Screen.width - 300), 18f);
                    this.mPropertyToolbarRect = new Rect((float) (Screen.width - 300), 0f, 300f, 18f);
                    this.mPropertyBoxRect = new Rect((float) (Screen.width - 300), this.mPropertyToolbarRect.height, 300f, (Screen.height - this.mPropertyToolbarRect.height) - 21f);
                    this.mGraphRect = new Rect(0f, 18f, (float) ((Screen.width - 300) - 15), (float) (((Screen.height - 0x24) - 0x15) - 15));
                    this.mPreferencesPaneRect = new Rect(this.mGraphRect.width - 290f, (float) (0x12 + (!EditorGUIUtility.isProSkin ? 2 : 1)), 290f, 348f);
                }
                this.mDebugToolBarRect = new Rect(this.mGraphRect.x, (float) ((Screen.height - 0x12) - 0x15), this.mGraphRect.width + 15f, 18f);
                this.mGraphScrollRect.Set(this.mGraphRect.xMin + 15f, this.mGraphRect.yMin + 15f, this.mGraphRect.width - 30f, this.mGraphRect.height - 30f);
                if (this.mGraphScrollPosition == new Vector2(-1f, -1f))
                {
                    this.mGraphScrollPosition = (Vector2) (((this.mGraphScrollSize - new Vector2(this.mGraphRect.width, this.mGraphRect.height)) / 2f) - (2f * new Vector2(15f, 15f)));
                }
                this.mPrevScreenWidth = Screen.width;
                this.mPrevScreenHeight = Screen.height;
                this.mPropertiesPanelOnLeft = BehaviorDesignerPreferences.GetBool(BDPreferences.PropertiesPanelOnLeft);
            }
        }

        [UnityEditor.MenuItem("Tools/Behavior Designer/Editor", false, 0)]
        public static void ShowWindow()
        {
            BehaviorDesignerWindow window = EditorWindow.GetWindow<BehaviorDesignerWindow>(false, "Behavior Designer");
            window.wantsMouseMove = true;
            window.minSize = new Vector2(500f, 100f);
            BehaviorDesignerPreferences.InitPrefernces();
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.ShowWelcomeScreen))
            {
                WelcomeScreen.ShowWindow();
            }
        }

        private void TakeScreenshot()
        {
            this.mScreenshotPath = EditorUtility.SaveFilePanel("Save Screenshot", "Assets", this.mActiveBehaviorSource.behaviorName + "Screenshot.png", "png");
            if ((this.mScreenshotPath.Length != 0) && (Application.dataPath.Length < this.mScreenshotPath.Length))
            {
                this.mTakingScreenshot = true;
                this.mScreenshotGraphSize = this.mGraphDesigner.GraphSize();
                this.mGraphDesigner.GraphDirty();
                if ((this.mScreenshotGraphSize.width == 0f) || (this.mScreenshotGraphSize.height == 0f))
                {
                    this.mScreenshotGraphSize = new Rect(0f, 0f, 100f, 100f);
                }
                this.mScreenshotStartGraphZoom = this.mGraphZoom;
                this.mScreenshotStartGraphOffset = this.mGraphOffset;
                this.mGraphZoom = 1f;
                this.mGraphOffset.x -= this.mScreenshotGraphSize.xMin - 10f;
                this.mGraphOffset.y -= this.mScreenshotGraphSize.yMin - 10f;
                this.mScreenshotGraphOffset = this.mGraphOffset;
                this.mScreenshotGraphSize.Set(this.mScreenshotGraphSize.xMin - 9f, this.mScreenshotGraphSize.yMin, this.mScreenshotGraphSize.width + 18f, this.mScreenshotGraphSize.height + 18f);
                this.mScreenshotTexture = new Texture2D((int) this.mScreenshotGraphSize.width, (int) this.mScreenshotGraphSize.height, TextureFormat.RGB24, false);
                base.Repaint();
            }
            else if (Path.GetExtension(this.mScreenshotPath).Equals(".png"))
            {
                Debug.LogError("Error: Unable to save screenshot. The save location must be within the Asset directory.");
            }
        }

        private void ToggleBreakpoint(object obj)
        {
            (obj as NodeDesigner).ToggleBreakpoint();
            this.SaveBehavior();
            base.Repaint();
        }

        private void ToggleCollapseState(object obj)
        {
            NodeDesigner nodeDesigner = obj as NodeDesigner;
            if (nodeDesigner.ToggleCollapseState())
            {
                this.mGraphDesigner.DeselectWithParent(nodeDesigner);
            }
            this.SaveBehavior();
            base.Repaint();
        }

        private void ToggleEnableState(object obj)
        {
            (obj as NodeDesigner).ToggleEnableState();
            this.SaveBehavior();
            base.Repaint();
        }

        public void ToggleReferenceTasks()
        {
            this.ToggleReferenceTasks(null, null);
        }

        public void ToggleReferenceTasks(Task task, System.Reflection.FieldInfo fieldInfo)
        {
            bool flag = !this.IsReferencingTasks();
            this.mTaskInspector.SetActiveReferencedTasks(!flag ? null : task, !flag ? null : fieldInfo);
            this.UpdateGraphStatus();
        }

        public void Update()
        {
            if (this.mTakingScreenshot)
            {
                base.Repaint();
            }
        }

        private bool UpdateCheck()
        {
            if ((this.mUpdateCheckRequest != null) && this.mUpdateCheckRequest.isDone)
            {
                if (!string.IsNullOrEmpty(this.mUpdateCheckRequest.error))
                {
                    this.mUpdateCheckRequest = null;
                    return false;
                }
                if (!"1.5.5".ToString().Equals(this.mUpdateCheckRequest.text))
                {
                    this.LatestVersion = this.mUpdateCheckRequest.text;
                }
                this.mUpdateCheckRequest = null;
            }
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.UpdateCheck) && (DateTime.Compare(this.LastUpdateCheck.AddDays(1.0), DateTime.UtcNow) < 0))
            {
                object[] args = new object[] { "1.5.5", Application.unityVersion, Application.platform, EditorUserBuildSettings.activeBuildTarget };
                string url = string.Format("http://www.opsive.com/assets/BehaviorDesigner/UpdateCheck.php?version={0}&unityversion={1}&devplatform={2}&targetplatform={3}", args);
                this.mUpdateCheckRequest = new WWW(url);
                this.LastUpdateCheck = DateTime.UtcNow;
            }
            return (this.mUpdateCheckRequest != null);
        }

        public void UpdateGraphStatus()
        {
            if (((this.mActiveObject == null) || (this.mGraphDesigner == null)) || (!(this.mActiveObject is GameObject) && !(this.mActiveObject is ExternalBehavior)))
            {
                this.mGraphStatus = "Select a GameObject";
            }
            else if ((this.mActiveObject is GameObject) && object.ReferenceEquals((this.mActiveObject as GameObject).GetComponent<Behavior>(), null))
            {
                this.mGraphStatus = "Right Click, Add a Behavior Tree Component";
            }
            else if (this.ViewOnlyMode(true) && (this.mActiveBehaviorSource != null))
            {
                ExternalBehavior externalBehavior = (this.mActiveBehaviorSource.Owner.GetObject() as Behavior).ExternalBehavior;
                if (externalBehavior != null)
                {
                    this.mGraphStatus = externalBehavior.BehaviorSource.ToString() + " (View Only Mode)";
                }
                else
                {
                    this.mGraphStatus = this.mActiveBehaviorSource.ToString() + " (View Only Mode)";
                }
            }
            else if (!this.mGraphDesigner.HasEntryNode())
            {
                this.mGraphStatus = "Add a Task";
            }
            else if (this.IsReferencingTasks())
            {
                this.mGraphStatus = "Select tasks to reference (right click to exit)";
            }
            else if (((this.mActiveBehaviorSource != null) && (this.mActiveBehaviorSource.Owner != null)) && (this.mActiveBehaviorSource.Owner.GetObject() != null))
            {
                this.mGraphStatus = this.mActiveBehaviorSource.ToString();
            }
        }

        private void UpdateNodeTaskMap()
        {
            if (this.mUpdateNodeTaskMap && (this.mBehaviorManager != null))
            {
                Behavior owner = this.mActiveBehaviorSource.Owner as Behavior;
                List<Task> taskList = this.mBehaviorManager.GetTaskList(owner);
                if (taskList != null)
                {
                    this.mNodeDesignerTaskMap = new Dictionary<NodeDesigner, Task>();
                    for (int i = 0; i < taskList.Count; i++)
                    {
                        NodeDesigner nodeDesigner = taskList[i].NodeData.NodeDesigner as NodeDesigner;
                        if ((nodeDesigner != null) && !this.mNodeDesignerTaskMap.ContainsKey(nodeDesigner))
                        {
                            this.mNodeDesignerTaskMap.Add(nodeDesigner, taskList[i]);
                        }
                    }
                    this.mUpdateNodeTaskMap = false;
                }
            }
        }

        private void UpdateTree(bool firstLoad)
        {
            bool flag = firstLoad;
            if (Selection.activeObject == null)
            {
                if ((this.mActiveObject != null) && (this.mActiveBehaviorSource != null))
                {
                    this.mPrevActiveObject = this.mActiveObject;
                }
                this.mActiveObject = null;
                this.ClearGraph();
                return;
            }
            bool loadPrevBehavior = false;
            if (!Selection.activeObject.Equals(this.mActiveObject))
            {
                this.mActiveObject = Selection.activeObject;
                flag = true;
            }
            BehaviorSource behaviorSource = null;
            GameObject mActiveObject = this.mActiveObject as GameObject;
            if ((mActiveObject == null) || (mActiveObject.GetComponent<Behavior>() == null))
            {
                if (this.mActiveObject is ExternalBehavior)
                {
                    ExternalBehavior behavior = this.mActiveObject as ExternalBehavior;
                    if (behavior.BehaviorSource.Owner == null)
                    {
                        behavior.BehaviorSource.Owner = behavior;
                    }
                    if (flag && this.mActiveObject.Equals(this.mPrevActiveObject))
                    {
                        loadPrevBehavior = true;
                    }
                    behaviorSource = behavior.BehaviorSource;
                }
                else
                {
                    this.mPrevActiveObject = null;
                }
            }
            else
            {
                bool flag3;
                if (!flag)
                {
                    Behavior[] components = mActiveObject.GetComponents<Behavior>();
                    flag3 = false;
                    if (this.mActiveBehaviorSource != null)
                    {
                        for (int i = 0; i < components.Length; i++)
                        {
                            if (components[i].Equals(this.mActiveBehaviorSource.Owner))
                            {
                                flag3 = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (!this.mActiveObject.Equals(this.mPrevActiveObject) || (this.mActiveBehaviorID == -1))
                    {
                        behaviorSource = mActiveObject.GetComponents<Behavior>()[0].GetBehaviorSource();
                    }
                    else
                    {
                        loadPrevBehavior = true;
                        int index = -1;
                        Behavior[] source = (this.mActiveObject as GameObject).GetComponents<Behavior>();
                        for (int j = 0; j < source.Length; j++)
                        {
                            if (source[j].GetInstanceID() == this.mActiveBehaviorID)
                            {
                                index = j;
                                break;
                            }
                        }
                        if (index != -1)
                        {
                            behaviorSource = mActiveObject.GetComponents<Behavior>()[index].GetBehaviorSource();
                        }
                        else if (source.Count<Behavior>() > 0)
                        {
                            behaviorSource = mActiveObject.GetComponents<Behavior>()[0].GetBehaviorSource();
                        }
                    }
                    goto Label_020D;
                }
                if (!flag3)
                {
                    behaviorSource = mActiveObject.GetComponents<Behavior>()[0].GetBehaviorSource();
                }
                else
                {
                    behaviorSource = this.mActiveBehaviorSource;
                    loadPrevBehavior = true;
                }
            }
        Label_020D:
            if (behaviorSource != null)
            {
                this.LoadBehavior(behaviorSource, loadPrevBehavior, false);
            }
            else if (behaviorSource == null)
            {
                this.ClearGraph();
            }
        }

        public bool ViewOnlyMode(bool checkExternal)
        {
            if (!Application.isPlaying)
            {
                if (((this.mActiveBehaviorSource == null) || (this.mActiveBehaviorSource.Owner == null)) || this.mActiveBehaviorSource.Owner.Equals(null))
                {
                    return false;
                }
                Behavior behavior = this.mActiveBehaviorSource.Owner.GetObject() as Behavior;
                if (behavior != null)
                {
                    if (!BehaviorDesignerPreferences.GetBool(BDPreferences.EditablePrefabInstances) && (PrefabUtility.GetPrefabType(this.mActiveBehaviorSource.Owner.GetObject()) == PrefabType.PrefabInstance))
                    {
                        return true;
                    }
                    if (checkExternal && (behavior.ExternalBehavior != null))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public int ActiveBehaviorID
        {
            get
            {
                return this.mActiveBehaviorID;
            }
        }

        public List<BehaviorDesigner.Editor.ErrorDetails> ErrorDetails
        {
            get
            {
                return this.mErrorDetails;
            }
        }

        private DateTime LastUpdateCheck
        {
            get
            {
                try
                {
                    if (this.mLastUpdateCheck != DateTime.MinValue)
                    {
                        return this.mLastUpdateCheck;
                    }
                    this.mLastUpdateCheck = DateTime.Parse(EditorPrefs.GetString("BehaviorDesignerLastUpdateCheck", "1/1/1971 00:00:01"), CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    this.mLastUpdateCheck = DateTime.UtcNow;
                }
                return this.mLastUpdateCheck;
            }
            set
            {
                this.mLastUpdateCheck = value;
                EditorPrefs.SetString("BehaviorDesignerLastUpdateCheck", this.mLastUpdateCheck.ToString(CultureInfo.InvariantCulture));
            }
        }

        public string LatestVersion
        {
            get
            {
                if (string.IsNullOrEmpty(this.mLatestVersion))
                {
                    this.mLatestVersion = EditorPrefs.GetString("BehaviorDesignerLatestVersion", "1.5.5".ToString());
                }
                return this.mLatestVersion;
            }
            set
            {
                this.mLatestVersion = value;
                EditorPrefs.SetString("BehaviorDesignerLatestVersion", this.mLatestVersion);
            }
        }

        private enum BreadcrumbMenuType
        {
            GameObjectBehavior,
            GameObject,
            Behavior
        }
    }
}

