#include "GetProjectInfo.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"

namespace AddOnCommands
{

namespace FieldNames
{
static const char* ProjectName = "name";
static const char* ProjectLocation = "location";
static const char* ProjectLengthUnits = "lengthUnit";
static const char* ProjectAreaUnits = "areaUnit";
static const char* ProjectVolumeUnits = "volumeUnit";
static const char* ProjectAngleUnits = "angleUnit";
}


GS::String GetProjectInfo::GetName () const
{
	return GetProjectInfoCommandName;
}


GS::ObjectState GetProjectInfo::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{
	API_ProjectInfo projectInfo{};

	const GSErrCode err = ACAPI_Environment (APIEnv_ProjectID, &projectInfo, nullptr);
	if (err != NoError) {
		return GS::ObjectState{};
	}

	//if (projectInfo.untitled)
	//{
	//  return GS::ObjectState{ProjectName, Untitled};
	//}

	GS::ObjectState os;
	os.Add (FieldNames::ProjectName, *projectInfo.projectName);
	os.Add (FieldNames::ProjectLocation, *projectInfo.projectPath);

	API_WorkingUnitPrefs unitPrefs;
	ACAPI_Environment (APIEnv_GetPreferencesID, &unitPrefs, (void*) APIPrefs_WorkingUnitsID);
	os.Add (FieldNames::ProjectLengthUnits, unitPrefs.lengthUnit);
	os.Add (FieldNames::ProjectAreaUnits, unitPrefs.areaUnit);
	os.Add (FieldNames::ProjectVolumeUnits, unitPrefs.volumeUnit);
	os.Add (FieldNames::ProjectAngleUnits, unitPrefs.angleUnit);

	return os;
}


}
