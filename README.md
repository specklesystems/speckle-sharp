<h1 align="center">
  <img src="https://user-images.githubusercontent.com/2679513/131189167-18ea5fe1-c578-47f6-9785-3748178e4312.png" width="150px"/><br/>
  Speckle | Sharp
</h1>
<h3 align="center">
    .NET SDK, tooling, schema and Connectors
</h3>
<p align="center"><b>Speckle</b> is data infrastructure for the AEC industry.</p><br/>

<p align="center"><a href="https://twitter.com/SpeckleSystems"><img src="https://img.shields.io/twitter/follow/SpeckleSystems?style=social" alt="Twitter Follow"></a> <a href="https://speckle.community"><img src="https://img.shields.io/discourse/users?server=https%3A%2F%2Fspeckle.community&amp;style=flat-square&amp;logo=discourse&amp;logoColor=white" alt="Community forum users"></a> <a href="https://speckle.systems"><img src="https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square" alt="website"></a> <a href="https://speckle.guide/dev/"><img src="https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&amp;logo=read-the-docs&amp;logoColor=white" alt="docs"></a></p>
<p align="center"><a href="https://circleci.com/gh/specklesystems/speckle-sharp"><img src="https://circleci.com/gh/specklesystems/speckle-sharp.svg?style=svg" alt=".NET Core"></a></p>

# About Speckle

What is Speckle? Check our ![YouTube Video Views](https://img.shields.io/youtube/views/B9humiSpHzM?label=Speckle%20in%201%20minute%20video&style=social)

### Features

- **Object-based:** say goodbye to files! Speckle is the first object based platform for the AEC industry
- **Version control:** Speckle is the Git & Hub for geometry and BIM data
- **Collaboration:** share your designs collaborate with others
- **3D Viewer:** see your CAD and BIM models online, share and embed them anywhere
- **Interoperability:** get your CAD and BIM models into other software without exporting or importing
- **Real time:** get real time updates and notifications and changes
- **GraphQL API:** get what you need anywhere you want it
- **Webhooks:** the base for a automation and next-gen pipelines
- **Built for developers:** we are building Speckle with developers in mind and got tools for every stack
- **Built for the AEC industry:** Speckle connectors are plugins for the most common software used in the industry such as Revit, Rhino, Grasshopper, AutoCAD, Civil 3D, Excel, Unreal Engine, Unity, QGIS, Blender and more!

### Try Speckle now!

Give Speckle a try in no time by:

- [![speckle XYZ](https://img.shields.io/badge/https://-speckle.xyz-0069ff?style=flat-square&logo=hackthebox&logoColor=white)](https://speckle.xyz) ⇒ creating an account at 
- [![create a droplet](https://img.shields.io/badge/Create%20a%20Droplet-0069ff?style=flat-square&logo=digitalocean&logoColor=white)](https://marketplace.digitalocean.com/apps/speckle-server?refcode=947a2b5d7dc1) ⇒ deploying an instance in 1 click 

### Resources

- [![Community forum users](https://img.shields.io/badge/community-forum-green?style=for-the-badge&logo=discourse&logoColor=white)](https://speckle.community) for help, feature requests or just to hang with other speckle enthusiasts, check out our community forum!
- [![website](https://img.shields.io/badge/tutorials-speckle.systems-royalblue?style=for-the-badge&logo=youtube)](https://speckle.systems) our tutorials portal is full of resources to get you started using Speckle
- [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=for-the-badge&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/) reference on almost any end-user and developer functionality

![Untitled](https://user-images.githubusercontent.com/2679513/132021739-15140299-624d-4410-98dc-b6ae6d9027ab.png)

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
- [`DesktopUI`](https://github.com/specklesystems/speckle-sharp/tree/main/DesktopUI): reusable UI for all connectors (except visual programming)

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
