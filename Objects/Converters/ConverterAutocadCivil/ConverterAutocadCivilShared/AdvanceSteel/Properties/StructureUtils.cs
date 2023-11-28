#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AdvanceSteel.BuildingStructure;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AutoCAD.DatabaseServices;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;

namespace Objects.Converter.AutocadCivil;

internal static class StructureUtils
{
  private static Dictionary<string, BuildingStructureObject> GetStructures()
  {
    Dictionary<string, BuildingStructureObject> dictionary = new();

    BuildingStructureManager buildStructMan = BuildingStructureManager.getBuildingStructureManager();
    // get the list objects from the manager
    BuildingStructureManagerListObject buildStructManListObject = buildStructMan.ListObject;
    // get all of the structures in the BuildingStructureManagerListObject 
    List<BuildingStructureObject> bsoList = buildStructManListObject.Structures;

    foreach (BuildingStructureObject currBSO in bsoList)
    {
      // building structure objects have a name
      string name = buildStructManListObject.GetStructureName(currBSO);

      if (dictionary.ContainsKey(name))
      {
        continue;
      }

      dictionary.Add(name, currBSO);
    }

    return dictionary;
  }

  private static Dictionary<string, ObjectsGroup> GetObjectsGroup(BuildingStructureObject structure)
  {
    Dictionary<string, ObjectsGroup> dictionaryGroup = new();

    BuildingStructureTreeObject groupsTreeObject = structure.GroupsTreeObject;

    foreach (BuildingStructureItem currentItem in groupsTreeObject.StructureItems)
    {
      if (!(currentItem is ObjectsGroup objectsGroup))
      {
        continue;
      }

      BuildingStructureTreeObject parent = objectsGroup.Parent;
      string nameGroup = parent.GetStructureItemName(objectsGroup);

      dictionaryGroup.Add(nameGroup, objectsGroup);
    }

    return dictionaryGroup;
  }

  private static Dictionary<string, Dictionary<string, List<string>>> objectHandleHierarch;

  public static Dictionary<string, Dictionary<string, List<string>>> GetObjectHandleHierarchy()
  {
    if(objectHandleHierarch == null)
    {
      objectHandleHierarch = GetObjectHandleHierarchyIntern();
    }

    return objectHandleHierarch;
  }

  private static Dictionary<string, Dictionary<string, List<string>>> GetObjectHandleHierarchyIntern()
  {
    Dictionary<string, Dictionary<string, List<string>>> dictionaryHierarchy = new();

    var structures = GetStructures();

    foreach (var structure in structures)
    {
      var groups = GetObjectsGroup(structure.Value);

      foreach (var itemGroup in groups)
      {
        IEnumerable<string> listHandle = itemGroup.Value.getObjectsIDs().Select(x => new ObjectId(x.AsOldId()).Handle.ToString()).Where(x => !x.Equals("0"));

        foreach (var handle in listHandle)
        {
          Dictionary<string, List<string>> dictionaryStructureGroup;
          if (dictionaryHierarchy.ContainsKey(handle))
          {
            dictionaryStructureGroup = dictionaryHierarchy[handle];
          }
          else
          {
            dictionaryStructureGroup = new Dictionary<string, List<string>>();
            dictionaryHierarchy.Add(handle, dictionaryStructureGroup);
          }

          List<string> listGroup = null;

          if (dictionaryStructureGroup.ContainsKey(structure.Key))
          {
            listGroup = dictionaryStructureGroup[structure.Key];
          }
          else
          {
            listGroup = new List<string>();
            dictionaryStructureGroup.Add(structure.Key, listGroup);
          }

          listGroup.Add(itemGroup.Key);
        
        }//foreach handle

      }//foreach group

    }//foreach structure

    return dictionaryHierarchy;
  }

}

#endif
