# Connector AutoCAD Civil3D

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This is the ⚠Beta⚠ version of the Speckle 2.0 AutoCAD Civil3D Connector. Currently, it supports the basic objects for both Autocad and Civil3D (refer to our docs for a full list) - please leave any comments, suggestions, and feature requests in our [Making Speckle](https://discourse.speckle.works/t/new-speckle-2-0-autocad-civil3d-suggestions/1155) forum discussion thread!

## Documentation

Comprehensive developer and user documentation can be found in our:

#### 📚 [Speckle Docs website](https://speckle.guide/user/autocadcivil.html)

## Developing & Debugging

### Requirements

- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

#### Supported versions

- AutoCAD: 2021, 2022, 2023, 2024, 2025
- Civil3D: 2021, 2022, 2023, 2024, 2025

### Getting Started

#### Server

In order to test Speckle in all its glory, you'll need a server running. You can run a local one by simply following the instructions in the [Server Repo](https://github.com/specklesystems/Server).

#### Accounts

The connector itself doesn't have features to manage your Speckle account - this functionality has been delegated to the Speckle Manager desktop app. After installing the [alpha version](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe), use the manager to add or create an account on the Server.

### Debugging

- Start a Visual Studio debug session with the target connector as your startup project, and wait for your AutoCAD, Civil3D, or Civil3D as AutoCAD application to open

You should now see the Speckle Connector plugin in the `Add-ins` tab, and the command `SPECKLE` will also initialize the plugin. If this isn't the case, you can try:

- Enter `NETLOAD` in the command prompt
- Navigate to and select the SpeckleConnectorAutocad.dll or SpeckleConnectorCivil.dll in the corresponding local repo Debug bin (ex: `speckle-sharp\ConnectorAutocadCivil\ConnectorCivil2021\bin\Debug`)
- The Speckle connector should now appear in the Add-ins ribbon! Click this to get started, or enter `SPECKLE` in the command prompt.

If you are experiencing a LoaderLock exception when firing up debug, select `Debug -> Windows -> Exception Settings` and uncheck the `LoaderLock` option under `Managed Debugging Assistants`.

### Features

For an updated table of supported elements, refer to our [documentation](https://speckle.guide/user/support-tables.html#autocad)

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
