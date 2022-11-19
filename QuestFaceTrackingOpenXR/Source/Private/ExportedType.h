#ifndef EXPORTEDTYPR_H
#define EXPORTEDTYPR_H

#ifdef __cplusplus
extern "C" {
#endif

	typedef struct XrQuat
	{
		float x;
		float y;
		float z;
		float w;
	}XrQuat;

	typedef struct FaceExpressionFB
	{
		float FaceExpression[63];
	} FaceExpressionFB;

#ifdef __cplusplus
}
#endif
#endif // !QUESTFACETRACKINGOPENXR_H#pragma once
