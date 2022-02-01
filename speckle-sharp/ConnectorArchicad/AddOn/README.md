# Archicad Add-On CMake Template

This repository contains a CMake based Add-On for Archicad. You can use it to generate native Visual Studio or XCode project or to develop an Add-On directly in Visual Studio Code without using any other environments.

## Prerequisites

- [CMake](https://cmake.org) (3.16 minimum version is needed). on windows its the most painless to install cmake with choco -> `choco install cmake --installargs 'ADD_CMAKE_TO_PATH=User'`
- [Python](https://www.python.org) for resource compilation (version 2.7+ or 3.8+).

On Windows, run the following in an elevated Powershell to install `cmake`:
    
    choco install cmake --installargs 'ADD_CMAKE_TO_PATH=User'

## Build

- [Download the Archicad Add-On Development Kit from here](http://archicadapi.graphisoft.com).
- Generate the IDE project with CMake, and set the following variables:
  - `AC_API_DEVKIT_DIR`: The root folder of the installed Archicad Add-On Development Kit. You can also set an environment variable with the same name so you don't have to provide this value during project generation.
  - `AC_ADDON_NAME`: (optional) The name of the project file and the result binary Add-On file (default is "ExampleAddOn").
  - `AC_ADDON_LANGUAGE`: (optional) The language code of the Add-On (default is "INT").
  - `AC_MDID_DEV`: (optional) Your Developer ID. Ommitting this will result in a 1 value.
  - `AC_MDID_LOC`: (optional) Add-On Local ID. Ommitting this will result in a 1 value. 
- Without setting the last two variables AC will only load the addon in demo mode. To release your Add-On you have to provide valid MDIDs

Note: when testing on Windows, none of these env variables needed to be set.

### Visual Studio (Windows)

Run these commands from the command line.

```
mkdir Build
cd Build
cmake -G "Visual Studio 17 2022" -A "x64" -DAC_API_DEVKIT_DIR="C:\API Development Kit 25.3006" ..
cd ..
```

Alternatively, you can run the following one-liner:

    cmake -B ./Build/ -DAC_API_DEVKIT_DIR="C:\Program Files\GRAPHISOFT\API Development Kit 25.3002" 

### XCode (MacOS)

Run these commands from the command line.

```
mkdir Build
cd Build
cmake -G "Xcode" -DAC_API_DEVKIT_DIR=/Applications/GRAPHISOFT\ ARCHICAD\ API\ DevKit\ 25.3006 ..
cd ..
```

### Visual Studio Code (Platform Independent)

- Install the "CMake Tools" extension for Visual Studio Code.
- Set the "AC_API_DEVKIT_DIR" environment variable to the installed Development Kit folder.
- Open the root folder in Visual Studio Code, configure and build the solution.

## Compile and run the full addon

The connector is built up from two parts. One is the C++ AC addon project, and the other is a C# application that runs the Speckle DUI2 and the core logic.
Both of these project have to be built before the addon can be loaded into AC.
The first project is created by the previous cmake command.
This is because the cmake script generates the platform dependent project configuration based on the installed AC SDK.
The build result of the C++ project contains the AC addon files, like the `Speckle Connector.apx`.

The AC C++ addon expects the C# files to be present in a relative `./ConnectorArchicad` folder.
Build the ConnectorArchicad C# project and copy the results next to the C++ artifacts in the `./ConnectorArchicad` folder.

If the files are copied into the `Addons` folder in the AC installation folder, on startup, AC will try to load the addon.
Or the addon can be manually loaded from the AddonManager found in the Options menu in AC.

The connector can be started from the File/Interoperability/Speckle menu entry.

## Use in Archicad

To use the Add-On in Archicad, you have to add your compiled .apx file in Add-On Manager. The example Add-On registers a new command into the Interoperability menu. Please note that the example Add-On works only in the demo version of Archicad. 

You can start Archicad in demo mode with the following command line commands:
Windows:

    cd "C:\Program Files\GRAPHISOFT\ARCHICAD 25"
    ./ARCHICAD.exe -DEMO

MacOS: 

    ARCHICAD\ 25.app/Contents/MacOS/ARCHICAD -demo

## Archicad Compatibility

This template is tested with all Archicad versions starting from Archicad 25.
