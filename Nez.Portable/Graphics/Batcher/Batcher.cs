// based on the FNA SpriteBatch implementation by Ethan Lee: https://github.com/FNA-XNA/FNA

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using Nez.Textures;
using Vector2 = System.Numerics.Vector2;


namespace Nez
{
	public class Batcher : GraphicsResource
	{
		/// <summary>
		/// Matrix to be used when creating the projection matrix
		/// </summary>
		/// <value>The transform matrix.</value>
		public Matrix TransformMatrix => _transformMatrix;

		/// <summary>
		/// If true, destination positions will be rounded before being drawn.
		/// </summary>
		public bool ShouldRoundDestinations = true;


		#region variables

		private bool _shouldIgnoreRoundingDestinations;

		// Buffer objects used for actual drawing
		private DynamicVertexBuffer _vertexBuffer;
		private IndexBuffer _indexBuffer;

		// Local data stored before buffering to GPU
		private VertexPositionColorTexture4[] _vertexInfo;
		private Texture2D[] _textureInfo;

		// Default SpriteEffect
		private SpriteEffect _spriteEffect;
		private EffectPass _spriteEffectPass;

		// Tracks Begin/End calls
		private bool _beginCalled;

		// Keep render state for non-Immediate modes.
		private BlendState _blendState;
		private SamplerState _samplerState;
		private DepthStencilState _depthStencilState;
		private RasterizerState _rasterizerState;
		private bool _disableBatching;

		// How many sprites are in the current batch?
		private int _numSprites;

		// Matrix to be used when creating the projection matrix
		private Matrix _transformMatrix;

		// Matrix used internally to calculate the cameras projection
		private Matrix _projectionMatrix;

		// this is the calculated MatrixTransform parameter in sprite shaders
		private Matrix _matrixTransformMatrix;

		// User-provided Effect, if applicable
		private Effect _customEffect;

		#endregion


		#region static variables and constants

		private const int MAX_SPRITES = 2048;
		private const int MAX_VERTICES = MAX_SPRITES * 4;
		private const int MAX_INDICES = MAX_SPRITES * 6;

		// Used to calculate texture coordinates
		private static readonly float[] _cornerOffsetX = new float[] {0.0f, 1.0f, 0.0f, 1.0f};
		private static readonly float[] _cornerOffsetY = new float[] {0.0f, 0.0f, 1.0f, 1.0f};

		private static readonly Vector2[] _cornerOffset = {
			new Vector2(0,0),
			new Vector2(1,0),
			new Vector2(0,1),
			new Vector2(1,1)
		};

		private static readonly short[] _indexData = GenerateIndexArray();

		#endregion


