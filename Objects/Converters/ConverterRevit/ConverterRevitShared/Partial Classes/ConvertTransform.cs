using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // transforms
    private Other.Transform TransformToSpeckle(
      Transform transform,
      Document doc,
      bool skipDocReferencePointTransform = false
    )
    {
      var externalTransform = transform;

      // get the reference point transform and apply if this is a top level instance
      if (!skipDocReferencePointTransform)
      {
        var docTransform = GetDocReferencePointTransform(doc);
        externalTransform = docTransform.Inverse.Multiply(transform);
      }

      // translation
      var tX = ScaleToSpeckle(externalTransform.Origin.X, ModelUnits);
      var tY = ScaleToSpeckle(externalTransform.Origin.Y, ModelUnits);
      var tZ = ScaleToSpeckle(externalTransform.Origin.Z, ModelUnits);
      var t = new Vector(tX, tY, tZ, ModelUnits);

      // basis vectors
      var vX = new Vector(
        externalTransform.BasisX.X,
        externalTransform.BasisX.Y,
        externalTransform.BasisX.Z,
        ModelUnits
      );
      var vY = new Vector(
        externalTransform.BasisY.X,
        externalTransform.BasisY.Y,
        externalTransform.BasisY.Z,
        ModelUnits
      );
      var vZ = new Vector(
        externalTransform.BasisZ.X,
        externalTransform.BasisZ.Y,
        externalTransform.BasisZ.Z,
        ModelUnits
      );

      // get the scale: TODO: do revit transforms ever have scaling?
      var scale = (float)transform.Scale;

      return new Other.Transform(vX, vY, vZ, t) { units = ModelUnits };
    }

    private Transform TransformToNative(Other.Transform transform)
    {
      var _transform = new Transform(Transform.Identity);

      // translation
      if (transform.matrix.M44 == 0)
        return _transform;
      var tX = ScaleToNative(transform.matrix.M14 / transform.matrix.M44, transform.units);
      var tY = ScaleToNative(transform.matrix.M24 / transform.matrix.M44, transform.units);
      var tZ = ScaleToNative(transform.matrix.M34 / transform.matrix.M44, transform.units);
      var t = new XYZ(tX, tY, tZ);

      // basis vectors
      XYZ vX = new XYZ(transform.matrix.M11, transform.matrix.M21, transform.matrix.M31);
      XYZ vY = new XYZ(transform.matrix.M12, transform.matrix.M22, transform.matrix.M32);
      XYZ vZ = new XYZ(transform.matrix.M13, transform.matrix.M23, transform.matrix.M33);

      // apply to new transform
      _transform.Origin = t;
      _transform.BasisX = vX.Normalize();
      _transform.BasisY = vY.Normalize();
      _transform.BasisZ = vZ.Normalize();

      // apply doc transform
      var docTransform = GetDocReferencePointTransform(Doc);
      var internalTransform = docTransform.Multiply(_transform);

      return internalTransform;
    }
  }
}
