// C# version has some comments & explanation; this is just the same thing

#include "math-utils.h"
#include <iostream>
#include <cmath>

using namespace std;

unsigned long long threePowers[] = {
	1ull, 3ull, 9ull, 27ull, 81ull, 243ull, 729ull, 2187ull, 6561ull, 19683ull, 59049ull, 177147ull,
	531441ull, 1594323ull, 4782969ull, 14348907ull, 43046721ull, 129140163ull, 387420489ull, 1162261467ull,
	3486784401ull, 10460353203ull, 31381059609ull, 94143178827ull, 282429536481ull, 847288609443ull,
	2541865828329ull, 7625597484987ull, 22876792454961ull, 68630377364883ull, 205891132094649ull,
	617673396283947ull, 1853020188851841ull, 5559060566555523ull, 16677181699666569ull, 50031545098999707ull,
	150094635296999121ull, 450283905890997363ull, 1350851717672992089ull, 4052555153018976267ull
};
unsigned long long threeToThe(int power) {
	if (power >= sizeof(threePowers)/sizeof(unsigned long long)) {
		cout << "Error: overflow in 3^n function" << endl;
		exit(0);
	}
	return threePowers[power];
}

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
char floorLog2_64bit(unsigned long long n)
{
	n |= n >> 1;
	n |= n >> 2;
	n |= n >> 4;
	n |= n >> 8;
	n |= n >> 16;
	n |= n >> 32;

	return floorLog2Lookup_64bit[(n * 0x03f6eaf2cd271461) >> 58];
}