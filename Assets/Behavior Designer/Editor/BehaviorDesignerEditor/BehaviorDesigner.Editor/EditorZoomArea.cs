using System;
using UnityEngine;

namespace BehaviorDesigner.Editor
{
	public class EditorZoomArea
	{
		private static Matrix4x4 _prevGuiMatrix;

		private static Rect groupRect = default(Rect);

		public static Rect Begin(Rect screenCoordsArea, float zoomScale)
		{
			GUI.EndGroup();
			Rect rect = screenCoordsArea.ScaleSizeBy(1f / zoomScale, screenCoordsArea.TopLeft());
			rect.y=(rect.y + 21f);
			GUI.BeginGroup(rect);
			EditorZoomArea._prevGuiMatrix = GUI.matrix;
			Matrix4x4 matrix4x = Matrix4x4.TRS(rect.TopLeft(), Quaternion.identity, Vector3.one);
			Vector3 one = Vector3.one;
			one.y = zoomScale;
			one.x = zoomScale;
			Matrix4x4 matrix4x2 = Matrix4x4.Scale(one);
			GUI.matrix=(matrix4x * matrix4x2 * matrix4x.inverse * GUI.matrix);
			return rect;
		}

		public static void End()
		{
			GUI.matrix=(EditorZoomArea._prevGuiMatrix);
			GUI.EndGroup();
			EditorZoomArea.groupRect.y=(21f);
			EditorZoomArea.groupRect.width=((float)Screen.width);
			EditorZoomArea.groupRect.height=((float)Screen.height);
			GUI.BeginGroup(EditorZoomArea.groupRect);
		}
	}
}
