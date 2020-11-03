# Speckle Sharp

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works)
[![Slack Invite](https://img.shields.io/badge/-slack-grey?style=flat-square&logo=slack)](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)


> Speckle is the Open Source data platform for AEC.

Since you're here, you might be interested also in the [Speckle Server](https://github.com/specklesystems/Server).

## Introduction

Speckle Sharp is home to our Speckle 2.0 C# projects, more specifically:

- [Core](Core), the .NET SDK
- [Objects](Objects), the default .NET Speckle Kit
- .NET Connectors:
  - [Grasshopper](ConnectorGrasshopper)
  - [Dynamo](ConnectorDynamo)
  - [Revit](ConnectorRevit)
- [DesktopUI](DesktopUI), the reusable ui for connectors.

Speckle Sharp is currently released as ⚠ **ALPHA** ⚠, please use at your own risk. 

## Developing & Debugging

Below are some general instruction on how to get started developing and debugging Speckle Sharp, **please check each subfolder for instructions on how to set up each individual project**. 

We encourage everyone interested to debug / hack /contribute / give feedback to this project.

### Requirements

- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started 🏁

Following instructions on how to get started debugging and contributing.

#### First Step

Clone this monorepo and check the readme of you project you're interested in.

#### Server

In order to test Speckle in all its glory you'll need a server running, you can run a local one by simply following these instructions:

- https://github.com/specklesystems/Server

If you're facing any errors make sure Postgress and Redis are up and running. 

#### Accounts

The connectors themselves doesn't have features to manage your Speckle accounts, this functionality has been delegated to the Speckle Manager desktop app.

You can install an alpha version of it from: https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe

After installing it, you can use it to add/create an account on the Server.



### Questions and Feedback 💬

Hey, this is an alpha release, I'm sure you'll have plenty of feedback, and we want to hear all about it! Get in touch with us on [the forum](https://discourse.speckle.works)! 



## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.



## Community

The Speckle Community hangs out in two main places, usually:

- on [the forum](https://discourse.speckle.works)
- on [the chat](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI)

Do join and introduce yourself!



## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
