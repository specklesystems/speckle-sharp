# Connector Grasshopper

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works)
[![Slack Invite](https://img.shields.io/badge/-slack-grey?style=flat-square&logo=slack)](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)

## Introduction

This repo holds Speckle's Grasshopper Connector and it is currently released as ⚠ **ALPHA** ⚠, please use at your own risk!

The connector is structured in 1 c# project:

- ConnectorGrasshopper: contains the grasshopper component nodes and parameters.

## Developing & Debugging

We encourage everyone interested to debug / hack /contribute / give feedback to this project.

### Requirements

- Rhino 6 or above (we're currently testing with 6.30)
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started

Following instructions on how to get started debugging and contributing to this connector.

#### Dependencies

The c# projects have local dependencies, in the future these will be referenced as NuGet packages, but for the time being **make sure also to clone the following repos** in a folder adjacent to the one of this repo:

- https://github.com/specklesystems/Core
    - This includes other dependencies included in the `Core` repo:
        - DiskTransport
        - ServerTransport
- https://github.com/specklesystems/Objects
    - This includes other dependencies included in the `Objects` repo:
        - ConverterRhinoGH
- https://github.com/specklesystems/GrasshopperAsyncComponent

It'd be a good solution to just clone all the Speckle repos you're working on in one folder.

#### Server

In order to test Speckle in all its glory you'll need a server running, you can run a local one by simply following these instructions:

- https://github.com/specklesystems/Server

If you're facing any errors make sure Postgress and Redis are up and running. 

#### Accounts

The connector itself doesn't have features to manage your Speckle accounts, this functionality has been delegated to the Speckle Manager desktop app.

You can install an alpha version of it from: TODO LINK HERE 

After installing it, you can use it to add/create an account on the Server.

### Debugging

After setting up dependencies, server and accounts you're good to go. Just make sure of the following:

- the Solution builds fine in your IDE
- you IDE is set to start the correct version of Rhino on Debug
    TODO Add image of config here
  

The first time you run Rhino after having built the project, you need to add the `bin\` folder in the `GrasshopperConnector` project to the _Library Folders_ in the _Grasshopper Developer Settings_. To do that, follow these steps.

- Execute the command `GrasshopperDeveloperSettings` in Rhino.

  <img width="500" alt="Screenshot 2020-10-29 at 11 03 15" src="https://user-images.githubusercontent.com/2316535/97555637-a5c95e80-19d8-11eb-9079-46d44a19f565.png">

- Click on the `Add folder` button.

  <img width="500" alt="Screenshot 2020-10-29 at 11 03 29" src="https://user-images.githubusercontent.com/2316535/97555638-a5c95e80-19d8-11eb-86a8-b938c0033763.png">
  
- Click on the `...` button on the new empty line that appeared below.

  <img width="500" alt="Screenshot 2020-10-29 at 11 03 41" src="https://user-images.githubusercontent.com/2316535/97555640-a661f500-19d8-11eb-9140-e8363b50ecd8.png">
  
- Select the `bin\` folder in your project and click ok.

  <img width="432" alt="Screenshot 2020-10-29 at 11 02 43" src="https://user-images.githubusercontent.com/2316535/97555636-a530c800-19d8-11eb-8b33-f80702da3cbe.png">
  
- In order to hit breakpoints in your code, it's important to **deactivate** the option `Memory load assemblies using COFF byte arrays`.

  <img width="589" alt="Screenshot 2020-10-29 at 11 04 06" src="https://user-images.githubusercontent.com/2316535/97555643-a661f500-19d8-11eb-87fd-9044acea138e.png">

And voila', the `Speckle 2` tab should now appear along any other Grasshopper plugins you may have installed:

<img width="1028" alt="Screenshot 2020-10-29 at 11 08 03" src="https://user-images.githubusercontent.com/2316535/97555645-a661f500-19d8-11eb-8436-1e60d1133e28.png">


## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out in two main places, usually:

- on [the forum](https://discourse.speckle.works)
- on [the chat](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI)

Do join and introduce yourself!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
