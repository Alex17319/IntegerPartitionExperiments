#include "math-utils.h"
#include <atomic>
#include <algorithm>
#include <bitset>
#include <chrono>
#include <climits>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <new>
#include <stdint.h>
#include <string>
#include <thread>
#include <vector>

using namespace std;

const int CHUNK_BITS = 64;

void printTime() {
	time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
	
	// from https://stackoverflow.com/a/44360248/4149474 and https://stackoverflow.com/a/9101683/4149474
	auto gmt_time = gmtime(&time_now);
    auto timestamp = std::put_time(gmt_time, "%c");
	cout << timestamp;
}

// prints from bytes in memory, so affected by endianness
void printIntBits_memory(unsigned int x) {
	char bytes[sizeof(unsigned int)];
	*((unsigned int *)(&(bytes[0]))) = x;
	for (int i = 0; i < sizeof(bytes); i++) {
		if (i > 0) cout << ":";
		cout << bitset<8>(bytes[i]).to_string('-', '#');
	}
}

// prints from bytes in memory, so affected by endianness
void printUInt64Bits_memory(uint64_t x) {
	char bytes[sizeof(uint64_t)];
	*((uint64_t *)(&(bytes[0]))) = x;
	for (int i = 0; i < sizeof(bytes); i++) {
		if (i > 0) cout << ":";
		cout << bitset<8>(bytes[i]).to_string('-', '#');
	}
}

// prints the number the way mathematical operations see it
void printUInt64Bits_cpu(uint64_t x) {
	string b = bitset<64>(x).to_string('-', '#');
	for (int i = 0; i < 8; i++) {
		//if (i > 0) cout << ":";
		cout << b.substr(i * 8, 8);
	}
}

// prints the number the way mathematical operations see it, but reversed
void printUInt64Bits_cpu_reverse(uint64_t x) {
	string b = bitset<64>(x).to_string('-', '#');
	for (int i = 63; i >= 0; i--) {
		cout << b[i];
		//if (i > 0 && i % 8 == 0) cout << ":";
	}
}

// These are accurate when the first bit represents the value 1.
// Otherwise, you need to adjust the input/output (TODO: Detail how)
//	#define numToBitPos(number) \
//		((number) - ((number) / 3) - 1)
//	#define bitPosToNum(bitPos) \
//		((bitPos) + ((bitPos) / 2) + 1)
uint64_t numToBitPos(uint64_t number) {
	return number - (uint64_t)(number/3) - 1;
}
uint64_t bitPosToNum(uint64_t bitPos) {
	return bitPos + (uint64_t)(bitPos/2) + 1;
}

// the process is more complex for first chunk (as doubling
// can leave you in the same chunk; in later chunks it never does)
void initialiseColFirstChunk(uint64_t* chunk, uint64_t firstBitValueRepresented) {
	
	// Note: must never shift too far (i.e. >= 64 bits), as that's undefined behaviour and e.g. may wrap around.
	// To avoid this, just make sure that the destination to copy to is inside the first chunk.
	
	// For each ON bit at some position j, representing value n,
	// in the first chunk, turn the bit representing 2*n ON.
	for (int j = 0; true; j++) {
		int n = bitPosToNum(j) - 1 + firstBitValueRepresented;
		
		int doubleN = n * 2; // number to mark as ON
		int doubleNBit = numToBitPos(doubleN - firstBitValueRepresented + 1); // position of bit representing that number
		
		if (doubleNBit >= CHUNK_BITS) break;
		
		if ((*chunk & (1ULL << j)) != 0) {
			*chunk |= (1ULL << doubleNBit);
		}
	}
}

//	bitshift alternatives that erase when shifting by 64:
//	uint64_t num = 0;
//	uint64_t shift = 0;
//	uint64_t res = (num << (shift / 2)) << ((shift + 1) / 2);
//	res = (num << (shift >> 1)) << ((shift + 1) >> 1);
//	res = (num << (shift - (shift > 0))) << (shift > 0);
//	res = (shift > 0) * (num << shift);
//	res = -(shift > 0) & (num << shift);

