using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;


namespace Nez.Particles
{
	public class ParticleEmitter : RenderableComponent, IUpdatable
	{
		public override RectangleF Bounds => _bounds;

		public bool IsPaused { get; private set; }
		public bool IsPlaying => _active && !IsPaused;
		public bool IsStopped => !_active && !IsPaused;
		public bool IsEmitting { get; private set; }
		public float ElapsedTime { get; private set; }


		/// <summary>
		/// convenience method for setting ParticleEmitterConfig.simulateInWorldSpace. If true, particles will simulate in world space. ie when the
		/// parent Transform moves it will have no effect on any already active Particles.
		/// </summary>
		public bool SimulateInWorldSpace
		{
			set => _emitterConfig.SimulateInWorldSpace = value;
		}

		/// <summary>
		/// config object with various properties to deal with particle collisions
		/// </summary>
		public ParticleCollisionConfig CollisionConfig;

		private Action<ParticleEmitter> _onAllParticlesExpired;

		/// <summary>
		/// event that's going to be called when particles count becomes 0 after stopping emission.
		/// emission can stop after either we stop it manually or when we run for entire duration specified in ParticleEmitterConfig.
		/// </summary>
		public event Action<ParticleEmitter> OnAllParticlesExpired
		{
			add 
			{
				if (_onAllParticlesExpired == null)
				{
					_onAllParticlesExpired = value;
				}else if (!_onAllParticlesExpired.GetInvocationList().Contains(value))
				{
					_onAllParticlesExpired += value;
				}
			}
			// ReSharper disable once DelegateSubtraction
			remove { if(_onAllParticlesExpired != null) _onAllParticlesExpired -= value;}
		}

		/// <summary>
		/// event that's going to be called when emission is stopped due to reaching duration specified in ParticleEmitterConfig
		/// </summary>
		public event Action<ParticleEmitter> OnEmissionDurationReached;

		/// <summary>
		/// keeps track of how many particles should be emitted
		/// </summary>
		private float _emitCounter;

		private bool _active = false;
		private readonly List<Particle> _particles;
		private readonly bool _playOnAwake;
		[Inspectable] private readonly ParticleEmitterConfig _emitterConfig;


		public ParticleEmitter() : this(new ParticleEmitterConfig())
		{
		}

		public ParticleEmitter(ParticleEmitterConfig emitterConfig, bool playOnAwake = true)
		{
			_emitterConfig = emitterConfig;
			_playOnAwake = playOnAwake;
			_particles = new List<Particle>((int) _emitterConfig.MaxParticles);
			Pool<Particle>.WarmCache((int) _emitterConfig.MaxParticles);

			// set some sensible defaults
			CollisionConfig.Elasticity = 0.5f;
			CollisionConfig.Friction = 0.5f;
			CollisionConfig.CollidesWithLayers = Physics.AllLayers;
			CollisionConfig.Gravity = _emitterConfig.Gravity;
			CollisionConfig.LifetimeLoss = 0f;
			CollisionConfig.MinKillSpeedSquared = float.MinValue;
			CollisionConfig.RadiusScale = 0.8f;

			Init();
		}

		internal void Explode(float v1, float v2)
		{
			PauseEmission();
			foreach (var particle in _particles)
			{
				particle.Explode(v1, v2);
			}
		}

		/// <summary>
		/// creates the Batcher and loads the texture if it is available
		/// </summary>
		private void Init()
		{
			// prep our custom BlendState and set the Material with it
			var blendState = new BlendState();
			blendState.ColorSourceBlend = blendState.AlphaSourceBlend = _emitterConfig.BlendFuncSource;
			blendState.ColorDestinationBlend = blendState.AlphaDestinationBlend = _emitterConfig.BlendFuncDestination;

			Material = new Material(blendState);
		}


		#region Component/RenderableComponent

		public override void OnAddedToEntity()
		{
			if (_playOnAwake)
				Play();
		}


