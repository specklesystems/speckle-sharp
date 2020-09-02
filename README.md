# Core

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works)
[![Slack Invite](https://img.shields.io/badge/-slack-grey?style=flat-square&logo=slack)](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)

**Status**

[![.NET Core](https://circleci.com/gh/specklesystems/Core.svg?style=svg)](https://circleci.com/gh/specklesystems/Core)

### **Disclaimer**

This is an early alpha release, not meant for use in production! We're working to stabilise the 2.0 API, and until then there will be breaking changes. You have been warned! 


## Introduction

### Core

Core is the .NET SDK for Speckle 2.0. It uses .NET Standard 2.0 and has been tested on Windows and MacOS.

## Developing & Debugging

### Building

Make sure you clone this repository together with its submodules: `git clone https://github.com/specklesystems/Core.git -recursive`. 
Afterwards, just restore all the NuGet packages and hit Build!

### Developing

This project is evolving fast, to better understand how to use Core we suggest checking out the Unit and Integration tests. Running the integration tests locally requires a local server running on your computer.

We'll be also adding [preliminary documentation on our forum](https://discourse.speckle.works/c/speckle-insider/10).

### Tests

There are two test projects, one for unit tests and one for integration tests. The latter needs a server running locally in order to run.

## Contributing

Before embarking on submitting a patch, please make sure you read:

- [Contribution Guidelines](CONTRIBUTING.md), 
- [Code of Conduct](CODE_OF_CONDUCT.md)

## Community 

The Speckle Community hangs out in two main places, usually: 

- on [the forum](https://discourse.speckle.works)
- on [the chat](https://speckle-works.slack.com/join/shared_invite/enQtNjY5Mzk2NTYxNTA4LTU4MWI5ZjdhMjFmMTIxZDIzOTAzMzRmMTZhY2QxMmM1ZjVmNzJmZGMzMDVlZmJjYWQxYWU0MWJkYmY3N2JjNGI) 

Do join and introduce yourself! 

## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 license.
