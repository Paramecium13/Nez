using System.Xml.Serialization;
using Microsoft.Xna.Framework;


namespace Nez.Svg
{
	public class SvgPolyline : SvgElement
	{
		[XmlAttribute("points")]
		public string PointsAttribute
		{
			get => null;
			set => ParsePoints(value);
		}

		public System.Numerics.Vector2[] Points;


		void ParsePoints(string str)
		{
			// normalize commas and spaces since some programs use comma separate points and others use spaces
			str = str.Replace(',', ' ');
			var pairs = str.Split(new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
			Points = new System.Numerics.Vector2[pairs.Length / 2];

			var pointIndex = 0;
			for (var i = 0; i < pairs.Length; i += 2)
				Points[pointIndex++] = new System.Numerics.Vector2(float.Parse(pairs[i]), float.Parse(pairs[i + 1]));
		}


		public System.Numerics.Vector2[] GetTransformedPoints()
		{
			var pts = new System.Numerics.Vector2[Points.Length];
			var mat = GetCombinedMatrix();
			Vector2Ext.Transform(Points, ref mat, pts);

			return pts;
		}
	}
}