//using Rhino.Render;
//using RenderMaterial = Rhino.Render.RenderMaterial;

//using RH = Rhino.DocObjects;

//public interface IRhinoMaterialConverter
//{
//  public other.RenderMaterial RenderMaterialToSpeckle(RH.Material material)
//  {
//    var renderMaterial = new RenderMaterial();
//    if (material == null)
//      return renderMaterial;

//    renderMaterial.name = material.Name ?? "default"; // default rhino material has no name or id
//#if RHINO6

//    renderMaterial.diffuse = material.DiffuseColor.ToArgb();
//    renderMaterial.emissive = material.EmissionColor.ToArgb();
//    renderMaterial.opacity = 1 - material.Transparency;

//    // for some reason some default material transparency props are 1 when they shouldn't be - use this hack for now
//    if ((renderMaterial.name.ToLower().Contains("glass") || renderMaterial.name.ToLower().Contains("gem")) && renderMaterial.opacity == 0)
//      renderMaterial.opacity = 0.3;
//#else
//    RH.Material matToUse = material;
//    if (!material.IsPhysicallyBased)
//    {
//      matToUse = new RH.Material();
//      matToUse.CopyFrom(material);
//      matToUse.ToPhysicallyBased();
//    }
//    using (var rm = RenderMaterial.FromMaterial(matToUse, null))
//    {
//      RH.PhysicallyBasedMaterial pbrMaterial = rm.ConvertToPhysicallyBased(RenderTexture.TextureGeneration.Allow);
//      renderMaterial.diffuse = pbrMaterial.BaseColor.AsSystemColor().ToArgb();
//      renderMaterial.emissive = pbrMaterial.Emission.AsSystemColor().ToArgb();
//      renderMaterial.opacity = pbrMaterial.Opacity;
//      renderMaterial.metalness = pbrMaterial.Metallic;
//      renderMaterial.roughness = pbrMaterial.Roughness;
//    }
//#endif

//    return renderMaterial;
//  }
//}
