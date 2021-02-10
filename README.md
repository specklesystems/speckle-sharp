# Speckle Sharp: The Speckle 2.0 .NET SDK, Connectors, and Interoperability Kit

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)

<details>
  <summary>What is Speckle?</summary>
  
  
  Speckle is the Open Source Data Platform for AEC. Speckle allows you to say goodbye to files: we give you object-level control of what you share, infinite versioning history & changelogs. Read more on [our website](https://speckle.systems).

</details>

**Status**

[![.NET Core](https://circleci.com/gh/specklesystems/speckle-sharp.svg?style=svg)](https://circleci.com/gh/specklesystems/speckle-sharp)

## Introduction

This monorepo is the home to our Speckle 2.0 C# projects. The [Speckle Server](https://github.com/specklesystems/Server) is providing all the web-facing functionality and can be found [here](https://github.com/specklesystems/Server).

Specifically, this repo holds:

### ➡️ [Core](Core), the .NET SDK.

[Speckle Core](Core) is the canconical SDK for Speckle. It supports multiple [data transports](https://discourse.speckle.works/t/core-2-0-transports/919), and advanced [decomposition API](https://discourse.speckle.works/t/core-2-0-decomposition-api/911) for design data, and offers a [dynamic base](https://discourse.speckle.works/t/core-2-0-the-base-object/782) for object definition.

### ➡️ [Objects](Objects)

The Objects Kit is our default interoperability kit. Read more about kits and their role in the Speckle ecosystem [here](https://discourse.speckle.works/t/introducing-kits-2-0/710).

### ➡️ Speckle Connectors:

The Speckle Connectors are plugins that embed with an application and provide the interface between its API and Speckle. Currently we have:

- [Grasshopper](ConnectorGrasshopper)
- [Dynamo](ConnectorDynamo)
- [Revit](ConnectorRevit)
- [Rhino](ConnectorRhino)
- [AutocadCivil](ConnectorAutocadCivil)

### ➡️ [DesktopUI](DesktopUI)

The DesktopUI project contains the reusable ui for all non-visual programming connectors. If you're embarking on developing a new connector, we recommend starting from here.

## Developing & Debugging: First Step

Clone this monorepo and check the readme of you project you're interested in! We encourage everyone interested to debug / hack /contribute / give feedback to this project.

> **A note on Accounts:**
> The connectors themselves doesn't have features to manage your Speckle accounts, this functionality has been delegated to the Speckle Manager desktop app. You can install [an alpha version of it from here](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe). After installing it, you can use it to add/create an account on the Server.

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community, Questions and Feeback:

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
