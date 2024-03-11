import os
import sys
from os import listdir
from os.path import isfile, join

import glob
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


###################################### get classes from files (assuming 1 class per file)
def get_trimmed_strings_per_line_from_files(
    trim_0_start, trim_0_end, trim_1_start, trim_1_end, files
):
    all_classes = []
    for file in files:
        with open(file) as f:
            start_str = None
            end_str = None

            for line in f.readlines():
                if not line.startswith("//") and not line.startswith("      //"):

                    if trim_0_start in line and trim_0_end in line:
                        start_str = line.split(trim_0_start)[1].split(trim_0_end)[0]
                    elif trim_1_start in line and trim_1_end in line:
                        end_str = line.split(trim_1_start)[1].split(trim_1_end)[0]

                    if start_str and end_str:
                        all_classes.append(start_str + "." + end_str)
                        break

    return all_classes


result_all_classes.extend(
    get_trimmed_strings_per_line_from_files(
        "namespace ", ";", "public class ", " :", files
    )
)

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


def get_abbreviations_functions_from_file(file, trim_start, start_line_condition=None):
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
                if trim_start in line and (
                    start_line_condition is None or start_line_condition(line) is True
                ):  # start writing string
                    for key_replace, val_replace in replace_from_to.items():
                        if key_replace in line:
                            line = line.replace(key_replace, val_replace)
                    string += line
                elif len(string) > 0 and brackets_open == 0 and brackets_started > 0:
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
    return string


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
                        string = get_abbreviations_functions_from_file(
                            file, " " + func_name, start_line_condition
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

    # print(files)