		public virtual void Update()
		{
			if (IsPaused)
				return;

			// prep data for the particle.update method
			var rootPosition = Entity.Position + _localOffset;

			// if the emitter is active and the emission rate is greater than zero then emit particles
			if (_active && _emitterConfig.EmissionRate > 0)
			{
				if (IsEmitting)
				{
					var rate = 1.0f / _emitterConfig.EmissionRate;

					if (_particles.Count < _emitterConfig.MaxParticles)
						_emitCounter += Time.DeltaTime;

					while (_particles.Count < _emitterConfig.MaxParticles && _emitCounter > rate)
					{
						AddParticle(rootPosition);
						_emitCounter -= rate;
					}

					ElapsedTime += Time.DeltaTime;

					if (_emitterConfig.Duration != -1 && _emitterConfig.Duration < ElapsedTime)
					{
						// when we hit our duration we dont emit any more particles
						IsEmitting = false;

						OnEmissionDurationReached?.Invoke(this);
					}
				}

				// once all our particles are done we stop the emitter
				if (!IsEmitting && _particles.Count == 0)
				{
					Stop();

					_onAllParticlesExpired?.Invoke(this);
				}
			}

			var min = new System.Numerics.Vector2(float.MaxValue, float.MaxValue);
			var max = new System.Numerics.Vector2(float.MinValue, float.MinValue);
			var maxParticleSize = float.MinValue;

			// loop through all the particles updating their location and color
			for (var i = _particles.Count - 1; i >= 0; i--)
			{
				// get the current particle and update it
				var currentParticle = _particles[i];

				// if update returns true that means the particle is done
				if (currentParticle.Update(_emitterConfig, ref CollisionConfig, rootPosition))
				{
					Pool<Particle>.Free(currentParticle);
					_particles.RemoveAt(i);
				}
				else
				{
					// particle is good. collect min/max positions for the bounds
					var pos = _emitterConfig.SimulateInWorldSpace ? currentParticle.spawnPosition : rootPosition;
					pos += currentParticle.position;
					min = System.Numerics.Vector2.Min(min, pos);
					max = System.Numerics.Vector2.Max(max, pos);
					maxParticleSize = Math.Max(maxParticleSize, currentParticle.particleSize);
				}
			}

			_bounds.Location = min;
			_bounds.Width = max.X - min.X;
			_bounds.Height = max.Y - min.Y;

			if (_emitterConfig.Sprite == null)
			{
				_bounds.Inflate(1 * maxParticleSize, 1 * maxParticleSize);
			}
			else
			{
				maxParticleSize /= _emitterConfig.Sprite.SourceRect.Width;
				_bounds.Inflate(_emitterConfig.Sprite.SourceRect.Width * maxParticleSize,
					_emitterConfig.Sprite.SourceRect.Height * maxParticleSize);
			}
		}


		public override void Render(Batcher batcher, Camera camera)
		{
			// we still render when we are paused
			if (!_active && !IsPaused)
				return;

			var rootPosition = Entity.Position + _localOffset;

			// loop through all the particles updating their location and color
			for (var i = 0; i < _particles.Count; i++)
			{
				var currentParticle = _particles[i];
				var pos = _emitterConfig.SimulateInWorldSpace ? currentParticle.spawnPosition : rootPosition;

				if (_emitterConfig.Sprite == null)
					batcher.Draw(Graphics.Instance.PixelTexture, pos + currentParticle.position, currentParticle.color,
						currentParticle.rotation, System.Numerics.Vector2.One, currentParticle.particleSize * 0.5f, SpriteEffects.None,
						LayerDepth);
				else
					batcher.Draw(_emitterConfig.Sprite, pos + currentParticle.position,
						currentParticle.color, currentParticle.rotation, _emitterConfig.Sprite.Center,
						currentParticle.particleSize / _emitterConfig.Sprite.SourceRect.Width, SpriteEffects.None,
						LayerDepth);
			}
		}

		#endregion


		/// <summary>
		/// removes all particles from the particle emitter
		/// </summary>
		public void Clear()
		{
			for (var i = 0; i < _particles.Count; i++)
				Pool<Particle>.Free(_particles[i]);
			_particles.Clear();
		}

		/// <summary>
		/// plays the particle emitter
		/// </summary>
		public void Play()
		{
			// if we are just unpausing, we only toggle flags and we dont mess with any other parameters
			if (IsPaused)
			{
				_active = true;
				IsPaused = false;
				return;
			}

			_active = true;
			IsEmitting = true;
			ElapsedTime = 0;
			_emitCounter = 0;
		}

		/// <summary>
		/// stops the particle emitter
		/// </summary>
		public void Stop()
		{
			_active = false;
			IsPaused = false;
			ElapsedTime = 0;
			_emitCounter = 0;
			Clear();
		}

		/// <summary>
		/// pauses the particle emitter
		/// </summary>
		public void Pause()
		{
			IsPaused = true;
			_active = false;
		}

		/// <summary>
		/// resumes emission of particles.
		/// this is possible only if stop() wasn't called and emission wasn't stopped due to duration
		/// </summary>
		public void ResumeEmission()
		{
			if (IsStopped || (_emitterConfig.Duration != -1 && _emitterConfig.Duration < ElapsedTime))
				return;

			IsEmitting = true;
		}

		/// <summary>
		/// pauses emission of particles while allowing existing particles to expire
		/// </summary>
		public void PauseEmission()
		{
			IsEmitting = false;
		}

		/// <summary>
		/// manually emit some particles
		/// </summary>
		/// <param name="count">Count.</param>
		public void Emit(int count)
		{
			var rootPosition = Entity.Position + _localOffset;

			Init();
			_active = true;
			for (var i = 0; i < count; i++)
				AddParticle(rootPosition);
		}

		/// <summary>
		/// adds a Particle to the emitter
		/// </summary>
		private void AddParticle(System.Numerics.Vector2 position)
		{
			// take the next particle out of the particle pool we have created and initialize it
			var particle = Pool<Particle>.Obtain();
			particle.Initialize(_emitterConfig, position);
			_particles.Add(particle);
		}
	}
}