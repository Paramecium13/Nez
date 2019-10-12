using System.Xml.Serialization;
using Microsoft.Xna.Framework;


namespace Nez.Svg
{
	public class SvgRectangle : SvgElement
	{
		[XmlAttribute("x")] public float X;

		[XmlAttribute("y")] public float Y;

		[XmlAttribute("width")] public float Width;

		[XmlAttribute("height")] public float Height;

		public System.Numerics.Vector2 Center => new System.Numerics.Vector2(X + Width / 2, Y + Height / 2);


		/// <summary>
		/// gets the points for the rectangle with all transforms applied
		/// </summary>
		/// <returns>The transformed points.</returns>
		public System.Numerics.Vector2[] GetTransformedPoints()
		{
			var pts = new System.Numerics.Vector2[]
			{
				new System.Numerics.Vector2(X, Y), new System.Numerics.Vector2(X + Width, Y), new System.Numerics.Vector2(X + Width, Y + Height),
				new System.Numerics.Vector2(X, Y + Height)
			};
			var mat = GetCombinedMatrix();
			Vector2Ext.Transform(pts, ref mat, pts);

			return pts;
		}
	}
}