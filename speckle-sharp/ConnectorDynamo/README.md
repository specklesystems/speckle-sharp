# Connector Dynamo

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)

## Introduction

This repo holds Speckle's Dynamo Connector and it is currently released as ⚠ **ALPHA** ⚠, please use at your own risk!

The connector is structured in 3 c# projects:

- ConnectorDynamo: contains the NodeModel nodes
- ConnectorDynamoExtension: contains a Dynamo extension, currently doesn't do much but it's scaffolded
- ConnectorDynamoFunctions: contains the ZeroTouch nodes and functions invoked by the NodeModel nodes

## Documentation

Comprehensive developer and user documentation can be found in our:

#### 📚 [Speckle Docs website](https://speckle.guide/dev/)

## Developing & Debugging

We encourage everyone interested to debug / hack /contribute / give feedback to this project.

### Requirements

- Dynamo 2.1 or above (we're currently testing with 2.7+)
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started 🏁

Following instructions on how to get started debugging and contributing to this connector.

#### Server

In order to test Speckle in all its glory you'll need a server running, you can run a local one by simply following these instructions:

- https://github.com/specklesystems/Server

If you're facing any errors make sure Postgress and Redis are up and running.

#### Accounts

The connector itself doesn't have features to manage your Speckle accounts, this functionality has been delegated to the Speckle Manager desktop app.

You can install an alpha version of it from: [https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe)

After installing it, you can use it to add/create an account on the Server.

### Debugging

After setting up dependencies, server and accounts you're good to go. Just make sure of the following:

- `ConnectorDynamo.csproj` is set as start project
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

## How to use

Using this connector is pretty much similar to using the 1.x one, but there are a few key differences. Below a visual guide of the main features.

### Accounts

#### Selection

Use the "Account.Select" node to switch accounts.

![account-selection](https://user-images.githubusercontent.com/2679513/97777914-38016c00-1b6b-11eb-8988-85d5a166fe5a.gif)

#### Default account

If you only add one account in the Manager, **that will also be your default account**. If you have multiple accounts, you can **switch default account using the Manager**.

![image](https://user-images.githubusercontent.com/2679513/97778543-c4159280-1b6f-11eb-924e-04b3fb1ed3e0.png)

Some nodes accept an optional "account" input, **if not provided the default account will be used.**

![image-20201031111912748](https://user-images.githubusercontent.com/2679513/97778555-da235300-1b6f-11eb-9c24-aa50908fcacf.png)

### Streams

#### **Creating a stream** 🌊

Can be done with the "Create" node or from the web.

NOTE: the "Create" node is one use only, it disables itself after clicking the button.

![stream-create](https://user-images.githubusercontent.com/2679513/97777062-61b79480-1b65-11eb-8035-dbc1dcf6053e.gif)

![stream-create-web](https://user-images.githubusercontent.com/2679513/97777103-c4a92b80-1b65-11eb-91e7-6ded86b59eb8.gif)

#### Using an existing stream

You can retrieve a previously created stream or streams in 2 ways.

**By URL**

![image](https://user-images.githubusercontent.com/2679513/97777656-9af20380-1b69-11eb-91c8-b543d8837e6a.png)

**Using Stream.List**

![image-20201031111912748](https://user-images.githubusercontent.com/2679513/97778555-da235300-1b6f-11eb-9c24-aa50908fcacf.png)

#### Sending & Receiving 📩

Sending and receiving is pretty straightforward.

- each time you send something, a new "Commit" is created with the data sent
- sending is manual only (need to click on the button)
- receiving is manual by default and can be toggle to automatic
- if passing a stream url **containing a commit Id** to the receiver, it will be pulling only that commit and no other updates to that stream
- most geometry and data types are supported a part from surfaces and polysurfaces
- an optional "branchName" can be passed on to send to / receive from a specific branch on the stream
  - the branch has to exist
  - if receiving a specific commit the branch input is ignored
- an optional "message" can be passed to the sender, this is the commit message

![stream-send](https://user-images.githubusercontent.com/2679513/97778157-2c16a980-1b6d-11eb-8bee-805db5f54258.gif)

#### Viewing Streams 🕶

The Dynamo UI, doesn't let us copy text (c'mon Dynamo team), so we have made a node to let you open and view streams online.

- if using a stream pointing to a specific commit, the commit page will be opened

![view-stream](https://user-images.githubusercontent.com/2679513/97778366-a136ae80-1b6e-11eb-8123-b7701ab6678c.gif)

### Questions and Feedback 💬

Hey, this is an alpha release, I'm sure you'll have plenty of feedback, and we want to hear all about it! Get in touch with us on [the forum](https://discourse.speckle.works)!

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself & feel free to ask us questions!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).
