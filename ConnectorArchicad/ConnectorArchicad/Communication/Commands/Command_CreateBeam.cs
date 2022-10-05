﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateBeam : ICommand<IEnumerable<string>>
  {

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {

      [JsonProperty("beams")]
      private IEnumerable<ArchicadBeam> Datas { get; }

      public Parameters(IEnumerable<ArchicadBeam> datas)
      {
        Datas = datas;
      }

    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {

      [JsonProperty("applicationIds")]
      public IEnumerable<string> ApplicationIds { get; private set; }

    }

    private IEnumerable<ArchicadBeam> Datas { get; }

    public CreateBeam(IEnumerable<ArchicadBeam> datas)
    {
      foreach (var data in datas)
      {
        data.displayValue = null;
        data.baseLine = null;
      }

      Datas = datas;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateBeam", new Parameters(Datas));
      return result == null ? null : result.ApplicationIds;
    }

  }
}
