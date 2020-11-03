# Speckle Desktop UI

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works)
[![Slack Invite](https://img.shields.io/badge/-slack-grey?style=flat-square&logo=slack)](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)

## Introduction

This is the Desktop UI for Speckle 2.0 Desktop Connectors 🎉

![screenshots of desktop ui](https://user-images.githubusercontent.com/7717434/97719565-21054000-1abf-11eb-9477-43e4c9715827.png)

This project on its own is just a WPF application that doesn't do a whole lot. The magic comes when you implement the `ConnectorBindings` to make it a functional connector for whatever software application you require! It is currently up and running in the [Revit Connector](https://github.com/specklesystems/ConnectorRevit) - take a look!

This UI is currently in ⚠ALPHA⚠ and is a bit rough around the edges -- please use at your own risk! Play with it, break some stuff, and come back to us with feedback comments, suggestions, etc ✨

## Trying it out
The UI will run on it's own, but it won't do much beyond creating and adding empty streams. You can have a look around though to get a feel for it. It uses `DummyBindings` which you can edit to try implementing some of the intended functionality.

### Requirements
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started 

To get the UI running, all you need to do is hit debug in your IDE. There are however a few things you'll need to set up beforehand.

#### Dependencies

The C# projects have local dependencies. In the future, these will be referenced as NuGet packages, but for the time being make sure also to clone the following repos in a folder adjacent to the one of this repo:

- [Core](https://github.com/specklesystems/Core)

It'd be a good idea to clone all the Speckle repos you're working on in one folder.

#### Server

In order to test Speckle in all its glory, you'll need a server running. You can run a local one by simply following the instructions in the [Server Repo](https://github.com/specklesystems/Server)

If you're facing any errors, make sure Postgres and Redis are up and running.

#### Accounts

The connector itself doesn't have features to manage your Speckle accounts. This functionality has been delegated to the Speckle Manager desktop app.

You can install an alpha version of it from: [here](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe)

After installing it, you can use it to add/create an account on the Server.

## Implementing your own connector

You'll need to implement the `ConnectorBindings` for your application to allow the UI to interact with it. The [Revit Connector](https://github.com/specklesystems/ConnectorRevit) is a good place to look for reference.

To start the UI from within your appliation, you'll need to create an instance of the UI `Bootstrapper` and give it an instance of your bindings. You then just need to call `Setup` on the `Bootstrapper` and you're on your way 🚀

### Basic example

```cs
// create a new bindings instance
var bindings = new MyConnectorBindings();

// give it to the bootstrapper
var bootstrapper = new Bootstrapper()
{
  Bindings = bindings
};

// fire it up, baby!
bootstrapper.Setup(Application.Current);

```

## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.

## Community

The Speckle Community hangs out in two main places, usually:

- on [the forum](https://discourse.speckle.works)
- on [the chat](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI)

Do join and introduce yourself!

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).