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

//	void copyAlongByPowerOfThree(uint64_t* prevExpRegCol, uint64_t* newExpRegCol, uint64_t sourceChunkNum, uint64_t computedPowOf3, uint64_t colLength) {
//		uint64_t shift = computedPowOf3 / 3 * 2; // adjust for missing multiples of 3
//		uint64_t offset = shift % CHUNK_BITS;
//		
//		uint64_t destChunk1Num = sourceChunkNum + (shift - offset)/CHUNK_BITS;
//		uint64_t destChunk2Num = destChunk1Num + 1;
//		
//		if (destChunk1Num < colLength) newExpRegCol[destChunk1Num] |= prevExpRegCol[sourceChunkNum] << offset;
//		if (destChunk2Num < colLength) newExpRegCol[destChunk2Num] |= prevExpRegCol[sourceChunkNum] >> (CHUNK_BITS - offset);
//	}

// returns true if did anything
bool copyAlongToDoubleCurrentPos(uint64_t* expRegCol, uint64_t sourceChunkNum, uint64_t firstBitValueRepresented, uint64_t colLength) {
	uint64_t spread1 = 0;
	uint64_t spread2 = 0;
	
	spreadBitsPaired(expRegCol[sourceChunkNum], &spread1, &spread2);
	
	uint64_t adjustment = numToBitPos(firstBitValueRepresented) + 1;
	// ^ +1 for the offset that's present in spreadAndOrBits_noMult3() but not spreadBitsPaired()
	uint64_t chunksAdjustment = adjustment / CHUNK_BITS;
	uint64_t bitsAdjustment = adjustment % CHUNK_BITS;
	
	uint64_t destChunksPos = sourceChunkNum * 2 + chunksAdjustment;
	if (destChunksPos >= colLength) return false;
	expRegCol[destChunksPos] |= spread1 << bitsAdjustment; // bitsAdjustment < 64 so this is safe
	
	if (destChunksPos + 1 >= colLength) return true;
	expRegCol[destChunksPos + 1] |=
		(spread2 << bitsAdjustment)
		| ((bitsAdjustment > 0) * (spread1 >> (CHUNK_BITS - bitsAdjustment)));
	// if bitsAdjustment == 0 then the second shift will be 64 bits, which is undefined behaviour,
	// and may be treated as a shift by 0 bits - not what we want. We want to just erase the
	// value completely when shifting by 64, so instead multiply by zero (rather than 1) to ignore the result.
	// bitsAdjustment will always be less than 64 though, so the first shift (and the shift
	// earlier) are fine.
	
	if (destChunksPos + 2 >= colLength) return true;
	expRegCol[destChunksPos + 2] |= (bitsAdjustment > 0) * (spread2 >> (CHUNK_BITS - bitsAdjustment));
	// if bitsAdjustment == 0 then the shift will be 64 bits, which is undefined behaviour as before
	
	return true;
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

void findAndPrintZeros() {
	//uint64_t estimatedMem = estimateMemAvailable();
	uint64_t estimatedMem = 20000000L;
	//uint64_t estimatedMem = 200000000L;
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
	
	uint64_t *expRegCol = new uint64_t[colLength]();
	uint64_t *colsAggregate = new uint64_t[colLength]();
	
	time_t time_alloc = chrono::system_clock::to_time_t(chrono::system_clock::now());
	cout << "Allocated @ " << ctime(&time_alloc); // ctime() adds a newline
	
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
	
	time_t time_setupdone = chrono::system_clock::to_time_t(chrono::system_clock::now());
	cout << "Finished setup @ " << ctime(&time_setupdone); // ctime() adds a newline
	
	// Fill in each next column, until doing so does nothing
	uint64_t firstBitValueRepresented = 1;
	for (int powOf3 = 1; true; powOf3++) {
		firstBitValueRepresented += threeToThe(powOf3);
		
		uint64_t nextRoundFirstBitValueRepresented = firstBitValueRepresented + threeToThe(powOf3 + 1);
		uint64_t nextRoundAdjustment = numToBitPos(nextRoundFirstBitValueRepresented);
		
		if (firstBitValueRepresented > maxValueRepresentable) break;
		
		initialiseColFirstChunk(expRegCol, firstBitValueRepresented);
		
		// Note: Don't print progress too often, or flush, as either may slow things
		// I chose a power of 2 as the interval to possibly be nice to the branch
		// predictor etc, also being able to do '&' instead of '%' is neat.
		
		uint64_t lastBitSettable = numToBitPos(maxValueRepresentable - firstBitValueRepresented + 1);
		// bits beyond this are redundant - they don't overlap with the aggregate column
		
		uint64_t lastChunkSettable = lastBitSettable / CHUNK_BITS;
		
		uint64_t adjustment = numToBitPos(firstBitValueRepresented);
		uint64_t chunksAdjustment = adjustment / CHUNK_BITS;
		uint64_t bitsAdjustment = adjustment % CHUNK_BITS;
		
		uint64_t chunk = 0;
		for (; chunk < colLength; chunk++) {
			bool copied = copyAlongToDoubleCurrentPos(expRegCol, chunk, firstBitValueRepresented, colLength);
			if (!copied) break;
			
			if (chunk > lastChunkSettable) break;
			
			colsAggregate[chunk + chunksAdjustment] |= expRegCol[chunk] << bitsAdjustment;
			colsAggregate[chunk + chunksAdjustment + 1] |= (bitsAdjustment > 0) * (expRegCol[chunk] >> (CHUNK_BITS - bitsAdjustment));
			
			if (chunk + chunksAdjustment > 0 && chunk + chunksAdjustment < (nextRoundAdjustment / CHUNK_BITS)) {
				if (~colsAggregate[chunk + chunksAdjustment] != 0) { // If any bits OFF
					// Then find & print the position of the OFF bits
					for (uint64_t i = 0; i < CHUNK_BITS; i++) {
						if ((~colsAggregate[chunk + chunksAdjustment]) & (1ULL << i)) {
							time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
							
							cout << "\r" << "found zero: " << bitPosToNum((chunk + chunksAdjustment) * 64 + i) << "\r\n";
						}
					}
				}
			}
			
			if ((chunk & 0xFFFF) == 0) { \
				cout << "\r" << "at: " << powOf3 << ", " << (chunk * 64); \
			}
		}
		
		for (; chunk <= lastChunkSettable; chunk++) {
			colsAggregate[chunk + chunksAdjustment] |= expRegCol[chunk] << bitsAdjustment;
			colsAggregate[chunk + chunksAdjustment + 1] |= (bitsAdjustment > 0) * (expRegCol[chunk] >> (CHUNK_BITS - bitsAdjustment));
			
			if (chunk + chunksAdjustment > 0 && chunk + chunksAdjustment < (nextRoundAdjustment / CHUNK_BITS)) {
				if (~colsAggregate[chunk + chunksAdjustment] != 0) { // If any bits OFF
					// Then find & print the position of the OFF bits
					for (uint64_t i = 0; i < CHUNK_BITS; i++) {
						if ((~colsAggregate[chunk + chunksAdjustment]) & (1ULL << i)) {
							time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
							
							cout << "\r" << "found zero: " << bitPosToNum((chunk + chunksAdjustment) * 64 + i) << "\r\n";
						}
					}
				}
			}
			
			if ((chunk & 0xFFFF) == 0) { \
				cout << "\r" << "at: " << powOf3 << ", " << (chunk * 64); \
			}
		}
		
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
		
		time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
		cout << "\rFinished column for shift of 3^" << powOf3 << " @ " << ctime(&time_now); // ctime() adds a newline
		//	for (uint64_t i = colLength - 500; i < colLength; i++) {
		//		printUInt64Bits_cpu(prevExpRegCol[i]);
		//		cout << "\r\n";
		//	}
		
		time_t time_movenext = chrono::system_clock::to_time_t(chrono::system_clock::now());
		cout << "Beginning next column @ " << ctime(&time_movenext); // ctime() adds a newline
	}
	
	// Go through the columns aggregate, checking for any chunks with any zero bits
	for (uint64_t chunk = 0; chunk < colLength; chunk++) {
		if (~colsAggregate[chunk] != 0) { // If any bits OFF
			// Then find & print the position of the OFF bits
			for (uint64_t i = 0; i < CHUNK_BITS; i++) {
				if ((~colsAggregate[chunk]) & (1ULL << i)) {
					time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
					
					cout << "\r" << "found zero: " << bitPosToNum(chunk * 64 + i) << "\r\n";
				}
			}
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
	
	auto start = chrono::system_clock::now();
	time_t start_time = chrono::system_clock::to_time_t(start);
	cout << "Started at: " << ctime(&start_time); // ctime() adds a newline
	cout << endl;
	
	findAndPrintZeros();
	
	cout << endl;
	time_t finish_time = chrono::system_clock::to_time_t(chrono::system_clock::now());
	cout << "Finished at: " << ctime(&finish_time); // ctime() adds a newline
}