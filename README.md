<h1 align="center">Breakdance - by CloudNimble</h1> <br>
<p align="center">
  A testing framework for managing the dangerous dance of shipping public APIs. Built by @CloudNimble.
</p>

<div align="center">

<img src="https://cloud.githubusercontent.com/assets/1657085/26813617/6489768e-4a4d-11e7-8a49-3864333ebde9.png" alt="Breakdance Logo">

[Releases](https://github.com/CloudNimble/Breakdance/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;

[![Build status](https://dev.azure.com/cloudnimble/Breakdance/_apis/build/status/Breakdance-RTM)](https://dev.azure.com/cloudnimble/Breakdance/_build/latest?definitionId=10)
[![Release Status](https://vsrm.dev.azure.com/cloudnimble/_apis/public/Release/badge/7f9e2e9c-c38f-43dd-a5f2-0b909c883db2/2/2)](https://dev.azure.com/cloudnimble/Breakdance/_release?_a=releases&view=mine&definitionId=2)
[![Twitter][twitter-img]][twitter-intent]

</div>

## Introduction
Managing breakage in public APIs is a HUGE pain in the ass. With the current tools, you never really know when you're going to break someone. And with NuGet
making weak references mainstream, even Microsoft breaks people... sometimes without realizing it for months.

It's time to change all that. Breakdance integrates surface testing into your build and deployment process, in just a few lines of code. Instead of waiting until GitHub issues break your inbox, Breakdance will break YOUR build first, forcing you to make critical decisions about how to keep your customers happy BEFORE they grab their pitchforks.

### Components
- **Breakdance.Assemblies:**   .NET assemblies.
- **Breakdance.WebApi:**       WebApi services.
- **Breakdance.OData:**        OData services.
- **Breakdance.Restier:**      Restier-based OData services.

## Installation

You can install Testier from NuGet by opening up Package Manager and typing `install-package CloudNimble.Breakdance -pre`. Please note that the current version 
requires [CloudNimble's flavor of Restier](https://github.com/robertmclaws/RESTier), which we are currently shipping because Microsoft has not officially updated 
their NuGet packages for quite some time.

## Using Breakdance
[TBD]

### Assembly-Level Tests
[TBD]

### WebApi Tests
[TBD]

### WebApi OData Tests
[TBD]

### Restier Tests
Because Restier uses a set of built-in conventions to reflect over your API and _automagically_ find the methods it needs, it can be difficult to know what methods the system is expecting to find.

Testier uses a copy of the same convention lookup code, and the same method naming patterns, to generate a list of all possible method names. It can also generate a "Visibility Matrix" for your API, showing you which ones it expected, and which ones it found. That matrix looks something like this:

```
--------------------------------------------------
Function Name                            |   Found
--------------------------------------------------
CanInsertPlayers                         |   False
CanUpdatePlayers                         |   False
CanDeletePlayers                         |   False
OnInsertingPlayers                       |   False
OnUpdatingPlayers                        |   False
OnDeletingPlayers                        |   False
OnFilterPlayer                           |   False
OnInsertedPlayers                        |   False
OnUpdatedPlayers                         |   False
OnDeletedPlayers                         |   False
CanInsertSports                          |   False
CanUpdateSports                          |   False
CanDeleteSports                          |   False
OnInsertingSports                        |   False
OnUpdatingSports                         |   False
OnDeletingSports                         |   False
OnFilterSport                            |    True
OnInsertedSports                         |    True
OnUpdatedSports                          |   False
OnDeletedSports                          |   False
CanInsertTeams                           |   False
CanUpdateTeams                           |   False
CanDeleteTeams                           |   False
OnInsertingTeams                         |   False
OnUpdatingTeams                          |   False
OnDeletingTeams                          |   False
OnFilterTeam                             |   False
OnInsertedTeams                          |   False
OnUpdatedTeams                           |   False
OnDeletedTeams                           |   False
CanExecuteTestMethod                     |   False
OnExecutingTestMethod                    |   False
OnExecutedTestMethod                     |   False
--------------------------------------------------
```

The included unit tests show how to take this information, and build tests that compare a baseline against your current version. That way, if something changes, your tests break, and you're forced to understand what changed.

### Swagger Tests
[TBD]

## Known Issues

 - Testier can't currently map OData functions that are bound to datasets. If you know how to do this, please feel free to submit a PR.

## Feedback

Feel free to send us feedback on [Twitter](https://twitter.com/cloud_nimble) or [file an issue](https://github.com/CloudNimble/Breakdance/issues/new). Feature requests are always welcome. If you wish to contribute, please take a quick look at the [guidelines](./CONTRIBUTING.md)!

<!--
Link References
-->

[twitter-intent]:https://twitter.com/intent/tweet?via=cloud_nimble&text=Check%20out%20Breakdance%2C%20a%20testing%20framework%20for%20managing%20the%20dangerous%20dance%20of%20shipping%20public%20APIs.&hashtags=API%2Ctesting
[twitter-img]:https://img.shields.io/badge/share-on%20twitter-55acee.svg?style=for-the-badge&logo=twitter