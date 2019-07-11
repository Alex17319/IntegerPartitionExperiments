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
	public static class SpeedTests
	{
		public static TimeSpan SpeedTest(Action action, int iterations, Stopwatch s = null) {
			s = s ?? new Stopwatch();
	
			s.Reset();
			s.Start();
			for (int i = 0; i < iterations; i++) {
				s.Stop();
				GC.Collect();
				s.Start();
		
				action();
			}
			s.Stop();
	
			return s.Elapsed;
		}

		public static TimeSpan[] SpeedTest(Action action, int trials, int iterationsPerTrial, Stopwatch s = null)
		{
			s = s ?? new Stopwatch();
			TimeSpan[] results = new TimeSpan[trials];
	
			for (int i = 0; i < trials; i++) {
				results[i] = SpeedTest(action: action, iterations: iterationsPerTrial, s: s);
			}
	
			return results;
		}

		public static IEnumerable<KeyValuePair<T, TimeSpan[]>> SpeedTestAscendingInputs<T>(Action<T> action, IEnumerable<T> inputs, int trials, int simulatedIterationsPerTrial, TimeSpan targetTrialTime, Stopwatch s = null)
		{
			s = s ?? new Stopwatch();
	
			double prevTimesAvgMs = 0.0;
			int prevIterations = 0;
			foreach (T input in inputs)
			{
		
				int iterations = (
					prevTimesAvgMs <= targetTrialTime.TotalMilliseconds //or prevTimesAvgMs == 0.0
					? simulatedIterationsPerTrial
					: (int)Math.Ceiling((targetTrialTime.TotalMilliseconds/prevTimesAvgMs) * prevIterations) //TODO: Check this
				);
		
				var times = (
					SpeedTest(
						action:	() => action(input),
						trials: trials,
						iterationsPerTrial: iterations,
						s: s
					)
					.Select(x => x.TotalMilliseconds / (double)iterations * simulatedIterationsPerTrial)
					.Select(x => TimeSpan.FromMilliseconds(x))
					.ToArray()
				);
				yield return new KeyValuePair<T, TimeSpan[]>(input, times);
		
				prevTimesAvgMs = times.Select(x => x.TotalMilliseconds).Average();
				prevIterations = iterations;
			}
		}
	}
}

//*/