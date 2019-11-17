using System;
using Microsoft.Xna.Framework;


namespace Nez
{
	public static class Collisions
	{
		[Flags]
		public enum PointSectors
		{
			Center = 0,
			Top = 1,
			Bottom = 2,
			TopLeft = 9,
			TopRight = 5,
			Left = 8,
			Right = 4,
			BottomLeft = 10,
			BottomRight = 6
		};


		#region Line

		public static bool LineToLine(System.Numerics.Vector2 a1, System.Numerics.Vector2 a2, System.Numerics.Vector2 b1, System.Numerics.Vector2 b2)
		{
			System.Numerics.Vector2 b = a2 - a1;
			System.Numerics.Vector2 d = b2 - b1;
			float bDotDPerp = b.X * d.Y - b.Y * d.X;

			// if b dot d == 0, it means the lines are parallel so have infinite intersection points
			if (bDotDPerp == 0)
				return false;

			System.Numerics.Vector2 c = b1 - a1;
			float t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
			if (t < 0 || t > 1)
				return false;

			float u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
			if (u < 0 || u > 1)
				return false;

			return true;
		}


		public static bool LineToLine(System.Numerics.Vector2 a1, System.Numerics.Vector2 a2, System.Numerics.Vector2 b1, System.Numerics.Vector2 b2, out System.Numerics.Vector2 intersection)
		{
			intersection = System.Numerics.Vector2.Zero;

			var b = a2 - a1;
			var d = b2 - b1;
			var bDotDPerp = b.X * d.Y - b.Y * d.X;

			// if b dot d == 0, it means the lines are parallel so have infinite intersection points
			if (bDotDPerp == 0)
				return false;

			var c = b1 - a1;
			var t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
			if (t < 0 || t > 1)
				return false;

			var u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
			if (u < 0 || u > 1)
				return false;

			intersection = a1 + t * b;

			return true;
		}


		public static System.Numerics.Vector2 ClosestPointOnLine(System.Numerics.Vector2 lineA, System.Numerics.Vector2 lineB, System.Numerics.Vector2 closestTo)
		{
			var v = lineB - lineA;
			var w = closestTo - lineA;
			var t = System.Numerics.Vector2.Dot(w, v) / System.Numerics.Vector2.Dot(v, v);
			t = MathHelper.Clamp(t, 0, 1);

			return lineA + v * t;
		}

		#endregion


		#region Circle

		public static bool CircleToCircle(System.Numerics.Vector2 circleCenter1, float circleRadius1, System.Numerics.Vector2 circleCenter2,
										  float circleRadius2)
		{
			return System.Numerics.Vector2.DistanceSquared(circleCenter1, circleCenter2) <
				   (circleRadius1 + circleRadius2) * (circleRadius1 + circleRadius2);
		}


		public static bool CircleToLine(System.Numerics.Vector2 circleCenter, float radius, System.Numerics.Vector2 lineFrom, System.Numerics.Vector2 lineTo)
		{
			return System.Numerics.Vector2.DistanceSquared(circleCenter, ClosestPointOnLine(lineFrom, lineTo, circleCenter)) <
				   radius * radius;
		}


		public static bool CircleToPoint(System.Numerics.Vector2 circleCenter, float radius, System.Numerics.Vector2 point)
		{
			return System.Numerics.Vector2.DistanceSquared(circleCenter, point) < radius * radius;
		}

		#endregion


		#region Bounds/Rect

		public static bool RectToCircle(RectangleF rect, System.Numerics.Vector2 cPosition, float cRadius)
		{
			return RectToCircle(rect.X, rect.Y, rect.Width, rect.Height, cPosition, cRadius);
		}


		public static bool RectToCircle(ref RectangleF rect, System.Numerics.Vector2 cPosition, float cRadius)
		{
			return RectToCircle(rect.X, rect.Y, rect.Width, rect.Height, cPosition, cRadius);
		}


		static public bool RectToCircle(float rectX, float rectY, float rectWidth, float rectHeight,
										Vector2 circleCenter, float radius)
		{
			//Check if the rectangle contains the circle's center-point
			if (RectToPoint(rectX, rectY, rectWidth, rectHeight, circleCenter))
				return true;

			// Check the circle against the relevant edges
			System.Numerics.Vector2 edgeFrom;
			System.Numerics.Vector2 edgeTo;
			var sector = GetSector(rectX, rectY, rectWidth, rectHeight, circleCenter);

			if ((sector & PointSectors.Top) != 0)
			{
				edgeFrom = new System.Numerics.Vector2(rectX, rectY);
				edgeTo = new System.Numerics.Vector2(rectX + rectWidth, rectY);
				if (CircleToLine(circleCenter, radius, edgeFrom, edgeTo))
					return true;
			}

			if ((sector & PointSectors.Bottom) != 0)
			{
				edgeFrom = new System.Numerics.Vector2(rectX, rectY + rectHeight);
				edgeTo = new System.Numerics.Vector2(rectX + rectWidth, rectY + rectHeight);
				if (CircleToLine(circleCenter, radius, edgeFrom, edgeTo))
					return true;
			}

			if ((sector & PointSectors.Left) != 0)
			{
				edgeFrom = new System.Numerics.Vector2(rectX, rectY);
				edgeTo = new System.Numerics.Vector2(rectX, rectY + rectHeight);
				if (CircleToLine(circleCenter, radius, edgeFrom, edgeTo))
					return true;
			}

			if ((sector & PointSectors.Right) != 0)
			{
				edgeFrom = new System.Numerics.Vector2(rectX + rectWidth, rectY);
				edgeTo = new System.Numerics.Vector2(rectX + rectWidth, rectY + rectHeight);
				if (CircleToLine(circleCenter, radius, edgeFrom, edgeTo))
					return true;
			}

			return false;
		}


