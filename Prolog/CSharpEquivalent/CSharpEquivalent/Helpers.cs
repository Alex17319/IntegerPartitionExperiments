using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpEquivalent
{
	public static class Helpers
	{
		/// <param name="filtered">Must only be enumerated once</param>
		public static bool SucceedIfAny<T>(this IEnumerable<T> source, out IEnumerable<T> result)
		{
			if (source == null) Logic.Fail(out result);

			var enumerator = source.GetEnumerator();

			if (!enumerator.MoveNext()) return Logic.Fail(out result);
			else return Logic.Succeed(out result, iterator());
			
			IEnumerable<T> iterator()
			{
				yield return enumerator.Current;
				while (enumerator.MoveNext()) yield return enumerator.Current;
			}
		}

		public static bool Write(object o) {
			Console.Write(o);
			return true;
		}

		public static bool NL() {
			Console.WriteLine();
			return true;
		}

		public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> source, bool condition, T element)
			=> condition
			? source.Append(element)
			: source;

		public static IEnumerable<T> ConcatIf<T>(this IEnumerable<T> source, bool condition, IEnumerable<T> elements)
			=> condition && elements != null
			? source.Concat(elements)
			: source;

		public static IEnumerable<TResult> SelectIf<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, (bool condition, TResult value)> selector)
		{
			foreach (var s in source)
			{
				var (condition, value) = selector(s);
				if (condition) yield return value;
			}
		}

		public static IEnumerable<TResult> SelectManyIf<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, (bool condition, IEnumerable<TResult> values)> selector)
		{
			foreach (var s in source)
			{
				var (condition, values) = selector(s);
				if (condition)
					foreach (var v in values)
						yield return v;
			}
		}
	}
}
