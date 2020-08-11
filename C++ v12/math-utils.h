#include <stdint.h>

#ifndef MATH_UTILS_H
#define MATH_UTILS_H

extern int math_power_copy;
extern uint64_t math_n_copy;

extern uint64_t threePowers[40];
extern char floorLog2Lookup_64bit[64];

void spreadAndOrBits(uint64_t x, uint64_t *low, uint64_t *high);
void spreadAndOrBits_noMult3(uint64_t x, uint64_t *low, uint64_t *high);
void spreadBitsPaired(uint64_t x, uint64_t *low, uint64_t *high);

#define threeToThe(power) ( \
	math_power_copy = (power), \
	math_power_copy >= 40 \
	? (cout << "Error: overflow in 3^n function" << endl, exit(-1), 0) \
	: \
	threePowers[math_power_copy] \
)

#define floorLog2_64bit(n) ( \
	math_n_copy = (n), \
	math_n_copy |= math_n_copy >> 1, \
	math_n_copy |= math_n_copy >> 2, \
	math_n_copy |= math_n_copy >> 4, \
	math_n_copy |= math_n_copy >> 8, \
	math_n_copy |= math_n_copy >> 16, \
	math_n_copy |= math_n_copy >> 32, \
	floorLog2Lookup_64bit[(math_n_copy * 0x03f6eaf2cd271461) >> 58] \
)

//Note: returns true for n == 0
//Source: http://www.graphics.stanford.edu/~seander/bithacks.html#DetermineIfPowerOf2
#define isPowerOf2(n) (math_n_copy = (n), (math_n_copy & (math_n_copy - 1)) == 0)

// input:   1  2  4  5  7  8  10 11 13 14 16 17 19 20 22 23 25 26 28 29 31 ...
// maps to: 0  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 ...
#define mapToAvoidMult3s(n) \
	((n) - (uint64_t)((n) / 3) - 1)

#define spreadBitsPaired_macro(x, low, high) { \
	low = (x) & 0x00000000FFFFFFFF; \
	high = ((x) & 0xFFFFFFFF00000000) >> 32; \
	\
	low = (low | (low << 16)) & 0x0000FFFF0000FFFF; \
	low = (low | (low << 8 )) & 0x00FF00FF00FF00FF; \
	low = (low | (low << 4 )) & 0x0F0F0F0F0F0F0F0F; \
	low = (low | (low << 2 )) & 0x3333333333333333; \
	\
	high = (high | (high << 16)) & 0x0000FFFF0000FFFF; \
	high = (high | (high << 8 )) & 0x00FF00FF00FF00FF; \
	high = (high | (high << 4 )) & 0x0F0F0F0F0F0F0F0F; \
	high = (high | (high << 2 )) & 0x3333333333333333; \
}

#endif