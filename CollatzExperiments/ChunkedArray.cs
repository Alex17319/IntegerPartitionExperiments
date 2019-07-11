using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CollatzExperiments
{
	public class ChunkedArray<T> : IReadOnlyList<T>
	{
		private T[][] _arrays;
	
		public int Length { get; private set; }
		private readonly int ChunkSize;
	
		public IEnumerator<T> GetEnumerator() {
			for (int i = 0; i < _arrays.Length; i++) {
				for (int j = 0; j < _arrays[i].Length; j++) {
					yield return _arrays[i][j];
				}
			}
		}
	
		int IReadOnlyCollection<T>.Count => this.Length;
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	
		public ChunkedArray(int length, int chunkSize)
		{
			this.Length = length;
			this.ChunkSize = chunkSize;
		
			this._arrays = new T[length / chunkSize + 1][];
			for (int i = 0; i * this.ChunkSize < this.Length; i++) {
				this._arrays[i] = new T[this.ChunkSize];
			}
		}
	
		public T this[int index] {
			get {
				int rem;
				int div = Math.DivRem(index, ChunkSize, out rem);
				return _arrays[div][rem];
			}
			set {
				int rem;
				int div = Math.DivRem(index, ChunkSize, out rem);
				_arrays[div][rem] = value;
			}
		}
	}
}

//*/