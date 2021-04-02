using System;
using System.Collections.Immutable;
using System.Linq;

namespace CSharpEquivalent
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Started");

			//Predicates.findZeros(m: 2, s: 3, ignoreRule: num => num % 3 == 0);

			PermutationSort.psort(
				//ImmutableStack.Create(20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1),
				//ImmutableStack.CreateRange(Enumerable.Range(1, 10).Select(x => new Random(x).Next() % 100)),
				ImmutableStack.Create(18, 37, 9, 28, 0, 19, 91, 10, 82, 1),
				out var sortedSolns
			);

			foreach (var sortedSoln in sortedSolns)
			{
				Console.Write("[");
				Console.Write(string.Join(", ", sortedSoln));
				Console.Write("]");
				Console.WriteLine();
			}

			Console.WriteLine("Done");

			Console.ReadLine();
		}
	}
}
