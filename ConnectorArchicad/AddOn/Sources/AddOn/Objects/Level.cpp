#include "Level.hpp"
#include "ObjectState.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "Utility.hpp"
#include "ResourceIds.hpp"

using namespace Objects;


static const GSResID SoryNameFormatID = ID_DEFAULT_STORY_FORMAT;
static const Int32 DefaultFormatID = 1;


Level::Level ()
	: floorID(0), floorIndex(0), name (""), elevation (0.0)
{
}


Level::Level (short floorID, short floorIndex, const GS::UniString& name, double elevation)
	: floorID(floorID), floorIndex(floorIndex), name (name), elevation (elevation)
{
}


Objects::Level::Level (const API_StoryType& story)
	: floorID(story.floorId), floorIndex(story.index), name(story.uName), elevation(story.level)
{
	if (0 == name.GetLength ()) {
		GS::UniString storyNameFormat;
		RSGetIndString (&storyNameFormat, SoryNameFormatID, DefaultFormatID, ACAPI_GetOwnResModule ());
		name = GS::UniString::Printf (storyNameFormat, floorIndex);
	}
}


Objects::Level::Level (const Level& other)
	: floorID(other.floorID), floorIndex(other.floorIndex), name(other.name), elevation(other.elevation)
{
}


const API_StoryType Level::ToAPI_Story () const
{
	API_StoryType story{};
	story.floorId = floorID;
	story.index = floorIndex;
	GS::ucscpy (story.uName, name.ToUStr ());
	story.level = elevation;

	return story;
}


Level& Level::operator=(const Level& other)
{
	if (this == &other) {
		return *this;
	}
	floorID = other.floorID;
	floorIndex = other.floorIndex;
	name = other.name;
	elevation = other.elevation;
	units = other.units;

	return *this;
}


GSErrCode Level::Restore (const GS::ObjectState& os)
{
	os.Get (FieldNames::ElementBase::ApplicationId, floorID);
	os.Get (FieldNames::Level::Index, floorIndex);
	os.Get (FieldNames::Level::Name, name);
	os.Get (FieldNames::Level::Elevation, elevation);
	os.Get (FieldNames::Level::Units, units);

	return NoError;
}


GSErrCode Level::Store (GS::ObjectState& os) const
{
	os.Add (FieldNames::ElementBase::ApplicationId, floorID);
	os.Add (FieldNames::Level::Index, floorIndex);
	os.Add(FieldNames::Level::Name, name);
	os.Add(FieldNames::Level::Elevation, elevation);
	os.Add(FieldNames::Level::Units, units);

	return NoError;
}
