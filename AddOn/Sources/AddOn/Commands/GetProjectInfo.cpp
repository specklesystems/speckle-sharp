#include "GetProjectInfo.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"


namespace AddOnCommands {


static const char*		Untitled						= "Untitled";
static const char*		ProjectNameFieldName			= "name";
static const char*		ProjectLocationFieldName		= "location";


GS::String GetProjectInfo::GetNamespace () const
{
	return CommandNamespace;
}


GS::String GetProjectInfo::GetName () const
{
	return GetProjectInfoCommandName;
}


GS::ObjectState GetProjectInfo::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{
	API_ProjectInfo projectInfo {};

	const GSErrCode	err = ACAPI_Environment (APIEnv_ProjectID, &projectInfo, nullptr);
	if (err != NoError) {
		return GS::ObjectState{};
	}

	if (projectInfo.untitled) {
		return GS::ObjectState{ ProjectNameFieldName, Untitled };
	}

	GS::ObjectState os;
	os.Add (ProjectNameFieldName, *projectInfo.projectName);
	os.Add (ProjectLocationFieldName, *projectInfo.projectPath);

	return os;
}


}