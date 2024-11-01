<h1 align="center">
  <img src="https://user-images.githubusercontent.com/2679513/131189167-18ea5fe1-c578-47f6-9785-3748178e4312.png" width="150px"/><br/>
  Speckle | Sharp (Legacy)
</h1>

<p align="center"><a href="https://twitter.com/SpeckleSystems"><img src="https://img.shields.io/twitter/follow/SpeckleSystems?style=social" alt="Twitter Follow"></a> <a href="https://speckle.community"><img src="https://img.shields.io/discourse/users?server=https%3A%2F%2Fspeckle.community&amp;style=flat-square&amp;logo=discourse&amp;logoColor=white" alt="Community forum users"></a> <a href="https://speckle.systems"><img src="https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square" alt="website"></a> <a href="https://speckle.guide/dev/"><img src="https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&amp;logo=read-the-docs&amp;logoColor=white" alt="docs"></a></p>

> Speckle is the first AEC data hub that connects with your favorite AEC tools. Speckle exists to overcome the challenges of working in a fragmented industry where communication, creative workflows, and the exchange of data are often hindered by siloed software and processes. It is here to make the industry better.

<h3 align="center">
    .NET SDK, tooling, schema and Connectors
</h3>

<p align="center"><a href="https://circleci.com/gh/specklesystems/speckle-sharp"><img src="https://circleci.com/gh/specklesystems/speckle-sharp.svg?style=svg" alt=".NET Core"></a></p>

> [!WARNING]
> This is our legacy .NET repo! Check out our active .NET repos here:
> [`speckle-sharp-connectors`](https://github.com/specklesystems/speckle-sharp-connectors): our .NET next generation connectors and desktop UI
> [`speckle-sharp-sdk`](https://github.com/specklesystems/speckle-sharp-sdk): our .NET SDK, Tests, and Objects

# Repo structure

This monorepo is the home to our Speckle 2.0 .NET projects:

- [`Core`](https://github.com/specklesystems/speckle-sharp/tree/main/Core): the canonical SDK for Speckle. It supports multiple [data transports](https://discourse.speckle.works/t/core-2-0-transports/919), and advanced [decomposition API](https://discourse.speckle.works/t/core-2-0-decomposition-api/911) for design data, and offers a [dynamic base](https://discourse.speckle.works/t/core-2-0-the-base-object/782) for object definition.
- [`Objects`](https://github.com/specklesystems/speckle-sharp/tree/main/Objects): the Objects Kit is our default interoperability kit. Read more about kits and their role in the Speckle ecosystem [here](https://discourse.speckle.works/t/introducing-kits-2-0/710).
  - [`Converters`](https://github.com/specklesystems/speckle-sharp/tree/main/Objects/Converters): conversion routines for each of the connectors mentioned below
- Speckle Connectors
  - [`ConnectorAutocadCivil`](https://github.com/specklesystems/speckle-sharp/tree/main/ConnectorAutocadCivil): for Autodesk AutoCAD and Civil3D 2021+
  - [`ConnectorDynamo`](https://github.com/specklesystems/speckle-sharp/tree/main/ConnectorDynamo): for Autodesk Dynamo
  - [`ConnectorGrasshopper`](https://github.com/specklesystems/speckle-sharp/tree/main/ConnectorGrasshopper): for McNeel Grasshopper
  - [`ConnectorRevit`](https://github.com/specklesystems/speckle-sharp/tree/main/ConnectorRevit): for Autodesk Revit 2019+
  - [`ConnectorRhino`](https://github.com/specklesystems/speckle-sharp/tree/main/ConnectorRhino): for McNeel Rhino 6+
- [`DesktopUI2`](https://github.com/specklesystems/speckle-sharp/tree/main/DesktopUI2): reusable UI for all connectors (except visual programming)

### Other repos

Make sure to also check and ⭐️ these other Speckle repositories:

- [`speckle-server`](https://github.com/specklesystems/speckle-server): Server and Web packages
- [`specklepy`](https://github.com/specklesystems/specklepy): Python SDK 🐍
- [`speckle-excel`](https://github.com/specklesystems/speckle-excel): Excel connector
- [`speckle-unity`](https://github.com/specklesystems/speckle-unity): Unity 3D connector
- [`speckle-blender`](https://github.com/specklesystems/speckle-blender): Blender connector
- [`speckle-unreal`](https://github.com/specklesystems/speckle-unreal): Unreal Engine Connector
- [`speckle-qgis`](https://github.com/specklesystems/speckle-qgis): QGIS connectod
- [`speckle-powerbi`](https://github.com/specklesystems/speckle-powerbi): PowerBi connector
- and more [connectors & tooling](https://github.com/specklesystems/)!

## Developing and Debugging

Clone this monorepo; **each section has its own readme**, so then just follow those instructions.

Issues or questions? We encourage everyone interested to debug / hack / contribute / give feedback to this project.

> **A note on Accounts:**
> The connectors themselves don't have features to manage your Speckle accounts; this functionality is delegated to the Speckle Manager desktop app. You can install it [from here](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe).

### Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

### Security

For any security vulnerabilities or concerns, please contact us directly at security[at]speckle.systems.

### License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
