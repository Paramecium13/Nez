using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Nez
{
	/// <summary>
	/// helper methods for drawing text with NezSpriteFonts
	/// </summary>
	public static class BatcherSpriteFontExt
	{
		public static void DrawString(this Batcher batcher, NezSpriteFont spriteFont, StringBuilder text,
		                              System.Numerics.Vector2 position, Color color)
		{
			batcher.DrawString(spriteFont, text, position, color, 0.0f, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(1.0f),
				SpriteEffects.None, 0.0f);
		}


		public static void DrawString(this Batcher batcher, NezSpriteFont spriteFont, StringBuilder text,
		                              System.Numerics.Vector2 position, Color color,
		                              float rotation, System.Numerics.Vector2 origin, float scale, SpriteEffects effects,
		                              float layerDepth)
		{
			batcher.DrawString(spriteFont, text, position, color, rotation, origin, new System.Numerics.Vector2(scale), effects,
				layerDepth);
		}


		public static void DrawString(this Batcher batcher, NezSpriteFont spriteFont, string text, System.Numerics.Vector2 position,
		                              Color color)
		{
			batcher.DrawString(spriteFont, text, position, color, 0.0f, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(1.0f),
				SpriteEffects.None, 0.0f);
		}


		public static void DrawString(this Batcher batcher, NezSpriteFont spriteFont, string text, System.Numerics.Vector2 position,
		                              Color color, float rotation,
		                              System.Numerics.Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
		{
			batcher.DrawString(spriteFont, text, position, color, rotation, origin, new System.Numerics.Vector2(scale), effects,
				layerDepth);
		}


		public static void DrawString(this Batcher batcher, NezSpriteFont spriteFont, StringBuilder text,
		                              System.Numerics.Vector2 position, Color color,
		                              float rotation, System.Numerics.Vector2 origin, System.Numerics.Vector2 scale, SpriteEffects effects,
		                              float layerDepth)
		{
			Insist.IsFalse(text == null);

			if (text.Length == 0)
				return;

			var source = new FontCharacterSource(text);
			spriteFont.DrawInto(batcher, ref source, position, color, rotation, origin, scale, effects, layerDepth);
		}


		public static void DrawString(this Batcher batcher, NezSpriteFont spriteFont, string text, System.Numerics.Vector2 position,
		                              Color color, float rotation,
		                              System.Numerics.Vector2 origin, System.Numerics.Vector2 scale, SpriteEffects effects, float layerDepth)
		{
			Insist.IsFalse(text == null);

			if (text.Length == 0)
				return;

			var source = new FontCharacterSource(text);
			spriteFont.DrawInto(batcher, ref source, position, color, rotation, origin, scale, effects, layerDepth);
		}
	}
}