		public static bool RectToLine(ref RectangleF rect, System.Numerics.Vector2 lineFrom, System.Numerics.Vector2 lineTo)
		{
			return RectToLine(rect.X, rect.Y, rect.Width, rect.Height, lineFrom, lineTo);
		}


		public static bool RectToLine(RectangleF rect, System.Numerics.Vector2 lineFrom, System.Numerics.Vector2 lineTo)
		{
			return RectToLine(rect.X, rect.Y, rect.Width, rect.Height, lineFrom, lineTo);
		}


		public static bool RectToLine(float rectX, float rectY, float rectWidth, float rectHeight, System.Numerics.Vector2 lineFrom,
		                              System.Numerics.Vector2 lineTo)
		{
			var fromSector = GetSector(rectX, rectY, rectWidth, rectHeight, lineFrom);
			var toSector = GetSector(rectX, rectY, rectWidth, rectHeight, lineTo);

			if (fromSector == PointSectors.Center || toSector == PointSectors.Center)
				return true;
			else if ((fromSector & toSector) != 0)
				return false;
			else
			{
				var both = fromSector | toSector;

				// Do line checks against the edges
				System.Numerics.Vector2 edgeFrom;
				System.Numerics.Vector2 edgeTo;

				if ((both & PointSectors.Top) != 0)
				{
					edgeFrom = new System.Numerics.Vector2(rectX, rectY);
					edgeTo = new System.Numerics.Vector2(rectX + rectWidth, rectY);
					if (LineToLine(edgeFrom, edgeTo, lineFrom, lineTo))
						return true;
				}

				if ((both & PointSectors.Bottom) != 0)
				{
					edgeFrom = new System.Numerics.Vector2(rectX, rectY + rectHeight);
					edgeTo = new System.Numerics.Vector2(rectX + rectWidth, rectY + rectHeight);
					if (LineToLine(edgeFrom, edgeTo, lineFrom, lineTo))
						return true;
				}

				if ((both & PointSectors.Left) != 0)
				{
					edgeFrom = new System.Numerics.Vector2(rectX, rectY);
					edgeTo = new System.Numerics.Vector2(rectX, rectY + rectHeight);
					if (LineToLine(edgeFrom, edgeTo, lineFrom, lineTo))
						return true;
				}

				if ((both & PointSectors.Right) != 0)
				{
					edgeFrom = new System.Numerics.Vector2(rectX + rectWidth, rectY);
					edgeTo = new System.Numerics.Vector2(rectX + rectWidth, rectY + rectHeight);
					if (LineToLine(edgeFrom, edgeTo, lineFrom, lineTo))
						return true;
				}
			}

			return false;
		}


		public static bool RectToPoint(float rX, float rY, float rW, float rH, System.Numerics.Vector2 point)
		{
			return point.X >= rX && point.Y >= rY && point.X < rX + rW && point.Y < rY + rH;
		}


		public static bool RectToPoint(RectangleF rect, System.Numerics.Vector2 point)
		{
			return RectToPoint(rect.X, rect.Y, rect.Width, rect.Height, point);
		}

		#endregion


		#region Sectors

		/*
		 *  Bitflags and helpers for using the Cohen–Sutherland algorithm
		 *  http://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm
		 *  
		 *  Sector bitflags:
		 *      1001  1000  1010
		 *      0001  0000  0010
		 *      0101  0100  0110
		 */

		public static PointSectors GetSector(RectangleF rect, System.Numerics.Vector2 point)
		{
			PointSectors sector = PointSectors.Center;

			if (point.X < rect.Left)
				sector |= PointSectors.Left;
			else if (point.X >= rect.Right)
				sector |= PointSectors.Right;

			if (point.Y < rect.Top)
				sector |= PointSectors.Top;
			else if (point.Y >= rect.Bottom)
				sector |= PointSectors.Bottom;

			return sector;
		}


		public static PointSectors GetSector(float rX, float rY, float rW, float rH, System.Numerics.Vector2 point)
		{
			PointSectors sector = PointSectors.Center;

			if (point.X < rX)
				sector |= PointSectors.Left;
			else if (point.X >= rX + rW)
				sector |= PointSectors.Right;

			if (point.Y < rY)
				sector |= PointSectors.Top;
			else if (point.Y >= rY + rH)
				sector |= PointSectors.Bottom;

			return sector;
		}

		#endregion
	}
}