void copyAlongToDoubleCurrentPos_old(uint64_t* expRegCol, uint64_t sourceChunkNum, uint64_t chunksAdjustment, uint64_t bitsAdjustment) {
	uint64_t spread1 = 0;
	uint64_t spread2 = 0;
	
	spreadBitsPaired(expRegCol[sourceChunkNum], &spread1, &spread2);
	
	// do the offset that's present in spreadAndOrBits_noMult3()
	// but not spreadBitsPaired()
	spread1 <<= 1;
	spread2 <<= 1;
	
	uint64_t destChunksPos = sourceChunkNum * 2 + chunksAdjustment;
	//if (destChunksPos >= colLength) return;
	expRegCol[destChunksPos] |= spread1 << bitsAdjustment; // bitsAdjustment < 64 so this is safe
	
	//	uint64_t num = 0;
	//	uint64_t shift = 0;
	//	uint64_t res = (num << (shift / 2)) << ((shift + 1) / 2);
	//	res = (num << (shift >> 1)) << ((shift + 1) >> 1);
	//	res = (num << (shift - (shift > 0))) << (shift > 0);
	//	res = (shift > 0) * (num << shift);
	//	res = -(shift > 0) & (num << shift);
	
	//if (destChunksPos + 1 >= colLength) return;
	expRegCol[destChunksPos + 1] |=
		(spread2 << bitsAdjustment)
		| ((bitsAdjustment > 0) * (spread1 >> (CHUNK_BITS - bitsAdjustment)));
	// if bitsAdjustment == 0 then the second shift will be 64 bits, which is undefined behaviour,
	// and may be treated as a shift by 0 bits - not what we want. We want to just erase the
	// value completely when shifting by 64, so instead multiply by zero (rather than 1) to ignore the result.
	// bitsAdjustment will always be less than 64 though, so the first shift (and the shift
	// earlier) are fine.
	
	//if (destChunksPos + 2 >= colLength) return;
	expRegCol[destChunksPos + 2] |= (bitsAdjustment > 0) * (spread2 >> (CHUNK_BITS - bitsAdjustment));
	// if bitsAdjustment == 0 then the shift will be 64 bits, which is undefined behaviour as before
}

void copyAlongToDoubleCurrentPos(uint64_t* expRegCol, uint64_t sourceChunkNum, uint64_t chunksAdjustment) {
	uint64_t spread1 = 0;
	uint64_t spread2 = 0;
	
	spreadBitsPaired(expRegCol[sourceChunkNum], &spread1, &spread2);
	
	// do the offset that's present in spreadAndOrBits_noMult3()
	// but not spreadBitsPaired()
	spread1 <<= 1;
	spread2 <<= 1;
	
	uint64_t destChunksPos = (sourceChunkNum << 1) + chunksAdjustment; // == sourceChunkNum * 2 + chunksAdjustment
	expRegCol[destChunksPos] |= spread1;
	expRegCol[destChunksPos + 1] |= spread2;
}

// bitsAdjustment must be between 1 and 63 both inclusive.
// If bitsAdjustment == 0 then some of the shifts will be by 64 bits, which is undefined behaviour,
// and may be treated as a shift by 0 bits - not what we want. We want to just erase the value completely
// when shifting by 64, so instead, use the version that does not have a bitsAdjustment argument.
// bitsAdjustmentComplement = CHUNK_BITS - bitsAdjustment.
void copyAlongToDoubleCurrentPos(uint64_t* expRegCol, uint64_t sourceChunkNum, uint64_t chunksAdjustment, uint64_t bitsAdjustment, uint64_t bitsAdjustmentComplement) {
	uint64_t spread1 = 0;
	uint64_t spread2 = 0;
	
	spreadBitsPaired(expRegCol[sourceChunkNum], &spread1, &spread2);
	
	// do the offset that's present in spreadAndOrBits_noMult3()
	// but not spreadBitsPaired()
	spread1 <<= 1;
	spread2 <<= 1;
	
	uint64_t destChunksPos = (sourceChunkNum << 1) + chunksAdjustment; // == sourceChunkNum * 2 + chunksAdjustment
	expRegCol[destChunksPos] |= spread1 << bitsAdjustment;
	
	expRegCol[destChunksPos + 1] |= (spread2 << bitsAdjustment) | (spread1 >> (CHUNK_BITS - bitsAdjustment));
	
	expRegCol[destChunksPos + 2] |= spread2 >> (CHUNK_BITS - bitsAdjustment);
}

#define copyAlongToDoubleCurrentPos_macro(expRegCol, sourceChunkNum, chunksAdjustment) { \
	uint64_t spread1 = 0; \
	uint64_t spread2 = 0; \
	\
	spreadBitsPaired_macro((expRegCol)[sourceChunkNum], spread1, spread2); \
	\
	spread1 <<= 1; \
	spread2 <<= 1; \
	\
	uint64_t destChunksPos = ((sourceChunkNum) << 1) + (chunksAdjustment); \
	(expRegCol)[destChunksPos] |= spread1; \
	(expRegCol)[destChunksPos + 1] |= spread2; \
}

