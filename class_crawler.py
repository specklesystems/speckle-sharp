from os import listdir
from os.path import isfile, join
from typing import List

import plotly
from class_data import (
    CLASSES_PYTHON,
    get_detailed_classes_from_files,
    get_function_from_file,
    get_speckle_class_full_name,
    get_trimmed_strings_per_line_from_files,
)
from plot_builder import plot_flowchart

import pathlib

APPS = [
    "rhino",
    "gh",
    "revit",
    "tekla",
    "autocad",
    "civil",
    "bentley",
    "csi",
    "dynamo",
    "navisworks",
]

files = []
result_all_classes = []
result_all_apps_convertable = {}
[
    result_all_apps_convertable.update({app: {"to_native": {}, "to_speckle": {"TODO"}}})
    for app in APPS
]

path_objects = f"{pathlib.Path(__file__).parent.resolve()}\\Objects\\Objects"
path_converters = f"{pathlib.Path(__file__).parent.resolve()}\\Objects\\Converters"


def get_files_in_path(
    mypath, existing_files, folder_condition=None, file_condition=None, app_name=""
):
    """Recursively get all files in folder and subfolders."""
    all_folders = get_folders_in_path(mypath, [])
    for folder in all_folders:
        if folder_condition is None or folder_condition(folder, app_name) is True:

            for f in listdir(folder):
                if isfile(join(folder, f)) and (
                    file_condition is None or file_condition(f) is True
                ):
                    existing_files.append(folder + "\\" + f)
    return existing_files


def get_folders_in_path(mypath, existing_folders):
    """Recursively get all subfolders."""
    all_folders = [
        mypath + "\\" + f for f in listdir(mypath) if not isfile(join(mypath, f))
    ]
    for folder in all_folders:
        if not folder.endswith("bin") and not folder.endswith("obj"):
            existing_folders.append(folder)
            get_folders_in_path(folder, existing_folders)
    return existing_folders


def file_condition_classes(file_name):
    """Condition to select files for extractig class names."""
    if file_name.endswith(".cs"):
        return True
    else:
        return False


files = get_files_in_path(path_objects, [], file_condition=file_condition_classes)
all_cl, all_f = get_trimmed_strings_per_line_from_files(
    "namespace ", ";", ["public class ", "public abstract class "], " :", files
)
for cl in all_cl:
    print(cl)

########## all correct till now
result_all_classes = all_cl


# get very detailed classes composition
all_classes_dict = get_detailed_classes_from_files(all_cl, all_f, result_all_classes)


def add_subclasses_from_parent_class(class_edit, all_classes_dict, current_class=None):
    """Recursively add subclasses up class inheritance line."""
    if current_class:
        parent = all_classes_dict[current_class]["parent"]
    else:
        parent = all_classes_dict[class_edit]["parent"]
    if parent == "Base" or parent.endswith(".Collection"):
        return

    existing_subclasses = all_classes_dict[class_edit][
        "subclasses"
    ]  # just for debugging info
    all_classes_dict[class_edit]["all_parents"].append(parent)
    subclasses_to_add: dict[str, list] = all_classes_dict[parent]["subclasses"]
    for key, val in subclasses_to_add.items():
        all_classes_dict[class_edit]["subclasses"].update({key: val})
    add_subclasses_from_parent_class(class_edit, all_classes_dict, parent)
    return


for class_edit, _ in all_classes_dict.items():
    add_subclasses_from_parent_class(class_edit, all_classes_dict)
for class_edit, _ in all_classes_dict.items():
    add_subclasses_from_parent_class(class_edit, all_classes_dict)

result_all_classes.extend(CLASSES_PYTHON)
result_all_classes = list(set(result_all_classes))
result_all_classes.sort()

for c in result_all_classes:
    print(c)


################################################## get files for conversions
def folder_condition_converter(folder_name, string2):
    if folder_name.endswith("Shared"):
        return True
    else:
        return False


def file_condition_converter(file_name):
    if (
        file_name.endswith(".cs")
        and file_name.startswith("Converter")
        and len(file_name.split(".")) == 2
        and "Utils" not in file_name
    ):
        return True
    else:
        return False


files_conversions = get_files_in_path(
    path_converters, [], folder_condition_converter, file_condition_converter
)


################################################## get CanConvertToNative function
def flip_convertable(convertable: float):
    if convertable > 0:
        convertable = 0
    else:
        convertable = 1
    return convertable


