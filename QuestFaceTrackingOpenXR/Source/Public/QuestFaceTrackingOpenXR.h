#ifndef QUESTFACETRACKINGOPENXR_H
#define QUESTFACETRACKINGOPENXR_H

#include "ExportedType.h"
#include <windows.h> 
#ifdef __cplusplus
extern "C" {
#endif

__declspec(dllexport) int InitOpenXRRuntime();
__declspec(dllexport) int UpdateOpenXRFaceTracker();
__declspec(dllexport) float GetCheekPuff(int cheekIndex);

__declspec(dllexport) void GetEyeOrientation(int eyeIndex, XrQuat* outData);
__declspec(dllexport) void GetFaceWeights(float outFaceExpressionFB[10]);

#ifdef __cplusplus
}
#endif
#endif // !QUESTFACETRACKINGOPENXR_H
//FaceExpressionFB FaceExpression;
int main()
{
	InitOpenXRRuntime();
	while (1)
	{
		Sleep(10);
		UpdateOpenXRFaceTracker();
		//GetFaceWeights(FaceExpression);
	}
	return 0;
}
