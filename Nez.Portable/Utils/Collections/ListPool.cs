using System;
using System.Collections.Generic;


namespace Nez
{
	/// <summary>
	/// simple static class that can be used to pool Lists
	/// </summary>
	public static class ListPool<T>
	{
		static readonly Queue<List<T>> _objectQueue = new Queue<List<T>>();


		/// <summary>
		/// warms up the cache filling it with a max of cacheCount objects
		/// </summary>
		/// <param name="cacheCount">new cache count</param>
		public static void WarmCache(int cacheCount)
		{
			cacheCount -= _objectQueue.Count;
			if (cacheCount > 0)
			{
				for (var i = 0; i < cacheCount; i++)
					_objectQueue.Enqueue(new List<T>());
			}
		}

		/// <summary>
		/// trims the cache down to cacheCount items
		/// </summary>
		/// <param name="cacheCount">Cache count.</param>
		public static void TrimCache(int cacheCount)
		{
			while (cacheCount > _objectQueue.Count)
				_objectQueue.Dequeue();
		}

		/// <summary>
		/// clears out the cache
		/// </summary>
		public static void ClearCache()
		{
			_objectQueue.Clear();
		}

		/// <summary>
		/// pops an item off the stack if available creating a new item as necessary
		/// </summary>
		public static List<T> Obtain()
		{
			if (_objectQueue.Count > 0)
				return _objectQueue.Dequeue();

			return new List<T>();
		}

		/// <summary>
		/// pushes an item back on the stack
		/// </summary>
		/// <param name="obj">Object.</param>
		public static void Free(List<T> obj)
		{
			_objectQueue.Enqueue(obj);
			obj.Clear();
		}
	}
	/// <summary>
	/// simple static class that can be used to pool Lists
	/// </summary>
	public class FastListPool<T>
	{
		readonly Queue<FastList<T>> _objectQueue = new Queue<FastList<T>>();

		[ThreadStatic]
		private static FastListPool<T> _instance;

		public static FastListPool<T> Instance
		{
			get
			{
				if(_instance == null)
				{
					_instance = new FastListPool<T>();
					_instance.WarmCache(4);
				}

				return _instance;
			}
		}

		/// <summary>
		/// warms up the cache filling it with a max of cacheCount objects
		/// </summary>
		/// <param name="cacheCount">new cache count</param>
		public void WarmCache(int cacheCount)
		{
			cacheCount -= _objectQueue.Count;
			if (cacheCount > 0)
			{
				for (var i = 0; i < cacheCount; i++)
					_objectQueue.Enqueue(new FastList<T>());
			}
		}

		/// <summary>
		/// trims the cache down to cacheCount items
		/// </summary>
		/// <param name="cacheCount">Cache count.</param>
		public void TrimCache(int cacheCount)
		{
			while (cacheCount > _objectQueue.Count)
				_objectQueue.Dequeue();
		}

		/// <summary>
		/// clears out the cache
		/// </summary>
		public void ClearCache()
		{
			_objectQueue.Clear();
		}

		/// <summary>
		/// pops an item off the stack if available creating a new item as necessary
		/// </summary>
		public FastList<T> Obtain()
		{
			if (_objectQueue.Count > 0)
				return _objectQueue.Dequeue();

			return new FastList<T>();
		}

		/// <summary>
		/// pushes an item back on the stack
		/// </summary>
		/// <param name="obj">Object.</param>
		public void Free(FastList<T> obj)
		{
			_objectQueue.Enqueue(obj);
			obj.Clear();
		}
	}
}