#ifndef UTILITY_HPP
#define UTILITY_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace Utility {


  API_ElemTypeID GetElementType(const API_Guid& guid);

  bool ElementExists(const API_Guid& guid);

  bool IsElement3D(const API_Guid& guid);

  GSErrCode GetBaseElementData(API_Element& elem, API_ElementMemo* memo = nullptr);

  GS::Array<API_StoryType> GetStoryItems();

  double GetStoryLevel(short floorNumber);

  void SetStoryLevelAndFloor(const double& inLevel, short& floorInd, double& level);

  void SetStoryLevel(const double& inLevel, const short& floorInd, double& level);

  GS::Array<API_Guid> GetWallSubelements(API_WallType& wall);

}


#endif