using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nez
{
	public static class SimdTypes
	{
		public static Matrix ToXna4x4(this Matrix3x2 mat)
			=> new Matrix
			(
				mat.M11, mat.M12, 0, 0,
				mat.M21, mat.M22, 0, 0,
				0, 0, 1, 0,
				mat.M31, mat.M32, 0, 1
			);

		public static Matrix3x2 ToSimd(this ref Matrix self)
			=> new Matrix3x2(self.M11, self.M12, self.M21, self.M22, self.M31, self.M32);

		public static Matrix3x2 ToSimd(this ref Matrix2D self)
			=> Unsafe.As<Matrix2D, Matrix3x2>(ref self);

		public static Point ToPoint(ref System.Numerics.Vector2 self) => new Point((int)self.X, (int)self.Y);

		public static Point ToPoint(this System.Numerics.Vector2 self) => new Point((int)self.X, (int)self.Y);
	}
}
