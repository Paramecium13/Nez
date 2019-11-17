using Microsoft.Xna.Framework;
using Num = System.Numerics;


namespace Nez.ImGuiTools
{
	/// <summary>
	/// helpers to convert to/from System.Numberics
	/// </summary>
	public static class NumericsExt
	{
		public static Vector2 ToXNA(this Num.Vector2 self) => System.Runtime.CompilerServices.Unsafe.As<Num.Vector2,Vector2>(ref self);

		public static Num.Vector2 ToNumerics(this Vector2 self) => System.Runtime.CompilerServices.Unsafe.As<Vector2,Num.Vector2>(ref self);

		public static Num.Vector2 ToNumerics(this Point self) => new Num.Vector2(self.X, self.Y);

		public static Vector3 ToXNA(this Num.Vector3 self) => System.Runtime.CompilerServices.Unsafe.As<Num.Vector3, Vector3>(ref self);

		public static Num.Vector3 ToNumerics(this Vector3 self) => System.Runtime.CompilerServices.Unsafe.As<Vector3, Num.Vector3>(ref self);

		public static Vector4 ToXNA(this Num.Vector4 self) => System.Runtime.CompilerServices.Unsafe.As<Num.Vector4,Vector4>(ref self);

		public static Num.Vector4 ToNumerics(this Vector4 self) => System.Runtime.CompilerServices.Unsafe.As<Vector4,Num.Vector4>(ref self);

		public static Num.Vector4 ToNumerics(this Color self) => new Num.Vector4(self.R / 255.0f, self.G / 255.0f, self.B / 255.0f, self.A / 255.0f);

		public static Color ToXNAColor(this Num.Vector4 self) => new Color(self.X * 1.0f, self.Y * 1.0f, self.Z * 1.0f, self.W * 1.0f);
	}
}