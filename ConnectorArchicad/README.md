# speckle-archicad
Private collaboration repository between the Graphisoft and Speckle teams for Archicad Speckle connector development


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

### Adding the Connector to ArchiCAD

You will now need to manually move the build files for both of the projects you've just built into the ArchiCAD `Add-Ons` directory. On Windows, you will find this directory at:

    C:\Program Files\GRAPHISOFT\ARCHICAD 25\Add-Ons

First, head to the ArchiCAD `Add-Ons` directory specified above and create a new folder called `ConnectorArchicad`

![image](https://user-images.githubusercontent.com/7717434/149930481-deedabbf-bf99-49ca-9758-88c4c092c9df.png)

Now, we'll move our `Speckle Connector` build files. Open your build files at `./AddOn/Build/Debug` and copy the whole contents of the folder into the `ConnectorArchicad` folder you just created at `C:\Program Files\GRAPHISOFT\ARCHICAD 25\Add-Ons\ConnectorArchicad`. You'll also create another new folder called `ConnectorArchicad` inside this folder.

![image](https://user-images.githubusercontent.com/7717434/149930098-4fc73382-955e-42e9-bba9-a048630f49ec.png)

Next, we'll add out `ConnectorArchicad` build files. Open up your build files at `./speckle-sharp/ConnectorArchicad/bin/Debug/net6.0`. Copy the entire contents of this folder to the second `ConnectorArchicad` folder you just created at `C:\Program Files\GRAPHISOFT\ARCHICAD 25\Add-Ons\ConnectorArchicad\ConnectorArchicad`

![image](https://user-images.githubusercontent.com/7717434/149930127-c7ec8de0-89be-4dfc-9943-940b55913a0d.png)

### Starting ArchiCAD in Demo Mode

As the connector is currently unsigned, you can only run it in demo mode.

You can start ArchiCAD in demo mode with the following commands:

**Windows:**

    cd "C:\Program Files\GRAPHISOFT\ARCHICAD 25"
    ./ARCHICAD.exe -DEMO

**MacOS:**

    ARCHICAD\ 25.app/Contents/MacOS/ARCHICAD -demo

### Launching the Connector

You should now be able to launch the connector from the `Interoperability` menu:

![image](https://user-images.githubusercontent.com/7717434/149931619-2944a730-c9ae-4092-90c3-fd62c2dd37da.png)
