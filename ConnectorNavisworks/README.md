# ConnectorNavisworks

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This repo holds the Speckle Navisworks Connector. We'd really appreciate any feedback, comments, suggestions, etc ✨

## Documentation

Comprehensive developer and user documentation can be found in our:

#### 📚 [Speckle Docs website](https://speckle.guide/dev/)

## Developing & Debugging

### Requirements

- Navisworks 2020 - 2024
- Visual Studio 2019 or above (or similar IDE)
- A Speckle account (you can make one at https://speckle.xyz/)

### Getting Started

#### Accounts

To use Speckle, you'll need a Speckle account. This can be in our XYZ server, in a local server or anywhere else.
You can log in to your account:
- from Manager, available at https://speckle.systems/download/
- or, if using our latest release, you can also log in directly from the Navisworks Connector.

![login](https://github.com/specklesystems/speckle-sharp/assets/760691/f5f816fa-ded1-46cc-acca-c424916bed9b)

### Debugging

In your IDE, select the version of the Navisworks project you want to debug:

<img width="245" alt="image" src="https://user-images.githubusercontent.com/760691/223118454-5d543a11-15ad-4513-989d-c1ae49c2bc78.png">

Ensure the right Executable set in you Debug Launch profile:

<img width="604" alt="image" src="https://user-images.githubusercontent.com/760691/223118968-e89110df-2ec9-415a-a8a3-55ab011e3a91.png">

And then you can click debug. The post build actions will copy all necessary files, so next you just need to launch the connector:

<img width="479" alt="image" src="https://user-images.githubusercontent.com/760691/223145841-9fd6c9a4-9036-4853-85a0-b43bcebdb5b2.png">

By default the Connector installs for the active user in the AppData roaming folder. You can change this by editing the `ConnectorNavisworks.csproj` in the PostBuildEvent.
It builds a .bundle form of plugin and uses the package.xml to install it in the Navisworks plugins folder, this maps the versions supported by Speckle Connector for Navisworks.

<img width="642" alt="image" src="https://user-images.githubusercontent.com/760691/231252939-73a230bf-529e-4f78-8d85-c10a8d1256e7.png">

#### Conversions

All the conversion routines are in the converter project:

<img width="230" alt="image" src="https://user-images.githubusercontent.com/760691/223146069-f6c40682-f423-4dba-a15e-87995deb5264.png">

You might need to debug the Converter project instead of the Connector one if your IDE doesn't reach the breakpoints correctly.

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best
