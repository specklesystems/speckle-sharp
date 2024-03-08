import os
import sys
from os import listdir
from os.path import isfile, join

import glob
import pathlib

files = []
all_classes = []
path_objects = f"{pathlib.Path(__file__).parent.resolve()}\\Objects\\Objects"
path_converters = f"{pathlib.Path(__file__).parent.resolve()}\\Objects\\Converters"


def get_files_in_path(mypath, existing_files, condition=None):
    """Recursively get all files in folder and subfolders."""
    all_folders = get_folders_in_path(mypath, [])
    for folder in all_folders:
        if condition is None or condition(folder) is True:
            local_files = [f for f in listdir(folder) if isfile(join(folder, f))]
            existing_files.extend(
                [folder + "\\" + f for f in local_files if f.endswith(".cs")]
            )
    # for file in existing_files:
    #     print(file)
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


files = get_files_in_path(path_objects, [])

# get classes from files (assuming 1 class per file)
for file in files:
    with open(file) as f:
        x = None
        class_name = None
        full_class_name = None

        for line in f.readlines():

            if not line.startswith("//") and "namespace " in line:
                namespace = line.split("namespace ")[1].split(";")[0]
            elif not line.startswith("//") and "public class " in line:
                class_name = line.split("public class ")[1].split(" :")[0]

            if class_name and namespace:
                full_class_name = namespace + "." + class_name
                break
    if full_class_name:
        all_classes.append(full_class_name)

for c in all_classes:
    print(c)


# get files for conversions
def condition(folder_name):
    if folder_name.endswith("Shared"):
        return True
    else:
        return False


files_conversions = get_files_in_path(path_converters, [], condition)
print(files_conversions)


def get_conversion_to_native_bool():
    keyword = "public bool CanConvertToNative("
