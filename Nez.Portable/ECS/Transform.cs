using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Nez
{
	public class Transform
	{
		[Flags]
		enum DirtyType
		{
			Clean,
			PositionDirty,
			ScaleDirty,
			RotationDirty
		}

		public enum Component
		{
			Position,
			Scale,
			Rotation
		}


		#region properties and fields

		/// <summary>
		/// the Entity associated with this transform
		/// </summary>
		public readonly Entity Entity;

		/// <summary>
		/// the parent Transform of this Transform
		/// </summary>
		/// <value>The parent.</value>
		public Transform Parent
		{
			get => _parent;
			set => SetParent(value);
		}


		/// <summary>
		/// total children of this Transform
		/// </summary>
		/// <value>The child count.</value>
		public int ChildCount => _children.Count;


		/// <summary>
		/// position of the transform in world space
		/// </summary>
		/// <value>The position.</value>
		public System.Numerics.Vector2 Position
		{
			get
			{
				UpdateTransform();
				if (_positionDirty)
				{
					if (Parent == null)
					{
						_position = _localPosition;
					}
					else
					{
						Parent.UpdateTransform();
						_position = System.Numerics.Vector2.Transform(_localPosition, Parent._worldTransform);
					}

					_positionDirty = false;
				}

				return _position;
			}
			set => SetPosition(value);
		}


		/// <summary>
		/// position of the transform relative to the parent transform. If the transform has no parent, it is the same as Transform.position
		/// </summary>
		/// <value>The local position.</value>
		public System.Numerics.Vector2 LocalPosition
		{
			get
			{
				UpdateTransform();
				return _localPosition;
			}
			set => SetLocalPosition(value);
		}


		/// <summary>
		/// rotation of the transform in world space in radians
		/// </summary>
		/// <value>The rotation.</value>
		public float Rotation
		{
			get
			{
				UpdateTransform();
				return _rotation;
			}
			set => SetRotation(value);
		}


		/// <summary>
		/// rotation of the transform in world space in degrees
		/// </summary>
		/// <value>The rotation degrees.</value>
		public float RotationDegrees
		{
			get => MathHelper.ToDegrees(_rotation);
			set => SetRotation(MathHelper.ToRadians(value));
		}


		/// <summary>
		/// the rotation of the transform relative to the parent transform's rotation. If the transform has no parent, it is the same as Transform.rotation
		/// </summary>
		/// <value>The local rotation.</value>
		public float LocalRotation
		{
			get
			{
				UpdateTransform();
				return _localRotation;
			}
			set => SetLocalRotation(value);
		}


		/// <summary>
		/// rotation of the transform relative to the parent transform's rotation in degrees
		/// </summary>
		/// <value>The rotation degrees.</value>
		public float LocalRotationDegrees
		{
			get => MathHelper.ToDegrees(_localRotation);
			set => LocalRotation = MathHelper.ToRadians(value);
		}


		/// <summary>
		/// global scale of the transform
		/// </summary>
		/// <value>The scale.</value>
		public System.Numerics.Vector2 Scale
		{
			get
			{
				UpdateTransform();
				return _scale;
			}
			set => SetScale(value);
		}


		/// <summary>
		/// the scale of the transform relative to the parent. If the transform has no parent, it is the same as Transform.scale
		/// </summary>
		/// <value>The local scale.</value>
		public System.Numerics.Vector2 LocalScale
		{
			get
			{
				UpdateTransform();
				return _localScale;
			}
			set => SetLocalScale(value);
		}


		public System.Numerics.Matrix3x2 WorldInverseTransform
		{
			get
			{
				UpdateTransform();
				if (_worldInverseDirty)
				{
					System.Numerics.Matrix3x2.Invert(_worldTransform, out _worldInverseTransform);
					_worldInverseDirty = false;
				}

				return _worldInverseTransform;
			}
		}


		public System.Numerics.Matrix3x2 LocalToWorldTransform
		{
			get
			{
				UpdateTransform();
				return _worldTransform;
			}
		}


		public System.Numerics.Matrix3x2 WorldToLocalTransform
		{
			get
			{
				if (_worldToLocalDirty)
				{
					if (Parent == null)
					{
						_worldToLocalTransform = System.Numerics.Matrix3x2.Identity;
					}
					else
					{
						Parent.UpdateTransform();
						System.Numerics.Matrix3x2.Invert( Parent._worldTransform, out _worldToLocalTransform);
					}

					_worldToLocalDirty = false;
				}

				return _worldToLocalTransform;
			}
		}


		Transform _parent;
		DirtyType hierarchyDirty;

		bool _localDirty;
		bool _localPositionDirty;
		bool _localScaleDirty;
		bool _localRotationDirty;
		bool _positionDirty;
		bool _worldToLocalDirty;
		bool _worldInverseDirty;

		// value is automatically recomputed from the position, rotation and scale
		System.Numerics.Matrix3x2 _localTransform;

		// value is automatically recomputed from the local and the parent matrices.
		System.Numerics.Matrix3x2 _worldTransform = System.Numerics.Matrix3x2.Identity;
		System.Numerics.Matrix3x2 _worldToLocalTransform = System.Numerics.Matrix3x2.Identity;
		System.Numerics.Matrix3x2 _worldInverseTransform = System.Numerics.Matrix3x2.Identity;

		System.Numerics.Matrix3x2 _rotationMatrix;
		System.Numerics.Matrix3x2 _translationMatrix;
		System.Numerics.Matrix3x2 _scaleMatrix;

		System.Numerics.Vector2 _position;
		System.Numerics.Vector2 _scale;
		float _rotation;

		System.Numerics.Vector2 _localPosition;
		System.Numerics.Vector2 _localScale;
		float _localRotation;

		List<Transform> _children = new List<Transform>();

		#endregion


		public Transform(Entity entity)
		{
			Entity = entity;
			_scale = _localScale = System.Numerics.Vector2.One;
		}


		/// <summary>
		/// returns the Transform child at index
		/// </summary>
		/// <returns>The child.</returns>
		/// <param name="index">Index.</param>
		public Transform GetChild(int index)
		{
			return _children[index];
		}


		#region Fluent setters

		/// <summary>
		/// sets the parent Transform of this Transform
		/// </summary>
		/// <returns>The parent.</returns>
		/// <param name="parent">Parent.</param>
		public Transform SetParent(Transform parent)
		{
			if (_parent == parent)
				return this;

			if (_parent != null)
				_parent._children.Remove(this);

			if (parent != null)
				parent._children.Add(this);

			_parent = parent;
			SetDirty(DirtyType.PositionDirty);

			return this;
		}


		/// <summary>
		/// sets the position of the transform in world space
		/// </summary>
		/// <returns>The position.</returns>
		/// <param name="position">Position.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetPosition(System.Numerics.Vector2 position)
		{
			if (position == _position)
				return this;

			_position = position;
			if (Parent != null)
				LocalPosition = System.Numerics.Vector2.Transform(_position, WorldToLocalTransform);
			else
				LocalPosition = position;

			_positionDirty = false;

			return this;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetPosition(float x, float y)
		{
			return SetPosition(new System.Numerics.Vector2(x, y));
		}


		/// <summary>
		/// sets the position of the transform relative to the parent transform. If the transform has no parent, it is the same
		/// as Transform.position
		/// </summary>
		/// <returns>The local position.</returns>
		/// <param name="localPosition">Local position.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetLocalPosition(System.Numerics.Vector2 localPosition)
		{
			if (localPosition == _localPosition)
				return this;

			_localPosition = localPosition;
			_localDirty = _positionDirty = _localPositionDirty = _localRotationDirty = _localScaleDirty = true;
			SetDirty(DirtyType.PositionDirty);

			return this;
		}


		/// <summary>
		/// sets the rotation of the transform in world space in radians
		/// </summary>
		/// <returns>The rotation.</returns>
		/// <param name="radians">Radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetRotation(float radians)
		{
			_rotation = radians;
			if (Parent != null)
				LocalRotation = Parent.Rotation + radians;
			else
				LocalRotation = radians;

			return this;
		}


		/// <summary>
		/// sets the rotation of the transform in world space in degrees
		/// </summary>
		/// <returns>The rotation.</returns>
		/// <param name="radians">Radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetRotationDegrees(float degrees)
		{
			return SetRotation(MathHelper.ToRadians(degrees));
		}


		/// <summary>
		/// sets the the rotation of the transform relative to the parent transform's rotation. If the transform has no parent, it is the
		/// same as Transform.rotation
		/// </summary>
		/// <returns>The local rotation.</returns>
		/// <param name="radians">Radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetLocalRotation(float radians)
		{
			_localRotation = radians;
			_localDirty = _positionDirty = _localPositionDirty = _localRotationDirty = _localScaleDirty = true;
			SetDirty(DirtyType.RotationDirty);

			return this;
		}


		/// <summary>
		/// sets the the rotation of the transform relative to the parent transform's rotation. If the transform has no parent, it is the
		/// same as Transform.rotation
		/// </summary>
		/// <returns>The local rotation.</returns>
		/// <param name="radians">Radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetLocalRotationDegrees(float degrees)
		{
			return SetLocalRotation(MathHelper.ToRadians(degrees));
		}

		/// <summary>
		/// Rotate so the top of the sprite is facing <see cref="pos"/>
		/// </summary>
		/// <param name="pos">The position to look at</param>
		public void LookAt(System.Numerics.Vector2 pos)
		{
			var sign = _position.X > pos.X ? -1 : 1;
			var vectorToAlignTo = System.Numerics.Vector2.Normalize(_position - pos);
			Rotation = sign * Mathf.Acos(System.Numerics.Vector2.Dot(vectorToAlignTo, System.Numerics.Vector2.UnitY));
		}

		/// <summary>
		/// sets the global scale of the transform
		/// </summary>
		/// <returns>The scale.</returns>
		/// <param name="scale">Scale.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetScale(System.Numerics.Vector2 scale)
		{
			_scale = scale;
			if (Parent != null)
				LocalScale = scale / Parent._scale;
			else
				LocalScale = scale;

			return this;
		}


		/// <summary>
		/// sets the global scale of the transform
		/// </summary>
		/// <returns>The scale.</returns>
		/// <param name="scale">Scale.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetScale(float scale)
		{
			return SetScale(new System.Numerics.Vector2(scale));
		}


		/// <summary>
		/// sets the scale of the transform relative to the parent. If the transform has no parent, it is the same as Transform.scale
		/// </summary>
		/// <returns>The local scale.</returns>
		/// <param name="scale">Scale.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetLocalScale(System.Numerics.Vector2 scale)
		{
			_localScale = scale;
			_localDirty = _positionDirty = _localScaleDirty = true;
			SetDirty(DirtyType.ScaleDirty);

			return this;
		}


		/// <summary>
		/// sets the scale of the transform relative to the parent. If the transform has no parent, it is the same as Transform.scale
		/// </summary>
		/// <returns>The local scale.</returns>
		/// <param name="scale">Scale.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform SetLocalScale(float scale)
		{
			return SetLocalScale(new System.Numerics.Vector2(scale));
		}

		#endregion


		/// <summary>
		/// rounds the position of the Transform
		/// </summary>
		public void RoundPosition()
		{
			Position = _position.Round();
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void UpdateTransform()
		{
			if (hierarchyDirty != DirtyType.Clean)
			{
				if (Parent != null)
					Parent.UpdateTransform();

				if (_localDirty)
				{
					if (_localPositionDirty)
					{
						_translationMatrix = System.Numerics.Matrix3x2.CreateTranslation(_localPosition);
						_localPositionDirty = false;
					}

					if (_localRotationDirty)
					{
						_rotationMatrix = System.Numerics.Matrix3x2.CreateRotation(_localRotation);
						_localRotationDirty = false;
					}

					if (_localScaleDirty)
					{
						_scaleMatrix = System.Numerics.Matrix3x2.CreateScale(_localScale);
						_localScaleDirty = false;
					}

					_localTransform = _scaleMatrix * _rotationMatrix;
					_localTransform = _localTransform * _translationMatrix;

					if (Parent == null)
					{
						_worldTransform = _localTransform;
						_rotation = _localRotation;
						_scale = _localScale;
						_worldInverseDirty = true;
					}

					_localDirty = false;
				}

				if (Parent != null)
				{
					_worldTransform = _localTransform * Parent._worldTransform;

					_rotation = _localRotation + Parent._rotation;
					_scale = Parent._scale * _localScale;
					_worldInverseDirty = true;
				}

				_worldToLocalDirty = true;
				_positionDirty = true;
				hierarchyDirty = DirtyType.Clean;
			}
		}


		/// <summary>
		/// sets the dirty flag on the enum and passes it down to our children
		/// </summary>
		/// <param name="dirtyFlagType">Dirty flag type.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void SetDirty(DirtyType dirtyFlagType)
		{
			if ((hierarchyDirty & dirtyFlagType) == 0)
			{
				hierarchyDirty |= dirtyFlagType;

				switch (dirtyFlagType)
				{
					case DirtyType.PositionDirty:
						Entity.OnTransformChanged(Component.Position);
						break;
					case DirtyType.RotationDirty:
						Entity.OnTransformChanged(Component.Rotation);
						break;
					case DirtyType.ScaleDirty:
						Entity.OnTransformChanged(Component.Scale);
						break;
				}

				// dirty our children as well so they know of the changes
				for (var i = 0; i < _children.Count; i++)
					_children[i].SetDirty(dirtyFlagType);
			}
		}


		public void CopyFrom(Transform transform)
		{
			_position = transform.Position;
			_localPosition = transform._localPosition;
			_rotation = transform._rotation;
			_localRotation = transform._localRotation;
			_scale = transform._scale;
			_localScale = transform._localScale;

			SetDirty(DirtyType.PositionDirty);
			SetDirty(DirtyType.RotationDirty);
			SetDirty(DirtyType.ScaleDirty);
		}


		public override string ToString()
		{
			return string.Format(
				"[Transform: parent: {0}, position: {1}, rotation: {2}, scale: {3}, localPosition: {4}, localRotation: {5}, localScale: {6}]",
				Parent != null, Position, Rotation, Scale, LocalPosition, LocalRotation, LocalScale);
		}
	}
}