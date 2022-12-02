#include "GetProjectInfo.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"

namespace AddOnCommands
{

static const char* ProjectNameFieldName = "name";
static const char* ProjectLocationFieldName = "location";
static const char* ProjectLengthUnitsFieldName = "lengthUnit";
static const char* ProjectAreaUnitsFieldName = "areaUnit";
static const char* ProjectVolumeUnitsFieldName = "volumeUnit";
static const char* ProjectAngleUnitsFieldName = "angleUnit";

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
	//  return GS::ObjectState{ProjectNameFieldName, Untitled};
	//}

	GS::ObjectState os;
	os.Add (ProjectNameFieldName, *projectInfo.projectName);
	os.Add (ProjectLocationFieldName, *projectInfo.projectPath);

	API_WorkingUnitPrefs unitPrefs;
	ACAPI_Environment (APIEnv_GetPreferencesID, &unitPrefs, (void*) APIPrefs_WorkingUnitsID);
	os.Add (ProjectLengthUnitsFieldName, unitPrefs.lengthUnit);
	os.Add (ProjectAreaUnitsFieldName, unitPrefs.areaUnit);
	os.Add (ProjectVolumeUnitsFieldName, unitPrefs.volumeUnit);
	os.Add (ProjectAngleUnitsFieldName, unitPrefs.angleUnit);

	return os;
}

}
