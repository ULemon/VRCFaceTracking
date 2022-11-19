#ifndef QUESTFACETRACKINGOPENXR_H
#define QUESTFACETRACKINGOPENXR_H
#include <windows.h> 
#ifdef __cplusplus
extern "C" {
#endif

__declspec(dllexport) int InitOpenXRRuntime();
__declspec(dllexport) int UpdateOpenXRFaceTracker();
__declspec(dllexport) float GetCheekPuff(int cheekIndex);



#ifdef __cplusplus
}
#endif
#endif // !QUESTFACETRACKINGOPENXR_H

int main()
{
	InitOpenXRRuntime();
	while (1)
	{
		Sleep(10);
		UpdateOpenXRFaceTracker();
	}
	return 0;
}
