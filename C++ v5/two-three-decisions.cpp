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
// hopefully __attribute__((__packed__)) is enough to make this work in arrays, it seems to be so far
struct __attribute__((__packed__)) tripleByte {
	char bytes[3];
};

// hopefully __attribute__((__packed__)) is enough to make this work
// fourthByte is expected to always be 0
struct __attribute__((__packed__)) tripleByteContainer {
	struct tripleByte firstThreeBytes;
	char fourthByte;
};

struct expansionRegister {
	struct tripleByte *array;
	unsigned long long capacity;
	unsigned long long currentArrayPos;
	unsigned long long currentVal;
	unsigned long long size;
};

bool print_all = false;

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
	result.array = new struct tripleByte[capacity]();
	result.capacity = capacity;
	result.currentArrayPos = 0;
	result.currentVal = currentVal;
	
	result.array[0].bytes[2] = (char)0b00000001u;
	result.size = 1;
	
	return result;
}

void stepForward(struct expansionRegister *expReg) {
	// cout << "#2.1: " << expReg->currentArrayPos << "." << endl;
	expReg->array[expReg->currentArrayPos].bytes[0] = (char)0;
	
	// cout << "#2.2: " << expReg->currentArrayPos << "." << endl;
	expReg->array[expReg->currentArrayPos].bytes[1] = (char)0;
	
	// cout << "#2.3: " << expReg->currentArrayPos << "." << endl;
	expReg->array[expReg->currentArrayPos].bytes[2] = (char)0;
	
	// cout << "#2.4: " << expReg->currentArrayPos << "." << endl;
	expReg->currentVal++;
	
	// cout << "#2.5: " << expReg->currentArrayPos << "." << endl;
	expReg->currentArrayPos = (expReg->currentArrayPos + 1) % expReg->capacity;
	
	if (print_all) cout << "#110: " << expReg->size << " -> ";
	
	// cout << "#2.6: " << expReg->currentArrayPos << "." << endl;
	expReg->size--;
	
	if (print_all) cout << expReg->size << "." << endl;
	
	// cout << "#2.7: " << expReg->currentArrayPos << "." << endl;
}

bool valueInWritableRange(struct expansionRegister *expReg, unsigned long long value) {
	// check if value is not stored/storable any more as its too small
	if (value < expReg->currentVal) return false;
	
	unsigned long long distToVal = value - expReg->currentVal;
	
	return distToVal < expReg->capacity;
	
	// This checks if it's in the range that's already been written to - that's not what we want
	//	if (expReg->currentArrayPos <= expReg->furthestWrittenPos) {		
	//		return expReg->currentArrayPos + distToVal <= expReg->furthestWrittenPos;
	//	} else {
	//		return expReg->currentArrayPos + distToVal <= expReg->capacity + expReg->furthestWrittenPos;
	//	}
}