# check again for IF statements before
def get_condition(convertable, string, line, app):
    separated_strings = string.split(line)
    if len(separated_strings) <= 1:
        latest_condition = separated_strings[-1]
    else:
        latest_condition = line.join(separated_strings[1:-1])
    if "#if " in latest_condition:
        latest_condition = "#if " + latest_condition.split("#if ")[-1]
        if "#endif" not in latest_condition.split("#if ")[-1]:
            condition_statement = latest_condition.split("#if ")[-1].split("\n")[0]
            condition_statement = condition_statement.replace("GRASSHOPPER", "GH")
            partial_host_app = False
            suffix = condition_statement.lower().split(app)[-1][:1]
            if (
                len(condition_statement.lower().split(app)) == 1
                or condition_statement.lower() == app
            ):
                suffix = " "
            # print(suffix)
            if app in condition_statement.lower() and suffix in "0123456789":
                partial_host_app = True

            # get original condition for the app
            if partial_host_app is True:  # if specific condition for current app
                if convertable != 0:
                    convertable = 0.5
            elif f"!{app}" in condition_statement.lower():  # if NOT current app
                convertable = flip_convertable(convertable)
            elif app in condition_statement.lower():  # keep the app condition
                pass
            elif (
                app not in condition_statement.lower() and "!" in condition_statement
            ):  # if NOT other_app
                pass
            elif (
                app not in condition_statement.lower()
                and "!" not in condition_statement
            ):  # if other app
                convertable = flip_convertable(convertable)

            # see if ELSE was used to reverse condition
            if "#else" in latest_condition.split("#if ")[-1]:  # opposite condition
                convertable = flip_convertable(convertable)
                condition_statement = "!" + condition_statement
            # print(condition_statement)
    return convertable


def update_apps_convertables(
    result_all_apps_convertable,
    cases,
    file,
    line,
    replace_from_to,
    string,
    keywords,
    condition_function,
):
    """Get all convertable to native classes per app."""
    apps_in_file = [app for app in APPS if app in file.split("\\")[-1].lower()]

    if "case " in line:
        if "when " not in line:
            cases.append(
                [
                    line.split("case ")[1].split(":")[0].split(" ")[0],
                    1,
                ]
            )
        else:
            cases.append(
                [
                    line.split("case ")[1].split(":")[0].split(" ")[0],
                    0.5,
                ]
            )

    result_all_apps_convertable, cases, string = condition_function(
        string,
        line,
        replace_from_to,
        cases,
        apps_in_file,
        keywords,
        result_all_apps_convertable,
    )

    return result_all_apps_convertable, cases, string


def get_convert_to_native_classes_per_app(
    trim_start,
    trim_end,
    files,
    result_all_apps_convertable,
    keywords,
    condition_function,
):
    strings = []
    for file in files:
        print(file)
        replace_from_to = {}

        with open(file, encoding="utf-8") as f:
            string = ""
            cases = []

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
                    if trim_start in line:  # start writing string
                        string += line
                    elif len(string) > 0 and trim_end in line:
                        string += line.split(trim_end)[0]
                        break
                    elif len(string) > 0:  # keep adding lines
                        string += line
                        result_all_apps_convertable, cases, string = (
                            update_apps_convertables(
                                result_all_apps_convertable,
                                cases,
                                file,
                                line,
                                replace_from_to,
                                string,
                                keywords,
                                condition_function,
                            )
                        )
        strings.append(string)

    return result_all_apps_convertable


def condition_can_convert_to_native(
    string,
    line,
    replace_from_to,
    cases,
    apps_in_file,
    keywords,
    result_all_apps_convertable,
):
    if "return " in line and len(cases) > 0:

        for key, val in replace_from_to.items():
            if key in line:
                line = line.replace(key, val)
            # rename classes to full names
            for i, (class_to_convert, condition) in enumerate(cases):
                if key in class_to_convert:
                    new_class_to_convert = class_to_convert.replace(key, val)
                    cases[i] = [new_class_to_convert, condition]

        for class_to_convert, condition in cases:
            if line.split("return ")[1].split(";")[0] == "true":
                convertable = 1
                if condition != 1:
                    convertable = 0.5
            else:
                convertable = 0
            # add a convertable case to every app in file
            for app in apps_in_file:
                final_convertable = get_condition(convertable, string, line, app)
                try:
                    result_all_apps_convertable[app][keywords[0]][
                        class_to_convert
                    ].update({keywords[1]: final_convertable, "function": ""})
                except KeyError:
                    result_all_apps_convertable[app][keywords[0]].update(
                        {
                            class_to_convert: {
                                keywords[1]: final_convertable,
                                "function": "",
                            }
                        }
                    )
        cases = []
    elif " _ => " in line:

        for key, val in replace_from_to.items():
            if key in line:
                line = line.replace(key, val)
            # rename classes to full names
            for i, (class_to_convert, condition) in enumerate(cases):
                if key in class_to_convert:
                    new_class_to_convert = class_to_convert.replace(key, val)
                    cases[i] = [new_class_to_convert, condition]

        if line.split(" _ => ")[1].split(",")[0] == "true":
            convertable = 1
            if "when" in line:
                convertable = 0.5
        else:
            convertable = 0
        # add a convertable case to every app in file
        class_to_convert = line.split(" _ => ")[0].replace(" ", "")
        for app in apps_in_file:
            final_convertable = get_condition(convertable, string, line, app)
            try:
                result_all_apps_convertable[app][keywords[0]][class_to_convert].update(
                    {keywords[1]: final_convertable, "function": ""}
                )
            except KeyError:
                result_all_apps_convertable[app][keywords[0]].update(
                    {class_to_convert: {keywords[1]: final_convertable, "function": ""}}
                )

    return result_all_apps_convertable, cases, string


