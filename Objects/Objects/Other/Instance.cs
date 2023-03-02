using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

using Objects.Geometry;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Plane = Objects.Geometry.Plane;
using Vector = Objects.Geometry.Vector;

namespace Objects.Other
{
  
  public abstract class Instance: Base
  {
    /// <summary>
    /// The column-dominant 4x4 transform of this instance.
    /// </summary>
    /// <remarks>
    /// Indicates transform from internal origin [0,0,0]
    /// </remarks>
    public Transform transform { get; set; }
    
    public abstract Base definition { get; set; }
    
    /// <summary>
    /// The units of this Instance, should be the same as the instance transform units
    /// </summary>
    public string units { get; set; }

    protected Instance(Transform transform)
    {
      this.transform = transform ?? new Transform();
    }
  }
  
  /// <summary>
  /// Generic instance class
  /// </summary>
  public abstract class Instance<T> : Instance where T: Base
  {
    [JsonIgnore]
    protected T _definition;

    protected Instance(T definition, Transform transform): base(transform)
    {
      _definition = definition;
    }
    
    public Instance(): base(new Transform()) {}

    [DetachProperty]
    public override Base definition
    {
      get => _definition;
      set
      {
        if (value is T type)
          _definition = type;
      }
    }
    
  }

  /// <summary>
  /// Block instance class 
  /// </summary>
  public class BlockInstance : Instance<BlockDefinition>
  {
    [DetachProperty, Obsolete("Use definition property")]
    public BlockDefinition blockDefinition { get => _definition; set => _definition = value; }

    public BlockInstance() { }

    [SchemaInfo("Block Instance", "A Speckle Block Instance")]
    public BlockInstance(BlockDefinition blockDefinition, Transform transform): base(blockDefinition, transform)
    {
      // OLD: TODO: need to verify
      // Add base translation to transform. This assumes the transform is based on the world origin,
      // whereas the instance transform assumes it contains the basePoint translation already.
      //this.transform = transform * blockDefinition.GetBasePointTransform();
    }

    public List<ITransformable> GetTransformedGeometry()
    {
      return _definition.geometry.SelectMany(b =>
      {
        switch (b)
        {
          case BlockInstance bi:
            return bi.GetTransformedGeometry()?.Select(b =>
            {
              ITransformable childTransformed = null;
              b?.TransformTo(transform, out childTransformed);
              return childTransformed;
            });
          case ITransformable bt:
            var res = bt.TransformTo(transform, out var transformed);
            return new List<ITransformable> { res ? transformed : null };
          default:
            return new List<ITransformable>();
        }
      }).Where(b => b != null).ToList();
    }

    /// <summary>
    /// Returns a plane representing the insertion point and orientation of this Block instance.
    /// </summary>
    /// <remarks>This method will skip scaling. If you need scaling, we recommend using the transform instead.</remarks>
    /// <returns>A Plane on the insertion point of this Block Instance, with the correct 3-axis rotations.</returns>
    public Plane GetInsertionPlane()
    {
      // TODO: UPDATE!
      var plane = new Plane(_definition.basePoint ?? new Point(0,0,0, units), new Vector(0, 0, 1, units), new Vector(1, 0, 0, units), new Vector(0, 1, 0, units), units);
      plane.TransformTo(transform, out Plane tPlane);
      return tPlane;
    }
    
    /// <inheritdoc cref="GetInsertionPlane"/>
    [JsonIgnore]
    public Plane insertionPlane => GetInsertionPlane();
    
    /// <inheritdoc cref="GetTransformedGeometry"/>
    [JsonIgnore]
    public List<ITransformable> transformedGeometry => GetTransformedGeometry();
  }
}

namespace Objects.Other.Revit
{
  public class RevitInstance : Instance<RevitSymbolElementType>
  {
    public Level level { get; set; }
    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    
    public List<ITransformable> GetTransformedGeometry()
    {
      var allChildren = _definition.elements ?? new List<Base>();
      if (_definition.displayValue.Any())
      {
        allChildren.AddRange(_definition.displayValue);
      }

      // get transformed definition objs
      var transformed = allChildren.SelectMany(b =>
      {
        switch (b)
        {
          case RevitInstance ri:
            return ri.GetTransformedGeometry()?.Select(b =>
            {
              ITransformable childTransformed = null;
              b?.TransformTo(transform, out childTransformed);
              return childTransformed;
            });
          case ITransformable bt:
            var res = bt.TransformTo(transform, out var transformed);
            return new List<ITransformable> { res ? transformed : null };
          default:
            return new List<ITransformable>();
        }
      }).Where(b => b != null).ToList();

      // add any dynamically attached elements on this instance
      var elements = this["elements"] as List<object>;
      if (elements != null)
      {
        foreach (var element in elements)
        {
          var display = ((Base)element)["displayValue"] as List<object>;
          if (display != null)
          {
            transformed.AddRange(display.Cast<ITransformable>());
          }
        }
      }

      return transformed;
    }

    /// <summary>
    /// Returns a plane representing the insertion point and orientation of this revit instance.
    /// </summary>
    /// <remarks>This method will skip scaling. If you need scaling, we recommend using the transform instead.</remarks>
    /// <returns>A Plane on the insertion point of this Block Instance, with the correct 3-axis rotations.</returns>
    public Plane GetInsertionPlane()
    {
      // TODO: Check for Revit in GH/DYN
      var plane = new Plane(new Point(0, 0, 0, units), new Vector(0, 0, 1, units), new Vector(1, 0, 0, units), new Vector(0, 1, 0, units), units);
      plane.TransformTo(transform, out Plane tPlane);
      return tPlane;
    }

    /// <inheritdoc cref="GetInsertionPlane"/>
    [JsonIgnore]
    public Plane insertionPlane => GetInsertionPlane();
    
    /// <inheritdoc cref="GetTransformedGeometry"/>
    [JsonIgnore]
    public List<ITransformable> transformedGeometry => GetTransformedGeometry();
    
    public RevitInstance() { }
  }
}