#define copyAlongToDoubleCurrentPos_macroBitAdjusted(expRegCol, sourceChunkNum, chunksAdjustment, bitsAdjustment, bitsAdjustmentComplement) { \
	uint64_t spread1 = 0; \
	uint64_t spread2 = 0; \
	\
	spreadBitsPaired_macro((expRegCol)[sourceChunkNum], spread1, spread2); \
	\
	spread1 <<= 1; \
	spread2 <<= 1; \
	\
	uint64_t destChunksPos = ((sourceChunkNum) << 1) + (chunksAdjustment); \
	(expRegCol)[destChunksPos] |= spread1 << (bitsAdjustment); \
	\
	(expRegCol)[destChunksPos + 1] |= (spread2 << (bitsAdjustment)) | (spread1 >> (bitsAdjustmentComplement)); \
	\
	(expRegCol)[destChunksPos + 2] |= spread2 >> (bitsAdjustmentComplement); \
}

// Based on https://stackoverflow.com/a/26639774/4149474
// and https://stackoverflow.com/a/239307/4149474
// Easier to just do this than keep reallocating the arrays etc. That approach may also limit how much of the
// memory we can use (as we have to copy stuff, so need both the old and new array in memory at once).
uint64_t estimateMemAvailable()
{
	// Allocate chunks of memory, doubling in size each time
	// When that fails, halve once to get back to what was successful,
	// then start adding an extra block ontop of what was successful,
	// doubling the size of the extra block each time.
	// Keep repeating that until an extra block of size 1 fails.
	// That could be either due to the memory limit being reached,
	// or the memory limit shrinking during execution - either is fine.
	uint64_t successfulSize = 0;
	for (uint64_t extraSize = 1; ; extraSize *= 2) {
		uint64_t* arr = new (std::nothrow) uint64_t[successfulSize + extraSize]();
		
		if (arr == NULL) {
			if (extraSize == 1) return successfulSize;
			
			successfulSize += extraSize / 2;
			extraSize = 1;
		}
		
		delete[] arr;
	}
}

void printZeros(uint64_t chunk, uint64_t printOffset) {
	// Find & print the position of the OFF bits, offset by printOffset
	for (uint64_t i = 0; i < CHUNK_BITS; i++) {
		if ((~chunk) & (1ULL << i)) {
			printTime();
			cout << ": found zero: " << bitPosToNum(printOffset + i) << endl;
		}
	}
}

