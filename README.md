# Connector Dynamo

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works)
[![Slack Invite](https://img.shields.io/badge/-slack-grey?style=flat-square&logo=slack)](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)

## Introduction

This repo holds Speckle's Dynamo Connector and it is currently released as ⚠ **ALPHA** ⚠, please use at your own risk!

The connector is structured in 3 c# projects:

- ConnectorDynamo: contains the NodeModel nodes
- ConnectorDynamoExtension: contains a Dynamo extension, currently doesn't do much but it's scaffolded
- ConnectorDynamoFunctions: contains the ZeroTouch nodes and functions invoked by the NodeModel nodes

## Developing & Debugging

We encourage everyone interested to debug / hack /contribute / give feedback to this project.

### Requirements

- Dynamo 2.1 or above (we're currently testing with 2.8)
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started

Following instructions on how to get started debugging and contributing to this connector.

#### Dependencies

The c# projects have local dependencies, in the future these will be referenced as NuGet packages, but for the time being **make sure also to clone the following repos** in a folder adjacent to the one of this repo:

- https://github.com/specklesystems/Core
- https://github.com/specklesystems/Objects

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
- you IDE is set to start the correct version of Dynamo or Revit on Debug
  ![image](https://user-images.githubusercontent.com/2679513/97479008-a666e400-1949-11eb-983a-3fccc066b597.png)

The first time you run Dynamo after having built the project, you need to add the `dist` folder to the list of Dynamo's Package paths:

- Click on `Settings` > `Manage Node and Package Paths...`
  ![image](https://user-images.githubusercontent.com/2679513/97480730-baabe080-194b-11eb-92e8-0655a9765b3a.png)
- Add the `dist` folder in your `repo folder\ConnectorDynamo\ConnectorDynamo\dist` 
  ![image](https://user-images.githubusercontent.com/2679513/97480369-35c0c700-194b-11eb-994a-3f03ee55ebee.png)



And voila', the Speckle packages should now show in the library:

![image](https://user-images.githubusercontent.com/2679513/97480950-03639980-194c-11eb-8474-7d14a427ecc0.png)



#### Dynamo Sandbox

You don't need to run Revit to debug Dynamo, you can just use the [Sandbox version](https://dynamobim.org/download/).

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out in two main places, usually:

- on [the forum](https://discourse.speckle.works)
- on [the chat](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI)

Do join and introduce yourself!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
