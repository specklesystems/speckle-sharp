using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements.Revit
{
  public class Parameter : Base
  {
    public string name { get; set; }
    public object value { get; set; }
    public string revitUnitType { get; set; } //eg UnitType UT_Length
    public string revitUnit { get; set; } //DisplayUnitType eg DUT_MILLIMITERS

    /// <summary>
    /// If True it's a Shared Parameter, in which case the ApplicationId field will contain this parameter GUID, 
    /// otherwise it will store its BuiltInParameter name
    /// </summary>
    public bool isShared { get; set; } = false;
    public bool isReadOnly { get; set; } = false;

    /// <summary>
    /// True = Type Parameter, False = Instance Parameter 
    /// </summary>
    public bool isTypeParameter { get; set; } = false;


  }
}
