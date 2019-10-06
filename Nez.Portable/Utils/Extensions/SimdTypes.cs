using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
	}
}
