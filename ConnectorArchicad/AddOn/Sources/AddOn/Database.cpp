#include "ACAPinc.h"
#include "APIMigrationHelper.hpp"
#include "Database.hpp"


using namespace Utility;

Database::Database () :
	currentDatabaseType (API_ZombieWindowID)
{
	originalDatabaseInfo = Utility::Database::GetCurrent ();
	currentDatabaseType = originalDatabaseInfo.typeID;
}


Database::~Database ()
{
	Utility::Database::ChangeCurrent (originalDatabaseInfo);
}


bool Database::IsInFloorPlan (void) const
{
	return currentDatabaseType == APIWind_FloorPlanID;
}


bool Database::IsIn3DWindow (void) const
{
	return currentDatabaseType == APIWind_3DModelID;
}


void Database::SwitchToFloorPlan (void)
{
	API_DatabaseInfo databaseInfo;
	BNZeroMemory (&databaseInfo, sizeof (API_DatabaseInfo));
	databaseInfo.typeID = APIWind_FloorPlanID;

	ChangeCurrent (databaseInfo);
	currentDatabaseType = databaseInfo.typeID;
}


/*static*/ API_DatabaseInfo Database::GetCurrent (void)
{
	API_DatabaseInfo databaseInfo;
	BNZeroMemory (&databaseInfo, sizeof (API_DatabaseInfo));
	GSErrCode err = ACAPI_Database_GetCurrentDatabase (&databaseInfo);

	if (err != NoError)
		databaseInfo.typeID = API_ZombieWindowID;

	return databaseInfo;
}


/*static*/ void Database::ChangeCurrent (API_DatabaseInfo& databaseInfo)
{
	ACAPI_Database_ChangeCurrentDatabase (&databaseInfo);
}
