using Microsoft.Xna.Framework;


namespace Nez
{
	/// <summary>
	/// while technically not a ray (rays are just start and direction) it does double duty as both a line and a ray.
	/// </summary>
	public struct Ray2D
	{
		public System.Numerics.Vector2 Start;
		public System.Numerics.Vector2 End;
		public System.Numerics.Vector2 Direction;


		public Ray2D(System.Numerics.Vector2 position, System.Numerics.Vector2 end)
		{
			Start = position;
			End = end;
			Direction = end - Start;
		}
	}
}