// C# version has some comments & explanation; this is just the same thing

#include "math-utils.h"
#include "two-three-decision-tracker.h"
#include <iostream>
#include <cmath>

using namespace std;

unsigned long long tracker_max;
int* doublingsSinceLastPowerOfThree;
int tracker_capacity; //TODO: will bounds checking be needed?
int tracker_count;
unsigned long long tracker_current;

void initDecisionTracker(int maximum) {
	destructDecisionTracker(); // to be safe
	
	tracker_max = maximum;
	tracker_capacity = getRequiredCapacity(tracker_max);
	doublingsSinceLastPowerOfThree = new int[tracker_capacity]();
	tracker_count = 1;
	tracker_current = 1;
}

void destructDecisionTracker() {
	delete[] doublingsSinceLastPowerOfThree;
}

int getTrackerCurrent() {
	return tracker_current;
}

bool trackerAtRoot() {
	return tracker_count == 1 && doublingsSinceLastPowerOfThree[0] == 0;
}

int lastAddedThreeExponent() {
	return tracker_count - 1;
}

int getRequiredCapacity(int tracker_max) {
	unsigned long long x = 1;
	unsigned long long i = 1;
	while (x <= tracker_max) {
		x += (unsigned long long)pow(3, i);
		i++;
	}
	return i; //might be one higher than needed, idk, but that's fine anyway
}

void doubleNTimes(int n) {
	doublingsSinceLastPowerOfThree[tracker_count - 1] += n;
	tracker_current = tracker_current << n;
	//for (int i = 0; i < n; i++) {
	//	tracker_current *= 2;
	//}
}

void halve() {
	doublingsSinceLastPowerOfThree[tracker_count - 1]--;
	tracker_current /= 2;
}

bool tryAddNextPowerOf3() {
	unsigned long long next = tracker_current + threeToThe(tracker_count);
	if (next <= tracker_max) {
		tracker_count++;
		tracker_current = next;
		return true;
	} else {
		return false;
	}
}

bool backtrackAndCheckIfWasDoublingOp() {
	if (doublingsSinceLastPowerOfThree[tracker_count - 1] > 0) {
		tracker_current /= 2;
		doublingsSinceLastPowerOfThree[tracker_count - 1]--;
		return true;
	} else {
		tracker_count--;
		tracker_current -= threeToThe(tracker_count);
		return false;
	}
}

int getNumDoublingsBeforeExceedingMax() {
	return (int)floorLog2_64bit(tracker_max/tracker_current);
}

int doubleRepeatedlyUpToMax() {
	int doublings = getNumDoublingsBeforeExceedingMax();
	doubleNTimes(doublings);
	//int doublings;
	//while (tracker_current * 2 <= tracker_max) {
	//	doubleNTimes(1);
	//	doublings++;
	//}
	
	return doublings;
}