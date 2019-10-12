using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text;


namespace Nez
{
	public interface IFont
	{
		/// <summary>
		/// line height of the font
		/// </summary>
		/// <value>The height of the line.</value>
		float LineSpacing { get; }

		/// <summary>
		/// returns the size in pixels of text when rendered in this font
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="text">Text.</param>
		System.Numerics.Vector2 MeasureString(string text);

		/// <summary>
		/// returns the size in pixels of text when rendered in this font
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="text">Text.</param>
		System.Numerics.Vector2 MeasureString(StringBuilder text);

		/// <summary>
		/// returns true if the character exists in the font or false if it does not
		/// </summary>
		/// <returns><c>true</c>, if character was hased, <c>false</c> otherwise.</returns>
		/// <param name="c">C.</param>
		bool HasCharacter(char c);

		void DrawInto(Batcher batcher, string text, System.Numerics.Vector2 position, Color color,
		              float rotation, System.Numerics.Vector2 origin, System.Numerics.Vector2 scale, SpriteEffects effect, float depth);

		void DrawInto(Batcher batcher, StringBuilder text, System.Numerics.Vector2 position, Color color,
		              float rotation, System.Numerics.Vector2 origin, System.Numerics.Vector2 scale, SpriteEffects effect, float depth);
	}
}