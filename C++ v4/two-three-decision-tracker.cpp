// C# version has more comments & explanation; this is just the same thing with some further optimisations

#include "math-utils.h"
#include "two-three-decision-tracker.h"
#include <iostream>
#include <cmath>

using namespace std;

int tracker_n_copy;
unsigned long long tracker_next;

unsigned long long tracker_max;
int* doublingsSinceLastPowerOfThree;
int tracker_capacity; //TODO: will bounds checking be needed?
int tracker_count;
unsigned long long tracker_current;

unsigned long long repeated_doubling_ops;

void initDecisionTracker(unsigned long long maximum) {
	destructDecisionTracker(); // to be safe
	
	tracker_max = maximum;
	tracker_capacity = getRequiredCapacity(tracker_max);
	doublingsSinceLastPowerOfThree = new int[tracker_capacity]();
	tracker_count = 1;
	tracker_current = 1;
}

void destructDecisionTracker() {
	delete[] doublingsSinceLastPowerOfThree;
	doublingsSinceLastPowerOfThree = nullptr;
}

int getRequiredCapacity(unsigned long long tracker_max) {
	unsigned long long x = 1;
	int i = 1;
	while (x <= tracker_max) {
		x += (unsigned long long)pow(3, i);
		i++;
	}
	return i; //might be one higher than needed, idk, but that's fine anyway
}