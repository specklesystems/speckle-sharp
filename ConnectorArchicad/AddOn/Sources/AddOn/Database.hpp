#ifndef DATABASE_HPP
#define DATABASE_HPP

#pragma once

//API
#include "APIdefs_Database.h"


namespace Utility {


class Database {
public:
	Database ();
	virtual ~Database ();

	bool IsInFloorPlan (void) const;
	bool IsIn3DWindow (void) const;

	void SwitchToFloorPlan (void);
	void SwitchToPreviousState (void);

private:
	static API_DatabaseInfo GetCurrent (void);
	static void ChangeCurrent (API_DatabaseInfo&);

private:
	API_DatabaseInfo originalDatabaseInfo;
	API_DatabaseTypeID currentDatabaseType;
};


} //namespace Utility


#endif //DATABASE_HPP
