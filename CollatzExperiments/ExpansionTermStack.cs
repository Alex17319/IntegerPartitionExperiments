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
using System.Numerics;

namespace CollatzExperiments
{
	public class ExpansionTermStack : IReadOnlyCollection<ExpansionTerm>, ICollection
	{
		private Stack<ExpansionTerm> _stack = new Stack<ExpansionTerm>();
	
		public BigInteger Sum { get; private set; } = 0;
	
		public ExpansionTermStack() { }
	
		public BigInteger SumWithoutHead => this.Sum - (this.Count > 0 ? this.Peek().Evaluate() : 0);
		public BigInteger SumWithAlternateHead(ExpansionTerm alternateHead) => this.SumWithoutHead + alternateHead.Evaluate();
	
		public int Count => _stack.Count;
		public IEnumerator<ExpansionTerm> GetEnumerator() => _stack.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_stack).GetEnumerator();
	
		public void Push(ExpansionTerm item) {
			_stack.Push(item);
			Sum += item.Evaluate();
		}
	
		public ExpansionTerm Pop() {
			var result = _stack.Pop();
			Sum -= result.Evaluate();
			return result;
		}
	
		public ExpansionTerm Peek() => _stack.Peek();
	
		public void Clear() {
			_stack.Clear();
			Sum = 0;
		}
	
		public bool Contains(ExpansionTerm item) => _stack.Contains(item);
		public void CopyTo(ExpansionTerm[] array, int arrayIndex) => _stack.CopyTo(array, arrayIndex);
		public ExpansionTerm[] ToArray() => _stack.ToArray();
		public void TrimExcess() => _stack.TrimExcess();
	
		bool ICollection.IsSynchronized => ((ICollection)_stack).IsSynchronized;
		object ICollection.SyncRoot => ((ICollection)_stack).SyncRoot;
		void ICollection.CopyTo(Array array, int arrayIndex) => ((ICollection)_stack).CopyTo(array, arrayIndex);
	}
}

//*/