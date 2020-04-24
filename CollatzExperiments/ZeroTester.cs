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
	public static class ZeroTester
	{
		//	public static bool RouteFrom1Exists_TopDown(long end, long[] stackBuffer)
		//	{
		//		long[] stack = stackBuffer != null && stackBuffer.Length >= 128
		//			? stackBuffer
		//			: new long[128]; //Should be plenty, think it needs about 41 + 64 at most for longs
		//		int stackPos = 0;
		//		int lastPow3Subtracted = 0;
		//		long current = end;
		//	
		//		MathUtils.HighestPow3SumAtMost(current, out long sumPow3s, out long maxPow3, out long _);
		//		if (sumPow3s == current) return true;
		//		current -= (sumPow3s - 1); //Don't subtract away 3^0 as that's the starting point
		//		stack[stackPos] = 3;
		//		lastPow3Subtracted = 3;
		//	
		//		//TODO: Check if power of 2. If not, backtrack
		//		
		//		//The method I was planning overlooks some paths, eg 113 - 9 and 113 - 3.
		//		//If that's fixed, not sure if it provides any benefit over other methods anyway.
		//	}
	}
}

//*/