using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CollatzExperiments;
using static Executor.Printing;

namespace Executor
{
	//This project is a bunch of code around a maths problem I was investigating
	//mostly out of curiosity (the problem was loosely related to the collatz
	//conjecture, hence some of the naming).

	//The problem here is:
	//Take two integers M (multiplier) and S (summand), and a target integer Z,
	//and start at 1. Then, at each step either add S or multiply by a power of M,
	//starting with the 1st power (ie. M itself) and then continuing to each consecutive
	//exponent - i.e. multiply by M^n on the nth multiply-by-M step.
	//Using this setup, how many ways are there to reach the target value Z?

	//This code started out as a few short functions in a CSX file without the
	//intention for it to end up as much more. Then it continued to grow with
	//different experiments & different variants of experiments, which
	//probably explains the current structure. I haven't got around to refactoring
	//it all that much yet (and given that a lot of it is sort of different-versions-
	//of-the-same-thing I'm not sure how successful that will be).
	//The code wasn't really written for anyone else to view - if I needed to, I'd
	//take out the relevant few functions & tidy up those.

	//Most of the versions here focus on the case where M=2 and S=3. Some of the code
	//also uses a rephrased version of the problem:
	//How many ways can a target integer Z be written as a series of consecutive
	//ascending powers of 3, starting from 3^0 = 1, where each power of 3 is multiplied
	//by a power of 2, and each power of two is less than or equal to the previous one?
	//Eg. 3^0*2^5 + 3^1*2^5 + 3^2*2^2 + 3^3*2^0
	//Factoring 2 out of parts of this expression gives:
	//  2^2 * (2^3 * (3^0 + 3^1) + 3^2) + 3^3
	// =((1 + 3^1) * 2 * 2 * 2 + 3^2) * 2 *2 + 3^3
	//Which fits the original phrasing of the problem.
	//This second phrasing is what's being used when talking about expansion terms,
	//expansions, term options matrices, and so on.
	//The first phrasing is what's being used when talking about 'binary decisions',
	//or 'two-three decisions' in the case where M=2 and S=3.

	//Much of this code was designed around increasing efficiency - initially
	//it reached inputs of around 40,000 across several hours, now its been reaching
	//inputs of 1 billion in around one hour or 100 million in roughly 2 hours,
	//depending on the mode. However, these tests approach the limits of my computer's
	//memory.
	//Note: This applies to the M=2, S=3 case

	//In the M=2, S=3 case, every multiple of 3 is unreachable by any
	//expression/sequence-of-steps of the required type. However, there are also
	//some other numbers that cannot be reached (here usually called non-trivial zeros).
	//The non-trivial zeros less than 1.5 trillion are:
	//	113
	//	226
	//	985
	//	1970
	//	3211
	//	6422
	//	27875
	//	55750
	//	242683
	//	485366
	//	793585
	//	1587170
	//	6880121
	//	13760242
	//	59823937
	//	119647874
	//	521638217
	//	1043276434
	//	1699132379
	//	3398264758
	//	14755320499
	//	29510640998
	//	128502917195
	//	257005834390
	//	419868489953
	//	839736979906
	//This is approximately exponential, but not exactly. The numbers appear to come in pairs,
	//with the second being double the first.
	//This sequence does not appear to be in the OEIS or easily visible in any sequences that are there,
	//which is what encouraged me to continue investigating this. The sequence of the number of
	//paths to each number is also not in the OEIS, and begins:
	//	1, 1, 0, 2, 1, 0, 1, 2, 0, 1, 1, 0, 1, 2, 0, 3, 1, 0,
	//	2, 2, 0, 1, 1, 0, 1, 1, 0, 3, 1, 0, 1, 3, 0, 1, 1, 0,
	//	1, 2, 0, 3, 2, 0, 1, 3, 0, 2, 2, 0, 1, 2, 0, 2, 2, 0,
	//	2, 4, 0, 2, 1, 0, 1, 1, 0, 4, 2, 0, 2, 2, 0, 1, 1, 0,
	//	2, 2, 0, 4, 1, 0, 2, 4, 0, 2, 2, 0, 2, 1, 0, 3, 2, 0,
	//	1, 3, 0, 2, 1, 0, 1, 1, 0, 3, 1, 0, 2, 2, 0, 3, 1, 0,
	//	1, 2, 0, 5, 0, 0, 1, 3, 0, 1, 1, 0, 3, 2, 0, 3, 2, 0,
	//	2, 5, 0, 2, 3, 0, 2, 3, 0, 4, 2, 0, 3, 3, 0, 2, 2, 0...