void expand(struct expansionRegister *expReg) {
	struct expansionRegister result;
	result.array = new struct tripleByte[expReg->capacity * 2]();
	result.capacity = expReg->capacity * 2;
	result.currentArrayPos = 0;
	result.currentVal = expReg->currentVal;
	result.size = expReg->size;
	
	for (unsigned long long i = 0; i < expReg->size; i++) {
		result.array[i] = expReg->array[(expReg->currentArrayPos + i) % expReg->capacity];
	}
	
	delete[] expReg->array;
	*expReg = result;
	
	//	if (expReg->currentArrayPos <= expReg->furthestWrittenPos) {
	//		//	cout << "#71 "
	//		//		<< ", " << expReg->capacity
	//		//		<< ", " << expReg->currentArrayPos
	//		//		<< ", " << expReg->currentVal
	//		//		<< ", " << expReg->furthestWrittenPos
	//		//		<< "." << endl;
	//		//	cout << "#72 "
	//		//		<< ", " << result.capacity
	//		//		<< ", " << result.currentArrayPos
	//		//		<< ", " << result.currentVal
	//		//		<< ", " << result.furthestWrittenPos
	//		//		<< "." << endl;
	//		//	memcpy(
	//		//		result.array,
	//		//		expReg->array + expReg->currentArrayPos,
	//		//		(expReg->furthestWrittenPos - expReg->currentArrayPos + 1) * sizeof(struct tripleByte)
	//		//	);
	//		int cpyLen = expReg->furthestWrittenPos - expReg->currentArrayPos + 1;
	//		for (int i = 0; i < cpyLen; i++) {
	//			result.array[i] = (expReg->array)[expReg->currentArrayPos + i];
	//		}
	//		//	cout << "#73 "
	//		//		<< ", " << expReg->capacity
	//		//		<< ", " << expReg->currentArrayPos
	//		//		<< ", " << expReg->currentVal
	//		//		<< ", " << expReg->furthestWrittenPos
	//		//		<< "." << endl;
	//		//	cout << "#74 "
	//		//		<< ", " << result.capacity
	//		//		<< ", " << result.currentArrayPos
	//		//		<< ", " << result.currentVal
	//		//		<< ", " << result.furthestWrittenPos
	//		//		<< "." << endl;
	//		result.furthestWrittenPos = expReg->furthestWrittenPos - expReg->currentArrayPos;
	//	} else {
	//		//	cout << "#75 "
	//		//		<< ", " << expReg->capacity
	//		//		<< ", " << expReg->currentArrayPos
	//		//		<< ", " << expReg->currentVal
	//		//		<< ", " << expReg->furthestWrittenPos
	//		//		<< "." << endl;
	//		//	cout << "#76 "
	//		//		<< ", " << result.capacity
	//		//		<< ", " << result.currentArrayPos
	//		//		<< ", " << result.currentVal
	//		//		<< ", " << result.furthestWrittenPos
	//		//		<< "." << endl;
	//		//	memcpy(
	//		//		result.array,
	//		//		expReg->array + expReg->currentArrayPos,
	//		//		(expReg->capacity - expReg->currentArrayPos) * sizeof(struct tripleByte)
	//		//	);
	//		int cpyLen = expReg->capacity - expReg->currentArrayPos;
	//		for (int i = 0; i < cpyLen; i++) {
	//			result.array[i] = (expReg->array)[expReg->currentArrayPos + i];
	//		}
	//		//	cout << "#77 "
	//		//		<< ", " << expReg->capacity
	//		//		<< ", " << expReg->currentArrayPos
	//		//		<< ", " << expReg->currentVal
	//		//		<< ", " << expReg->furthestWrittenPos
	//		//		<< "." << endl;
	//		//	cout << "#78 "
	//		//		<< ", " << result.capacity
	//		//		<< ", " << result.currentArrayPos
	//		//		<< ", " << result.currentVal
	//		//		<< ", " << result.furthestWrittenPos
	//		//		<< "." << endl;
	//		memcpy(
	//			result.array + (expReg->capacity - expReg->currentArrayPos),
	//			expReg->array,
	//			(expReg->furthestWrittenPos + 1) * sizeof(struct tripleByte)
	//		);
	//		//	cout << "#79 "
	//		//		<< ", " << expReg->capacity
	//		//		<< ", " << expReg->currentArrayPos
	//		//		<< ", " << expReg->currentVal
	//		//		<< ", " << expReg->furthestWrittenPos
	//		//		<< "." << endl;
	//		//	cout << "#80 "
	//		//		<< ", " << result.capacity
	//		//		<< ", " << result.currentArrayPos
	//		//		<< ", " << result.currentVal
	//		//		<< ", " << result.furthestWrittenPos
	//		//		<< "." << endl;
	//		result.furthestWrittenPos = expReg->furthestWrittenPos + expReg->capacity - expReg->currentArrayPos;
	//	}
	//	
	//	//	cout << "#81 "
	//	//		<< ", " << expReg->capacity
	//	//		<< ", " << expReg->currentArrayPos
	//	//		<< ", " << expReg->currentVal
	//	//		<< ", " << expReg->furthestWrittenPos
	//	//		<< "." << endl;
	//	//	cout << "#82 "
	//	//		<< ", " << result.capacity
	//	//		<< ", " << result.currentArrayPos
	//	//		<< ", " << result.currentVal
	//	//		<< ", " << result.furthestWrittenPos
	//	//		<< "." << endl;
	//	memset(result.array + result.furthestWrittenPos + 1, 0, result.capacity - 1 - result.furthestWrittenPos);
	//	//	cout << "#83 "
	//	//		<< ", " << expReg->capacity
	//	//		<< ", " << expReg->currentArrayPos
	//	//		<< ", " << expReg->currentVal
	//	//		<< ", " << expReg->furthestWrittenPos
	//	//		<< "." << endl;
	//	//	cout << "#84 "
	//	//		<< ", " << result.capacity
	//	//		<< ", " << result.currentArrayPos
	//	//		<< ", " << result.currentVal
	//	//		<< ", " << result.furthestWrittenPos
	//	//		<< "." << endl;
	//	
	//	delete[] expReg->array;
	//	*expReg = result;
	//	
	//	//	cout << "#85 "
	//	//		<< ", " << expReg->capacity
	//	//		<< ", " << expReg->currentArrayPos
	//	//		<< ", " << expReg->currentVal
	//	//		<< ", " << expReg->furthestWrittenPos
	//	//		<< "." << endl;
}

