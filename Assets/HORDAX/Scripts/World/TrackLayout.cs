using UnityEngine;

namespace HORDAX.World
{
    /// <summary>
    /// Canonical HORDAX track geometry.
    /// The approved game layout is one forward track split into two parallel lanes:
    /// arsenal/progression on the left and horde/combat on the right.
    /// </summary>
    public static class TrackLayout
    {
        public const float ArsenalCenterX = -3.35f;
        public const float HordeCenterX = 3.35f;
        public const float LaneHalfWidth = 2.75f;
        public const float DividerHalfWidth = 0.18f;
        public const float OuterRailX = 6.45f;
        public const float TrackHalfWidth = 6.65f;

        public static float ArsenalMinX => ArsenalCenterX - LaneHalfWidth;
        public static float ArsenalMaxX => ArsenalCenterX + LaneHalfWidth;
        public static float HordeMinX => HordeCenterX - LaneHalfWidth;
        public static float HordeMaxX => HordeCenterX + LaneHalfWidth;

        public static float ArsenalXFromOffset(float offset)
        {
            return ArsenalCenterX + Mathf.Clamp(offset, -LaneHalfWidth + 0.35f, LaneHalfWidth - 0.35f);
        }

        public static float ClampHordeX(float x)
        {
            return Mathf.Clamp(x, HordeMinX, HordeMaxX);
        }
    }
}
