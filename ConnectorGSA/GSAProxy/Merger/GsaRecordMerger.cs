using AutoMapper;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Proxy.Merger
{
  public class GsaRecordMerger : IGsaRecordMerger
  {
    private IMapper mapper;

    public void Initialise(List<Type> typesToRecognise)
    {
      var config = new MapperConfiguration(cfg =>
      {
        foreach (var t in typesToRecognise)
        {
          cfg.CreateMap(t, t);
        }
        cfg.ForAllPropertyMaps(pm => true, (pm, c) => c.MapFrom(new IgnoreNullResolver(), pm.SourceMember.Name));
      });

      config.AssertConfigurationIsValid();

      mapper = config.CreateMapper();
    }

    public GsaRecord Merge(GsaRecord newObject, GsaRecord oldObject)
    {
      if (newObject == oldObject)
      {
        return oldObject;
      }
      var resultingObject = mapper.Map(newObject, oldObject);
      return resultingObject;
    }
  }
}