void addFlagsAt(struct expansionRegister *expReg, unsigned long long value, struct tripleByteContainer flags) {
	if (value < expReg->currentVal) {
		throw out_of_range(
			string("Error: flags at value '")
			+ to_string(value)
			+ "' not stored any more in expansionRegister, as value is smaller than currentVal '"
			+ to_string(expReg->currentVal)
			+ "'."
		);
	}
	
	if (print_all) {
		cout << "#60: ";
		printIntBits(*((unsigned int *)(&flags)));
		cout << ", ";		
	}
	
	flags.fourthByte = 0; // just in case its not, it should be
	
	if (print_all) {
		printIntBits(*((unsigned int *)(&flags)));
		cout << ", " << value << "." << endl;
	}
	
	unsigned long long value_copy = value;
	while (!valueInWritableRange(expReg, value)) {
		//	print_all = true;
		//	cout << "#70"
		//		<< ", " << value
		//		<< ", " << value_copy
		//		<< "; " << expReg->capacity
		//		<< ", " << expReg->currentArrayPos
		//		<< ", " << expReg->currentVal
		//		<< ", " << expReg->size
		//		<< "." << endl;
		expand(expReg);
	}
	//	if (expReg->currentArrayPos == 0) cout << "#90" << endl;
	
	unsigned long long distToVal = value - expReg->currentVal;
	
	unsigned long long unwrappedDestPos = expReg->currentArrayPos + distToVal;
	unsigned long long destPos = unwrappedDestPos % expReg->capacity;
	// ^  we just ensured it must be in range, so if it's past the end it must wrap around
	
	if (print_all) {
		cout << "#61: ";
		printIntBits(*((unsigned int *)(expReg->array + destPos)));
		cout << ", ";
	}
	
	// index using tripleByte size, then treat it as an unsigned int to easily OR the flags
	*((unsigned int *)(expReg->array + destPos)) |= *((unsigned int *)(&flags));
	
	expReg->size = max(expReg->size, distToVal + 1);
	
	if (print_all) {
		printIntBits(*((unsigned int *)(expReg->array + destPos)));
		cout << ", " << expReg->currentVal
			<< ", " << expReg->currentArrayPos
			<< ", " << distToVal
			<< ", " << unwrappedDestPos
			<< ", " << destPos
			<< "." << endl;
	}
	
	//	unsigned long long unwrappedFurthestWrittenPos = (
	//		expReg->furthestWrittenPos
	//		+ (expReg->currentArrayPos <= expReg->furthestWrittenPos ? 0 : expReg->capacity)
	//	);
	//	expReg->furthestWrittenPos = max(unwrappedFurthestWrittenPos, unwrappedDestPos) % expReg->capacity;
	
	//	if (expReg->currentArrayPos + distToVal < expReg->capacity) {
	//		dest = expReg->array + expReg->currentArrayPos + distToVal;
	//		destPos = expReg->currentArrayPos + distToVal;
	//		if (expReg->furthestWrittenPos >= expReg->currentArrayPos && expReg->furthestWrittenPos < expReg->
	//	} else {
	//		// we just ensured it must be in range, so if it's past the end it must wrap around
	//		dest = expReg->array + expReg->currentArrayPos + distToVal - expReg->capacity;
	//		destPos = expReg->currentArrayPos + distToVal - expReg->capacity;
	//	}
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
	
	struct tripleByteContainer shifts[24] = { // bitshift works differently depending on endianess - this works either way
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
	
	for (unsigned long long curTrueVal = 1; true; curTrueVal++) {
		if (curTrueVal % 3 == 0) continue; // increment curTrueVal, don't increment expReg.currentVal or expReg.currentArrayPos
		
		// cout << "#1.1: " << expReg.currentArrayPos << "." << endl;
		
		struct tripleByteContainer currentFlags;
		currentFlags.fourthByte = 0; // ensure zeroed
		currentFlags.firstThreeBytes = expReg.array[expReg.currentArrayPos];
		
		// cout << "#1.2: " << expReg.currentArrayPos << "." << endl;
		
		unsigned int flagsInt = *((unsigned int *)(&currentFlags));
		
		//	if (curTrueVal > 20) exit(0);
		
		if (print_all) {
			cout << "#100"
				<< ", " << curTrueVal
				<< "." << endl;
		}
		
		if (flagsInt == 0) {
			cout << "found zero: " << curTrueVal;
			
			time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
			cout << " @ " << ctime(&time_now); // ctime() adds a newline
		}
		else if (curTrueVal % 100000 == 0) {
			cout << "at: " << curTrueVal << "\r" << flush;
		}
		
		if (print_all) cout << "#40" << endl;
		
		// cout << "#1.3: " << expReg.currentArrayPos << "." << endl;
		
		// mark entry at 2x current value as reachable using all the same last-added-powers-of-three
		addFlagsAt(&expReg, mapToAvoidMult3s(curTrueVal * 2), currentFlags);
		
		// cout << "#1.4: " << expReg.currentArrayPos << "." << endl;
		
		//TODO: Problem seems to be the memory copies when expanding, somehow they set some flags in 1970
		
		if (print_all) cout << "#41" << endl;
		
		// for each last-added-power-of-three that can be used to reach this position,
		// add the next power of three, and mark the resulting position as reachable
		// using that next power of three.
		for (int lastPower = 0; lastPower < 24; lastPower++) {
			unsigned int mask = *(unsigned int *)(shifts + lastPower);
			//	cout << "#42";
			//	printIntBits(flagsInt);
			//	cout << ", ";
			//	printIntBits(mask);
			//	cout << ", ";
			//	printIntBits(flagsInt & mask);
			//	cout << endl;
			//	<< bitset<8*sizeof(unsigned int)>(flagsInt) << ", "
			//	<< bitset<8*sizeof(unsigned int)>(mask) << ", "
			//	<< bitset<8*sizeof(unsigned int)>(flagsInt & mask) << "."
			//	<< endl;
			if ((flagsInt & mask) > 0) {
				unsigned int flagsToAdd = *(unsigned int *)(shifts + lastPower + 1);
				struct tripleByteContainer flagContainerToAdd = *(struct tripleByteContainer *)(&flagsToAdd);
				if (print_all) {					
					cout << "#43: ";
					printIntBits(flagsInt);
					cout << ", ";
					printIntBits(mask);
					cout << ", ";
					printIntBits(flagsInt & mask);
					cout << ", ";
					printIntBits(flagsToAdd);
					cout << ", "
						<< curTrueVal << ", "
						<< lastPower + 1 << ", "
						<< curTrueVal + threeToThe(lastPower + 1) << ", " 
						<< mapToAvoidMult3s(curTrueVal + threeToThe(lastPower + 1)) << "." << endl;
				}
				
				if (flagContainerToAdd.fourthByte != 0) throw logic_error("endianess wrong or something?");
				
				addFlagsAt(&expReg, mapToAvoidMult3s(curTrueVal + threeToThe(lastPower + 1)), flagContainerToAdd);
			}
		}
		
		// cout << "#1.5: " << expReg.currentArrayPos << "." << endl;
		
		//	for (int i = 0, lastPower = 0; i < 3; i++, lastPower++) {
		//		char byte = currentFlags.bytes[i];
		//		for (int j = 0; j < 8; j++, lastPower++) {
		//			char mask = ((char)1) << j;
		//			if (byte & mask) {
		//				addFlagsAt(&expReg, mapToAvoidMult3s(curTrueVal + threeToThe(lastPower + 1)
		//			}
		//		}
		//	}
		
		//	expReg.currentVal++;
		//	expReg.currentArrayPos = (expReg.currentArrayPos + 1) % expReg.capacity;
		stepForward(&expReg);
		
		// cout << "#1.6: " << expReg.currentArrayPos << "." << endl;
	}
}

int main(int argc, char *argv[]) {
	
	//	for (int i = 0; i < 32; i++) {		
	//		bitset<32> test;
	//		test[i] = true;
	//		struct tripleByteContainer testContainer;
	//		*((unsigned int *)(&testContainer)) = (unsigned int)test.to_ulong();
	//		cout << "#50: "
	//			<< test.to_string() << ", "
	//			<< bitset<8>(testContainer.firstThreeBytes.bytes[0]) << ", "
	//			<< bitset<8>(testContainer.firstThreeBytes.bytes[1]) << ", "
	//			<< bitset<8>(testContainer.firstThreeBytes.bytes[2]) << ", "
	//			<< bitset<8>(testContainer.fourthByte) << ", "
	//			<< test.to_ulong() << "."
	//			<< endl;
	//	}
	
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