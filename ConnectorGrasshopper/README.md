# Connector Grasshopper

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This repo holds Speckle's Grasshopper Connector and it is currently released as ⚠ **BETA** ⚠, please use at your own risk!

The connector is structured in 1 c# project:

- ConnectorGrasshopper: contains the grasshopper component nodes and parameters.

## Documentation

Comprehensive developer and user documentation can be found in our:

#### 📚 [Speckle Docs website](https://speckle.guide/dev/)

## Developing & Debugging

We encourage everyone interested to debug / hack / contribute / give feedback to this project.

### Requirements

- Rhino 6 or above (we're currently testing with 6.28)
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started

Following instructions on how to get started debugging and contributing to this connector.

#### Dependencies

All dependencies exist either on this repo (such as `Core`, `Objects`, etc...) or are installed via NuGet.

Worth mentioning is the use of our own asyncronous grasshopper component, which you can find here:

- https://github.com/specklesystems/GrasshopperAsyncComponent

We also make use of the following `dotnet tools` in this project:

- https://github.com/mono/t4

These are declared on the manifest file `.config/dotnet-tools.json`. To install them in your computer just run the following command on your terminal:

```
dotnet tools restore
```

#### Server

In order to test Speckle in all its glory you'll need a server running, you can run a local one by simply following these instructions:

- https://github.com/specklesystems/Server

If you're facing any errors make sure Postgress and Redis are up and running.

#### Accounts

The connector itself doesn't have features to manage your Speckle accounts, this functionality has been delegated to the Speckle Manager desktop app.

You can install an alpha version of it from: https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe

After installing it, you can use it to add/create an account on the Server.

### Debugging

After setting up dependencies, server and accounts you're good to go. Just make sure of the following:

- the Solution builds fine in your IDE

- you IDE is set to start the correct version of Rhino on Debug

    <img width="500" alt="Screenshot 2020-10-29 at 11 32 51" src="https://user-images.githubusercontent.com/2316535/97557069-94815180-19da-11eb-8e84-05022af3e944.png">

- your Post-Build event is set up correctly to rename the project `.dll` to `.gha`.

    <img width="500" alt="Screenshot 2020-10-29 at 11 32 45" src="https://user-images.githubusercontent.com/2316535/97557434-0f4a6c80-19db-11eb-90c8-2b5c92369b9b.png">

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

And voila', once you start Grasshopper, the `Speckle 2` tab should now appear along any other Grasshopper plugins you may have installed.

<img width="1028" alt="Screenshot 2020-10-29 at 11 08 03" src="https://user-images.githubusercontent.com/2316535/97555645-a661f500-19d8-11eb-8436-1e60d1133e28.png">

> If Grasshopper was already running, you may need to restart Rhino for the Speckle plugin to load.

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).

<!-- Fake change to force CI to run -->
