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
	public class LongChunkedArray<T> : IReadOnlyList<T>
	{
		private T[][] _arrays;
	
		public long Length { get; private set; }
		private readonly long ChunkSize;
	
		public IEnumerator<T> GetEnumerator() {
			for (long i = 0; i < _arrays.Length; i++) {
				for (long j = 0; j < _arrays[i].Length; j++) {
					yield return _arrays[i][j];
				}
			}
		}
	
		int IReadOnlyCollection<T>.Count => (int)this.Length;
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	
		public LongChunkedArray(long length, long chunkSize)
		{
			this.Length = length;
			this.ChunkSize = chunkSize;
		
			this._arrays = new T[length / chunkSize + 1][];
			for (long i = 0; i * this.ChunkSize < this.Length; i++) {
				this._arrays[i] = new T[this.ChunkSize];
			}
		}
	
		public T this[long index] {
			get {
				long rem;
				long div = Math.DivRem(index, ChunkSize, out rem);
				return _arrays[div][rem];
			}
			set {
				long rem;
				long div = Math.DivRem(index, ChunkSize, out rem);
				_arrays[div][rem] = value;
			}
		}
		T IReadOnlyList<T>.this[int index] => this[(long)index];
	}
}

//*/