	//For other numbers for M and S, many of the sequences seem to become sparser over time, i.e.
	//the average value appears to approach zero. For M=2, S=3, the sequence appears to grow roughly
	//logarithmically or something similar (eg. square root is also possible) - less than linear anyway.
	//For the cases where M>0 and S=1, the sequence starts contant, then at every multiple of M increases
	//by 1, then at every multiple of M^2 the amount it's increasing by each time steps up by 1, then
	//at every multiple of M^3 the amount the increase is stepping up by every M^2 increases by 1, and so
	//on - at each power of M, the next derivative comes into play (sort of, I think). This sequence *is*
	//in the OEIS, at least for M=2 and M=3, under https://oeis.org/A018819 and https://oeis.org/A062051,
	//which have some details of other ways to arrive at the same sequence.
	//Note: This is incorrect for the first term, where this description uses a 1 but the actual sequence
	//uses a 0, but that shouldn't be a big issue.

	//Notes from googling the (large) zeros:
	//Done all from 6880121 to 839736979906
	//	128502917195 = 2^35 + 3^23
	//	source: https://twitter.com/0916_imuhata/status/1244503257994547200?s=20 (then confirmed)
	//	
	//	14755320499 = 3^j + 4^k
	//	source: https://oeis.org/A226807/b226807.txt
	//	
	//	1043276434 is a sum of at most 5 positive 9th powers
	//	source: http://oeis.org/A004889/b004889.txt
	//	
	//	521638217 = 8^n + 9^n
	//	source: http://oeis.org/A074624
	//	521638217 = (n+1)^9 + n^9
	//	source: https://oeis.org/A036087
	//	
	//	119647874 is a sum of at most 5 nonzero 8th powers
	//	source: http://oeis.org/A004878/b004878.txt
	//	
	//	59823937 = (n+1)^8 + n^8
	//	source: https://oeis.org/A036086
	//	
	//	13760242 is a sum of 4 positive 7th powers
	//	source: https://oeis.org/A003371/b003371.txt
	//	13760242 is a sum of at most 5 positive 7th powers
	//	source: https://oeis.org/A004867/b004867.txt
	//	
	//	6880121 has loads of matches even just in the OEIS
	//	
	//	Potentially useful site: http://sequencedb.net/index.html

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine(DateTime.Now);

			//PrintExpansions(maxZ: 1000000);

			//PrintNonTrivialZeros(maxZ: 100000000, toFile: false);
			//PrintNonTrivialZeros_bitarray(maxZ: 100000, toFile: false);
			//PrintNonTrivialZeros_bitarray(maxZ: 100000, toFile: false);

			//PrintSortedTermOptions(max: 50000000);

			//PrintExpansionCounts(maxZ: 100);
			//PrintExpansionCounts_array(maxZ: 50000000, toFile: true, printInChunks: true);
			//PrintExpansionCounts_arrayAsync(maxZ: 50, printInChunks: true);
			//PrintExpansionCounts_twoThreeDecisions(maxZ: 10000000);
			//ValidateBinaryDecisionSequence(max: 68000000);
			//PrintNonTrivialZeros_twoThreeDecisions(maxZ: 1000000000);

			//PrintSpeedTests<int>(
			//	maxZ => TwoThreeDecisionZFinder.GetExpansionCounts_twoThreeDecisions_chunkedArray(maxZ),
			//	Enumerable.Range(186, 214).Select(x => x * x * x),
			//	trials: 1,
			//	simulatedIterationsPerTrial: 1,
			//	targetTrialTime: TimeSpan.FromSeconds(1)
			//);

			//PrintExpansionPaths(maxZ: 800, pathStartingPoint: new ExpansionTerm(3,3), capToMaximum: true);
			//PrintExpansionPaths(maxZ: 800, pathStartingPoint: new ExpansionTerm(0,3), capToMaximum: true);

			//PrintExpansionTermOptions(z: 50000000);

			//PrintExpansionCounts_twoThreeDecisions
			//Debugging (fixed now):
			//when going up to 50000000, sudden drop in some values (in the same positions where zeroes sometimes appear) at 29782973
			//when going up to 50000000, zeroes start appearing at 39348911
			//when going up to 50000000, zeroes continue past 50000000
			//when going up to 68000000, zeroes start appearing at 48348911
			//when going up to 68000000, zeroes start slowly growing again at 64570081 = 11111111111111111 in base 3, minus 1
			//when going up to 84000000, zeroes start appearing at 56348909
			//when going up to 84000000, zeroes start slowly growing again at 64570081 = 11111111111111111 in base 3, minus 1
			//when going up to 100000000, zeroes start appearing at 64348909
			//when going up to 100000000, zeroes start slowly growing again at 64570081 = 11111111111111111 in base 3, minus 1
			//when going up to 100000000, more zeroes start appearing at 93046723
			//when going up to 100000000, zeroes continue past 100000000

			//PrintBinaryDecisionZMSFinderResults(maxZ: 200, maxMultiplier: 20, maxSummand: 20);
			//PrintExpansionCounts_twoThreeDecisions(maxZ: 20000000);

			//PrintTwoThreeExpansionRegister(100000);

			Console.WriteLine(DateTime.Now);

			Console.ReadLine();
		}
	}
}