def condition_function_convert_to_native(
    string,
    line,
    replace_from_to,
    cases,
    apps_in_file,
    keywords,
    result_all_apps_convertable,
):
    if len(cases) > 0 and "return " in line:
        separator = "return "
    elif len(cases) > 0 and " = " in line:
        separator = " = "
    elif len(cases) > 0 and "(o" in line and line.startswith("        "):
        separator = "        "
    else:
        return result_all_apps_convertable, cases, string

    if len(string.split(line)) >= 1:
        search_string = string.split(line)[-2]
    else:
        search_string = string.split(line)[-1]
    search_string = search_string.split(";")[-1]
    if "case " in search_string[-200:] and "Try" not in line:
        class_to_convert, condition = cases[-1]

        # rename classes to full names
        for key, val in replace_from_to.items():
            if key in class_to_convert:
                class_to_convert = class_to_convert.replace(key, val)

        func = line.split(separator)[1].split("(")[0]
        if "? " in func:
            func = func.split("? ")[1]
        if len(func) > 0 and "To" in func:
            # add a convertable case to every app in file
            for app in apps_in_file:
                # if app == "autocad":
                #    pass
                try:
                    if (
                        result_all_apps_convertable[app][keywords[0]][class_to_convert][
                            "can_convert"
                        ]
                        > 0
                    ):
                        result_all_apps_convertable[app][keywords[0]][
                            class_to_convert
                        ].update({keywords[1]: func})
                    else:
                        result_all_apps_convertable[app][keywords[0]][
                            class_to_convert
                        ].update({keywords[1]: ""})
                except KeyError as e:

                    result_all_apps_convertable[app][keywords[0]].update(
                        {class_to_convert: {keywords[1]: func}}
                    )

                    result_all_apps_convertable[app][keywords[0]][
                        class_to_convert
                    ].update({"can_convert": 1})

            cases = []

    return result_all_apps_convertable, cases, string


trim_start = "public bool CanConvertToNative("
trim_end = "public bool "
keywords = ["to_native", "can_convert"]
result_all_apps_convertable = get_convert_to_native_classes_per_app(
    trim_start,
    trim_end,
    files_conversions,
    result_all_apps_convertable,
    keywords,
    condition_can_convert_to_native,
)
r"""
for app in APPS:
    print("\n")
    print(app)
    print(result_all_apps_convertable[app])
"""

trim_start = "public object ConvertToNative("
trim_end = "public object ConvertToNativeDisplayable"
keywords = ["to_native", "function"]
result_all_apps_convertable = get_convert_to_native_classes_per_app(
    trim_start,
    trim_end,
    files_conversions,
    result_all_apps_convertable,
    keywords,
    condition_function_convert_to_native,
)


def folder_condition_apps(folder_name, name_to_match):
    """Condition to select files for extractig class names."""
    if name_to_match in folder_name.lower():
        return True
    else:
        return False


