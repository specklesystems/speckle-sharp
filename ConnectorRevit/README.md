# ConnectorRevit

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This repo holds the Speckle Revit Connector. We'd really appreciate any feedback, comments, suggestions, etc ✨

## Documentation

Comprehensive developer and user documentation can be found in our:

#### 📚 [Speckle Docs website](https://speckle.guide/dev/)

## Developing & Debugging

### Requirements

- Revit 2019 or above
- Visual Studio 2019 or above (or similar IDE)
- A Speckle account (you can make one at https://speckle.xyz/)

### Getting Started

#### Accounts

To use Speckle, you'll need a Speckle account. This can be in our XYZ server, in a local server or anywhere else.
You can log in to your account:
- from Manager, available at https://speckle-releases.netlify.app/
- or, if using our latest release, you can also log in directly from the Revit connector

![login](https://user-images.githubusercontent.com/2679513/159454529-6b85eb3b-e964-4b39-87ba-286799771e3d.gif)


### Debugging

In your IDE, select the version of the Revit project you want to debug:

![image](https://user-images.githubusercontent.com/2679513/159453238-c4ef1203-0ab5-4193-83a2-7a4a0ba0506e.png)

Ensure the right Start action is set:
![image](https://user-images.githubusercontent.com/2679513/159453340-5055cf3d-6db8-4e80-8374-9d73d2b04427.png)

And then you can click debug. The post build actions will copy all necessary files, so next you just need to launch the connector:

![revit-launch](https://user-images.githubusercontent.com/2679513/159453862-2efd62b4-a881-4967-ace1-5298a40ffd0a.gif)

If you're having SQLite issues when building, make doubly sure that you're on x64.

#### Conversions

All the conversion routines are in the converter project:

![image](https://user-images.githubusercontent.com/2679513/159454133-999cc8ed-2568-4780-8a33-5aee628428dc.png)

You might need to debug the Converter project instead of the Connector one if your IDE doesn't reach the breakpoints correctly.

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
