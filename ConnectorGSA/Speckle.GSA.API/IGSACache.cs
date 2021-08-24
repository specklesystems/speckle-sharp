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

    string GetApplicationId<T>(int index);
    List<int> LookupIndices<T>();
    List<int?> LookupIndices<T>(IEnumerable<string> applicationIds);
    bool SetSpeckleObjects(GsaRecord gsaRecords, IEnumerable<object> speckleObjects);

    bool Upsert(GsaRecord gsaRecord);
    int ResolveIndex<T>(string applicationId = "");
  }
}
