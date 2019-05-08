namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    public static class BehaviorDesignerUtility
    {
        private static GUIStyle arrowSeparatorGUIStyle = null;
        [NonSerialized]
        private static Dictionary<System.Type, Dictionary<System.Reflection.FieldInfo, bool>> attributeFieldCache = new Dictionary<System.Type, Dictionary<System.Reflection.FieldInfo, bool>>();
        public const int BottomConnectionHeight = 0x10;
        private static Texture2D breakpointTexture = null;
        private static GUIStyle buttonGUIStyle = null;
        private static Regex camelCaseRegex = new Regex("(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
        private static Dictionary<string, string> camelCaseSplit = new Dictionary<string, string>();
        private static Texture2D collapseTaskTexture = null;
        private static Texture2D[] colorSelectorTexture = new Texture2D[9];
        public const int CompactAreaHeight = 0x16;
        private static Texture2D conditionalAbortBothTexture = null;
        private static Texture2D conditionalAbortLowerPriorityTexture = null;
        private static Texture2D conditionalAbortSelfTexture = null;
        public const int ConnectionWidth = 0x2a;
        private static Texture2D contentSeparatorTexture = null;
        private static Texture2D deleteButtonTexture = null;
        private static Texture2D disableTaskTexture = null;
        private static Texture2D docTexture = null;
        private static Texture2D downArrowButtonTexture = null;
        public const int EditorWindowTabHeight = 0x15;
        private static Texture2D enableTaskTexture = null;
        private static Texture2D errorIconTexture = null;
        private static GUIStyle errorListDarkBackground = null;
        private static GUIStyle errorListLightBackground = null;
        private static Texture2D executionFailureRepeatTexture = null;
        private static Texture2D executionFailureTexture = null;
        private static Texture2D executionSuccessRepeatTexture = null;
        private static Texture2D executionSuccessTexture = null;
        private static Texture2D expandTaskTexture = null;
        private static Texture2D gearTexture = null;
        public const float GraphAutoScrollEdgeDistance = 15f;
        public const float GraphAutoScrollEdgeSpeed = 3f;
        private static GUIStyle graphBackgroundGUIStyle = null;
        private static GUIStyle graphStatusGUIStyle = null;
        public const float GraphZoomMax = 1f;
        public const float GraphZoomMin = 0.4f;
        public const float GraphZoomSensitivity = 150f;
        public static Texture2D historyBackwardTexture = null;
        public static Texture2D historyForwardTexture = null;
        public const int IconAreaHeight = 0x34;
        public const int IconBorderSize = 0x2e;
        private static Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
        public const int IconSize = 0x2c;
        private static Texture2D identifyButtonTexture = null;
        public const int IdentifyUpdateFadeTime = 500;
        public const float InterruptTaskHighlightDuration = 0.75f;
        private static GUIStyle labelWrapGUIStyle = null;
        public const int LineSelectionThreshold = 7;
        public const int MajorGridTickSpacing = 50;
        public const int MaxCommentHeight = 100;
        public const int MaxIdentifyUpdateCount = 0x7d0;
        public const int MaxTaskDescriptionBoxHeight = 300;
        public const int MaxTaskDescriptionBoxWidth = 400;
        public const int MaxWidth = 220;
        public const int MinorGridTickSpacing = 10;
        public const int MinWidth = 100;
        public const float NodeFadeDuration = 0.5f;
        private static Texture2D pauseTexture = null;
        private static GUIStyle plainButtonGUIStyle = null;
        private static GUIStyle plainTextureGUIStyle = null;
        private static Texture2D playTexture = null;
        private static GUIStyle preferencesPaneGUIStyle = null;
        public const int PreferencesPaneHeight = 0x15c;
        public const int PreferencesPaneWidth = 290;
        private static GUIStyle propertyBoxGUIStyle = null;
        public const int PropertyBoxWidth = 300;
        private static Texture2D referencedTexture = null;
        public const int RepaintGUICount = 1;
        private static Texture2D screenshotBackgroundTexture = null;
        public const int ScrollBarSize = 15;
        private static GUIStyle selectedBackgroundGUIStyle = null;
        private static GUIStyle selectionGUIStyle = null;
        private static GUIStyle sharedVariableToolbarPopup = null;
        private static Texture2D smallErrorIconTexture = null;
        private static Texture2D stepTexture = null;
        public const int TaskBackgroundShadowSize = 3;
        private static Texture2D taskBorderIdentifyTexture = null;
        private static Texture2D taskBorderRunningTexture = null;
        private static Texture2D[] taskBorderTexture = new Texture2D[9];
        private static GUIStyle taskCommentGUIStyle = null;
        private static GUIStyle taskCommentLeftAlignGUIStyle = null;
        private static GUIStyle taskCommentRightAlignGUIStyle = null;
        private static GUIStyle[] taskCompactGUIStyle = new GUIStyle[9];
        private static Texture2D[] taskConnectionBottomTexture = new Texture2D[9];
        public const int TaskConnectionCollapsedHeight = 6;
        private static Texture2D taskConnectionCollapsedTexture = null;
        public const int TaskConnectionCollapsedWidth = 0x1a;
        private static Texture2D taskConnectionIdentifyBottomTexture = null;
        private static Texture2D taskConnectionIdentifyTopTexture = null;
        private static Texture2D taskConnectionRunningBottomTexture = null;
        private static Texture2D taskConnectionRunningTopTexture = null;
        private static Texture2D[] taskConnectionTopTexture = new Texture2D[9];
        private static GUIStyle taskDescriptionGUIStyle = null;
        private static GUIStyle taskFoldoutGUIStyle = null;
        private static GUIStyle[] taskGUIStyle = new GUIStyle[9];
        private static GUIStyle taskHighlightCompactGUIStyle = null;
        private static GUIStyle taskHighlightGUIStyle = null;
        private static GUIStyle taskIdentifyCompactGUIStyle = null;
        private static GUIStyle taskIdentifyGUIStyle = null;
        private static GUIStyle taskIdentifySelectedCompactGUIStyle = null;
        private static GUIStyle taskIdentifySelectedGUIStyle = null;
        private static GUIStyle taskInspectorCommentGUIStyle = null;
        private static GUIStyle taskInspectorGUIStyle = null;
        public const int TaskPropertiesLabelWidth = 150;
        private static GUIStyle taskRunningCompactGUIStyle = null;
        private static GUIStyle taskRunningGUIStyle = null;
        private static GUIStyle taskRunningSelectedCompactGUIStyle = null;
        private static GUIStyle taskRunningSelectedGUIStyle = null;
        private static GUIStyle[] taskSelectedCompactGUIStyle = new GUIStyle[9];
        private static GUIStyle[] taskSelectedGUIStyle = new GUIStyle[9];
        private static GUIStyle taskTitleGUIStyle = null;
        public const int TextPadding = 20;
        private static Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        public const int TitleCompactHeight = 0x1c;
        public const int TitleHeight = 20;
        private static GUIStyle tolbarButtonLeftAlignGUIStyle = null;
        private static GUIStyle toolbarButtonSelectionGUIStyle = null;
        public const int ToolBarHeight = 0x12;
        private static GUIStyle toolbarLabelGUIStyle = null;
        public const int TopConnectionHeight = 14;
        private static GUIStyle transparentButtonGUIStyle = null;
        private static GUIStyle transparentButtonOffsetGUIStyle = null;
        private static Texture2D upArrowButtonTexture = null;
        public const float UpdateCheckInterval = 1f;
        private static Texture2D variableButtonSelectedTexture = null;
        private static Texture2D variableButtonTexture = null;
        private static Texture2D variableDeleteButtonTexture = null;
        private static Texture2D variableMapButtonTexture = null;
        private static Texture2D variableWatchButtonSelectedTexture = null;
        private static Texture2D variableWatchButtonTexture = null;
        public const string Version = "1.5.5";
        private static GUIStyle welcomeScreenIntroGUIStyle = null;
        private static GUIStyle welcomeScreenTextDescriptionGUIStyle = null;
        private static GUIStyle welcomeScreenTextHeaderGUIStyle = null;

        public static bool AnyNullTasks(BehaviorSource behaviorSource)
        {
            if ((behaviorSource.RootTask != null) && AnyNullTasks(behaviorSource.RootTask))
            {
                return true;
            }
            if (behaviorSource.DetachedTasks != null)
            {
                for (int i = 0; i < behaviorSource.DetachedTasks.Count; i++)
                {
                    if (AnyNullTasks(behaviorSource.DetachedTasks[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool AnyNullTasks(Task task)
        {
            ParentTask task2;
            if (task == null)
            {
                return true;
            }
            if (((task2 = task as ParentTask) != null) && (task2.Children != null))
            {
                for (int i = 0; i < task2.Children.Count; i++)
                {
                    if (AnyNullTasks(task2.Children[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string ColorIndexToColorString(int index)
        {
            if (index != 0)
            {
                if (index == 1)
                {
                    return "Red";
                }
                if (index == 2)
                {
                    return "Pink";
                }
                if (index == 3)
                {
                    return "Brown";
                }
                if (index == 4)
                {
                    return "RedOrange";
                }
                if (index == 5)
                {
                    return "Turquoise";
                }
                if (index == 6)
                {
                    return "Cyan";
                }
                if (index == 7)
                {
                    return "Blue";
                }
                if (index == 8)
                {
                    return "Purple";
                }
            }
            return string.Empty;
        }

        public static Texture2D ColorSelectorTexture(int colorIndex)
        {
            if (colorSelectorTexture[colorIndex] == null)
            {
                InitColorSelectorTexture(colorIndex);
            }
            return colorSelectorTexture[colorIndex];
        }

        public static void DrawContentSeperator(int yOffset)
        {
            DrawContentSeperator(yOffset, 0);
        }

        public static void DrawContentSeperator(int yOffset, int widthExtension)
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.x = -5f;
            lastRect.y += lastRect.height + yOffset;
            lastRect.height = 2f;
            lastRect.width += 10 + widthExtension;
            GUI.DrawTexture(lastRect, ContentSeparatorTexture);
        }

        public static List<Task> GetAllTasks(BehaviorSource behaviorSource)
        {
            List<Task> taskList = new List<Task>();
            if (behaviorSource.RootTask != null)
            {
                GetAllTasks(behaviorSource.RootTask, ref taskList);
            }
            if (behaviorSource.DetachedTasks != null)
            {
                for (int i = 0; i < behaviorSource.DetachedTasks.Count; i++)
                {
                    GetAllTasks(behaviorSource.DetachedTasks[i], ref taskList);
                }
            }
            return taskList;
        }

        private static void GetAllTasks(Task task, ref List<Task> taskList)
        {
            ParentTask task2;
            taskList.Add(task);
            if (((task2 = task as ParentTask) != null) && (task2.Children != null))
            {
                for (int i = 0; i < task2.Children.Count; i++)
                {
                    GetAllTasks(task2.Children[i], ref taskList);
                }
            }
        }

        public static string GetEditorBaseDirectory(UnityEngine.Object obj = null)
        {
            return Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path).Substring(Application.dataPath.Length - 6));
        }

        public static Texture2D GetTaskBorderTexture(int colorIndex)
        {
            if (taskBorderTexture[colorIndex] == null)
            {
                InitTaskBorderTexture(colorIndex);
            }
            return taskBorderTexture[colorIndex];
        }

        public static GUIStyle GetTaskCompactGUIStyle(int colorIndex)
        {
            if (taskCompactGUIStyle[colorIndex] == null)
            {
                InitTaskCompactGUIStyle(colorIndex);
            }
            return taskCompactGUIStyle[colorIndex];
        }

        public static Texture2D GetTaskConnectionBottomTexture(int colorIndex)
        {
            if (taskConnectionBottomTexture[colorIndex] == null)
            {
                InitTaskConnectionBottomTexture(colorIndex);
            }
            return taskConnectionBottomTexture[colorIndex];
        }

        public static Texture2D GetTaskConnectionTopTexture(int colorIndex)
        {
            if (taskConnectionTopTexture[colorIndex] == null)
            {
                InitTaskConnectionTopTexture(colorIndex);
            }
            return taskConnectionTopTexture[colorIndex];
        }

        public static GUIStyle GetTaskGUIStyle(int colorIndex)
        {
            if (taskGUIStyle[colorIndex] == null)
            {
                InitTaskGUIStyle(colorIndex);
            }
            return taskGUIStyle[colorIndex];
        }

        public static GUIStyle GetTaskSelectedCompactGUIStyle(int colorIndex)
        {
            if (taskSelectedCompactGUIStyle[colorIndex] == null)
            {
                InitTaskSelectedCompactGUIStyle(colorIndex);
            }
            return taskSelectedCompactGUIStyle[colorIndex];
        }

        public static GUIStyle GetTaskSelectedGUIStyle(int colorIndex)
        {
            if (taskSelectedGUIStyle[colorIndex] == null)
            {
                InitTaskSelectedGUIStyle(colorIndex);
            }
            return taskSelectedGUIStyle[colorIndex];
        }

        public static bool HasAttribute(System.Reflection.FieldInfo field, System.Type attributeType)
        {
            Dictionary<System.Reflection.FieldInfo, bool> dictionary = null;
            if (attributeFieldCache.ContainsKey(attributeType))
            {
                dictionary = attributeFieldCache[attributeType];
            }
            if (dictionary == null)
            {
                dictionary = new Dictionary<System.Reflection.FieldInfo, bool>();
            }
            if (dictionary.ContainsKey(field))
            {
                return dictionary[field];
            }
            bool flag = field.GetCustomAttributes(attributeType, false).Length > 0;
            dictionary.Add(field, flag);
            if (!attributeFieldCache.ContainsKey(attributeType))
            {
                attributeFieldCache.Add(attributeType, dictionary);
            }
            return flag;
        }

        public static bool HasRootTask(string serialization)
        {
            if (string.IsNullOrEmpty(serialization))
            {
                return false;
            }
            Dictionary<string, object> dictionary = MiniJSON.Deserialize(serialization) as Dictionary<string, object>;
            return ((dictionary != null) && dictionary.ContainsKey("RootTask"));
        }

        private static void InitArrowSeparatorGUIStyle()
        {
            arrowSeparatorGUIStyle = new GUIStyle();
            arrowSeparatorGUIStyle.border = new RectOffset(0, 0, 0, 0);
            arrowSeparatorGUIStyle.margin = new RectOffset(0, 0, 3, 0);
            arrowSeparatorGUIStyle.padding = new RectOffset(0, 0, 0, 0);
            Texture2D textured = LoadTexture("ArrowSeparator.png", true, null);
            arrowSeparatorGUIStyle.normal.background = textured;
            arrowSeparatorGUIStyle.active.background = textured;
            arrowSeparatorGUIStyle.hover.background = textured;
            arrowSeparatorGUIStyle.focused.background = textured;
        }

        private static void InitBreakpointTexture()
        {
            breakpointTexture = LoadTexture("BreakpointIcon.png", false, null);
        }

        private static void InitButtonGUIStyle()
        {
            buttonGUIStyle = new GUIStyle(GUI.skin.button);
            buttonGUIStyle.margin = new RectOffset(0, 0, 2, 2);
            buttonGUIStyle.padding = new RectOffset(0, 0, 1, 1);
        }

        private static void InitCollapseTaskTexture()
        {
            collapseTaskTexture = LoadTexture("TaskCollapseIcon.png", false, null);
        }

        private static void InitColorSelectorTexture(int colorIndex)
        {
            colorSelectorTexture[colorIndex] = LoadTexture("ColorSelector" + ColorIndexToColorString(colorIndex) + ".png", true, null);
        }

        private static void InitConditionalAbortBothTexture()
        {
            conditionalAbortBothTexture = LoadTexture("ConditionalAbortBothIcon.png", true, null);
        }

        private static void InitConditionalAbortLowerPriorityTexture()
        {
            conditionalAbortLowerPriorityTexture = LoadTexture("ConditionalAbortLowerPriorityIcon.png", true, null);
        }

        private static void InitConditionalAbortSelfTexture()
        {
            conditionalAbortSelfTexture = LoadTexture("ConditionalAbortSelfIcon.png", true, null);
        }

        private static void InitContentSeparatorTexture()
        {
            contentSeparatorTexture = LoadTexture("ContentSeparator.png", true, null);
        }

        private static void InitDeleteButtonTexture()
        {
            deleteButtonTexture = LoadTexture("DeleteButton.png", true, null);
        }

        private static void InitDisableTaskTexture()
        {
            disableTaskTexture = LoadTexture("TaskDisableIcon.png", false, null);
        }

        private static void InitDocTexture()
        {
            docTexture = LoadTexture("DocIcon.png", true, null);
        }

        private static void InitDownArrowButtonTexture()
        {
            downArrowButtonTexture = LoadTexture("DownArrowButton.png", true, null);
        }

        private static void InitEnableTaskTexture()
        {
            enableTaskTexture = LoadTexture("TaskEnableIcon.png", false, null);
        }

        private static void InitErrorIconTexture()
        {
            errorIconTexture = LoadTexture("ErrorIcon.png", true, null);
        }

        private static void InitErrorListDarkBackground()
        {
            Texture2D textured = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            Color color = !EditorGUIUtility.isProSkin ? new Color(0.706f, 0.706f, 0.706f) : new Color(0.2f, 0.2f, 0.2f, 1f);
            textured.SetPixel(1, 1, color);
            textured.hideFlags = HideFlags.HideAndDontSave;
            textured.Apply();
            errorListDarkBackground = new GUIStyle();
            errorListDarkBackground.padding = new RectOffset(2, 0, 2, 0);
            errorListDarkBackground.normal.background = textured;
            errorListDarkBackground.active.background = textured;
            errorListDarkBackground.hover.background = textured;
            errorListDarkBackground.focused.background = textured;
            errorListDarkBackground.normal.textColor = !EditorGUIUtility.isProSkin ? new Color(0.206f, 0.206f, 0.206f) : new Color(0.706f, 0.706f, 0.706f);
            errorListDarkBackground.alignment = TextAnchor.UpperLeft;
            errorListDarkBackground.wordWrap = false;
        }

        private static void InitErrorListLightBackground()
        {
            errorListLightBackground = new GUIStyle();
            errorListLightBackground.padding = new RectOffset(2, 0, 2, 0);
            errorListLightBackground.normal.textColor = !EditorGUIUtility.isProSkin ? new Color(0.106f, 0.106f, 0.106f) : new Color(0.706f, 0.706f, 0.706f);
            errorListLightBackground.alignment = TextAnchor.UpperLeft;
            errorListLightBackground.wordWrap = false;
        }

        private static void InitExecutionFailureRepeatTexture()
        {
            executionFailureRepeatTexture = LoadTexture("ExecutionFailureRepeat.png", false, null);
        }

        private static void InitExecutionFailureTexture()
        {
            executionFailureTexture = LoadTexture("ExecutionFailure.png", false, null);
        }

        private static void InitExecutionSuccessRepeatTexture()
        {
            executionSuccessRepeatTexture = LoadTexture("ExecutionSuccessRepeat.png", false, null);
        }

        private static void InitExecutionSuccessTexture()
        {
            executionSuccessTexture = LoadTexture("ExecutionSuccess.png", false, null);
        }

        private static void InitExpandTaskTexture()
        {
            expandTaskTexture = LoadTexture("TaskExpandIcon.png", false, null);
        }

        private static void InitGearTexture()
        {
            gearTexture = LoadTexture("GearIcon.png", true, null);
        }

        private static void InitGraphBackgroundGUIStyle()
        {
            Texture2D textured = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            if (EditorGUIUtility.isProSkin)
            {
                textured.SetPixel(1, 1, new Color(0.1647f, 0.1647f, 0.1647f));
            }
            else
            {
                textured.SetPixel(1, 1, new Color(0.3647f, 0.3647f, 0.3647f));
            }
            textured.hideFlags = HideFlags.HideAndDontSave;
            textured.Apply();
            graphBackgroundGUIStyle = new GUIStyle(GUI.skin.box);
            graphBackgroundGUIStyle.normal.background = textured;
            graphBackgroundGUIStyle.active.background = textured;
            graphBackgroundGUIStyle.hover.background = textured;
            graphBackgroundGUIStyle.focused.background = textured;
            graphBackgroundGUIStyle.normal.textColor = Color.white;
            graphBackgroundGUIStyle.active.textColor = Color.white;
            graphBackgroundGUIStyle.hover.textColor = Color.white;
            graphBackgroundGUIStyle.focused.textColor = Color.white;
        }

        private static void InitGraphStatusGUIStyle()
        {
            graphStatusGUIStyle = new GUIStyle(GUI.skin.label);
            graphStatusGUIStyle.alignment = TextAnchor.MiddleLeft;
            graphStatusGUIStyle.fontSize = 20;
            graphStatusGUIStyle.fontStyle = FontStyle.Bold;
            if (EditorGUIUtility.isProSkin)
            {
                graphStatusGUIStyle.normal.textColor = new Color(0.7058f, 0.7058f, 0.7058f);
            }
            else
            {
                graphStatusGUIStyle.normal.textColor = new Color(0.8058f, 0.8058f, 0.8058f);
            }
        }

        private static void InitHistoryBackwardTexture()
        {
            historyBackwardTexture = LoadTexture("HistoryBackward.png", true, null);
        }

        private static void InitHistoryForwardTexture()
        {
            historyForwardTexture = LoadTexture("HistoryForward.png", true, null);
        }

        private static void InitIdentifyButtonTexture()
        {
            identifyButtonTexture = LoadTexture("IdentifyButton.png", true, null);
        }

        private static void InitLabelWrapGUIStyle()
        {
            labelWrapGUIStyle = new GUIStyle(GUI.skin.label);
            labelWrapGUIStyle.wordWrap = true;
            labelWrapGUIStyle.alignment = TextAnchor.MiddleCenter;
        }

        private static void InitPauseTexture()
        {
            pauseTexture = LoadTexture("Pause.png", true, null);
        }

        private static void InitPlainButtonGUIStyle()
        {
            plainButtonGUIStyle = new GUIStyle(GUI.skin.button);
            plainButtonGUIStyle.border = new RectOffset(0, 0, 0, 0);
            plainButtonGUIStyle.margin = new RectOffset(0, 0, 2, 2);
            plainButtonGUIStyle.padding = new RectOffset(0, 0, 1, 0);
            plainButtonGUIStyle.normal.background = null;
            plainButtonGUIStyle.active.background = null;
            plainButtonGUIStyle.hover.background = null;
            plainButtonGUIStyle.focused.background = null;
            plainButtonGUIStyle.normal.textColor = Color.white;
            plainButtonGUIStyle.active.textColor = Color.white;
            plainButtonGUIStyle.hover.textColor = Color.white;
            plainButtonGUIStyle.focused.textColor = Color.white;
        }

        private static void InitPlainTextureGUIStyle()
        {
            plainTextureGUIStyle = new GUIStyle();
            plainTextureGUIStyle.border = new RectOffset(0, 0, 0, 0);
            plainTextureGUIStyle.margin = new RectOffset(0, 0, 0, 0);
            plainTextureGUIStyle.padding = new RectOffset(0, 0, 0, 0);
            plainTextureGUIStyle.normal.background = null;
            plainTextureGUIStyle.active.background = null;
            plainTextureGUIStyle.hover.background = null;
            plainTextureGUIStyle.focused.background = null;
        }

        private static void InitPlayTexture()
        {
            playTexture = LoadTexture("Play.png", true, null);
        }

        private static void InitPreferencesPaneGUIStyle()
        {
            preferencesPaneGUIStyle = new GUIStyle(GUI.skin.box);
            preferencesPaneGUIStyle.normal.background = EditorStyles.toolbarButton.normal.background;
        }

        private static void InitPropertyBoxGUIStyle()
        {
            propertyBoxGUIStyle = new GUIStyle();
            propertyBoxGUIStyle.padding = new RectOffset(2, 2, 0, 0);
        }

        private static void InitReferencedTexture()
        {
            referencedTexture = LoadTexture("LinkedIcon.png", true, null);
        }

        private static void InitScreenshotBackgroundTexture()
        {
            screenshotBackgroundTexture = new Texture2D(1, 1, TextureFormat.RGB24, false, true);
            if (EditorGUIUtility.isProSkin)
            {
                screenshotBackgroundTexture.SetPixel(1, 1, new Color(0.1647f, 0.1647f, 0.1647f));
            }
            else
            {
                screenshotBackgroundTexture.SetPixel(1, 1, new Color(0.3647f, 0.3647f, 0.3647f));
            }
            screenshotBackgroundTexture.Apply();
        }

        private static void InitSelectedBackgroundGUIStyle()
        {
            Texture2D textured = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            Color color = !EditorGUIUtility.isProSkin ? new Color(0.243f, 0.5686f, 0.839f, 0.5f) : new Color(0.188f, 0.4588f, 0.6862f, 0.5f);
            textured.SetPixel(1, 1, color);
            textured.hideFlags = HideFlags.HideAndDontSave;
            textured.Apply();
            selectedBackgroundGUIStyle = new GUIStyle();
            selectedBackgroundGUIStyle.border = new RectOffset(0, 0, 0, 0);
            selectedBackgroundGUIStyle.margin = new RectOffset(0, 0, -2, 2);
            selectedBackgroundGUIStyle.normal.background = textured;
            selectedBackgroundGUIStyle.active.background = textured;
            selectedBackgroundGUIStyle.hover.background = textured;
            selectedBackgroundGUIStyle.focused.background = textured;
        }

        private static void InitSelectionGUIStyle()
        {
            Texture2D textured = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            Color color = !EditorGUIUtility.isProSkin ? new Color(0.243f, 0.5686f, 0.839f, 0.5f) : new Color(0.188f, 0.4588f, 0.6862f, 0.5f);
            textured.SetPixel(1, 1, color);
            textured.hideFlags = HideFlags.HideAndDontSave;
            textured.Apply();
            selectionGUIStyle = new GUIStyle(GUI.skin.box);
            selectionGUIStyle.normal.background = textured;
            selectionGUIStyle.active.background = textured;
            selectionGUIStyle.hover.background = textured;
            selectionGUIStyle.focused.background = textured;
            selectionGUIStyle.normal.textColor = Color.white;
            selectionGUIStyle.active.textColor = Color.white;
            selectionGUIStyle.hover.textColor = Color.white;
            selectionGUIStyle.focused.textColor = Color.white;
        }

        private static void InitSharedVariableToolbarPopup()
        {
            sharedVariableToolbarPopup = new GUIStyle(EditorStyles.toolbarPopup);
            sharedVariableToolbarPopup.margin = new RectOffset(4, 4, 0, 0);
        }

        private static void InitSmallErrorIconTexture()
        {
            smallErrorIconTexture = LoadTexture("SmallErrorIcon.png", true, null);
        }

        private static void InitStepTexture()
        {
            stepTexture = LoadTexture("Step.png", true, null);
        }

        private static void InitTaskBorderIdentifyTexture()
        {
            taskBorderIdentifyTexture = LoadTaskTexture("TaskBorderIdentify.png", true, null);
        }

        private static void InitTaskBorderRunningTexture()
        {
            taskBorderRunningTexture = LoadTaskTexture("TaskBorderRunning.png", true, null);
        }

        private static void InitTaskBorderTexture(int colorIndex)
        {
            taskBorderTexture[colorIndex] = LoadTaskTexture("TaskBorder" + ColorIndexToColorString(colorIndex) + ".png", true, null);
        }

        private static void InitTaskCommentGUIStyle()
        {
            taskCommentGUIStyle = new GUIStyle(GUI.skin.label);
            taskCommentGUIStyle.alignment = TextAnchor.UpperCenter;
            taskCommentGUIStyle.fontSize = 12;
            taskCommentGUIStyle.fontStyle = FontStyle.Normal;
            taskCommentGUIStyle.wordWrap = true;
        }

        private static void InitTaskCommentLeftAlignGUIStyle()
        {
            taskCommentLeftAlignGUIStyle = new GUIStyle(GUI.skin.label);
            taskCommentLeftAlignGUIStyle.alignment = TextAnchor.UpperLeft;
            taskCommentLeftAlignGUIStyle.fontSize = 12;
            taskCommentLeftAlignGUIStyle.fontStyle = FontStyle.Normal;
            taskCommentLeftAlignGUIStyle.wordWrap = false;
        }

        private static void InitTaskCommentRightAlignGUIStyle()
        {
            taskCommentRightAlignGUIStyle = new GUIStyle(GUI.skin.label);
            taskCommentRightAlignGUIStyle.alignment = TextAnchor.UpperRight;
            taskCommentRightAlignGUIStyle.fontSize = 12;
            taskCommentRightAlignGUIStyle.fontStyle = FontStyle.Normal;
            taskCommentRightAlignGUIStyle.wordWrap = false;
        }

        private static void InitTaskCompactGUIStyle(int colorIndex)
        {
            taskCompactGUIStyle[colorIndex] = InitTaskGUIStyle(LoadTaskTexture("TaskCompact" + ColorIndexToColorString(colorIndex) + ".png", true, null), new RectOffset(5, 4, 4, 5));
        }

        private static void InitTaskConnectionBottomTexture(int colorIndex)
        {
            taskConnectionBottomTexture[colorIndex] = LoadTaskTexture("TaskConnectionBottom" + ColorIndexToColorString(colorIndex) + ".png", true, null);
        }

        private static void InitTaskConnectionCollapsedTexture()
        {
            taskConnectionCollapsedTexture = LoadTexture("TaskConnectionCollapsed.png", true, null);
        }

        private static void InitTaskConnectionIdentifyBottomTexture()
        {
            taskConnectionIdentifyBottomTexture = LoadTaskTexture("TaskConnectionIdentifyBottom.png", true, null);
        }

        private static void InitTaskConnectionIdentifyTopTexture()
        {
            taskConnectionIdentifyTopTexture = LoadTaskTexture("TaskConnectionIdentifyTop.png", true, null);
        }

        private static void InitTaskConnectionRunningBottomTexture()
        {
            taskConnectionRunningBottomTexture = LoadTaskTexture("TaskConnectionRunningBottom.png", true, null);
        }

        private static void InitTaskConnectionRunningTopTexture()
        {
            taskConnectionRunningTopTexture = LoadTaskTexture("TaskConnectionRunningTop.png", true, null);
        }

        private static void InitTaskConnectionTopTexture(int colorIndex)
        {
            taskConnectionTopTexture[colorIndex] = LoadTaskTexture("TaskConnectionTop" + ColorIndexToColorString(colorIndex) + ".png", true, null);
        }

        private static void InitTaskDescriptionGUIStyle()
        {
            Texture2D textured = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            if (EditorGUIUtility.isProSkin)
            {
                textured.SetPixel(1, 1, new Color(0.1647f, 0.1647f, 0.1647f));
            }
            else
            {
                textured.SetPixel(1, 1, new Color(0.75f, 0.75f, 0.75f));
            }
            textured.hideFlags = HideFlags.HideAndDontSave;
            textured.Apply();
            taskDescriptionGUIStyle = new GUIStyle(GUI.skin.box);
            taskDescriptionGUIStyle.normal.background = textured;
            taskDescriptionGUIStyle.active.background = textured;
            taskDescriptionGUIStyle.hover.background = textured;
            taskDescriptionGUIStyle.focused.background = textured;
        }

        private static void InitTaskFoldoutGUIStyle()
        {
            taskFoldoutGUIStyle = new GUIStyle(EditorStyles.foldout);
            taskFoldoutGUIStyle.alignment = TextAnchor.MiddleLeft;
            taskFoldoutGUIStyle.fontSize = 13;
            taskFoldoutGUIStyle.fontStyle = FontStyle.Bold;
        }

        private static void InitTaskGUIStyle(int colorIndex)
        {
            taskGUIStyle[colorIndex] = InitTaskGUIStyle(LoadTaskTexture("Task" + ColorIndexToColorString(colorIndex) + ".png", true, null), new RectOffset(5, 3, 3, 5));
        }

        private static GUIStyle InitTaskGUIStyle(Texture2D texture, RectOffset overflow)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box) {
                border = new RectOffset(10, 10, 10, 10),
                overflow = overflow
            };
            style.normal.background = texture;
            style.active.background = texture;
            style.hover.background = texture;
            style.focused.background = texture;
            style.normal.textColor = Color.white;
            style.active.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.focused.textColor = Color.white;
            style.stretchHeight = true;
            style.stretchWidth = true;
            return style;
        }

        private static void InitTaskHighlightCompactGUIStyle()
        {
            taskHighlightCompactGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskHighlightCompact.png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskHighlightGUIStyle()
        {
            taskHighlightGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskHighlight.png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskIdentifyCompactGUIStyle()
        {
            taskIdentifyCompactGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskIdentifyCompact.png", true, null), new RectOffset(5, 4, 4, 5));
        }

        private static void InitTaskIdentifyGUIStyle()
        {
            taskIdentifyGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskIdentify.png", true, null), new RectOffset(5, 3, 3, 5));
        }

        private static void InitTaskIdentifySelectedCompactGUIStyle()
        {
            taskIdentifySelectedCompactGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskIdentifySelectedCompact.png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskIdentifySelectedGUIStyle()
        {
            taskIdentifySelectedGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskIdentifySelected.png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskInspectorCommentGUIStyle()
        {
            taskInspectorCommentGUIStyle = new GUIStyle(GUI.skin.textArea);
            taskInspectorCommentGUIStyle.wordWrap = true;
        }

        private static void InitTaskInspectorGUIStyle()
        {
            taskInspectorGUIStyle = new GUIStyle(GUI.skin.label);
            taskInspectorGUIStyle.alignment = TextAnchor.MiddleLeft;
            taskInspectorGUIStyle.fontSize = 11;
            taskInspectorGUIStyle.fontStyle = FontStyle.Normal;
        }

        private static void InitTaskRunningCompactGUIStyle()
        {
            taskRunningCompactGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskRunningCompact.png", true, null), new RectOffset(5, 4, 4, 5));
        }

        private static void InitTaskRunningGUIStyle()
        {
            taskRunningGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskRunning.png", true, null), new RectOffset(5, 3, 3, 5));
        }

        private static void InitTaskRunningSelectedCompactGUIStyle()
        {
            taskRunningSelectedCompactGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskRunningSelectedCompact.png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskRunningSelectedGUIStyle()
        {
            taskRunningSelectedGUIStyle = InitTaskGUIStyle(LoadTaskTexture("TaskRunningSelected.png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskSelectedCompactGUIStyle(int colorIndex)
        {
            taskSelectedCompactGUIStyle[colorIndex] = InitTaskGUIStyle(LoadTaskTexture("TaskSelectedCompact" + ColorIndexToColorString(colorIndex) + ".png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskSelectedGUIStyle(int colorIndex)
        {
            taskSelectedGUIStyle[colorIndex] = InitTaskGUIStyle(LoadTaskTexture("TaskSelected" + ColorIndexToColorString(colorIndex) + ".png", true, null), new RectOffset(5, 4, 4, 4));
        }

        private static void InitTaskTitleGUIStyle()
        {
            taskTitleGUIStyle = new GUIStyle(GUI.skin.label);
            taskTitleGUIStyle.alignment = TextAnchor.UpperCenter;
            taskTitleGUIStyle.fontSize = 12;
            taskTitleGUIStyle.fontStyle = FontStyle.Normal;
        }

        private static void InitToolbarButtonLeftAlignGUIStyle()
        {
            tolbarButtonLeftAlignGUIStyle = new GUIStyle(EditorStyles.toolbarButton);
            tolbarButtonLeftAlignGUIStyle.alignment = TextAnchor.MiddleLeft;
        }

        private static void InitToolbarButtonSelectionGUIStyle()
        {
            toolbarButtonSelectionGUIStyle = new GUIStyle(EditorStyles.toolbarButton);
            toolbarButtonSelectionGUIStyle.normal.background = toolbarButtonSelectionGUIStyle.active.background;
        }

        private static void InitToolbarLabelGUIStyle()
        {
            toolbarLabelGUIStyle = new GUIStyle(EditorStyles.label);
            toolbarLabelGUIStyle.normal.textColor = !EditorGUIUtility.isProSkin ? new Color(0f, 0.5f, 0f) : new Color(0f, 0.7f, 0f);
        }

        private static void InitTransparentButtonGUIStyle()
        {
            transparentButtonGUIStyle = new GUIStyle(GUI.skin.button);
            transparentButtonGUIStyle.border = new RectOffset(0, 0, 0, 0);
            transparentButtonGUIStyle.margin = new RectOffset(4, 4, 2, 2);
            transparentButtonGUIStyle.padding = new RectOffset(2, 2, 1, 0);
            transparentButtonGUIStyle.normal.background = null;
            transparentButtonGUIStyle.active.background = null;
            transparentButtonGUIStyle.hover.background = null;
            transparentButtonGUIStyle.focused.background = null;
            transparentButtonGUIStyle.normal.textColor = Color.white;
            transparentButtonGUIStyle.active.textColor = Color.white;
            transparentButtonGUIStyle.hover.textColor = Color.white;
            transparentButtonGUIStyle.focused.textColor = Color.white;
        }

        private static void InitTransparentButtonOffsetGUIStyle()
        {
            transparentButtonOffsetGUIStyle = new GUIStyle(GUI.skin.button);
            transparentButtonOffsetGUIStyle.border = new RectOffset(0, 0, 0, 0);
            transparentButtonOffsetGUIStyle.margin = new RectOffset(4, 4, 4, 2);
            transparentButtonOffsetGUIStyle.padding = new RectOffset(2, 2, 1, 0);
            transparentButtonOffsetGUIStyle.normal.background = null;
            transparentButtonOffsetGUIStyle.active.background = null;
            transparentButtonOffsetGUIStyle.hover.background = null;
            transparentButtonOffsetGUIStyle.focused.background = null;
            transparentButtonOffsetGUIStyle.normal.textColor = Color.white;
            transparentButtonOffsetGUIStyle.active.textColor = Color.white;
            transparentButtonOffsetGUIStyle.hover.textColor = Color.white;
            transparentButtonOffsetGUIStyle.focused.textColor = Color.white;
        }

        private static void InitUpArrowButtonTexture()
        {
            upArrowButtonTexture = LoadTexture("UpArrowButton.png", true, null);
        }

        private static void InitVariableButtonSelectedTexture()
        {
            variableButtonSelectedTexture = LoadTexture("VariableButtonSelected.png", true, null);
        }

        private static void InitVariableButtonTexture()
        {
            variableButtonTexture = LoadTexture("VariableButton.png", true, null);
        }

        private static void InitVariableDeleteButtonTexture()
        {
            variableDeleteButtonTexture = LoadTexture("VariableDeleteButton.png", true, null);
        }

        private static void InitVariableMapButtonTexture()
        {
            variableMapButtonTexture = LoadTexture("VariableMapButton.png", true, null);
        }

        private static void InitVariableWatchButtonSelectedTexture()
        {
            variableWatchButtonSelectedTexture = LoadTexture("VariableWatchButtonSelected.png", true, null);
        }

        private static void InitVariableWatchButtonTexture()
        {
            variableWatchButtonTexture = LoadTexture("VariableWatchButton.png", true, null);
        }

        private static void InitWelcomeScreenIntroGUIStyle()
        {
            welcomeScreenIntroGUIStyle = new GUIStyle(GUI.skin.label);
            welcomeScreenIntroGUIStyle.fontSize = 0x10;
            welcomeScreenIntroGUIStyle.fontStyle = FontStyle.Bold;
            welcomeScreenIntroGUIStyle.normal.textColor = new Color(0.706f, 0.706f, 0.706f);
        }

        private static void InitWelcomeScreenTextDescriptionGUIStyle()
        {
            welcomeScreenTextDescriptionGUIStyle = new GUIStyle(GUI.skin.label);
            welcomeScreenTextDescriptionGUIStyle.wordWrap = true;
        }

        private static void InitWelcomeScreenTextHeaderGUIStyle()
        {
            welcomeScreenTextHeaderGUIStyle = new GUIStyle(GUI.skin.label);
            welcomeScreenTextHeaderGUIStyle.alignment = TextAnchor.MiddleLeft;
            welcomeScreenTextHeaderGUIStyle.fontSize = 14;
            welcomeScreenTextHeaderGUIStyle.fontStyle = FontStyle.Bold;
        }

        public static Texture2D LoadIcon(string iconName, ScriptableObject obj = null)
        {
            if (iconCache.ContainsKey(iconName))
            {
                return iconCache[iconName];
            }
            Texture2D textured = null;
            string name = iconName.Replace("{SkinColor}", !EditorGUIUtility.isProSkin ? "Light" : "Dark");
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (manifestResourceStream == null)
            {
                name = string.Format("BehaviorDesignerEditor.Resources.{0}", iconName.Replace("{SkinColor}", !EditorGUIUtility.isProSkin ? "Light" : "Dark"));
                manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            }
            if (manifestResourceStream != null)
            {
                textured = new Texture2D(0, 0, TextureFormat.RGBA32, false, true);
                ImageConversion.LoadImage(textured, ReadToEnd(manifestResourceStream));
                manifestResourceStream.Close();
            }
            if (textured == null)
            {
                textured = AssetDatabase.LoadAssetAtPath(iconName.Replace("{SkinColor}", !EditorGUIUtility.isProSkin ? "Light" : "Dark"), typeof(Texture2D)) as Texture2D;
            }
            if (textured != null)
            {
                textured.hideFlags = HideFlags.HideAndDontSave;
            }
            iconCache.Add(iconName, textured);
            return textured;
        }

        private static Texture2D LoadTaskTexture(string imageName, bool useSkinColor = true, ScriptableObject obj = null)
        {
            if (textureCache.ContainsKey(imageName))
            {
                return textureCache[imageName];
            }
            Texture2D textured = null;
            string name = string.Format("{0}{1}", !useSkinColor ? string.Empty : (!EditorGUIUtility.isProSkin ? "Light" : "Dark"), imageName);
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (manifestResourceStream == null)
            {
                name = string.Format("BehaviorDesignerEditor.Resources.{0}{1}", !useSkinColor ? string.Empty : (!EditorGUIUtility.isProSkin ? "Light" : "Dark"), imageName);
                manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            }
            if (manifestResourceStream != null)
            {
                textured = new Texture2D(0, 0, TextureFormat.RGBA32, false, true);
                ImageConversion.LoadImage(textured, ReadToEnd(manifestResourceStream));
                manifestResourceStream.Close();
            }
            if (textured == null)
            {
                Debug.Log(string.Format("{0}/Images/Task Backgrounds/{1}{2}", GetEditorBaseDirectory(obj), !useSkinColor ? string.Empty : (!EditorGUIUtility.isProSkin ? "Light" : "Dark"), imageName));
            }
            textured.hideFlags = HideFlags.HideAndDontSave;
            textureCache.Add(imageName, textured);
            return textured;
        }

        public static Texture2D LoadTexture(string imageName, bool useSkinColor = true, UnityEngine.Object obj = null)
        {
            if (textureCache.ContainsKey(imageName))
            {
                return textureCache[imageName];
            }
            Texture2D textured = null;
            string name = string.Format("{0}{1}", !useSkinColor ? string.Empty : (!EditorGUIUtility.isProSkin ? "Light" : "Dark"), imageName);
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (manifestResourceStream == null)
            {
                name = string.Format("BehaviorDesignerEditor.Resources.{0}{1}", !useSkinColor ? string.Empty : (!EditorGUIUtility.isProSkin ? "Light" : "Dark"), imageName);
                manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            }
            if (manifestResourceStream != null)
            {
                textured = new Texture2D(0, 0, TextureFormat.RGBA32, false, true);
                ImageConversion.LoadImage(textured, ReadToEnd(manifestResourceStream));
                manifestResourceStream.Close();
            }
            textured.hideFlags = HideFlags.HideAndDontSave;
            textureCache.Add(imageName, textured);
            return textured;
        }

        private static byte[] ReadToEnd(Stream stream)
        {
            byte[] buffer = new byte[0x4000];
            using (MemoryStream stream2 = new MemoryStream())
            {
                int num;
                while ((num = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream2.Write(buffer, 0, num);
                }
                return stream2.ToArray();
            }
        }

        public static float RoundToNearest(float num, float baseNum)
        {
            return (((int) Math.Round((double) (num / baseNum), MidpointRounding.AwayFromZero)) * baseNum);
        }

        public static string SplitCamelCase(string s)
        {
            if (!s.Equals(string.Empty))
            {
                if (camelCaseSplit.ContainsKey(s))
                {
                    return camelCaseSplit[s];
                }
                string key = s;
                s = s.Replace("_uScript", "uScript");
                s = s.Replace("_PlayMaker", "PlayMaker");
                if ((s.Length > 2) && (s.Substring(0, 2).CompareTo("m_") == 0))
                {
                    s = s.Substring(2, s.Length - 2);
                }
                s = camelCaseRegex.Replace(s, " ");
                s = s.Replace("_", " ");
                s = s.Replace("u Script", " uScript");
                s = s.Replace("Play Maker", "PlayMaker");
                s = (char.ToUpper(s[0]) + s.Substring(1)).Trim();
                camelCaseSplit.Add(key, s);
            }
            return s;
        }

        public static GUIStyle ArrowSeparatorGUIStyle
        {
            get
            {
                if (arrowSeparatorGUIStyle == null)
                {
                    InitArrowSeparatorGUIStyle();
                }
                return arrowSeparatorGUIStyle;
            }
        }

        public static Texture2D BreakpointTexture
        {
            get
            {
                if (breakpointTexture == null)
                {
                    InitBreakpointTexture();
                }
                return breakpointTexture;
            }
        }

        public static GUIStyle ButtonGUIStyle
        {
            get
            {
                if (buttonGUIStyle == null)
                {
                    InitButtonGUIStyle();
                }
                return buttonGUIStyle;
            }
        }

        public static Texture2D CollapseTaskTexture
        {
            get
            {
                if (collapseTaskTexture == null)
                {
                    InitCollapseTaskTexture();
                }
                return collapseTaskTexture;
            }
        }

        public static Texture2D ConditionalAbortBothTexture
        {
            get
            {
                if (conditionalAbortBothTexture == null)
                {
                    InitConditionalAbortBothTexture();
                }
                return conditionalAbortBothTexture;
            }
        }

        public static Texture2D ConditionalAbortLowerPriorityTexture
        {
            get
            {
                if (conditionalAbortLowerPriorityTexture == null)
                {
                    InitConditionalAbortLowerPriorityTexture();
                }
                return conditionalAbortLowerPriorityTexture;
            }
        }

        public static Texture2D ConditionalAbortSelfTexture
        {
            get
            {
                if (conditionalAbortSelfTexture == null)
                {
                    InitConditionalAbortSelfTexture();
                }
                return conditionalAbortSelfTexture;
            }
        }

        public static Texture2D ContentSeparatorTexture
        {
            get
            {
                if (contentSeparatorTexture == null)
                {
                    InitContentSeparatorTexture();
                }
                return contentSeparatorTexture;
            }
        }

        public static Texture2D DeleteButtonTexture
        {
            get
            {
                if (deleteButtonTexture == null)
                {
                    InitDeleteButtonTexture();
                }
                return deleteButtonTexture;
            }
        }

        public static Texture2D DisableTaskTexture
        {
            get
            {
                if (disableTaskTexture == null)
                {
                    InitDisableTaskTexture();
                }
                return disableTaskTexture;
            }
        }

        public static Texture2D DocTexture
        {
            get
            {
                if (docTexture == null)
                {
                    InitDocTexture();
                }
                return docTexture;
            }
        }

        public static Texture2D DownArrowButtonTexture
        {
            get
            {
                if (downArrowButtonTexture == null)
                {
                    InitDownArrowButtonTexture();
                }
                return downArrowButtonTexture;
            }
        }

        public static Texture2D EnableTaskTexture
        {
            get
            {
                if (enableTaskTexture == null)
                {
                    InitEnableTaskTexture();
                }
                return enableTaskTexture;
            }
        }

        public static Texture2D ErrorIconTexture
        {
            get
            {
                if (errorIconTexture == null)
                {
                    InitErrorIconTexture();
                }
                return errorIconTexture;
            }
        }

        public static GUIStyle ErrorListDarkBackground
        {
            get
            {
                if (errorListDarkBackground == null)
                {
                    InitErrorListDarkBackground();
                }
                return errorListDarkBackground;
            }
        }

        public static GUIStyle ErrorListLightBackground
        {
            get
            {
                if (errorListLightBackground == null)
                {
                    InitErrorListLightBackground();
                }
                return errorListLightBackground;
            }
        }

        public static Texture2D ExecutionFailureRepeatTexture
        {
            get
            {
                if (executionFailureRepeatTexture == null)
                {
                    InitExecutionFailureRepeatTexture();
                }
                return executionFailureRepeatTexture;
            }
        }

        public static Texture2D ExecutionFailureTexture
        {
            get
            {
                if (executionFailureTexture == null)
                {
                    InitExecutionFailureTexture();
                }
                return executionFailureTexture;
            }
        }

        public static Texture2D ExecutionSuccessRepeatTexture
        {
            get
            {
                if (executionSuccessRepeatTexture == null)
                {
                    InitExecutionSuccessRepeatTexture();
                }
                return executionSuccessRepeatTexture;
            }
        }

        public static Texture2D ExecutionSuccessTexture
        {
            get
            {
                if (executionSuccessTexture == null)
                {
                    InitExecutionSuccessTexture();
                }
                return executionSuccessTexture;
            }
        }

        public static Texture2D ExpandTaskTexture
        {
            get
            {
                if (expandTaskTexture == null)
                {
                    InitExpandTaskTexture();
                }
                return expandTaskTexture;
            }
        }

        public static Texture2D GearTexture
        {
            get
            {
                if (gearTexture == null)
                {
                    InitGearTexture();
                }
                return gearTexture;
            }
        }

        public static GUIStyle GraphBackgroundGUIStyle
        {
            get
            {
                if (graphBackgroundGUIStyle == null)
                {
                    InitGraphBackgroundGUIStyle();
                }
                return graphBackgroundGUIStyle;
            }
        }

        public static GUIStyle GraphStatusGUIStyle
        {
            get
            {
                if (graphStatusGUIStyle == null)
                {
                    InitGraphStatusGUIStyle();
                }
                return graphStatusGUIStyle;
            }
        }

        public static Texture2D HistoryBackwardTexture
        {
            get
            {
                if (historyBackwardTexture == null)
                {
                    InitHistoryBackwardTexture();
                }
                return historyBackwardTexture;
            }
        }

        public static Texture2D HistoryForwardTexture
        {
            get
            {
                if (historyForwardTexture == null)
                {
                    InitHistoryForwardTexture();
                }
                return historyForwardTexture;
            }
        }

        public static Texture2D IdentifyButtonTexture
        {
            get
            {
                if (identifyButtonTexture == null)
                {
                    InitIdentifyButtonTexture();
                }
                return identifyButtonTexture;
            }
        }

        public static GUIStyle LabelWrapGUIStyle
        {
            get
            {
                if (labelWrapGUIStyle == null)
                {
                    InitLabelWrapGUIStyle();
                }
                return labelWrapGUIStyle;
            }
        }

        public static Texture2D PauseTexture
        {
            get
            {
                if (pauseTexture == null)
                {
                    InitPauseTexture();
                }
                return pauseTexture;
            }
        }

        public static GUIStyle PlainButtonGUIStyle
        {
            get
            {
                if (plainButtonGUIStyle == null)
                {
                    InitPlainButtonGUIStyle();
                }
                return plainButtonGUIStyle;
            }
        }

        public static GUIStyle PlainTextureGUIStyle
        {
            get
            {
                if (plainTextureGUIStyle == null)
                {
                    InitPlainTextureGUIStyle();
                }
                return plainTextureGUIStyle;
            }
        }

        public static Texture2D PlayTexture
        {
            get
            {
                if (playTexture == null)
                {
                    InitPlayTexture();
                }
                return playTexture;
            }
        }

        public static GUIStyle PreferencesPaneGUIStyle
        {
            get
            {
                if (preferencesPaneGUIStyle == null)
                {
                    InitPreferencesPaneGUIStyle();
                }
                return preferencesPaneGUIStyle;
            }
        }

        public static GUIStyle PropertyBoxGUIStyle
        {
            get
            {
                if (propertyBoxGUIStyle == null)
                {
                    InitPropertyBoxGUIStyle();
                }
                return propertyBoxGUIStyle;
            }
        }

        public static Texture2D ReferencedTexture
        {
            get
            {
                if (referencedTexture == null)
                {
                    InitReferencedTexture();
                }
                return referencedTexture;
            }
        }

        public static Texture2D ScreenshotBackgroundTexture
        {
            get
            {
                if (screenshotBackgroundTexture == null)
                {
                    InitScreenshotBackgroundTexture();
                }
                return screenshotBackgroundTexture;
            }
        }

        public static GUIStyle SelectedBackgroundGUIStyle
        {
            get
            {
                if (selectedBackgroundGUIStyle == null)
                {
                    InitSelectedBackgroundGUIStyle();
                }
                return selectedBackgroundGUIStyle;
            }
        }

        public static GUIStyle SelectionGUIStyle
        {
            get
            {
                if (selectionGUIStyle == null)
                {
                    InitSelectionGUIStyle();
                }
                return selectionGUIStyle;
            }
        }

        public static GUIStyle SharedVariableToolbarPopup
        {
            get
            {
                if (sharedVariableToolbarPopup == null)
                {
                    InitSharedVariableToolbarPopup();
                }
                return sharedVariableToolbarPopup;
            }
        }

        public static Texture2D SmallErrorIconTexture
        {
            get
            {
                if (smallErrorIconTexture == null)
                {
                    InitSmallErrorIconTexture();
                }
                return smallErrorIconTexture;
            }
        }

        public static Texture2D StepTexture
        {
            get
            {
                if (stepTexture == null)
                {
                    InitStepTexture();
                }
                return stepTexture;
            }
        }

        public static Texture2D TaskBorderIdentifyTexture
        {
            get
            {
                if (taskBorderIdentifyTexture == null)
                {
                    InitTaskBorderIdentifyTexture();
                }
                return taskBorderIdentifyTexture;
            }
        }

        public static Texture2D TaskBorderRunningTexture
        {
            get
            {
                if (taskBorderRunningTexture == null)
                {
                    InitTaskBorderRunningTexture();
                }
                return taskBorderRunningTexture;
            }
        }

        public static GUIStyle TaskCommentGUIStyle
        {
            get
            {
                if (taskCommentGUIStyle == null)
                {
                    InitTaskCommentGUIStyle();
                }
                return taskCommentGUIStyle;
            }
        }

        public static GUIStyle TaskCommentLeftAlignGUIStyle
        {
            get
            {
                if (taskCommentLeftAlignGUIStyle == null)
                {
                    InitTaskCommentLeftAlignGUIStyle();
                }
                return taskCommentLeftAlignGUIStyle;
            }
        }

        public static GUIStyle TaskCommentRightAlignGUIStyle
        {
            get
            {
                if (taskCommentRightAlignGUIStyle == null)
                {
                    InitTaskCommentRightAlignGUIStyle();
                }
                return taskCommentRightAlignGUIStyle;
            }
        }

        public static Texture2D TaskConnectionCollapsedTexture
        {
            get
            {
                if (taskConnectionCollapsedTexture == null)
                {
                    InitTaskConnectionCollapsedTexture();
                }
                return taskConnectionCollapsedTexture;
            }
        }

        public static Texture2D TaskConnectionIdentifyBottomTexture
        {
            get
            {
                if (taskConnectionIdentifyBottomTexture == null)
                {
                    InitTaskConnectionIdentifyBottomTexture();
                }
                return taskConnectionIdentifyBottomTexture;
            }
        }

        public static Texture2D TaskConnectionIdentifyTopTexture
        {
            get
            {
                if (taskConnectionIdentifyTopTexture == null)
                {
                    InitTaskConnectionIdentifyTopTexture();
                }
                return taskConnectionIdentifyTopTexture;
            }
        }

        public static Texture2D TaskConnectionRunningBottomTexture
        {
            get
            {
                if (taskConnectionRunningBottomTexture == null)
                {
                    InitTaskConnectionRunningBottomTexture();
                }
                return taskConnectionRunningBottomTexture;
            }
        }

        public static Texture2D TaskConnectionRunningTopTexture
        {
            get
            {
                if (taskConnectionRunningTopTexture == null)
                {
                    InitTaskConnectionRunningTopTexture();
                }
                return taskConnectionRunningTopTexture;
            }
        }

        public static GUIStyle TaskDescriptionGUIStyle
        {
            get
            {
                if (taskDescriptionGUIStyle == null)
                {
                    InitTaskDescriptionGUIStyle();
                }
                return taskDescriptionGUIStyle;
            }
        }

        public static GUIStyle TaskFoldoutGUIStyle
        {
            get
            {
                if (taskFoldoutGUIStyle == null)
                {
                    InitTaskFoldoutGUIStyle();
                }
                return taskFoldoutGUIStyle;
            }
        }

        public static GUIStyle TaskHighlightCompactGUIStyle
        {
            get
            {
                if (taskHighlightCompactGUIStyle == null)
                {
                    InitTaskHighlightCompactGUIStyle();
                }
                return taskHighlightCompactGUIStyle;
            }
        }

        public static GUIStyle TaskHighlightGUIStyle
        {
            get
            {
                if (taskHighlightGUIStyle == null)
                {
                    InitTaskHighlightGUIStyle();
                }
                return taskHighlightGUIStyle;
            }
        }

        public static GUIStyle TaskIdentifyCompactGUIStyle
        {
            get
            {
                if (taskIdentifyCompactGUIStyle == null)
                {
                    InitTaskIdentifyCompactGUIStyle();
                }
                return taskIdentifyCompactGUIStyle;
            }
        }

        public static GUIStyle TaskIdentifyGUIStyle
        {
            get
            {
                if (taskIdentifyGUIStyle == null)
                {
                    InitTaskIdentifyGUIStyle();
                }
                return taskIdentifyGUIStyle;
            }
        }

        public static GUIStyle TaskIdentifySelectedCompactGUIStyle
        {
            get
            {
                if (taskIdentifySelectedCompactGUIStyle == null)
                {
                    InitTaskIdentifySelectedCompactGUIStyle();
                }
                return taskIdentifySelectedCompactGUIStyle;
            }
        }

        public static GUIStyle TaskIdentifySelectedGUIStyle
        {
            get
            {
                if (taskIdentifySelectedGUIStyle == null)
                {
                    InitTaskIdentifySelectedGUIStyle();
                }
                return taskIdentifySelectedGUIStyle;
            }
        }

        public static GUIStyle TaskInspectorCommentGUIStyle
        {
            get
            {
                if (taskInspectorCommentGUIStyle == null)
                {
                    InitTaskInspectorCommentGUIStyle();
                }
                return taskInspectorCommentGUIStyle;
            }
        }

        public static GUIStyle TaskInspectorGUIStyle
        {
            get
            {
                if (taskInspectorGUIStyle == null)
                {
                    InitTaskInspectorGUIStyle();
                }
                return taskInspectorGUIStyle;
            }
        }

        public static GUIStyle TaskRunningCompactGUIStyle
        {
            get
            {
                if (taskRunningCompactGUIStyle == null)
                {
                    InitTaskRunningCompactGUIStyle();
                }
                return taskRunningCompactGUIStyle;
            }
        }

        public static GUIStyle TaskRunningGUIStyle
        {
            get
            {
                if (taskRunningGUIStyle == null)
                {
                    InitTaskRunningGUIStyle();
                }
                return taskRunningGUIStyle;
            }
        }

        public static GUIStyle TaskRunningSelectedCompactGUIStyle
        {
            get
            {
                if (taskRunningSelectedCompactGUIStyle == null)
                {
                    InitTaskRunningSelectedCompactGUIStyle();
                }
                return taskRunningSelectedCompactGUIStyle;
            }
        }

        public static GUIStyle TaskRunningSelectedGUIStyle
        {
            get
            {
                if (taskRunningSelectedGUIStyle == null)
                {
                    InitTaskRunningSelectedGUIStyle();
                }
                return taskRunningSelectedGUIStyle;
            }
        }

        public static GUIStyle TaskTitleGUIStyle
        {
            get
            {
                if (taskTitleGUIStyle == null)
                {
                    InitTaskTitleGUIStyle();
                }
                return taskTitleGUIStyle;
            }
        }

        public static GUIStyle ToolbarButtonLeftAlignGUIStyle
        {
            get
            {
                if (tolbarButtonLeftAlignGUIStyle == null)
                {
                    InitToolbarButtonLeftAlignGUIStyle();
                }
                return tolbarButtonLeftAlignGUIStyle;
            }
        }

        public static GUIStyle ToolbarButtonSelectionGUIStyle
        {
            get
            {
                if (toolbarButtonSelectionGUIStyle == null)
                {
                    InitToolbarButtonSelectionGUIStyle();
                }
                return toolbarButtonSelectionGUIStyle;
            }
        }

        public static GUIStyle ToolbarLabelGUIStyle
        {
            get
            {
                if (toolbarLabelGUIStyle == null)
                {
                    InitToolbarLabelGUIStyle();
                }
                return toolbarLabelGUIStyle;
            }
        }

        public static GUIStyle TransparentButtonGUIStyle
        {
            get
            {
                if (transparentButtonGUIStyle == null)
                {
                    InitTransparentButtonGUIStyle();
                }
                return transparentButtonGUIStyle;
            }
        }

        public static GUIStyle TransparentButtonOffsetGUIStyle
        {
            get
            {
                if (transparentButtonOffsetGUIStyle == null)
                {
                    InitTransparentButtonOffsetGUIStyle();
                }
                return transparentButtonOffsetGUIStyle;
            }
        }

        public static Texture2D UpArrowButtonTexture
        {
            get
            {
                if (upArrowButtonTexture == null)
                {
                    InitUpArrowButtonTexture();
                }
                return upArrowButtonTexture;
            }
        }

        public static Texture2D VariableButtonSelectedTexture
        {
            get
            {
                if (variableButtonSelectedTexture == null)
                {
                    InitVariableButtonSelectedTexture();
                }
                return variableButtonSelectedTexture;
            }
        }

        public static Texture2D VariableButtonTexture
        {
            get
            {
                if (variableButtonTexture == null)
                {
                    InitVariableButtonTexture();
                }
                return variableButtonTexture;
            }
        }

        public static Texture2D VariableDeleteButtonTexture
        {
            get
            {
                if (variableDeleteButtonTexture == null)
                {
                    InitVariableDeleteButtonTexture();
                }
                return variableDeleteButtonTexture;
            }
        }

        public static Texture2D VariableMapButtonTexture
        {
            get
            {
                if (variableMapButtonTexture == null)
                {
                    InitVariableMapButtonTexture();
                }
                return variableMapButtonTexture;
            }
        }

        public static Texture2D VariableWatchButtonSelectedTexture
        {
            get
            {
                if (variableWatchButtonSelectedTexture == null)
                {
                    InitVariableWatchButtonSelectedTexture();
                }
                return variableWatchButtonSelectedTexture;
            }
        }

        public static Texture2D VariableWatchButtonTexture
        {
            get
            {
                if (variableWatchButtonTexture == null)
                {
                    InitVariableWatchButtonTexture();
                }
                return variableWatchButtonTexture;
            }
        }

        public static GUIStyle WelcomeScreenIntroGUIStyle
        {
            get
            {
                if (welcomeScreenIntroGUIStyle == null)
                {
                    InitWelcomeScreenIntroGUIStyle();
                }
                return welcomeScreenIntroGUIStyle;
            }
        }

        public static GUIStyle WelcomeScreenTextDescriptionGUIStyle
        {
            get
            {
                if (welcomeScreenTextDescriptionGUIStyle == null)
                {
                    InitWelcomeScreenTextDescriptionGUIStyle();
                }
                return welcomeScreenTextDescriptionGUIStyle;
            }
        }

        public static GUIStyle WelcomeScreenTextHeaderGUIStyle
        {
            get
            {
                if (welcomeScreenTextHeaderGUIStyle == null)
                {
                    InitWelcomeScreenTextHeaderGUIStyle();
                }
                return welcomeScreenTextHeaderGUIStyle;
            }
        }
    }
}

