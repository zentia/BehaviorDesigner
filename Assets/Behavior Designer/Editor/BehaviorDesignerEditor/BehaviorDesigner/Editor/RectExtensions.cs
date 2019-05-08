namespace BehaviorDesigner.Editor
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public static class RectExtensions
    {
        public static Rect ScaleSizeBy(this Rect rect, float scale)
        {
            return rect.ScaleSizeBy(scale, rect.center);
        }

        public static Rect ScaleSizeBy(this Rect rect, Vector2 scale)
        {
            return rect.ScaleSizeBy(scale, rect.center);
        }

        public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
        {
            Rect rect2 = rect;
            rect2.x -= pivotPoint.x;
            rect2.y -= pivotPoint.y;
            rect2.xMin *= scale;
            rect2.xMax *= scale;
            rect2.yMin *= scale;
            rect2.yMax *= scale;
            rect2.x += pivotPoint.x;
            rect2.y += pivotPoint.y;
            return rect2;
        }

        public static Rect ScaleSizeBy(this Rect rect, Vector2 scale, Vector2 pivotPoint)
        {
            Rect rect2 = rect;
            rect2.x -= pivotPoint.x;
            rect2.y -= pivotPoint.y;
            rect2.xMin *= scale.x;
            rect2.xMax *= scale.x;
            rect2.yMin *= scale.y;
            rect2.yMax *= scale.y;
            rect2.x += pivotPoint.x;
            rect2.y += pivotPoint.y;
            return rect2;
        }

        public static Vector2 TopLeft(this Rect rect)
        {
            return new Vector2(rect.xMin, rect.yMin);
        }
    }
}

