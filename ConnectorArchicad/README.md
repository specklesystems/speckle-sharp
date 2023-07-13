# Speckle Connector for Archicad
Thanks to Graphisoft for their help in setting up this Connector.


## Dev

### Building the Connector

There are two projects to build:

1. The `Speckle Connector` C++ project located in the `AddOns` directory
2. The `ConnectorArchicad` C# project located in the `speckle-sharp/ConnectorArchicad` directory

Note: More detailed instructions for building the AddOn with CMake can be found in the `AddOn/README.md`.

Install `cmake` in an elevated powershell with chocolatey using the following command:
    
    choco install cmake --installargs 'ADD_CMAKE_TO_PATH=User'

From the `./AddOn` directory, run the following command to build using `cmake`:

    cmake -B ./Build/ -DAC_API_DEVKIT_DIR="C:\Program Files\GRAPHISOFT\API Development Kit 25.3002" 

You will now have a `AddOn/Build` directory containing a `Speckle Connector.sln` file.

Open the Speckle Connector solution file `Addon/Build/Speckle Connector.sln` in your editor of choice and build the entire solution.

Next, open `speckle-sharp/ConnectorArchicad/ConnectorArchicad.sln` and build that entire solution.

### Starting ArchiCAD in Demo Mode

As the connector is currently unsigned, you can only run it in demo mode.

You can start ArchiCAD in demo mode with the following commands:

**Windows:**

    cd "C:\Program Files\GRAPHISOFT\ARCHICAD 25"
    ./ARCHICAD.exe -DEMO

**MacOS:**

    ARCHICAD\ 25.app/Contents/MacOS/ARCHICAD -demo

### Adding the Connector to ArchiCAD

You will now need to manually add the add-on from Add-On Manager (Option menu) in Archicad. Under "Edit List of Available Add-Ons" press the "Add.." button and browse for the .apx file at `./AddOn/Build/Debug`.

### Launching the Connector

You should now be able to launch the connector from the `Interoperability` menu:

![image](https://user-images.githubusercontent.com/7717434/149931619-2944a730-c9ae-4092-90c3-fd62c2dd37da.png)
