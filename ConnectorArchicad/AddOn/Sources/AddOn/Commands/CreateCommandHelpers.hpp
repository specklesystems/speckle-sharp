#ifndef CREATE_COMMAND_HELPERS_HPP
#define CREATE_COMMAND_HELPERS_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"
#include "APIMigrationHelper.hpp"


namespace AddOnCommands {


class CreateCommandHelpers {
public:
	template<typename T>
	static GSErrCode GetCutFillPens (const GS::ObjectState& os,
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
};
}

#endif
