# ConnectorRevit

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This repo holds the Speckle 2.0 Revit Connector. It is currently in ⚠ALPHA⚠ and is a bit rough around the edges -- please use at your own risk! And of course, we'd really appreciate any feedback, comments, suggestions, etc ✨

## Documentation

Comprehensive developer and user documentation can be found in our:

#### 📚 [Speckle Docs website](https://speckle.guide/dev/)

## Developing & Debugging

### Requirements

- Revit 2019, 2020, or 2021
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started

#### Server

In order to test Speckle in all its glory, you'll need a server running. You can run a local one by simply following the instructions in the [Server Repo](https://github.com/specklesystems/Server)

If you're facing any errors, make sure Postgres and Redis are up and running.

#### Accounts

The connector itself doesn't have features to manage your Speckle accounts. This functionality has been delegated to the Speckle Manager desktop app.

You can install an alpha version of it from: [here](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe)

After installing it, you can use it to add/create an account on the Server.

### Debugging

In your IDE, you can select which Revit version you want to run. If you're having SQLite issues when building, make doubly sure that you're on x64.

![select debug version in IDE](https://user-images.githubusercontent.com/7717434/97556712-b9bd9200-19d1-11eb-9b4b-8c25832547bd.png)

The button to launch the connector should now appear in the Add-Ins ribbon. You're ready to go!

![speckle button on ribbon menu](https://user-images.githubusercontent.com/7717434/97557082-381a3400-19d2-11eb-8d10-13039d5ee7be.png)

Fire it up 🔥

![quick-revit-demo](https://user-images.githubusercontent.com/7717434/97557677-fe95f880-19d2-11eb-8ad3-439f7ad63015.gif)

### Features

Add a new stream:

![add-stream](https://user-images.githubusercontent.com/7717434/97726487-2bc3d300-1ac7-11eb-849e-7aed2d21ec76.gif)

Get fancy with some filters:

![add-with-filters](https://user-images.githubusercontent.com/7717434/97726494-2e262d00-1ac7-11eb-8a55-8a7939c8f378.gif)

Search for and add an existing stream from the server:

![receive-stream](https://user-images.githubusercontent.com/7717434/97726530-3716fe80-1ac7-11eb-9732-19117b5cef27.gif)

Update a stream and edit collaborators:

![update-stream](https://user-images.githubusercontent.com/7717434/97726509-31211d80-1ac7-11eb-945e-67d1ca29c16d.gif)

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
