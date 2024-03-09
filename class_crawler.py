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
    result_all_apps_convertable.update({app: {"to_native": {}, "to_speckle": {}}})
    for app in APPS
]

path_objects = f"{pathlib.Path(__file__).parent.resolve()}\\Objects\\Objects"
path_converters = f"{pathlib.Path(__file__).parent.resolve()}\\Objects\\Converters"


def get_files_in_path(
    mypath, existing_files, folder_condition=None, file_condition=None
):
    """Recursively get all files in folder and subfolders."""
    all_folders = get_folders_in_path(mypath, [])
    for folder in all_folders:
        if folder_condition is None or folder_condition(folder) is True:

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
def folder_condition_converter(folder_name):
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

for file in files_conversions:
    print(file)


################################################## get CanConvertToNative function
r"""
def get_trimmed_strings_multiline_from_files(trim_start, trim_end, files):
    converters = []
    for file in files:
        print(file)
        replace_from_to = {}
        classes_convertable = {}

        with open(file, encoding="utf-8") as f:
            string = ""
            cases = []

            for line in f.readlines():
                if not line.startswith("//") and not line.startswith("      //"):
                    # get replacement pairs
                    if "using " in line and " = " in line:
                        replace_from_to.update(
                            {
                                line.split("using ")[1]
                                .split(" = ")[0]: line.split(" = ")[1]
                                .split(";")[0]
                            }
                        )

                    # get actual string
                    if trim_start in line:  # start writing string
                        string += line
                    elif len(string) > 0 and trim_end in line:
                        string += line.split(trim_end)[0]
                        break
                    elif len(string) > 0:  # keep adding lines
                        for key, val in replace_from_to.items():
                            if key in line:
                                line = line.replace(key, val)
                        if "case " in line:
                            if "when " not in line:
                                cases.append(
                                    [
                                        line.split("case ")[1]
                                        .split(":")[0]
                                        .split(" ")[0],
                                        1,
                                    ]
                                )
                            else:
                                cases.append(
                                    [
                                        line.split("case ")[1]
                                        .split(":")[0]
                                        .split(" ")[0],
                                        0.5,
                                    ]
                                )
                        elif "return " in line and len(cases) > 0:
                            for case, condition in cases:
                                if line.split("return ")[1].split(";")[0] == "true":
                                    convertable = 1
                                    if condition != 1:
                                        convertable = 0.5
                                else:
                                    convertable = 0
                                classes_convertable.update({case: convertable})
                            cases = []
                        elif " _ => " in line:
                            if line.split(" _ => ")[1].split(",")[0] == "true":
                                convertable = 1
                                if "when" in line:
                                    convertable = 0.5
                            else:
                                convertable = 0
                            classes_convertable.update(
                                {line.split(" _ => ")[0].replace(" ", ""): convertable}
                            )

                        string += line

        # print(replace_from_to)
        #print(classes_convertable)
        #print(string)
        converters.append(string)

    return converters
"""


def update_apps_convertables(
    result_all_apps_convertable, cases, file, line, replace_from_to, string
):
    """Get all convertable to native classes per app."""
    apps_in_file = [app for app in APPS if app in file.split("\\")[-1].lower()]
    for key, val in replace_from_to.items():
        if key in line:
            line = line.replace(key, val)
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
    elif "return " in line and len(cases) > 0:
        for class_to_convert, condition in cases:
            if line.split("return ")[1].split(";")[0] == "true":
                convertable = 1
                if condition != 1:
                    convertable = 0.5
            else:
                convertable = 0
            print(class_to_convert)
            print(convertable)
            # add a convertable case to every app in file
            for app in apps_in_file:
                result_all_apps_convertable[app]["to_native"].update(
                    {class_to_convert: {"can_convert": convertable}}
                )
        cases = []
    elif " _ => " in line:
        if line.split(" _ => ")[1].split(",")[0] == "true":
            convertable = 1
            if "when" in line:
                convertable = 0.5
        else:
            convertable = 0
        # add a convertable case to every app in file
        class_to_convert = line.split(" _ => ")[0].replace(" ", "")
        for app in apps_in_file:
            result_all_apps_convertable[app]["to_native"].update(
                {class_to_convert: {"can_convert": convertable}}
            )

    string += line
    return result_all_apps_convertable, cases, string


def get_convert_to_native_classes_per_app(
    trim_start, trim_end, files, result_all_apps_convertable
):
    converters = []
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
                                line.split("using ")[1]
                                .split(" = ")[0]: line.split(" = ")[1]
                                .split(";")[0]
                            }
                        )
                    # get actual string
                    if trim_start in line:  # start writing string
                        string += line
                    elif len(string) > 0 and trim_end in line:
                        string += line.split(trim_end)[0]
                        break
                    elif len(string) > 0:  # keep adding lines
                        result_all_apps_convertable, cases, string = (
                            update_apps_convertables(
                                result_all_apps_convertable,
                                cases,
                                file,
                                line,
                                replace_from_to,
                                string,
                            )
                        )

        print(result_all_apps_convertable)
        print(string)
        converters.append(string)

    return converters


# def get_conversion_to_native_bool():
trim_start = "public bool CanConvertToNative("
trim_end = "public bool "
converters = get_convert_to_native_classes_per_app(
    trim_start, trim_end, files_conversions, result_all_apps_convertable
)
