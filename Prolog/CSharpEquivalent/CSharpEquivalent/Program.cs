using System;

namespace CSharpEquivalent
{
	class Program
	{
		static void Main(string[] args)
		{
			Predicates.findZeros(m: 2, s: 3, ignoreRule: num => num % 3 == 0);
		}
	}
}