		public Batcher(GraphicsDevice graphicsDevice)
		{
			Insist.IsTrue(graphicsDevice != null);

			GraphicsDevice = graphicsDevice;

			_vertexInfo = new VertexPositionColorTexture4[MAX_SPRITES];
			_textureInfo = new Texture2D[MAX_SPRITES];
			_vertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionColorTexture), MAX_VERTICES,
				BufferUsage.WriteOnly);
			_indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES,
				BufferUsage.WriteOnly);
			_indexBuffer.SetData(_indexData);

			_spriteEffect = new SpriteEffect();
			_spriteEffectPass = _spriteEffect.CurrentTechnique.Passes[0];

			_projectionMatrix = new Matrix(
				0f, //(float)( 2.0 / (double)viewport.Width ) is the actual value we will use
				0.0f,
				0.0f,
				0.0f,
				0.0f,
				0f, //(float)( -2.0 / (double)viewport.Height ) is the actual value we will use
				0.0f,
				0.0f,
				0.0f,
				0.0f,
				1.0f,
				0.0f,
				-1.0f,
				1.0f,
				0.0f,
				1.0f
			);
		}


		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed && disposing)
			{
				_spriteEffect.Dispose();
				_indexBuffer.Dispose();
				_vertexBuffer.Dispose();
			}

			base.Dispose(disposing);
		}


		/// <summary>
		/// sets if position rounding should be ignored. Useful when you are drawing primitives for debugging.
		/// </summary>
		/// <param name="shouldIgnore">If set to <c>true</c> should ignore.</param>
		public void SetIgnoreRoundingDestinations(bool shouldIgnore)
		{
			_shouldIgnoreRoundingDestinations = shouldIgnore;
		}


		#region Public begin/end methods

		public void Begin()
		{
			Begin(BlendState.AlphaBlend, Core.DefaultSamplerState, DepthStencilState.None,
				RasterizerState.CullCounterClockwise, null, Matrix.Identity, false);
		}


		public void Begin(Effect effect)
		{
			Begin(BlendState.AlphaBlend, Core.DefaultSamplerState, DepthStencilState.None,
				RasterizerState.CullCounterClockwise, effect, Matrix.Identity, false);
		}


		public void Begin(Material material)
		{
			Begin(material.BlendState, material.SamplerState, material.DepthStencilState,
				RasterizerState.CullCounterClockwise, material.Effect);
		}


		public void Begin(Matrix transformationMatrix)
		{
			Begin(BlendState.AlphaBlend, Core.DefaultSamplerState, DepthStencilState.None,
				RasterizerState.CullCounterClockwise, null, transformationMatrix, false);
		}


		public void Begin(BlendState blendState)
		{
			Begin(blendState, Core.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
				null, Matrix.Identity, false);
		}


		public void Begin(Material material, Matrix transformationMatrix)
		{
			Begin(material.BlendState, material.SamplerState, material.DepthStencilState,
				RasterizerState.CullCounterClockwise, material.Effect, transformationMatrix, false);
		}


		public void Begin(BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState,
		                  RasterizerState rasterizerState)
		{
			Begin(
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				null,
				Matrix.Identity,
				false
			);
		}


		public void Begin(BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState,
		                  RasterizerState rasterizerState, Effect effect)
		{
			Begin(
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				effect,
				Matrix.Identity,
				false
			);
		}


		public void Begin(BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState,
		                  RasterizerState rasterizerState,
		                  Effect effect, Matrix transformationMatrix)
		{
			Begin(
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				effect,
				transformationMatrix,
				false
			);
		}


		public void Begin(BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState,
		                  RasterizerState rasterizerState,
		                  Effect effect, Matrix transformationMatrix, bool disableBatching)
		{
			Insist.IsFalse(_beginCalled,
				"Begin has been called before calling End after the last call to Begin. Begin cannot be called again until End has been successfully called.");
			_beginCalled = true;

			_blendState = blendState ?? BlendState.AlphaBlend;
			_samplerState = samplerState ?? Core.DefaultSamplerState;
			_depthStencilState = depthStencilState ?? DepthStencilState.None;
			_rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;

			_customEffect = effect;
			_transformMatrix = transformationMatrix;
			_disableBatching = disableBatching;

			if (_disableBatching)
				PrepRenderState();
		}


		public void End()
		{
			Insist.IsTrue(_beginCalled,
				"End was called, but Begin has not yet been called. You must call Begin successfully before you can call End.");
			_beginCalled = false;

			if (!_disableBatching)
				FlushBatch();

			_customEffect = null;
		}

		#endregion


		#region Public draw methods

		public void Draw(Texture2D texture, Vector2 position)
		{
			CheckBegin();
			PushSprite(texture, null, position, Vector2.One,
				Color.White, Vector2.Zero, 0.0f, 0.0f, 0, false, 0, 0, 0, 0);
		}


		public void Draw(Texture2D texture, Vector2 position, Color color)
		{
			CheckBegin();
			PushSprite(texture, null, position, Vector2.One,
				color, Vector2.Zero, 0.0f, 0.0f, 0, false, 0, 0, 0, 0);
		}


		public void Draw(Texture2D texture, Rectangle destinationRectangle)
		{
			CheckBegin();
			PushSprite(texture, null, destinationRectangle.Location.ToSimd(), destinationRectangle.Size.ToSimd(),
				Color.White, Vector2.Zero, 0.0f, 0.0f, 0, true, 0, 0, 0, 0);
		}


		public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color)
		{
			CheckBegin();
			PushSprite(texture, null, destinationRectangle.Location.ToSimd(), destinationRectangle.Size.ToSimd(),
				color, Vector2.Zero, 0.0f, 0.0f, 0, true, 0, 0, 0, 0);
		}


		public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
		{
			CheckBegin();
			PushSprite(texture, sourceRectangle, destinationRectangle.Location.ToSimd(),
				destinationRectangle.Size.ToSimd(),
				color, Vector2.Zero, 0.0f, 0.0f, 0, true, 0, 0, 0, 0);
		}


		public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color,
		                 SpriteEffects effects)
		{
			CheckBegin();
			PushSprite(texture, sourceRectangle, destinationRectangle.Location.ToSimd(),
				destinationRectangle.Size.ToSimd(),
				color, Vector2.Zero, 0.0f, 0.0f, (byte) (effects & (SpriteEffects) 0x03), true, 0, 0, 0, 0);
		}


		public void Draw(
			Texture2D texture,
			Rectangle destinationRectangle,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			SpriteEffects effects,
			float layerDepth,
			float skewTopX, float skewBottomX, float skewLeftY, float skewRightY
		)
		{
			CheckBegin();
			PushSprite(
				texture,
				sourceRectangle,
				destinationRectangle.Location.ToSimd(),
				destinationRectangle.Size.ToSimd(),
				color,
				Vector2.Zero,
				rotation,
				layerDepth,
				(byte) (effects & (SpriteEffects) 0x03),
				true,
				skewTopX, skewBottomX, skewLeftY, skewRightY
			);
		}


		public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
		{
			CheckBegin();
			PushSprite(
				texture,
				sourceRectangle,
				position,
				Vector2.One,
				color,
				Vector2.Zero,
				0.0f,
				0.0f,
				0,
				false,
				0, 0, 0, 0
			);
		}


		public void Draw(
			Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effects,
			float layerDepth
		)
		{
			CheckBegin();
			PushSprite(
				texture,
				sourceRectangle,
				position,
				new Vector2(scale),
				color,
				origin,
				rotation,
				layerDepth,
				(byte) (effects & (SpriteEffects) 0x03),
				false,
				0, 0, 0, 0
			);
		}


		public void Draw(
			Sprite sprite,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effects,
			float layerDepth
		)
		{
			CheckBegin();
			PushSprite(
				sprite,
				position,
				scale,
				scale,
				color,
				origin,
				rotation,
				layerDepth,
				(byte) (effects & (SpriteEffects) 0x03),
				0, 0, 0, 0
			);
		}


		public void Draw(
			Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth
		)
		{
			CheckBegin();
			PushSprite(
				texture,
				sourceRectangle,
				position,
				scale,
				color,
				origin,
				rotation,
				layerDepth,
				(byte) (effects & (SpriteEffects) 0x03),
				false,
				0, 0, 0, 0
			);
		}

		public void Draw(
			Texture2D texture,
			Vector2 destination,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects sEffects,
			float depth
		)
		{
			CheckBegin();
			PushSprite(texture, destination, scale, color, origin, rotation, depth,
				(byte) (sEffects & (SpriteEffects) 0x3));
		}

		public void Draw(
			Sprite sprite,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth
		)
		{
			CheckBegin();
			PushSprite(
				sprite,
				position,
				scale.X,
				scale.Y,
				color,
				origin,
				rotation,
				layerDepth,
				(byte)(effects & (SpriteEffects)0x03),
				0, 0, 0, 0
			);
		}

		public void Draw(
			Sprite sprite,
			Vector2 position,
			Color color,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth
		)
		{
			CheckBegin();
			PushSprite(
				sprite,
				position,
				scale.X,
				scale.Y,
				color,
				Vector2.Zero,
				0f,
				layerDepth,
				(byte)(effects & (SpriteEffects)0x03),
				0, 0, 0, 0
			);
		}

		public void Draw(
			Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth,
			float skewTopX, float skewBottomX, float skewLeftY, float skewRightY
		)
		{
			CheckBegin();
			PushSprite(
				texture,
				sourceRectangle,
				position,
				scale,
				color,
				origin,
				rotation,
				layerDepth,
				(byte) (effects & (SpriteEffects) 0x03),
				false,
				skewTopX, skewBottomX, skewLeftY, skewRightY
			);
		}


		public void Draw(
			Texture2D texture,
			Rectangle destinationRectangle,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			SpriteEffects effects,
			float layerDepth
		)
		{
			CheckBegin();
			PushSprite(
				texture,
				sourceRectangle,
				destinationRectangle.Location.ToSimd(),
				destinationRectangle.Size.ToSimd(),
				color,
				origin,
				rotation,
				layerDepth,
				(byte) (effects & (SpriteEffects) 0x03),
				true,
				0, 0, 0, 0
			);
		}


		/// <summary>
		/// direct access to setting vert positions, UVs and colors. The order of elements is top-left, top-right, bottom-left, bottom-right
		/// </summary>
		/// <returns>The raw.</returns>
		/// <param name="texture">Texture.</param>
		/// <param name="verts">Verts.</param>
		/// <param name="textureCoords">Texture coords.</param>
		/// <param name="colors">Colors.</param>
		public void DrawRaw(Texture2D texture, System.Numerics.Vector3[] verts, Vector2[] textureCoords, Color[] colors)
		{
			Insist.IsTrue(verts.Length == 4, "there must be only 4 verts");
			Insist.IsTrue(textureCoords.Length == 4, "there must be only 4 texture coordinates");
			Insist.IsTrue(colors.Length == 4, "there must be only 4 colors");

			// we're out of space, flush
			if (_numSprites >= MAX_SPRITES)
				FlushBatch();

			_vertexInfo[_numSprites].Position0 = verts[0];
			_vertexInfo[_numSprites].Position1 = verts[1];
			_vertexInfo[_numSprites].Position2 = verts[2];
			_vertexInfo[_numSprites].Position3 = verts[3];

			_vertexInfo[_numSprites].TextureCoordinate0 = textureCoords[0];
			_vertexInfo[_numSprites].TextureCoordinate1 = textureCoords[1];
			_vertexInfo[_numSprites].TextureCoordinate2 = textureCoords[2];
			_vertexInfo[_numSprites].TextureCoordinate3 = textureCoords[3];

			_vertexInfo[_numSprites].Color0 = colors[0];
			_vertexInfo[_numSprites].Color1 = colors[1];
			_vertexInfo[_numSprites].Color2 = colors[2];
			_vertexInfo[_numSprites].Color3 = colors[3];

			if (_disableBatching)
			{
				_vertexBuffer.SetData(0, _vertexInfo, 0, 1, VertexPositionColorTexture4.RealStride,
					SetDataOptions.None);
				DrawPrimitives(texture, 0, 1);
			}
			else
			{
				_textureInfo[_numSprites] = texture;
				_numSprites += 1;
			}
		}


		/// <summary>
		/// direct access to setting vert positions, UVs and colors. The order of elements is top-left, top-right, bottom-left, bottom-right
		/// </summary>
		/// <returns>The raw.</returns>
		/// <param name="texture">Texture.</param>
		/// <param name="verts">Verts.</param>
		/// <param name="textureCoords">Texture coords.</param>
		/// <param name="color">Color.</param>
		public void DrawRaw(Texture2D texture, System.Numerics.Vector3[] verts, Vector2[] textureCoords, Color color)
		{
			Insist.IsTrue(verts.Length == 4, "there must be only 4 verts");
			Insist.IsTrue(textureCoords.Length == 4, "there must be only 4 texture coordinates");

			// we're out of space, flush
			if (_numSprites >= MAX_SPRITES)
				FlushBatch();

			_vertexInfo[_numSprites].Position0 = verts[0];
			_vertexInfo[_numSprites].Position1 = verts[1];
			_vertexInfo[_numSprites].Position2 = verts[2];
			_vertexInfo[_numSprites].Position3 = verts[3];

			_vertexInfo[_numSprites].TextureCoordinate0 = textureCoords[0];
			_vertexInfo[_numSprites].TextureCoordinate1 = textureCoords[1];
			_vertexInfo[_numSprites].TextureCoordinate2 = textureCoords[2];
			_vertexInfo[_numSprites].TextureCoordinate3 = textureCoords[3];

			_vertexInfo[_numSprites].Color0 = color;
			_vertexInfo[_numSprites].Color1 = color;
			_vertexInfo[_numSprites].Color2 = color;
			_vertexInfo[_numSprites].Color3 = color;

			if (_disableBatching)
			{
				_vertexBuffer.SetData(0, _vertexInfo, 0, 1, VertexPositionColorTexture4.RealStride,
					SetDataOptions.None);
				DrawPrimitives(texture, 0, 1);
			}
			else
			{
				_textureInfo[_numSprites] = texture;
				_numSprites += 1;
			}
		}

		#endregion


		[Obsolete("SpriteFont is too locked down to use directly. Wrap it in a NezSpriteFont")]
		public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation,
		                       Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
		{
			throw new NotImplementedException(
				"SpriteFont is too locked down to use directly. Wrap it in a NezSpriteFont");
		}


		private static short[] GenerateIndexArray()
		{
			var result = new short[MAX_INDICES];
			for (int i = 0, j = 0; i < MAX_INDICES; i += 6, j += 4)
			{
				result[i] = (short) (j);
				result[i + 1] = (short) (j + 1);
				result[i + 2] = (short) (j + 2);
				result[i + 3] = (short) (j + 3);
				result[i + 4] = (short) (j + 2);
				result[i + 5] = (short) (j + 1);
			}

			return result;
		}


		#region Methods

		/// <summary>
		/// the meat of the Batcher. This is where it all goes down
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PushSprite(Texture2D texture, Vector2 destination, Vector2 destinationSize, Color color,
			Vector2 origin, float rotation, float depth, byte effects)
		{
			// out of space, flush
			if (_numSprites >= MAX_SPRITES)
				FlushBatch();

			if (!_shouldIgnoreRoundingDestinations && ShouldRoundDestinations)
			{
				destination.X = Mathf.Round(destination.X);
				destination.Y = Mathf.Round(destination.Y);
			}

			// Source/Destination/Origin Calculations
			var source = Vector2.Zero;
			var sourceSize = Vector2.One;

			var tOrigin = origin * (Vector2.One / texture.Bounds.Size.ToSimd());
			destinationSize *= texture.Bounds.Size.ToSimd();


			// Rotation Calculations
			var matrix = System.Numerics.Matrix3x2.Identity;
			if (!Mathf.WithinEpsilon(rotation))
			{
				matrix = System.Numerics.Matrix3x2.CreateRotation(rotation);
			}
			matrix.Translation = destination;

			// calculate vertices
			// top-left
			var corner = -tOrigin * destinationSize;
			var result = Vector2.Transform(corner, matrix);
			_vertexInfo[_numSprites].Position0 = new System.Numerics.Vector3(result, depth);

			// top-right
			corner = (_cornerOffset[1] - tOrigin) * destinationSize;
			result = Vector2.Transform(corner, matrix);
			_vertexInfo[_numSprites].Position1 = new System.Numerics.Vector3(result, depth);

			// bottom-left
			corner = (_cornerOffset[2] - tOrigin) * destinationSize;
			result = Vector2.Transform(corner, matrix);
			_vertexInfo[_numSprites].Position2 = new System.Numerics.Vector3(result, depth);

			// bottom-right
			corner = (_cornerOffset[3] - tOrigin) * destinationSize;
			result = Vector2.Transform(corner, matrix);
			_vertexInfo[_numSprites].Position3 = new System.Numerics.Vector3(result, depth);

			_vertexInfo[_numSprites].TextureCoordinate0 = (_cornerOffset[0 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].TextureCoordinate1 = (_cornerOffset[1 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].TextureCoordinate2 = (_cornerOffset[2 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].TextureCoordinate3 = (_cornerOffset[3 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].Color0 = color;
			_vertexInfo[_numSprites].Color1 = color;
			_vertexInfo[_numSprites].Color2 = color;
			_vertexInfo[_numSprites].Color3 = color;

			if (_disableBatching)
			{
				_vertexBuffer.SetData(0, _vertexInfo, 0, 1, VertexPositionColorTexture4.RealStride,
					SetDataOptions.None);
				DrawPrimitives(texture, 0, 1);
			}
			else
			{
				_textureInfo[_numSprites] = texture;
				_numSprites += 1;
			}
		}
		/// <summary>
		/// the meat of the Batcher. This is where it all goes down
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PushSprite(Texture2D texture, Rectangle? sourceRectangle, Vector2 destination,
		                Vector2 destinationSize, Color color, Vector2 origin,
		                float rotation, float depth, byte effects, bool destSizeInPixels, float skewTopX,
		                float skewBottomX, float skewLeftY, float skewRightY)
		{
			// out of space, flush
			if (_numSprites >= MAX_SPRITES)
				FlushBatch();

			if (!_shouldIgnoreRoundingDestinations && ShouldRoundDestinations)
			{
				destination.X = Mathf.Round(destination.X);
				destination.Y = Mathf.Round(destination.Y);
			}

			// Source/Destination/Origin Calculations
			Vector2 source, sourceSize, tOrigin;
			if (sourceRectangle.HasValue)
			{
				var inverseTexSize = Vector2.One / texture.Bounds.Size.ToSimd();

				source = sourceRectangle.Value.Location.ToSimd() * inverseTexSize;
				sourceSize = sourceRectangle.Value.Size.ToSimd() * inverseTexSize;

				tOrigin = (origin / sourceSize) * inverseTexSize;

				if (!destSizeInPixels)
				{
					destinationSize *= sourceRectangle.Value.Location.ToSimd();
				}
			}
			else
			{
				source = Vector2.Zero;
				sourceSize = Vector2.One;

				tOrigin = origin * (Vector2.One / texture.Bounds.Size.ToSimd());


				if (!destSizeInPixels)
				{
					destinationSize *= texture.Bounds.Size.ToSimd();
				}
			}

			// Rotation Calculations
			float rotationMatrix1X;
			float rotationMatrix1Y;
			float rotationMatrix2X;
			float rotationMatrix2Y;
			if (!Mathf.WithinEpsilon(rotation))
			{
				var sin = Mathf.Sin(rotation);
				var cos = Mathf.Cos(rotation);
				rotationMatrix1X = cos;
				rotationMatrix1Y = sin;
				rotationMatrix2X = -sin;
				rotationMatrix2Y = cos;
			}
			else
			{
				rotationMatrix1X = 1.0f;
				rotationMatrix1Y = 0.0f;
				rotationMatrix2X = 0.0f;
				rotationMatrix2Y = 1.0f;
			}


			// flip our skew values if we have a flipped sprite
			if (effects != 0)
			{
				skewTopX *= -1;
				skewBottomX *= -1;
				skewLeftY *= -1;
				skewRightY *= -1;
			}

			// calculate vertices
			// top-left
			var cornerX = (_cornerOffsetX[0] - tOrigin.X) * destinationSize.X + skewTopX;
			var cornerY = (_cornerOffsetY[0] - tOrigin.Y) * destinationSize.Y - skewLeftY;
			_vertexInfo[_numSprites].Position0.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position0.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			// top-right
			cornerX = (_cornerOffsetX[1] - tOrigin.X) * destinationSize.X + skewTopX;
			cornerY = (_cornerOffsetY[1] - tOrigin.Y) * destinationSize.Y - skewRightY;
			_vertexInfo[_numSprites].Position1.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position1.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			// bottom-left
			cornerX = (_cornerOffsetX[2] - tOrigin.X) * destinationSize.X + skewBottomX;
			cornerY = (_cornerOffsetY[2] - tOrigin.Y) * destinationSize.Y - skewLeftY;
			_vertexInfo[_numSprites].Position2.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position2.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			// bottom-right
			cornerX = (_cornerOffsetX[3] - tOrigin.X) * destinationSize.X + skewBottomX;
			cornerY = (_cornerOffsetY[3] - tOrigin.Y) * destinationSize.Y - skewRightY;
			_vertexInfo[_numSprites].Position3.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position3.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			_vertexInfo[_numSprites].TextureCoordinate0 = (_cornerOffset[0 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].TextureCoordinate1 = (_cornerOffset[1 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].TextureCoordinate2 = (_cornerOffset[2 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].TextureCoordinate3 = (_cornerOffset[3 ^ effects] * sourceSize) + source;
			_vertexInfo[_numSprites].Position0.Z = depth;
			_vertexInfo[_numSprites].Position1.Z = depth;
			_vertexInfo[_numSprites].Position2.Z = depth;
			_vertexInfo[_numSprites].Position3.Z = depth;
			_vertexInfo[_numSprites].Color0 = color;
			_vertexInfo[_numSprites].Color1 = color;
			_vertexInfo[_numSprites].Color2 = color;
			_vertexInfo[_numSprites].Color3 = color;

			if (_disableBatching)
			{
				_vertexBuffer.SetData(0, _vertexInfo, 0, 1, VertexPositionColorTexture4.RealStride,
					SetDataOptions.None);
				DrawPrimitives(texture, 0, 1);
			}
			else
			{
				_textureInfo[_numSprites] = texture;
				_numSprites += 1;
			}
		}


		/// <summary>
		/// Sprite alternative to the old SpriteBatch pushSprite
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PushSprite(Sprite sprite, Vector2 destination, float destinationW,
		                float destinationH, Color color, Vector2 origin,
		                float rotation, float depth, byte effects, float skewTopX, float skewBottomX, float skewLeftY,
		                float skewRightY)
		{
			// out of space, flush
			if (_numSprites >= MAX_SPRITES)
				FlushBatch();

			// Source/Destination/Origin Calculations. destinationW/H is the scale value so we multiply by the size of the texture region
			var tOrigin = (origin / sprite.Uvs.Size) / sprite.Texture2D.Bounds.Size.ToSimd();
			destinationW *= sprite.SourceRect.Width;
			destinationH *= sprite.SourceRect.Height;

			// Rotation Calculations
			float rotationMatrix1X;
			float rotationMatrix1Y;
			float rotationMatrix2X;
			float rotationMatrix2Y;
			if (!Mathf.WithinEpsilon(rotation))
			{
				var sin = Mathf.Sin(rotation);
				var cos = Mathf.Cos(rotation);
				rotationMatrix1X = cos;
				rotationMatrix1Y = sin;
				rotationMatrix2X = -sin;
				rotationMatrix2Y = cos;
			}
			else
			{
				rotationMatrix1X = 1.0f;
				rotationMatrix1Y = 0.0f;
				rotationMatrix2X = 0.0f;
				rotationMatrix2Y = 1.0f;
			}


			// flip our skew values if we have a flipped sprite
			if (effects != 0)
			{
				skewTopX *= -1;
				skewBottomX *= -1;
				skewLeftY *= -1;
				skewRightY *= -1;
			}

			// calculate vertices
			// top-left
			//var corner = (_cornerOffset[0] -tOrigin) 
			var cornerX = (_cornerOffsetX[0] - tOrigin.X) * destinationW + skewTopX;
			var cornerY = (_cornerOffsetY[0] - tOrigin.Y) * destinationH - skewLeftY;
			_vertexInfo[_numSprites].Position0.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position0.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			// top-right
			cornerX = (_cornerOffsetX[1] - tOrigin.X) * destinationW + skewTopX;
			cornerY = (_cornerOffsetY[1] - tOrigin.Y) * destinationH - skewRightY;
			_vertexInfo[_numSprites].Position1.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position1.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			// bottom-left
			cornerX = (_cornerOffsetX[2] - tOrigin.X) * destinationW + skewBottomX;
			cornerY = (_cornerOffsetY[2] - tOrigin.Y) * destinationH - skewLeftY;
			_vertexInfo[_numSprites].Position2.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position2.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			// bottom-right
			cornerX = (_cornerOffsetX[3] - tOrigin.X) * destinationW + skewBottomX;
			cornerY = (_cornerOffsetY[3] - tOrigin.Y) * destinationH - skewRightY;
			_vertexInfo[_numSprites].Position3.X = (
				(rotationMatrix2X * cornerY) +
				(rotationMatrix1X * cornerX) +
				destination.X
			);
			_vertexInfo[_numSprites].Position3.Y = (
				(rotationMatrix2Y * cornerY) +
				(rotationMatrix1Y * cornerX) +
				destination.Y
			);

			_vertexInfo[_numSprites].TextureCoordinate0 = (_cornerOffset[0 ^ effects] * sprite.Uvs.Size) + sprite.Uvs.Location;
			_vertexInfo[_numSprites].TextureCoordinate1 = (_cornerOffset[1 ^ effects] * sprite.Uvs.Size) + sprite.Uvs.Location;
			_vertexInfo[_numSprites].TextureCoordinate2 = (_cornerOffset[2 ^ effects] * sprite.Uvs.Size) + sprite.Uvs.Location;
			_vertexInfo[_numSprites].TextureCoordinate3 = (_cornerOffset[3 ^ effects] * sprite.Uvs.Size) + sprite.Uvs.Location;
			_vertexInfo[_numSprites].Position0.Z = depth;
			_vertexInfo[_numSprites].Position1.Z = depth;
			_vertexInfo[_numSprites].Position2.Z = depth;
			_vertexInfo[_numSprites].Position3.Z = depth;
			_vertexInfo[_numSprites].Color0 = color;
			_vertexInfo[_numSprites].Color1 = color;
			_vertexInfo[_numSprites].Color2 = color;
			_vertexInfo[_numSprites].Color3 = color;

			if (_disableBatching)
			{
				_vertexBuffer.SetData(0, _vertexInfo, 0, 1, VertexPositionColorTexture4.RealStride,
					SetDataOptions.None);
				DrawPrimitives(sprite, 0, 1);
			}
			else
			{
				_textureInfo[_numSprites] = sprite;
				_numSprites += 1;
			}
		}


		public void FlushBatch()
		{
			if (_numSprites == 0)
				return;

			var offset = 0;
			Texture2D curTexture = null;

			PrepRenderState();

			_vertexBuffer.SetData(0, _vertexInfo, 0, _numSprites, VertexPositionColorTexture4.RealStride,
				SetDataOptions.None);

			curTexture = _textureInfo[0];
			for (var i = 1; i < _numSprites; i += 1)
			{
				if (_textureInfo[i] != curTexture)
				{
					DrawPrimitives(curTexture, offset, i - offset);
					curTexture = _textureInfo[i];
					offset = i;
				}
			}

			DrawPrimitives(curTexture, offset, _numSprites - offset);

			_numSprites = 0;
		}


		/// <summary>
		/// enables/disables scissor testing. If the RasterizerState changes it will cause a batch flush.
		/// </summary>
		/// <returns>The scissor test.</returns>
		/// <param name="shouldEnable">Should enable.</param>
		public void EnableScissorTest(bool shouldEnable)
		{
			var currentValue = _rasterizerState.ScissorTestEnable;
			if (currentValue == shouldEnable)
				return;

			FlushBatch();

			_rasterizerState = new RasterizerState
			{
				CullMode = _rasterizerState.CullMode,
				DepthBias = _rasterizerState.DepthBias,
				FillMode = _rasterizerState.FillMode,
				MultiSampleAntiAlias = _rasterizerState.MultiSampleAntiAlias,
				SlopeScaleDepthBias = _rasterizerState.SlopeScaleDepthBias,
				ScissorTestEnable = shouldEnable
			};
		}


		private void PrepRenderState()
		{
			GraphicsDevice.BlendState = _blendState;
			GraphicsDevice.SamplerStates[0] = _samplerState;
			GraphicsDevice.DepthStencilState = _depthStencilState;
			GraphicsDevice.RasterizerState = _rasterizerState;

			GraphicsDevice.SetVertexBuffer(_vertexBuffer);
			GraphicsDevice.Indices = _indexBuffer;

			var viewport = GraphicsDevice.Viewport;

			// inlined CreateOrthographicOffCenter
#if FNA
			_projectionMatrix.M11 = (float)( 2.0 / (double) ( viewport.Width / 2 * 2 - 1 ) );
			_projectionMatrix.M22 = (float)( -2.0 / (double) ( viewport.Height / 2 * 2 - 1 ) );
#else
			_projectionMatrix.M11 = (float) (2.0 / (double) viewport.Width);
			_projectionMatrix.M22 = (float) (-2.0 / (double) viewport.Height);
#endif

			_projectionMatrix.M41 = -1 - 0.5f * _projectionMatrix.M11;
			_projectionMatrix.M42 = 1 - 0.5f * _projectionMatrix.M22;

			Matrix.Multiply(ref _transformMatrix, ref _projectionMatrix, out _matrixTransformMatrix);
			_spriteEffect.SetMatrixTransform(ref _matrixTransformMatrix);

			// we have to Apply here because custom effects often wont have a vertex shader and we need the default SpriteEffect's
			_spriteEffectPass.Apply();
		}


		private void DrawPrimitives(Texture texture, int baseSprite, int batchSize)
		{
			if (_customEffect != null)
			{
				foreach (var pass in _customEffect.CurrentTechnique.Passes)
				{
					pass.Apply();

					// Whatever happens in pass.Apply, make sure the texture being drawn ends up in Textures[0].
					GraphicsDevice.Textures[0] = texture;
					GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 2);
				}
			}
			else
			{
				GraphicsDevice.Textures[0] = texture;
				GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 2);
			}
		}


		[System.Diagnostics.Conditional("DEBUG")]
		private void CheckBegin()
		{
			if (!_beginCalled)
				throw new InvalidOperationException(
					"Begin has not been called. Begin must be called before you can draw");
		}

		#endregion


		#region Sprite Data Container Class

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct VertexPositionColorTexture4 : IVertexType
		{
			public const int RealStride = 96;

			VertexDeclaration IVertexType.VertexDeclaration => throw new NotImplementedException();

			public System.Numerics.Vector3 Position0;
			public Color Color0;
			public Vector2 TextureCoordinate0;
			public System.Numerics.Vector3 Position1;
			public Color Color1;
			public Vector2 TextureCoordinate1;
			public System.Numerics.Vector3 Position2;
			public Color Color2;
			public Vector2 TextureCoordinate2;
			public System.Numerics.Vector3 Position3;
			public Color Color3;
			public Vector2 TextureCoordinate3;
		}

		#endregion
	}
}