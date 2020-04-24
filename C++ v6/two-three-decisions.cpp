#include "math-utils.h"
#include <atomic>
#include <bitset>
#include <chrono>
#include <climits>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <iomanip>
#include <iostream>
#include <string>
#include <thread>
#include <vector>

using namespace std;

// 3 bytes is enough to go up to 3^1 + 3^2 + 3^3 + ... + 3^24 = 423,644,304,721
// __attribute__((__packed__)) seems to be enough to make this work in arrays
struct __attribute__((__packed__)) tripleByte {
	char bytes[3];
};

// __attribute__((__packed__)) seems to be enough to make this work
// fourthByte is expected to always be 0
union __attribute__((__packed__)) tripleByteContainer {
	struct __attribute__((__packed__)) {
		struct tripleByte firstThreeBytes;
		char fourthByte;
	};
	unsigned int intView;
};

struct expansionRegister {
	struct tripleByte *array;
	unsigned long long capacity;
	unsigned long long currentArrayPos;
	unsigned long long currentVal;
	unsigned long long size;
};

union tripleByteContainer lastAddedPowerMasks[24] = { // bitshift works differently depending on endianess - this works either way
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b00000001u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b00000010u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b00000100u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b00001000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b00010000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b00100000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b01000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000000u, (char)0b10000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000001u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000010u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00000100u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00001000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00010000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b00100000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b01000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000000u, (char)0b10000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000001u, (char)0b00000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000010u, (char)0b00000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00000100u, (char)0b00000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00001000u, (char)0b00000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00010000u, (char)0b00000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b00100000u, (char)0b00000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b01000000u, (char)0b00000000u, (char)0b00000000u }},
	{ .firstThreeBytes = { (char)0b10000000u, (char)0b00000000u, (char)0b00000000u }},
};

void printIntBits(unsigned int x) {
	char bytes[sizeof(unsigned int)];
	*((unsigned int *)(&(bytes[0]))) = x;
	for (int i = 0; i < sizeof(bytes); i++) {
		if (i > 0) cout << ":";
		cout << bitset<8>(bytes[i]);
	}
}

struct expansionRegister createExpansionRegister(unsigned long long capacity, unsigned long long currentVal) {
	struct expansionRegister result;
	result.array = new struct tripleByte[capacity + 1]();
	// ^ 1 extra byte to allow OR-ing of unsigned ints without accessing memory outside array (see addFlagsAt)
	
	result.capacity = capacity;
	result.currentArrayPos = 0;
	result.currentVal = currentVal;
	
	result.array[0].bytes[2] = (char)0b00000001u;
	result.size = 1;
	
	return result;
}

void stepForward(struct expansionRegister *expReg) {
	expReg->array[expReg->currentArrayPos].bytes[0] = (char)0;
	expReg->array[expReg->currentArrayPos].bytes[1] = (char)0;
	expReg->array[expReg->currentArrayPos].bytes[2] = (char)0;
	
	expReg->currentVal++;
	expReg->currentArrayPos = (expReg->currentArrayPos + 1) % expReg->capacity;
	
	expReg->size--;
}

bool valueInWriteableRange(struct expansionRegister *expReg, unsigned long long value) {
	// check if value is not stored/storable any more as its too small
	if (value < expReg->currentVal) return false;
	
	unsigned long long distToVal = value - expReg->currentVal;
	
	return distToVal < expReg->capacity;
}

void expand(struct expansionRegister *expReg) {
	struct expansionRegister result;
	result.array = new struct tripleByte[expReg->capacity * 2 + 1]();
	// ^ 1 extra byte to allow OR-ing of unsigned ints without accessing memory outside array (see addFlagsAt)
	
	result.capacity = expReg->capacity * 2;
	result.currentArrayPos = 0;
	result.currentVal = expReg->currentVal;
	result.size = expReg->size;
	
	for (unsigned long long i = 0; i < expReg->size; i++) {
		result.array[i] = expReg->array[(expReg->currentArrayPos + i) % expReg->capacity];
	}
	
	delete[] expReg->array;
	*expReg = result;
}

