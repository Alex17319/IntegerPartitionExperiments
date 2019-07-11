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
	public static class Utils
	{
		public static T ThrowIfArgNull<T>(T arg, string name) {
			if (arg == null) throw new ArgumentNullException(name);
			else return arg;
		}

		public static Comparer<T> Invert<T>(this IComparer<T> comparer) =>
			comparer == null ? null : Comparer<T>.Create((a, b) => comparer.Compare(b, a));

		///<summary>Returns true if source has more than one element</summary>
		public static bool MoreThanOne<T>(this IEnumerable<T> source) {
			if (source == null) throw new ArgumentNullException(nameof(source));
			var enumerator = source.GetEnumerator();
			return enumerator.MoveNext() && enumerator.MoveNext(); //Can move forward at least twice
		}

		///<summary>Returns true if source has exactly one element</summary>
		public static bool One<T>(this IEnumerable<T> source) {
			if (source == null) throw new ArgumentNullException(nameof(source));
			var enumerator = source.GetEnumerator();
			return enumerator.MoveNext() && !enumerator.MoveNext(); //Can move forward once, but not twice
		}

		///<summary>An implementation of Enumerable.Max() that takes a custom IComparer&lt;TSource&gt;</summary>
		public static TSource Max<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			comparer = comparer ?? Comparer<TSource>.Default;
	
			using (IEnumerator<TSource> enumerator = source.GetEnumerator())
			{
				if (!enumerator.MoveNext()) {
					if (default(TSource) == null) return default(TSource);
					else throw new InvalidOperationException("No elements to compare in collection of non-nullable type '" + typeof(TSource) + "'.");
				}
		
				TSource value = enumerator.Current;
		
				while (enumerator.MoveNext()) {
					var x = enumerator.Current;
					comparer.Compare(x, value);
					if (x != null && (value == null || comparer.Compare(x, value) > 0)) value = x;
				}
		
				return value;
			}
		}

		public static TSource Min<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
			=> Max(source, comparer.Invert());
	
		///<summary>
		///Finds the maximum element in a sequence, using a selector function to choose what to compare on,
		///and returns the full element, not just the result of the selector function.
		///<para/>
		///I.e. Max() equivalent of OrderBy()
		///</summary>
		public static TSource MaxBy<TSource, TCompare>(this IEnumerable<TSource> source, Func<TSource, TCompare> selector, IComparer<TCompare> comparer = null) {
			ThrowIfArgNull(selector, nameof(selector));
			comparer = comparer ?? Comparer<TCompare>.Default;
			return Max(
				source,
				Comparer<TSource>.Create(
					(a, b) => comparer.Compare(selector(a), selector(b)) //Unfortunately, yes, means that selector() is called twice for each element. Ignoring this for now
				)
			);
		}

		///<summary>
		///Finds the minimum element in a sequence, using a selector function to choose what to compare on,
		///and returns the full element, not just the result of the selector function.
		///<para/>
		///I.e. Min() equivalent of OrderBy()
		///</summary>
		public static TSource MinBy<TSource, TCompare>(this IEnumerable<TSource> source, Func<TSource, TCompare> selector, IComparer<TCompare> comparer = null) {
			ThrowIfArgNull(selector, nameof(selector));
			comparer = comparer ?? Comparer<TCompare>.Default;
			return Max( //Use Max() and swap a and b
				source,
				Comparer<TSource>.Create(
					(a, b) => comparer.Compare(selector(b), selector(a)) //Unfortunately, yes, means that selector() is called twice for each element. Ignoring this for now
				)
			);
		}

		//Source: stackoverflow.com/a/2878000/4149474
		public static double StdDevSample(this IEnumerable<double> values)
		{
			double mean = 0.0;
			double sum = 0.0;
			double stdDev = 0.0;
			int n = 0;
			foreach (double val in values) {
				n++;
				double delta = val - mean;
				mean += delta / n;
				sum += delta * (val - mean);
			}
			if (n > 1) {
				stdDev = Math.Sqrt(sum / (n - 1));
			}
			return stdDev;
		}

		//Source: stackoverflow.com/a/2878000/4149474
		public static double StdDevPopulation(this IEnumerable<double> values)
		{
			double mean = 0.0;
			double sum = 0.0;
			double stdDev = 0.0;
			int n = 0;
			foreach (double val in values) {
				n++;
				double delta = val - mean;
				mean += delta / n;
				sum += delta * (val - mean);
			}
			if (n > 1) {
				stdDev = Math.Sqrt(sum / n);
			}
			return stdDev;
		}

		//Unused
		//	struct IndexedItem<T> {
		//		public readonly int Index;
		//		public readonly T Item;
		//		
		//		public IndexedItem(int index, T item) {
		//			this.Index = index;
		//			this.Item = item;
		//		}
		//	}
		//	
		//	static void Index<T>(this IEnumerable<T> source) {
		//		return source.Select((x, i) => new IndexedItem(index: i, item: x));
		//	}

		//Unused
		//	public static IEnumerable<int> FindAllIndices<T>(this List<T> source, Predicate<T> predicate)
		//	{
		//		if (source == null) throw new ArgumentNullException(nameof(source));
		//		if (predicate == null) throw new ArgumentNullException(nameof(predicate));
		//		
		//		for (int i = 0; i < source.Count; i++) {
		//			if (predicate(source[i])) yield return i;
		//		}
		//	}

		//Unused
		//	static void InsertAtOrAfterSortedPos<T>(this List<T> source, T item, IComparer<T> comparer = null)
		//	{
		//		if (source == null) throw new ArgumentNullException(nameof(source));
		//		comparer = comparer ?? Comparer<T>.Default;
		//		
		//		int existingIndex = source.BinarySearch(item, comparer);
		//		
		//		if (existingIndex >= 0)
		//		{
		//			//BinarySearch doesn't guarantee which element will be returned if there are duplicates,
		//			//so move forwards until we reach an element that's different
		//			while (existingIndex + 1 < source.Count && comparer.Compare(source[existingIndex + 1], item) == 0) {
		//				existingIndex++;
		//			}
		//			
		//			source.Insert(existingIndex + 1, item);
		//		}
		//		else
		//		{
		//			//BinarySearch returns the bitwise complement of the next element that is greater than
		//			//the search value, or the bitwise complement of the count if there isn't one, so:
		//			int greaterElementIndex = ~existingIndex;
		//			
		//			source.Insert(greaterElementIndex, item);
		//		}
		//	}

		//	public int BinarySearch(int index, int count, T item, IComparer<T> comparer) {
		//		if (index < 0)
		//			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		//		if (count < 0)
		//			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		//		if (_size - index < count)
		//			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		//		Contract.Ensures(Contract.Result<int>() <= index + count);
		//		Contract.EndContractBlock();
		//		
		//		return Array.BinarySearch<T>(_items, index, count, item, comparer);
		//	}
		//	
		//	[Pure]
		//	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		//	public static int BinarySearch<T>(T[] array, int index, int length, T value, System.Collections.Generic.IComparer<T> comparer) {
		//		if (array==null) 
		//			throw new ArgumentNullException("array");
		//		if (index < 0 || length < 0)
		//			throw new ArgumentOutOfRangeException((index<0 ? "index" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
		//		if (array.Length - index < length)
		//			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
		//		Contract.EndContractBlock();
		//	
		//	#if FEATURE_LEGACYNETCF
		//		if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
		//			return MangoArraySortHelper<T>.Default.BinarySearch(array, index, length, value, comparer);
		//		else
		//			return ArraySortHelper<T>.Default.BinarySearch(array, index, length, value, comparer);
		//	#else
		//		return ArraySortHelper<T>.Default.BinarySearch(array, index, length, value, comparer);
		//	#endif
		//	}
		//	
		//	//From https://referencesource.microsoft.com/#mscorlib/system/collections/generic/arraysorthelper.cs,f3d6c6df965a8a86,references
		//	internal static int InternalBinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
		//	{
		//		Contract.Requires(array != null, "Check the arguments in the caller!");
		//		Contract.Requires(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");
		//	
		//		int lo = index;
		//		int hi = index + length - 1;
		//		while (lo <= hi)
		//		{
		//			int i = lo + ((hi - lo) >> 1);
		//			int order = comparer.Compare(array[i], value);
		//	
		//			if (order == 0) return i;
		//			if (order < 0)
		//			{
		//				lo = i + 1;
		//			}
		//			else
		//			{
		//				hi = i - 1;
		//			}
		//		}
		//	
		//		return ~lo;
		//	}
	}
}

//*/