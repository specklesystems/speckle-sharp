from typing import List


def get_function_from_file(file, trim_startss, start_line_condition=None):
    replace_from_to = {}
    with open(file, encoding="utf-8") as f:
        string = ""
        brackets_open = 0
        brackets_started = 0
        for line in f.readlines():
            if not line.startswith("//") and not line.startswith("      //"):
                # get replacement pairs
                if "using " in line and " = " in line:
                    replace_from_to.update(
                        {
                            line.split("using ")[1].split(" = ")[0]
                            + ".": line.split(" = ")[1].split(";")[0]
                            + "."
                        }
                    )

                # get actual string
                for trim_start in trim_startss:
                    if trim_start in line and (
                        start_line_condition is None
                        or start_line_condition(line) is True
                    ):  # start writing string
                        for key_replace, val_replace in replace_from_to.items():
                            if key_replace in line:
                                line = line.replace(key_replace, val_replace)
                        string += line
                    elif (
                        len(string) > 0 and brackets_open == 0 and brackets_started > 0
                    ):
                        for key_replace, val_replace in replace_from_to.items():
                            if key_replace in line:
                                line = line.replace(key_replace, val_replace)
                        string += line
                        break
                    elif len(string) > 0:  # keep adding lines
                        # start counting brackets
                        brackets_open += line.count("{")
                        brackets_open -= line.count("}")
                        if brackets_open > 0:
                            brackets_started += 1
                        for key_replace, val_replace in replace_from_to.items():
                            if key_replace in line:
                                line = line.replace(key_replace, val_replace)
                        string += line
                if string != "":
                    break
    return string


###################################### get classes from files (assuming 1 class per file)
def get_trimmed_strings_per_line_from_files(
    trim_0_start, trim_0_end, trim_1_startss: list, trim_1_end, files
):
    all_classes = []
    all_files = []
    for file in files:
        with open(file) as f:
            start_str = None
            end_str = None

            for line in f.readlines():
                if not line.startswith("//") and not line.startswith("      //"):
                    if "ModelInfo" in line:
                        pass

                    for trim_1_start in trim_1_startss:
                        if (
                            trim_0_start in line
                            and trim_0_end in line
                            and start_str is None
                        ):
                            start_str = line.split(trim_0_start)[1].split(trim_0_end)[0]
                        elif trim_1_start in line and trim_1_end in line:
                            end_str = (
                                line.split(trim_1_start)[1]
                                .split(trim_1_end)[0]
                                .split("<")[0]
                            )
                        if start_str and end_str:
                            all_classes.append(start_str + "." + end_str)
                            all_files.append(file)
                            # start_str = None
                            end_str = None
                            # break
                    if start_str and end_str:
                        # start_str = None
                        end_str = None
                        # break

    return all_classes, all_files


def get_speckle_class_full_name(short_name: str, all_classes: List[str]):
    long_name = None
    short_name = short_name.split(".")[-1]
    if len(short_name) > 1 and short_name not in [
        "string",
        "bool",
        "int",
        "double",
        "string[]",
        "bool[]",
        "int[]",
        "double[]",
    ]:
        for cl in all_classes:
            if cl.endswith("." + short_name):
                long_name = cl
                return [long_name]
            elif short_name == "ICurve":
                return [
                    "Objects.Geometry.Circle",
                    "Objects.Geometry.Curve",
                    "Objects.Geometry.Ellipse",
                    "Objects.Geometry.Line",
                    "Objects.Geometry.Polycurve",
                    "Objects.Geometry.Polyline",
                    "Objects.Geometry.Spiral",
                ]


