using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;

namespace Speckle.GSA.API
{
  public interface IGSACache
  {
    GsaRecord GetNative<T>(int index);
    bool GetNative(Type t, int index, out GsaRecord gsaRecord);
    bool GetNative(Type t, out List<GsaRecord> gsaRecords);
    bool GetNatives(out List<GsaRecord> gsaRecords);
    bool GetNatives(Type t, out List<GsaRecord> gsaRecords);
    bool GetSpeckleObjects<T>(int index, out List<object> objects, GSALayer layer = GSALayer.Both);
    bool GetSpeckleObjects<T,U>(int index, out List<U> objects, GSALayer layer = GSALayer.Both);
    bool GetSpeckleObjects(out List<object> objects, GSALayer layer = GSALayer.Both);

    string GetApplicationId<T>(int index);
    string GetApplicationId(Type t, int gsaIndex);
    List<int> LookupIndices<T>();
    List<int?> LookupIndices<T>(IEnumerable<string> applicationIds);
    bool SetSpeckleObjects(GsaRecord gsaRecord, Dictionary<string, object> objectsByApplicationId, GSALayer layer = GSALayer.Both);
    bool SetNatives(Type speckleType, string applicationId, IEnumerable<GsaRecord> natives);

    bool Upsert(GsaRecord gsaRecord);
    bool Upsert(Dictionary<string, object> objectsByApplicationId, GSALayer layer = GSALayer.Both);
    bool Upsert(IEnumerable<GsaRecord> gsaRecords);
    int ResolveIndex<T>(string applicationId = "");
    int? LookupIndex<T>(string applicationId);

    double GetScalingFactor(UnitDimension unitDimension, string overrideUnits = null);
  }
}
