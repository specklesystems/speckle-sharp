#ifndef CREATE_COMMAND_HELPERS_HPP
#define CREATE_COMMAND_HELPERS_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"
#include "APIMigrationHelper.hpp"
#include "ObjectState.hpp"

namespace AddOnCommands {

namespace CommandHelpers {

template<class T>
GSErrCode GetCutfillPens (const T& element, GS::ObjectState& os, const GS::String& cutfillPenName, const GS::String& backgroundCutfillPenName)
{
	// Override cut fill pen
#ifdef ServerMainVers_2700
	if (element.cutFillPen.hasValue) {
		os.Add (cutfillPenName, element.cutFillPen.value);
	}
#else
	if (element.penOverride.overrideCutFillPen) {
		os.Add (cutfillPenName, element.penOverride.cutFillPen);
	}
#endif
	// Override cut fill background pen
#ifdef ServerMainVers_2700
	if (element.cutFillBackgroundPen.hasValue) {
		os.Add (backgroundCutfillPenName, element.cutFillBackgroundPen.value);
}
#else
	if (element.penOverride.overrideCutFillBackgroundPen) {
		os.Add (backgroundCutfillPenName, element.penOverride.cutFillBackgroundPen);
	}
#endif
	return NoError;
}

template<typename T>
static GSErrCode SetCutfillPens(const GS::ObjectState& os,
	const GS::String& cutFillPenIndexField,
	const GS::String& cutFillBackgroundPenIndexField,
	T& element,
	API_Element& mask)
{
	// Override cut fill pen
#ifdef ServerMainVers_2700
	element.cutFillPen = APINullValue;
	ACAPI_ELEMENT_MASK_SET (mask, T, cutFillPen);
	if (os.Contains (cutFillPenIndexField)) {
		os.Get (cutFillPenIndexField, element.cutFillPen.value);
	}
#else
	element.penOverride.overrideCutFillPen = false;
	ACAPI_ELEMENT_MASK_SET (mask, T, penOverride.overrideCutFillPen);
	if (os.Contains (cutFillPenIndexField)) {
		element.penOverride.overrideCutFillPen = true;
		os.Get (cutFillPenIndexField, element.penOverride.cutFillPen);
		ACAPI_ELEMENT_MASK_SET (mask, T, penOverride.cutFillPen);
	}
#endif

	// Override cut fill backgound pen
#ifdef ServerMainVers_2700
	element.cutFillBackgroundPen = APINullValue;
	ACAPI_ELEMENT_MASK_SET (mask, T, cutFillBackgroundPen);
	if (os.Contains (cutFillBackgroundPenIndexField)) {
		os.Get (cutFillBackgroundPenIndexField, element.cutFillBackgroundPen.value);
	}
#else
	element.penOverride.overrideCutFillBackgroundPen = false;
	ACAPI_ELEMENT_MASK_SET (mask, T, penOverride.overrideCutFillBackgroundPen);
	if (os.Contains (cutFillBackgroundPenIndexField)) {
		element.penOverride.overrideCutFillBackgroundPen = true;
		os.Get (cutFillBackgroundPenIndexField, element.penOverride.cutFillBackgroundPen);
		ACAPI_ELEMENT_MASK_SET (mask, T, penOverride.cutFillBackgroundPen);
	}
#endif
	return NoError;
}
}
}

#endif
