using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;


namespace Nez
{
	public static class GestureSampleExt
	{
		public static System.Numerics.Vector2 ScaledPosition(this GestureSample gestureSample)
		{
			return Input.ScaledPosition(gestureSample.Position.ToSimd());
		}

		public static System.Numerics.Vector2 ScaledPosition2(this GestureSample gestureSample)
		{
			return Input.ScaledPosition(gestureSample.Position2.ToSimd());
		}
	}
}