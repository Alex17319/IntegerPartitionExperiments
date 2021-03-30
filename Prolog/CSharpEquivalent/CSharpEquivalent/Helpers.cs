using System;
using System.Collections;
using System.Collections.Generic;
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

		public static bool NL {
			get {
				Console.WriteLine();
				return true;
			}
		}
	}
}
