#include "math-utils.h"
#include <cmath>
#include <iostream>
#include <stdint.h>

using namespace std;

int math_power_copy;
uint64_t math_n_copy;

uint64_t threePowers[40] = {
	1ull, 3ull, 9ull, 27ull, 81ull, 243ull, 729ull, 2187ull, 6561ull, 19683ull, 59049ull, 177147ull,
	531441ull, 1594323ull, 4782969ull, 14348907ull, 43046721ull, 129140163ull, 387420489ull, 1162261467ull,
	3486784401ull, 10460353203ull, 31381059609ull, 94143178827ull, 282429536481ull, 847288609443ull,
	2541865828329ull, 7625597484987ull, 22876792454961ull, 68630377364883ull, 205891132094649ull,
	617673396283947ull, 1853020188851841ull, 5559060566555523ull, 16677181699666569ull, 50031545098999707ull,
	150094635296999121ull, 450283905890997363ull, 1350851717672992089ull, 4052555153018976267ull
};
//	uint64_t threeToThe(int power) {
//		if (power >= 40) {
//			cout << "Error: overflow in 3^n function" << endl;
//			exit(-1);
//		}
//		return threePowers[power];
//	}

//Based on https://stackoverflow.com/a/23000588/4149474 which is based on https://stackoverflow.com/a/11398748/4149474
//which is based on https://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn
//"It's correct for all inputs except 0. It returns 0 for 0 which may be valid for what you're using it for. The lines
//with the shifts round n up to 1 less than the next power of 2. It basically sets all bits after the leading 1 bit to 1.
//This reduces all possible inputs to 64 possible values: 0x0, 0x1, 0x3, 0x7, 0xf, 0x1f, 0x3f, etc. Multiplying those 64
//values with the number 0x03f6eaf2cd271461 gives you another 64 unique values in the top 6 bits. The shift by 58 just
//positions those 6 bits for use as an index into table."
//Also, "0x03f6eaf2cd271461 is a De Bruijn sequence" (nwellnhof 2017)
char floorLog2Lookup_64bit[64] = {
	0, 58, 1, 59, 47, 53, 2, 60, 39, 48, 27, 54, 33, 42, 3, 61,
	51, 37, 40, 49, 18, 28, 20, 55, 30, 34, 11, 43, 14, 22, 4, 62,
	57, 46, 52, 38, 26, 32, 41, 50, 36, 17, 19, 29, 10, 13, 21, 56,
	45, 25, 31, 35, 16, 9, 12, 44, 24, 15, 8, 23, 7, 6, 5, 63
};
//	char floorLog2_64bit(uint64_t n)
//	{
//		n |= n >> 1;
//		n |= n >> 2;
//		n |= n >> 4;
//		n |= n >> 8;
//		n |= n >> 16;
//		n |= n >> 32;
//	
//		return floorLog2Lookup_64bit[(n * 0x03f6eaf2cd271461) >> 58];
//	}

// Takes the bits of x, and OR's the lower half into the even numbered positions (zero indexed) of *low,
// and the upper half into the even numbered positions of *high
// Adapted from: http://www.graphics.stanford.edu/~seander/bithacks.html#InterleaveBMN
void spreadAndOrBits(uint64_t x, uint64_t *low, uint64_t *high) {
	uint64_t xLow = x & 0x00000000FFFFFFFF;
	uint64_t xHigh = (x & 0xFFFFFFFF00000000) >> 32;
	
	xLow = (xLow | (xLow << 16)) & 0x0000FFFF0000FFFF; //16 0's, 16 1's, 16 0's, 16 1's
	xLow = (xLow | (xLow << 8 )) & 0x00FF00FF00FF00FF; //8 0's, 8 1's, 8 0's, ...
	xLow = (xLow | (xLow << 4 )) & 0x0F0F0F0F0F0F0F0F; //00001111...
	xLow = (xLow | (xLow << 2 )) & 0x3333333333333333; //00110011...
	xLow = (xLow | (xLow << 1 )) & 0x5555555555555555; //0101...
	
	xHigh = (xHigh | (xHigh << 16)) & 0x0000FFFF0000FFFF;
	xHigh = (xHigh | (xHigh << 8 )) & 0x00FF00FF00FF00FF;
	xHigh = (xHigh | (xHigh << 4 )) & 0x0F0F0F0F0F0F0F0F;
	xHigh = (xHigh | (xHigh << 2 )) & 0x3333333333333333;
	xHigh = (xHigh | (xHigh << 1 )) & 0x5555555555555555;
	
	*low |= xLow;
	*high |= xHigh;
}

void spreadAndOrBits_noMult3(uint64_t x, uint64_t *low, uint64_t *high) {
	// Workings spreadsheet ("omit multiples of 3 workings 2.xlsx") shows that when omitting
	// the multiples of 3, we still double the chunk position as usual, then in this method
	// we just leave off the last step when spreading the bits (so they remain in pairs rather
	// than fully spaced out), and then shift to the left by 1.
	
	uint64_t xLow = x & 0x00000000FFFFFFFF;
	uint64_t xHigh = (x & 0xFFFFFFFF00000000) >> 32;
	
	xLow = (xLow | (xLow << 16)) & 0x0000FFFF0000FFFF; //16 0's, 16 1's, 16 0's, 16 1's
	xLow = (xLow | (xLow << 8 )) & 0x00FF00FF00FF00FF; //8 0's, 8 1's, 8 0's, ...
	xLow = (xLow | (xLow << 4 )) & 0x0F0F0F0F0F0F0F0F; //00001111...
	xLow = (xLow | (xLow << 2 )) & 0x3333333333333333; //00110011...
	xLow = xLow << 1;
	
	xHigh = (xHigh | (xHigh << 16)) & 0x0000FFFF0000FFFF;
	xHigh = (xHigh | (xHigh << 8 )) & 0x00FF00FF00FF00FF;
	xHigh = (xHigh | (xHigh << 4 )) & 0x0F0F0F0F0F0F0F0F;
	xHigh = (xHigh | (xHigh << 2 )) & 0x3333333333333333;
	xHigh = xHigh << 1;
	
	*low |= xLow;
	*high |= xHigh;
}

// transforms something like:
// 11111111 to:
// 11001100 11001100
// bits in *low and *high are overwritten, not ORed or anything
void spreadBitsPaired(uint64_t x, uint64_t *low, uint64_t *high) {
	uint64_t xLow = x & 0x00000000FFFFFFFF;
	uint64_t xHigh = (x & 0xFFFFFFFF00000000) >> 32;
	
	xLow = (xLow | (xLow << 16)) & 0x0000FFFF0000FFFF; //16 0's, 16 1's, 16 0's, 16 1's
	xLow = (xLow | (xLow << 8 )) & 0x00FF00FF00FF00FF; //8 0's, 8 1's, 8 0's, ...
	xLow = (xLow | (xLow << 4 )) & 0x0F0F0F0F0F0F0F0F; //00001111...
	xLow = (xLow | (xLow << 2 )) & 0x3333333333333333; //00110011...
	
	xHigh = (xHigh | (xHigh << 16)) & 0x0000FFFF0000FFFF;
	xHigh = (xHigh | (xHigh << 8 )) & 0x00FF00FF00FF00FF;
	xHigh = (xHigh | (xHigh << 4 )) & 0x0F0F0F0F0F0F0F0F;
	xHigh = (xHigh | (xHigh << 2 )) & 0x3333333333333333;
	
	*low = xLow;
	*high = xHigh;
}