using Speckle.Converters.RevitShared;
using Speckle.Converters.RevitShared.Helpers;

// POC: this could perhaps becomes some RevitDocumentService
public class ReferencePointConverter : IReferencePointConverter
{
  // POC: probably not the best place for this
  private const string REFPOINT_INTERNAL_ORIGIN = "Internal Origin (default)";
  private const string REFPOINT_PROJECT_BASE = "Project Base";
  private const string REFPOINT_SURVEY = "Survey";

  private readonly RevitConversionSettings _revitSettings;
  private readonly IRevitConversionContextStack _contextStack;

  private Dictionary<string, DB.Transform> _docTransforms = new();

  public ReferencePointConverter(IRevitConversionContextStack contextStack, RevitConversionSettings revitSettings)
  {
    _contextStack = contextStack;
    _revitSettings = revitSettings;
  }

  // POC: the original allowed for the document to be passed in
  // if required, we would probably need to push the stack with a new document if the
  // doc can change during the lifeycycle of the conversions. This may need some looking into
  public DB.XYZ ConvertToExternalCoordindates(DB.XYZ inbound, bool isPoint)
  {
    var rpt = GetDocReferencePointTransform(_contextStack.Current.Document.Document);
    return (isPoint) ? rpt.OfPoint(inbound) : rpt.OfVector(inbound);
  }

  // POC: this might be better in some RevitDocumentService
  // we could probably return that instance instead of the Doc from the context, maybe...
  public DB.Transform GetDocReferencePointTransform(DB.Document doc)
  {
    //linked files are always saved to disc and will have a path name
    //if the current doc is unsaved it will not, but then it'll be the only one :)
    var id = doc.PathName;

    if (!_docTransforms.ContainsKey(id))
    {
      // get from settings
      var referencePointSetting = _revitSettings.TryGetSettingString("reference-point", out string value)
        ? value
        : string.Empty;
      _docTransforms[id] = GetReferencePointTransform(referencePointSetting);
    }

    return _docTransforms[id];
  }

  public DB.Transform GetReferencePointTransform(string referencePointSetting)
  {
    // first get the main doc base points and reference setting transform
    var referencePointTransform = DB.Transform.Identity;
    var points = new DB.FilteredElementCollector(_contextStack.Current.Document.Document)
      .OfClass(typeof(DB.BasePoint))
      .Cast<DB.BasePoint>()
      .ToList();
    var projectPoint = points.FirstOrDefault(o => o.IsShared == false);
    var surveyPoint = points.FirstOrDefault(o => o.IsShared);

    // POC:
    switch (referencePointSetting)
    {
      case REFPOINT_PROJECT_BASE: // note that the project base (ui) rotation is registered on the survey pt, not on the base point
        referencePointTransform = DB.Transform.CreateTranslation(projectPoint.Position);
        break;

      case REFPOINT_SURVEY:
        // note that the project base (ui) rotation is registered on the survey pt, not on the base point
        // retrieve the survey point rotation from the project point
        var angle = projectPoint.get_Parameter(DB.BuiltInParameter.BASEPOINT_ANGLETON_PARAM)?.AsDouble() ?? 0;

        // POC: following is not being disposed :(
        referencePointTransform = DB.Transform
          .CreateTranslation(surveyPoint.Position)
          .Multiply(DB.Transform.CreateRotation(DB.XYZ.BasisZ, angle));

        break;

      case REFPOINT_INTERNAL_ORIGIN:
        break;
    }

    return referencePointTransform;
  }
}
