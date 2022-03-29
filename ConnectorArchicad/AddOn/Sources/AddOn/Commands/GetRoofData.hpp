#ifndef GET_ROOF_DATA_HPP
#define GET_ROOF_DATA_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {


  class GetRoofData : public BaseCommand {
  public:
    virtual GS::String							GetName() const override;
    virtual GS::ObjectState						Execute(const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
  };


}


#endif