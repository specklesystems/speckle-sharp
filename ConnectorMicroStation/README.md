# Connector Microstation


## Introduction

This repo holds Speckle's MicroStation, OpenRail, OpenRoads and OpenBuildings Connectors as built and managed by Arup, and it is currently released as ⚠ **ALPHA** ⚠, please use at your own risk!

## Documentation

To be added!

## Developing & Debugging

### Requirements

- A Speckle Server running
- Speckle Manager

#### Supported versions

- MicroStation CONNECT Edition Update 14
- OpenRoads Designer CONNECT Edition 2020 R3
- OpenRail Designer CONNECT Edition 2020 R3
- OpenBuildings Designer CONNECT Edition Update 6

## Getting Started

The following instructions are for getting started debugging and contributing to this connector.

### Debugging

After setting up dependencies, server and accounts you're good to go - almost there! After the first build, make sure to:

- Open the corresponding connector config folder (e.g., `C:\ProgramData\Bentley\MicroStation CONNECT Edition\Configuration\Organization`)
- Copy the `SpeckleMicroStation2.cfg` located in the Connector folder to the Bentley confic folder

You can now start a Visual Studio's debug session for MicroStation, OpenRoad, OpenRail or OpenBuildings. This will launch the corresponding application for you, and the Speckle toolbar should be loaded!

### Features

Geometry conversions supported:

- Points, Lines, Arcs, Circles, Ellipses, Polylines, Polycurves, Splines and Meshes (send and receive)
- Alignments (send)

## Contributing


## Community


## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via email or on the Speckle Community Forum.