void addFlagsAt(struct expansionRegister *expReg, unsigned long long value, union tripleByteContainer flags) {
	if (value < expReg->currentVal) {
		throw out_of_range(
			string("Error: flags at value '")
			+ to_string(value)
			+ "' not stored any more in expansionRegister, as value is smaller than currentVal '"
			+ to_string(expReg->currentVal)
			+ "'."
		);
	}
	
	flags.fourthByte = 0; // just in case its not, it should be
	
	while (!valueInWriteableRange(expReg, value)) {
		expand(expReg);
	}
	
	unsigned long long distToVal = value - expReg->currentVal;
	
	unsigned long long destPos = expReg->currentArrayPos + distToVal;
	
	// we've already ensured that value is in the writeable range, so its safe to just wrap destPos around.
	struct tripleByte *dest = expReg->array + (destPos % expReg->capacity);
	
	// Index using tripleByte size, then treat it as an unsigned int to easily OR the flags
	// The extra byte in an unsigned int is just zeroes, so ORing it is fine (we allocated
	// an extra element at the end to make this possible without any risk of segfaults)
	*((unsigned int *)dest) |= flags.intView;
	
	expReg->size = max(expReg->size, distToVal + 1);
}

void findAndPrintZeros(unsigned long long startSize) {
	if (startSize < 1) {
		throw out_of_range("startSize < 1");
	}
	
	cout << "Allocating initial array of size " << (mapToAvoidMult3s(startSize) + 1) << " for startSize = " << startSize << endl;
	auto alloc_start = chrono::system_clock::now();
	
	struct expansionRegister expReg = createExpansionRegister(mapToAvoidMult3s(startSize) + 1, mapToAvoidMult3s(1));
	
	auto alloc_end = chrono::system_clock::now();
	std::chrono::duration<double> elapsed_seconds = alloc_end - alloc_start;
	cout << "Allocation done in " << elapsed_seconds.count() << "s" << endl;
	
	for (unsigned long long curTrueVal = 1; true; curTrueVal++) {
		if (curTrueVal % 3 == 0) {
			printIntBits(0);
			cout << endl;
			
			// increment curTrueVal, don't increment expReg.currentVal or expReg.currentArrayPos
			continue;
		}
		
		union tripleByteContainer currentFlags;
		currentFlags.fourthByte = 0; // ensure zeroed
		currentFlags.firstThreeBytes = expReg.array[expReg.currentArrayPos];
		
		printIntBits(currentFlags.intView);
		cout << endl;
		
		//	if (currentFlags.intView == 0) {
		//		cout << "\rfound zero: " << curTrueVal;
		//		
		//		time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
		//		cout << " @ " << ctime(&time_now); // ctime() adds a newline
		//	}
		//	else if (curTrueVal % 100000 == 0) {
		//		cout << "\rat: " << curTrueVal << flush;
		//	}
		
		// mark entry at 2x current value as reachable using all the same last-added-powers-of-three
		addFlagsAt(&expReg, mapToAvoidMult3s(curTrueVal * 2), currentFlags);
		
		// for each last-added-power-of-three that can be used to reach the current value,
		// add the next power of three to the current value, and mark the resulting
		// value as reachable using that next power of three.
		for (int lastPower = 0; lastPower < 24; lastPower++) {
			if ((currentFlags.intView & lastAddedPowerMasks[lastPower].intView) > 0) {
				addFlagsAt(
					&expReg,
					mapToAvoidMult3s(curTrueVal + threeToThe(lastPower + 1)),
					lastAddedPowerMasks[lastPower + 1]
				);
			}
		}
		
		stepForward(&expReg);
	}
}

int main(int argc, char *argv[]) {
	
	struct tripleByte testArr[10];
	if (sizeof(testArr) != 30) {
		cout << "Error: unexpected alignment/packing, array of 10 tripleBytes has length '"
			<< sizeof(testArr)
			<< "' instead of length 30" << endl;
		return -1;
	}
	
	if (sizeof(unsigned long long) != 8) {
		cout << "Error: unexpected unsigned long long size '" << sizeof(unsigned long long) << "', must be 8 bytes" << endl;
		return -1;
	}
	if (sizeof(unsigned int) != 4) {
		cout << "Error: unexpected unsigned int size '" << sizeof(unsigned int) << "', must be 4 bytes" << endl;
		return -1;
	}
	
	if (argc < 2) return -1;
	
	unsigned long long startSize = strtoull(argv[1], nullptr, 10);
	
	auto start = chrono::system_clock::now();
	time_t start_time = chrono::system_clock::to_time_t(start);
	cout << "Started at: " << ctime(&start_time); // ctime() adds a newline
	cout << endl;
	
	findAndPrintZeros(startSize);
}