for app in APPS:
    print("\n")
    print(app)
    # print(result_all_apps_convertable[app])

    files = get_files_in_path(
        path_converters,
        [],
        folder_condition=folder_condition_apps,
        file_condition=file_condition_classes,
        app_name=app,
    )
    for class_name, val in result_all_apps_convertable[app]["to_native"].items():
        all_result_types = []
        func_names = [val["function"]]

        if val["can_convert"] > 0 and val["function"] != "":

            def start_line_condition(line):
                if "(o" not in line and "public " in line:
                    return True
                else:
                    return False

            to_repeat_search = True
            while to_repeat_search is True:
                to_repeat_search = False

                all_result_types = []
                for func_name in func_names:
                    for file in files:
                        string = get_function_from_file(
                            file, [" " + func_name], start_line_condition
                        )
                        if len(string) > 0:
                            res_type = string.split("public ")[1].split(
                                " " + func_name
                            )[0]

                            if res_type == "void":
                                # search inside
                                found_new = False
                                for line in string.split("\n"):
                                    if " = new();" in line:
                                        res_type = line.split(" = new();")[0].split(
                                            " "
                                        )[-1]
                                        # print(file)
                                        found_new = True
                                        all_result_types.append(res_type)
                                        break
                                    if found_new is False:
                                        # print(string)
                                        pass
                                # print(res_type)
                                break
                            elif res_type == "ApplicationObject":
                                # repeat search
                                # print(res_type)
                                func_names = []
                                for line in string.split("\n"):
                                    if app == "revit":
                                        if (
                                            " = " in line
                                            and ".Create" in line
                                            and ": " not in line
                                        ):
                                            # keep overwriting till the last available Create
                                            res_type = line.split(" = ")[1].split(
                                                ".Create"
                                            )[0]
                                            all_result_types.append(res_type)
                                            # func_names = []
                                    if res_type == "ApplicationObject":
                                        if " = " in line and "ToNative(" in line:
                                            func_names.append(
                                                line.split(" = ")[1].split("ToNative(")[
                                                    0
                                                ]
                                                + "ToNative"
                                            )
                                        elif "return " in line and "ToNative(" in line:
                                            func_names.append(
                                                line.split("return ")[1].split(
                                                    "ToNative("
                                                )[0]
                                                + "ToNative"
                                            )
                                if len(func_names) > 0:
                                    # print(string)
                                    # print(func_names)
                                    to_repeat_search = True
                                break
                            else:
                                # accept the result
                                # print(file)
                                all_result_types.append(res_type)
                                # print(res_type)
                                break
                all_result_types = list(set(all_result_types))

        result_all_apps_convertable[app]["to_native"][class_name].update(
            {"to_native_classes": all_result_types}
        )
    print(result_all_apps_convertable[app]["to_native"])


result_all_apps_convertable_full_names = {}
for app, val in result_all_apps_convertable.items():
    result_all_apps_convertable_full_names.update(
        {app: {"to_native": {}, "to_speckle": "TODO"}}
    )

    for cl, val2 in val["to_native"].items():
        full_cl_name = get_speckle_class_full_name(cl, result_all_classes)
        if full_cl_name is not None:
            for new_cl in full_cl_name:
                result_all_apps_convertable_full_names[app]["to_native"].update(
                    {new_cl: val2.copy()}
                )


for app_receive in APPS:
    print(app_receive)
    class_send = []
    class_sp = []
    condition_1 = []
    class_receive = []
    condition_2 = []
    classes_branching_receive = {}
    for key, val in result_all_apps_convertable_full_names[app_receive][
        "to_native"
    ].items():

        # key_full = get_speckle_class_full_name(key, result_all_classes)
        # if key_full is not None:
        #    for k_item in key_full:
        class_receive.extend(val["to_native_classes"])
        class_sp.extend([key for _ in range(len(val["to_native_classes"]))])

        # if not convertible:
        if len(val["to_native_classes"]) == 0:
            # class_sp.append(k)
            # class_receive.append("NA")
            parent_classes: list = all_classes_dict[key]["all_parents"]
            subclasses: dict = all_classes_dict[key]["subclasses"]
            tree: str = ""
            for search_class in parent_classes:
                if (
                    search_class
                    in result_all_apps_convertable_full_names[app_receive]["to_native"]
                ):
                    val_item = result_all_apps_convertable_full_names[app_receive][
                        "to_native"
                    ][search_class]
                    resulted_native_classes = val_item["to_native_classes"]
                    if len(resulted_native_classes) > 0:
                        tree += "->" + search_class
                        for native_cl in resulted_native_classes:
                            classes_branching_receive.update({key: {tree: native_cl}})
                        break

            if len(classes_branching_receive) == 0:
                for search_classes in [v for _, v in subclasses.items()]:
                    for search_class in search_classes:
                        if (
                            search_class
                            in result_all_apps_convertable_full_names[app_receive][
                                "to_native"
                            ]
                        ):
                            val_item = result_all_apps_convertable_full_names[
                                app_receive
                            ]["to_native"][search_class]
                            resulted_native_classes = val_item["to_native_classes"]
                            if len(resulted_native_classes) > 0:
                                tree += "->" + search_class
                                for native_cl in resulted_native_classes:
                                    classes_branching_receive.update(
                                        {key: {tree: native_cl}}
                                    )
                                break

    fig = plot_flowchart(
        class_send,
        class_sp,
        condition_1,
        class_receive,
        condition_2,
        classes_branching_receive,
        title=app_receive,
    )
    if fig is not None:
        plotly.offline.plot(fig, filename=f"flowchart_{app_receive}.html")
