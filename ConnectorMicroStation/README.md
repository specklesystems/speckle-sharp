# Connector Microstation

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This is the ⚠ALPHA⚠ version of the Speckle 2.0 Bentley MicroStation, OpenBuildings, OpenRail, and OpenRoads Connectors, as built and managed by Arup. 

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

#### Server

In order to test Speckle in all its glory, you'll need a server running. You can run a local one by simply following the instructions in the [Server Repo](https://github.com/specklesystems/Server).

#### Accounts

The connector itself doesn't have features to manage your Speckle account - this functionality has been delegated to the Speckle Manager desktop app. After installing the [alpha version](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe), use the manager to add or create an account on the Server.

### Debugging

After setting up dependencies, server and accounts you're good to go - almost there! After the first build, make sure to:

- Open the corresponding connector config folder (e.g., `C:\ProgramData\Bentley\MicroStation CONNECT Edition\Configuration\Organization`)
- Copy the `SpeckleMicroStation2.cfg` located in the Connector folder to the Bentley confic folder

You can now start a Visual Studio's debug session for MicroStation, OpenRoad, OpenRail or OpenBuildings. This will launch the corresponding application for you, and the Speckle toolbar should be loaded!

### Features

Supported elements will be added!

## Contributing


## Community


## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via email or on the Speckle Community Forum.
