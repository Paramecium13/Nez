using Microsoft.Xna.Framework;


namespace Nez
{
	public static class Vector3Ext
	{
		/// <summary>
		/// returns a System.Numerics.Vector2 ignoring the z component
		/// </summary>
		/// <returns>The vector2.</returns>
		/// <param name="vec">Vec.</param>
		public static System.Numerics.Vector2 ToVector2(this Vector3 vec)
		{
			return new System.Numerics.Vector2(vec.X, vec.Y);
		}
	}
}