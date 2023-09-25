using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Newtonsoft.Json;

namespace Objects.Other
{
  public abstract class Instance : Base
  {
    protected Instance(Transform transform)
    {
      this.transform = transform ?? new Transform();
    }

    protected Instance() { }

    /// <summary>
    /// The column-dominant 4x4 transform of this instance.
    /// </summary>
    /// <remarks>
    /// Indicates transform from internal origin [0,0,0]
    /// </remarks>
    public Transform transform { get; set; }

    public abstract Base definition { get; internal set; }

    /// <summary>
    /// The units of this Instance, should be the same as the instance transform units
    /// </summary>
    public string units { get; set; }

    // helper method that scans an Instance for all transformable geometry and nested instances
    protected virtual IEnumerable<Base> GetTransformableGeometry()
    {
      var displayValueRule = TraversalRule
        .NewTraversalRule()
        .When(DefaultTraversal.HasDisplayValue)
        .ContinueTraversing(_ => DefaultTraversal.displayValueAndElementsPropAliases);

      var instanceRule = TraversalRule.NewTraversalRule()
        .When(b => b is Instance instance && instance != null)
        .ContinueTraversing(DefaultTraversal.None);

      var traversal = new GraphTraversal(instanceRule, displayValueRule, DefaultTraversal.DefaultRule);

      return traversal
        .Traverse(definition)
        .Select(tc => tc.current)
        .Where(b => b is ITransformable || b is Instance)
        .Where(b => b != null);
    }

    [SchemaComputed("transformedGeometry")]
    public virtual IEnumerable<ITransformable> GetTransformedGeometry()
    {
      return GetTransformableGeometry()
        .SelectMany(b =>
        {
          switch (b)
          {
            case Instance i:
              return i.GetTransformedGeometry()
                .Select(b => 
                {
                  b.TransformTo(transform, out var tranformed);
                  return tranformed;
                });
            case ITransformable bt:
              var res = bt.TransformTo(transform, out var transformed);
              return res ? new List<ITransformable> { transformed } : new();
            default:
              return new List<ITransformable>();
          }
        })
        .Where(b => b != null);
    }
  }

  /// <summary>
  /// Generic instance class
  /// </summary>
  public abstract class Instance<T> : Instance
    where T : Base
  {
    protected Instance(T definition, Transform transform)
      : base(transform)
    {
      typedDefinition = definition;
    }

    protected Instance()
      : base(new Transform()) { }

    [JsonIgnore]
    public T typedDefinition { get; set; }

    [DetachProperty]
    public override Base definition
    {
      get => typedDefinition;
      internal set
      {
        if (value is T type)
          typedDefinition = type;
      }
    }
  }

  /// <summary>
  /// Block instance class
  /// </summary>
  public class BlockInstance : Instance<BlockDefinition>
  {
    public BlockInstance() { }

    [SchemaInfo("Block Instance", "A Speckle Block Instance")]
    public BlockInstance(BlockDefinition blockDefinition, Transform transform)
      : base(blockDefinition, transform)
    {
      // OLD: TODO: need to verify
      // Add base translation to transform. This assumes the transform is based on the world origin,
      // whereas the instance transform assumes it contains the basePoint translation already.
      //this.transform = transform * blockDefinition.GetBasePointTransform();
    }

    [DetachProperty, Obsolete("Use definition property", true), JsonIgnore]
    public BlockDefinition blockDefinition
    {
      get => typedDefinition;
      set => typedDefinition = value;
    }

    protected override IEnumerable<Base> GetTransformableGeometry()
    {
      return typedDefinition.geometry;
    }

    /// <summary>
    /// Returns a plane representing the insertion point and orientation of this Block instance.
    /// </summary>
    /// <remarks>This method will skip scaling. If you need scaling, we recommend using the transform instead.</remarks>
    /// <returns>A Plane on the insertion point of this Block Instance, with the correct 3-axis rotations.</returns>
    [SchemaComputed("insertionPlane")]
    public Plane GetInsertionPlane()
    {
      // TODO: UPDATE!
      var plane = new Plane(
        typedDefinition.basePoint ?? new Point(0, 0, 0, units),
        new Vector(0, 0, 1, units),
        new Vector(1, 0, 0, units),
        new Vector(0, 1, 0, units),
        units
      );
      plane.TransformTo(transform, out Plane tPlane);
      return tPlane;
    }
  }
}

namespace Objects.Other.Revit
{
  public class RevitInstance : Instance<RevitSymbolElementType>
  {
    public Level level { get; set; }
    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public bool mirrored { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    protected override IEnumerable<Base> GetTransformableGeometry()
    {
      var allChildren = typedDefinition.elements ?? new List<Base>();
      if (typedDefinition.displayValue.Any())
        allChildren.AddRange(typedDefinition.displayValue);
      return allChildren;
    }

    [SchemaComputed("transformedGeometry")]
    public override IEnumerable<ITransformable> GetTransformedGeometry()
    {
      var transformed = base.GetTransformedGeometry().ToList();

      // add any dynamically attached elements on this instance
      var elements = (this["elements"] ?? this["@elements"]) as List<object>;
      if (elements != null)
        foreach (var element in elements)
        {
          var display = ((Base)element)["displayValue"] as List<object>;
          if (display != null)
            transformed.AddRange(display.Cast<ITransformable>());
        }

      return transformed;
    }

    /// <summary>
    /// Returns a plane representing the insertion point and orientation of this revit instance.
    /// </summary>
    /// <remarks>This method will skip scaling. If you need scaling, we recommend using the transform instead.</remarks>
    /// <returns>A Plane on the insertion point of this Block Instance, with the correct 3-axis rotations.</returns>
    [SchemaComputed("insertionPlane")]
    public Plane GetInsertionPlane()
    {
      // TODO: Check for Revit in GH/DYN
      var plane = new Plane(
        new Point(0, 0, 0, units),
        new Vector(0, 0, 1, units),
        new Vector(1, 0, 0, units),
        new Vector(0, 1, 0, units),
        units
      );
      plane.TransformTo(transform, out Plane tPlane);
      return tPlane;
    }
  }
}