def get_detailed_classes_from_files(all_cl, all_f, result_all_classes):
    all_classes_dict = {}
    for i, file in enumerate(all_f):
        short_name = all_cl[i].split(".")[-1]
        if short_name == "RebarGroup":
            pass

        def condition(line):
            if " : " in line:
                return True
            return False

        function_str = get_function_from_file(
            file,
            [f"public class {short_name} ", f"public abstract class {short_name} "],
            condition,
        )
        if len(function_str) < 3:
            function_str = get_function_from_file(
                file,
                [f"public class {short_name}<", f"public abstract class {short_name}<"],
                condition,
            )
        start = 0
        all_classes_dict.update(
            {all_cl[i]: {"parent": "", "all_parents": [], "subclasses": {}}}
        )

        for line in function_str.split("\n"):
            if len(line) < 3:
                continue
            elif (f"public class {short_name}" in line and " : " in line) or (
                f"public abstract class {short_name}" in line and " : " in line
            ):
                parent = line.split(" : ")[-1].split(",")[0].split(" ")[0].split("<")[0]
                if parent != "Base" and not parent.endswith(".Collection"):
                    parent = get_speckle_class_full_name(parent, result_all_classes)
                    if parent is not None:
                        all_classes_dict[all_cl[i]]["parent"] = parent[0]
                    else:
                        pass
                else:
                    all_classes_dict[all_cl[i]]["parent"] = parent
                if "IDisplayValue" in line:
                    val = (
                        line.split("IDisplayValue<")[-1]
                        .split(">")[0]
                        .replace("List<", "")
                        .replace(">", "")
                    )
                    val = get_speckle_class_full_name(val, result_all_classes)
                    if "IDisplayValue<List<" in line:
                        all_classes_dict[all_cl[i]]["subclasses"].update(
                            {"displayValue[]": val}
                        )
                    else:
                        all_classes_dict[all_cl[i]]["subclasses"].update(
                            {"displayValue": val}
                        )

            elif (
                f"public {short_name}(" in line
                and "()" not in line
                and "( )" not in line
            ):
                start = 1
                if ")" in line:
                    combos = line.split(")")[0].split("(")[0].split(",")
                    # if len(combos) > 2:
                    for combo in combos:
                        cl_name = combo.split(" ")[-1].split(")")[0]
                        cl_type = (
                            combo.split(" " + cl_name)[0].split("(")[-1].split(" ")[-1]
                        )
                        if "List<" in cl_type:
                            cl_name += "[]"
                            cl_type = cl_type.replace("List<", "").replace(">", "")
                        # if "RevitInstance" in line:
                        #    pass
                        cl_type = get_speckle_class_full_name(
                            cl_type, result_all_classes
                        )
                        if len(cl_name) > 1 and cl_type is not None:
                            all_classes_dict[all_cl[i]]["subclasses"].update(
                                {cl_name: cl_type}
                            )
                    break

            elif "public " in line and " { get; set; }" in line:
                start = 1
                cl_name = line.split(",")[0].split(" { get; set; }")[0].split(" ")[-1]
                cl_type = line.split(" " + cl_name)[0].split(" ")[-1]
                if "List<" in cl_type:
                    cl_name += "[]"
                    cl_type = cl_type.replace("List<", "").replace(">", "")
                cl_type = get_speckle_class_full_name(cl_type, result_all_classes)
                if len(cl_name) > 1 and cl_type is not None:
                    all_classes_dict[all_cl[i]]["subclasses"].update({cl_name: cl_type})

            elif (
                start == 1
                and ")" not in line
                and "=" not in line
                and "  ///" not in line
            ):
                cl_name = line.split(",")[0].split(" { get; set; }")[0].split(" ")[-1]
                cl_type = line.split(" " + cl_name)[0].split(" ")[-1]
                if "List<" in cl_type:
                    cl_name += "[]"
                    cl_type = cl_type.replace("List<", "").replace(">", "")
                cl_type = get_speckle_class_full_name(cl_type, result_all_classes)
                if len(cl_name) > 1 and cl_type is not None:
                    all_classes_dict[all_cl[i]]["subclasses"].update({cl_name: cl_type})
            elif start == 1 and ")" in line:
                break
    return all_classes_dict


CLASSES_PYTHON = """Objects.Geometry.Point
Objects.Geometry.Pointcloud
Objects.Geometry.Vector
Objects.Geometry.ControlPoint
Objects.Geometry.Plane
Objects.Geometry.Box
Objects.Geometry.Line
Objects.Geometry.Arc
Objects.Geometry.Circle
Objects.Geometry.Ellipse
Objects.Geometry.Polyline
Objects.Geometry.Spiral
Objects.Geometry.Curve
Objects.Geometry.Polycurve
Objects.Geometry.Extrusion
Objects.Geometry.Mesh
Objects.Geometry.Surface
Objects.Geometry.BrepFace
Objects.Geometry.BrepEdge
Objects.Geometry.BrepLoop
Objects.Geometry.BrepTrim
Objects.Geometry.Brep
Objects.Other.Material
Objects.Other.Revit.RevitMaterial
Objects.Other.RenderMaterial
Objects.Other.MaterialQuantity
Objects.Other.DisplayStyle
Objects.Other.Text
Objects.Other.Transform
Objects.Other.BlockDefinition
Objects.Other.Instance
Objects.Other.BlockInstance
Objects.Other.RevitInstance
Objects.BuiltElements.Revit.Parameter
Speckle.Core.Models.Collection
Objects.Primitive.Interval
Objects.Primitive.Interval2d
Objects.GIS.CRS
Objects.GIS.RasterLayer
Objects.GIS.VectorLayer
Objects.GIS.NonGeometryElement
Objects.GIS.PointElement
Objects.GIS.LineElement
Objects.GIS.PolygonElement
Objects.GIS.PolygonGeometry
Objects.GIS.RasterElement
Objects.GIS.GisTopography
Objects.Structural.Analysis.ModelUnits
Objects.Structural.Analysis.ModelSettings
Objects.Structural.Analysis.ModelInfo
Objects.Structural.Analysis.Model
Objects.Structural.Geometry.Axis
Objects.Structural.Geometry.Restraint
Objects.Structural.Geometry.Node
Objects.Structural.Geometry.Element1D
Objects.Structural.Geometry.Element2D
Objects.Structural.Geometry.Element3D
Objects.Structural.Loading.LoadCase
Objects.Structural.Loading.LoadCase
Objects.Structural.Loading.LoadBeam
Objects.Structural.Loading.LoadCombination
Objects.Structural.Loading.LoadFace
Objects.Structural.Loading.LoadGravity
Objects.Structural.Loading.LoadNode
Objects.Structural.Materials.StructuralMaterial
Objects.Structural.Materials.Steel
Objects.Structural.Materials.Timber
Objects.Structural.Properties.Profiles.SectionProfile
Objects.Structural.Properties.Property1D
Objects.Structural.Properties.Property2D
Objects.Structural.Properties.Property3D
Objects.Structural.Properties.PropertyDamper
Objects.Structural.Properties.PropertyMass
Objects.Structural.Properties.PropertySpring
Objects.Structural.Results.Result
Objects.Structural.Results.ResultSet1D
Objects.Structural.Results.Result1D
Objects.Structural.Results.ResultSet2D
Objects.Structural.Results.Result2D
Objects.Structural.Results.ResultSet3D
Objects.Structural.Results.Result3D
Objects.Structural.Results.ResultGlobal
Objects.Structural.Results.ResultSetNode
Objects.Structural.Results.ResultNode""".split(
    "\n"
)
print(CLASSES_PYTHON)
