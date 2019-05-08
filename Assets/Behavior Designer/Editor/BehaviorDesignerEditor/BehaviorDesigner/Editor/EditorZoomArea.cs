namespace BehaviorDesigner.Editor
{
    using System;
    using UnityEngine;

    public class EditorZoomArea
    {
        private static Matrix4x4 _prevGuiMatrix;
        private static Rect groupRect = new Rect();

        public static Rect Begin(Rect screenCoordsArea, float zoomScale)
        {
            GUI.EndGroup();
            Rect position = screenCoordsArea.ScaleSizeBy((float) (1f / zoomScale), screenCoordsArea.TopLeft());
            position.y += 21f;
            GUI.BeginGroup(position);
            _prevGuiMatrix = GUI.matrix;
            Matrix4x4 matrixx = Matrix4x4.TRS((Vector3) position.TopLeft(), Quaternion.identity, Vector3.one);
            Vector3 one = Vector3.one;
            one.x = one.y = zoomScale;
            Matrix4x4 matrixx2 = Matrix4x4.Scale(one);
            GUI.matrix = ((matrixx * matrixx2) * matrixx.inverse) * GUI.matrix;
            return position;
        }

        public static void End()
        {
            GUI.matrix = _prevGuiMatrix;
            GUI.EndGroup();
            groupRect.y = 21f;
            groupRect.width = Screen.width;
            groupRect.height = Screen.height;
            GUI.BeginGroup(groupRect);
        }
    }
}

