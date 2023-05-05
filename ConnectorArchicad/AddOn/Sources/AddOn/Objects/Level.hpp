#ifndef OBJECTS_LEVEL_HPP
#define OBJECTS_LEVEL_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"

namespace Objects
{

class Level {
public:
	short			floorID;
	short			floorIndex;
	GS::UniString	name;
	double			elevation;
	GS::UniString	units = "m";

	Level ();
	Level (short floorID, short floorIndex, const GS::UniString& name, double elevation);
	Level (const API_StoryType& sotry);
	Level (const Level& other);

	const API_StoryType ToAPI_Story () const;

	// bool operator==(const Level& rhs) const;
	Level& operator=(const Level& other);

	GSErrCode Restore (const GS::ObjectState& os);
	GSErrCode Store (GS::ObjectState& os) const;
};

}

#endif