void findAndPrintZeros() {
	//uint64_t estimatedMem = estimateMemAvailable();
	//uint64_t estimatedMem = 2000000000L;
	uint64_t estimatedMem = 100000000L;
	//uint64_t estimatedMem = 80000000000L;
	
	// Use 90% of the approx. available memory, rounded down to a multiple of 2
	// as there's 2 equal length arrays in use at any time.
	uint64_t memToUse = estimatedMem * 9 / 10;
	memToUse -= memToUse % 2;
	
	uint64_t colLength = memToUse / 2;
	//uint64_t colLength = 1;
	
	uint64_t maxBitPosition = colLength * CHUNK_BITS - 1;
	uint64_t maxValueRepresentable = bitPosToNum(maxBitPosition);
	
	cout << "Estimated memory = " << estimatedMem << " uint64_t's\r\n";
	cout << "Col length = " << colLength << "\r\n";
	cout << "Max bit position = " << maxBitPosition << "\r\n";
	cout << "Max value representable = " << maxValueRepresentable << "\r\n";
	cout << "\r\n";
	
	uint64_t *expRegCol = new uint64_t[colLength + 2](); // 2 chunks of overflow so doubling method can be branchless
	uint64_t *colsAggregate = new uint64_t[colLength]();
	
	printTime();
	cout << ": allocated" << endl;
	
	// zero the arrays
	for (uint64_t i = 0; i < colLength; i++) {
		expRegCol[i] = 0;
		colsAggregate[i] = 0;
	}
	
	// Setup column 0, i.e. ON at every power of 2, adjusted for missing multiples of 3:
	for (uint64_t i = 1; i < colLength * CHUNK_BITS; i *= 2) {
		uint64_t bitPos = numToBitPos(i);
		expRegCol[bitPos / CHUNK_BITS] |= 1ULL << (bitPos % CHUNK_BITS);
	}
	
	// Overlay column 0 onto the aggregate
	for (uint64_t i = 0; i < colLength; i++) {
		colsAggregate[i] |= expRegCol[i];
	}
	
	printTime();
	cout << ": finished setup" << endl << endl;
	
	uint64_t firstBitValueRepresented = 1;
	for (int powOf3 = 1; true; powOf3++) {
		firstBitValueRepresented += threeToThe(powOf3);
		
		uint64_t nextRoundFirstBitValueRepresented = firstBitValueRepresented + threeToThe(powOf3 + 1);
		uint64_t nextRoundAdjustment = numToBitPos(nextRoundFirstBitValueRepresented);
		
		if (firstBitValueRepresented > maxValueRepresentable) break;
		
		initialiseColFirstChunk(expRegCol, firstBitValueRepresented);
		
		uint64_t adjustment = numToBitPos(firstBitValueRepresented);
		uint64_t chunksAdjustment = adjustment / CHUNK_BITS;
		uint64_t bitsAdjustment = adjustment % CHUNK_BITS;
		uint64_t bitsAdjustmentComplement = CHUNK_BITS - bitsAdjustment;
		
		uint64_t lastBitToAggregate = numToBitPos(maxValueRepresentable - firstBitValueRepresented + 1);
		// bits beyond this are redundant - they don't overlap with the aggregate column
		
		uint64_t lastChunkToAggregate = lastBitToAggregate / CHUNK_BITS + 1; // not sure why +1 but it fixes it
		uint64_t lastChunkToDouble = (colLength - chunksAdjustment - 1) / 2;
		uint64_t lastChunkToCheckZeros = min((nextRoundAdjustment / CHUNK_BITS) - chunksAdjustment, lastChunkToAggregate);
		
		// only for when bitsAdjustment == 0
		#define aggregateAligned() \
			uint64_t aggChunksPos = chunk + chunksAdjustment; \
			uint64_t* aggChunks = colsAggregate + aggChunksPos; \
			aggChunks[0] |= expRegCol[chunk];
		
		// only for when bitsAdjustment is between 1 and 63 both inclusive
		#define aggregateBitAdjusted() \
			uint64_t aggChunksPos = chunk + chunksAdjustment; \
			uint64_t* aggChunks = colsAggregate + aggChunksPos; \
			aggChunks[0] |= expRegCol[chunk] << bitsAdjustment; \
			aggChunks[1] |= expRegCol[chunk] >> bitsAdjustmentComplement;
		
		// Tests if any bits are OFF. If so, then finds them & prints the numbers they represent
		#define checkForZeros() { \
			if (~aggChunks[0] != 0) { \
				cout << "\r"; \
				printZeros(aggChunks[0], aggChunksPos * 64); \
			} \
		}
		
		// Note: Don't print progress too often, or flush, as either may slow things
		// I chose a power of 2 as the interval to possibly be nice to the branch
		// predictor etc, also being able to do '&' instead of '%' is neat.
		#define printProgress() { \
			if ((chunk & 0xFFFF) == 0) { \
				cout << "\r" << "at: " << powOf3 << ", " << (chunk * 64); \
			} \
		}
		
		uint64_t chunk = 0;
		uint64_t firstLimit = min(lastChunkToDouble, lastChunkToCheckZeros);
		if (bitsAdjustment == 0) {
			for (; chunk < firstLimit; chunk++) {
				copyAlongToDoubleCurrentPos_macro(expRegCol, chunk, chunksAdjustment);
				aggregateAligned();
				checkForZeros();
				printProgress();
			}
		} else {
			for (; chunk < firstLimit; chunk++) {
				copyAlongToDoubleCurrentPos_macroBitAdjusted(expRegCol, chunk, chunksAdjustment, bitsAdjustment, bitsAdjustmentComplement);
				aggregateBitAdjusted();
				checkForZeros();
				printProgress();
			}
		}
		// Now we're either done doubling, or done checking for zeros
		
		// If we're done doubling, continue along until we'e done checking for zeros
		// Note that lastChunkToCheckZeros <= lastChunkToAggregate so aggregate() is always necessary (& won't go out of range)
		if (bitsAdjustment == 0) {
			for (; chunk < lastChunkToCheckZeros; chunk++) {
				aggregateAligned();
				checkForZeros();
				printProgress();
			}
		} else {
			for (; chunk < lastChunkToCheckZeros; chunk++) {
				aggregateBitAdjusted();
				checkForZeros();
				printProgress();
			}
		}
		// Now we're definitely done checking for zeros (and might also be done doubling)
		
		// Continue until we're either done doubling, or done aggregating
		uint64_t secondLimit = min(lastChunkToDouble, lastChunkToAggregate);
		if (bitsAdjustment == 0) {
			for (; chunk < secondLimit; chunk++) {
				copyAlongToDoubleCurrentPos_macro(expRegCol, chunk, chunksAdjustment);
				aggregateAligned();
				printProgress();
			}
		} else {
			for (; chunk < secondLimit; chunk++) {
				copyAlongToDoubleCurrentPos_macroBitAdjusted(expRegCol, chunk, chunksAdjustment, bitsAdjustment, bitsAdjustmentComplement);
				aggregateBitAdjusted();
				printProgress();
			}
		}
		
		// If we're done aggregating, continue along with the rest of the doubling
		if (bitsAdjustment == 0) {
			for (; chunk < lastChunkToDouble; chunk++) {
				copyAlongToDoubleCurrentPos_macro(expRegCol, chunk, chunksAdjustment);
				printProgress();
			}
		} else {
			for (; chunk < lastChunkToDouble; chunk++) {
				copyAlongToDoubleCurrentPos_macroBitAdjusted(expRegCol, chunk, chunksAdjustment, bitsAdjustment, bitsAdjustmentComplement);
				printProgress();
			}
		}
		
		// Otherwise, if we're done doubling, continue along with the rest of the aggregating
		if (bitsAdjustment == 0) {
			for (; chunk < lastChunkToAggregate; chunk++) {
				aggregateAligned();
				printProgress();
			}
		} else {
			for (; chunk < lastChunkToAggregate; chunk++) {
				aggregateBitAdjusted();
				printProgress();
			}
		}
		
		
		#undef aggregate
		#undef checkForZeros
		#undef printProgress
		
		//	cout << "col:\r\n";
		//	for (int i = 0; i < colLength; i++) {
		//		printUInt64Bits_cpu(expRegCol[i]);
		//		cout << "\r\n";
		//	}
		//	cout << "aggregate\r\n";
		//	for (int i = 0; i < colLength; i++) {
		//		printUInt64Bits_cpu(colsAggregate[i]);
		//		cout << "\r\n";
		//	}
		//	cout << "\r\n\r\n";
		
		//	if (powOf3 == 7) {
		//		ofstream outputfile;
		//		outputfile.open("tmp-output.txt");
		//		//Binary:
		//		//outputfile.write((char*)(void*)(newExpRegCol), colLength * sizeof(uint64_t));
		//		//Human-readable:
		//		for (uint64_t i = 0; i < colLength; i++) {
		//			string str = bitset<64>(newExpRegCol[i]).to_string('-', '#');
		//			for (int bit = 63; bit >= 0; bit--) {
		//				outputfile << str[bit];
		//			}
		//			outputfile << endl;
		//		}
		//	}
		
		cout << "\r";
		printTime();
		cout << ": finished column for shift of 3^" << powOf3 << endl;
		//	for (uint64_t i = colLength - 500; i < colLength; i++) {
		//		printUInt64Bits_cpu(prevExpRegCol[i]);
		//		cout << "\r\n";
		//	}
		
		printTime();
		cout << ": beginning next column" << endl;
	}
	
	cout << endl;
	printTime();
	cout << ": finished computing aggregate" << endl;
	cout << endl;
	
	// Go through the columns aggregate, checking for any chunks with any zero bits
	for (uint64_t chunk = 0; chunk < colLength; chunk++) {
		if (~colsAggregate[chunk] != 0) { // If any bits OFF
			printZeros(colsAggregate[chunk], chunk * 64);
		}
	}
}

int main(int argc, char *argv[]) {
	
	if (sizeof(uint64_t) != 8) {
		cout << "Error: unexpected uint64_t size '" << sizeof(uint64_t) << "', must be 8 bytes" << endl;
		return -1;
	}
	if (sizeof(unsigned int) != 4) {
		cout << "Error: unexpected unsigned int size '" << sizeof(unsigned int) << "', must be 4 bytes" << endl;
		return -1;
	}
	
	cout << "Started at: ";
	printTime();
	cout << endl;
	cout << endl;
	
	findAndPrintZeros();
	
	cout << endl;
	cout << "Finished at: ";
	printTime();
	cout << endl;
}