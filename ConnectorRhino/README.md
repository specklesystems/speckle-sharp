# Connector Rhino

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This repo holds Speckle's Rhino Connector and it is currently released as ⚠ **ALPHA** ⚠, please use at your own risk!

## Documentation

Comprehensive developer and user documentation can be found in our:

#### 📚 [Speckle Docs website](https://speckle.guide/dev/)

## Developing & Debugging

We encourage everyone interested to debug / hack /contribute / give feedback to this project.

### Requirements

- Rhino 6 or above (we're currently testing with 6.30)
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

## Getting Started

The following instructions try to help you with getting started debugging and contributing to this connector.

#### Dependencies

In order to test Speckle in all its glory you'll need a server running, you can run a local one by simply following the [instructions in the server's readme](https://github.com/specklesystems/Server). The connector itself doesn't have features to manage your Speckle accounts, this functionality has been delegated to [the Speckle Manager desktop app](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe). You can download it from [here](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe). After installing it, you can use it to add/create an account on the Server.

### Debugging

After setting up dependencies, server and accounts you're good to go - almost there! After the first build, make sure to:

- Open the bin folder (e.g., `C:\Users\Admin\Code\sharp\ConnectorRhino\bin\Debug`),
- Drag and drop the `ConnectorRhino.rhp` inside an already running instance of Rhino,
- Open the Speckle plugin by typing `Speckle` in the Rhino command line.

You can now close Rhino, and start a Visual Studio's debug session. This will launch Rhino for you, and the Speckle plugin should load!

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).

