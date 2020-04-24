#ifndef TWO_THREE_DECISION_TRACKER_H
#define TWO_THREE_DECISION_TRACKER_H

extern int tracker_n_copy;

extern unsigned long long tracker_max;
extern int* doublingsSinceLastPowerOfThree;
extern int tracker_capacity;
extern int tracker_count;
extern unsigned long long tracker_current;
extern unsigned long long tracker_next;

void initDecisionTracker(unsigned long long maximum);
void destructDecisionTracker();
int getRequiredCapacity(unsigned long long max);

#define trackerAtRoot() ( \
	tracker_count == 1 && doublingsSinceLastPowerOfThree[0] == 0 \
)

#define lastAddedThreeExponent() ( \
	tracker_count - 1 \
)

#define doubleNTimes(n) ( \
	tracker_n_copy = n, \
	doublingsSinceLastPowerOfThree[tracker_count - 1] += tracker_n_copy, \
	tracker_current <<= tracker_n_copy \
)

#define halve() ( \
	doublingsSinceLastPowerOfThree[tracker_count - 1]--, \
	tracker_current /= 2 \
)

#define tryAddNextPowerOf3() ( \
	tracker_next = tracker_current + threeToThe(tracker_count), ( \
		tracker_next <= tracker_max \
		? (tracker_count++, tracker_current = tracker_next, true) \
		: false \
	) \
)

#define backtrackAndCheckIfWasDoublingOp() ( \
	doublingsSinceLastPowerOfThree[tracker_count - 1] > 0 \
	? (tracker_current /= 2, doublingsSinceLastPowerOfThree[tracker_count - 1]--, true) \
	: (tracker_count--, tracker_current -= threeToThe(tracker_count), false) \
)

#define getNumDoublingsBeforeExceedingMax() ( \
	(int)floorLog2_64bit(tracker_max/tracker_current) \
)

#define doubleRepeatedlyUpToMax() ( \
	doubleNTimes(getNumDoublingsBeforeExceedingMax()) \
)

#endif