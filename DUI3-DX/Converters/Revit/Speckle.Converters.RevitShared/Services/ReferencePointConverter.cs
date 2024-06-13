using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared;

[GenerateAutoInterface]
public class RevitConversionSettings : IRevitConversionSettings
{
  private Dictionary<string, string> Settings { get; } = new();

  public bool TryGetSettingString(string key, out string value) => Settings.TryGetValue(key, out value);

  public string this[string key]
  {
    get => Settings[key];
    set => Settings[key] = value;
  }
}

[GenerateAutoInterface]
public class ReferencePointConverter : IReferencePointConverter
{
  // POC: probably not the best place for this
  private const string REFPOINT_INTERNAL_ORIGIN = "Internal Origin (default)";
  private const string REFPOINT_PROJECT_BASE = "Project Base";
  private const string REFPOINT_SURVEY = "Survey";

  private readonly IRevitConversionSettings _revitSettings;
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly IRevitTransformUtils _transformUtils;
  private readonly IRevitFilterFactory _revitFilterFactory;
  private readonly IRevitXYZUtils _revitXyzUtils;

  private readonly Dictionary<string, IRevitTransform> _docTransforms = new();

  public ReferencePointConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    IRevitConversionSettings revitSettings,
    IRevitFilterFactory revitFilterFactory,
    IRevitTransformUtils transformUtils,
    IRevitXYZUtils revitXyzUtils
  )
  {
    _contextStack = contextStack;
    _revitSettings = revitSettings;
    _revitFilterFactory = revitFilterFactory;
    _transformUtils = transformUtils;
    _revitXyzUtils = revitXyzUtils;
  }

  // POC: the original allowed for the document to be passed in
  // if required, we would probably need to push the stack with a new document if the
  // doc can change during the lifeycycle of the conversions. This may need some looking into
  public IRevitXYZ ConvertToExternalCoordindates(IRevitXYZ inbound, bool isPoint)
  {
    var rpt = GetDocReferencePointTransform(_contextStack.Current.Document);
    return isPoint ? rpt.OfPoint(inbound) : rpt.OfVector(inbound);
  }

  // POC: this might be better in some RevitDocumentService
  // we could probably return that instance instead of the Doc from the context, maybe...
  public IRevitTransform GetDocReferencePointTransform(IRevitDocument doc)
  {
    //linked files are always saved to disc and will have a path name
    //if the current doc is unsaved it will not, but then it'll be the only one :)
    var id = doc.PathName;

    if (!_docTransforms.TryGetValue(id, out IRevitTransform? transform))
    {
      // get from settings
      var referencePointSetting = _revitSettings.TryGetSettingString("reference-point", out string value)
        ? value
        : string.Empty;
      transform = GetReferencePointTransform(referencePointSetting);
      _docTransforms[id] = transform;
    }

    return transform;
  }

  [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
  public IRevitTransform GetReferencePointTransform(string referencePointSetting)
  {
    // first get the main doc base points and reference setting transform
    var referencePointTransform = _transformUtils.Identity;

    // POC: bogus disposal below
    var points = _revitFilterFactory
      .CreateFilteredElementCollector(_contextStack.Current.Document)
      .OfClass<IRevitBasePoint>()
      .ToList();

    var projectPoint = NotNullExtensions.NotNull(points.FirstOrDefault(o => o.IsShared == false), "No projectPoint");
    var surveyPoint = NotNullExtensions.NotNull(points.FirstOrDefault(o => o.IsShared), "No surveyPoint");

    // POC: it's not clear what support is needed for this
    switch (referencePointSetting)
    {
      case REFPOINT_PROJECT_BASE: // note that the project base (ui) rotation is registered on the survey pt, not on the base point
        referencePointTransform = _transformUtils.CreateTranslation(projectPoint.Position);
        break;

      case REFPOINT_SURVEY:
        // note that the project base (ui) rotation is registered on the survey pt, not on the base point
        // retrieve the survey point rotation from the project point

        // POC: should a null angle resolve to 0?
        var angle = projectPoint.GetParameter(RevitBuiltInParameter.BASEPOINT_ANGLETON_PARAM)?.AsDouble() ?? 0;

        // POC: following disposed incorrectly or early or maybe a false negative?
        referencePointTransform = _transformUtils
          .CreateTranslation(surveyPoint.Position)
          .Multiply(_transformUtils.CreateRotation(_revitXyzUtils.BasisZ, angle));

        break;

      case REFPOINT_INTERNAL_ORIGIN:
        break;
    }

    return referencePointTransform;
  }
}
