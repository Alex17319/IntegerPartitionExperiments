#ifndef TWO_THREE_DECISION_TRACKER_H
#define TWO_THREE_DECISION_TRACKER_H

extern unsigned long long tracker_current;

void initDecisionTracker(int maximum);
void destructDecisionTracker();
bool trackerAtRoot();
int lastAddedThreeExponent();
int getRequiredCapacity(int max);
void doubleNTimes(int n);
void halve();
bool tryAddNextPowerOf3();
bool backtrackAndCheckIfWasDoublingOp();
int getNumDoublingsBeforeExceedingMax();
int doubleRepeatedlyUpToMax();

#endif