<h1 align="center">Breakdance - by CloudNimble</h1> 
<br>
<p align="center">
  A testing framework for managing the dangerous dance of shipping public APIs. Built by @CloudNimble.
</p>

<div align="center">

<img src="https://cloud.githubusercontent.com/assets/1657085/26813617/6489768e-4a4d-11e7-8a49-3864333ebde9.png" alt="Breakdance Logo">

<br>

[Website][website-link] &nbsp;&nbsp;&nbsp; | &nbsp;&nbsp;&nbsp; [Releases][release-link] &nbsp;&nbsp;&nbsp;| &nbsp;&nbsp;&nbsp; [Documentation][doc-link] &nbsp;&nbsp;&nbsp;

[![Build Status][devops-rtm-build-img]][devops-rtm-build]
[![Release Status][devops-rtm-release-img]][devops-rtm-release]
[![Twitter][twitter-img]][twitter-intent]

</div>

## Introduction
Managing breakage in public APIs is a HUGE pain in the ass. With the current tools, you never really know when you're going to break someone. And with NuGet
making weak references mainstream, even Microsoft breaks people... sometimes without realizing it for months.

It's time to change all that. Breakdance integrates surface testing into your build and deployment process, in just a few lines of code. Instead of waiting until GitHub issues break your inbox, Breakdance will break YOUR build first, forcing you to make critical decisions about how to keep your customers happy BEFORE they grab their pitchforks.

### Components
- **Breakdance.AspNetCore:**     ASP.NET Core assemblies.
- **Breakdance.Assemblies:**     .NET assemblies.
- **Breakdance.Blazor:**         Blazor assemblies.
- **Breakdance.WebApi:**         WebApi services.
- **Breakdance.OData:**          OData services.
- **Breakdance.Restier:**        Restier-based OData services.

### Ecosystem

| Project | Release | Latest | Description |
|---------|--------|--------|-------------|
| [Breakdance.AspNetCore][bd-aspnetcore-nuget]    | [![bd-aspnetcore-rtm][bd-aspnetcore-rtm-nuget-img]][bd-aspnetcore-nuget] | [![bd-aspnetcore-ci][bd-aspnetcore-ci-nuget-img]][bd-aspnetcore-nuget] | ASP.NET Core assemblies.
| [Breakdance.AspNetCore.SignalR][bd-aspnetcore-sigr-nuget]    | [![bd-aspnetcore-sigr-rtm][bd-aspnetcore-sigr-rtm-nuget-img]][bd-aspnetcore-sigr-nuget] | [![bd-aspnetcore-sigr-ci][bd-aspnetcore-sigr-ci-nuget-img]][bd-aspnetcore-sigr-nuget] | ASP.NET Core assemblies for SignalR
| [Breakdance.Assemblies][bd-assemblies-nuget]    | [![bd-assemblies-rtm][bd-assemblies-rtm-nuget-img]][bd-assemblies-nuget] | [![bd-assemblies-ci][bd-assemblies-ci-nuget-img]][bd-assemblies-nuget] | .NET assemblies.
| [Breakdance.Blazor][bd-blazor-nuget]    | [![bd-blazor-rtm][bd-blazor-rtm-nuget-img]][bd-blazor-nuget] | [![bd-blazor-ci][bd-blazor-ci-nuget-img]][bd-blazor-nuget] | Blazor assemblies.
| [Breakdance.Extensions.MSTest2][bd-mstest-nuget]    | [![bd-mstest-rtm][bd-mstest-rtm-nuget-img]][bd-mstest-nuget] | [![bd-mstest-ci][bd-mstest-ci-nuget-img]][bd-mstest-nuget] | Microsoft Test assemblies.
| [Breakdance.Tools][bd-tools-nuget]    | [![bd-tools-rtm][bd-tools-rtm-nuget-img]][bd-tools-nuget] | [![bd-tools-ci][bd-tools-ci-nuget-img]][bd-tools-nuget]  | CLI tools  for Breakdance.
| [Breakdance.WebApi][bd-webapi-nuget]    | [![bd-webapi-rtm][bd-webapi-rtm-nuget-img]][bd-webapi-nuget] | [![bd-webapi-ci][bd-webapi-ci-nuget-img]][bd-webapi-nuget] | WebApi services.

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

Feel free to send us feedback on [Twitter][twitter-link] or [file an issue][issues-link]. Feature requests are always welcome. If you wish to contribute, please take a quick look at the [contribution guidelines](./.github/CONTRIBUTING.md).

## Code of Conduct

Please adhere to our [Code of Conduct](./CODE_OF_CONDUCT.md) during any interactions with 
CloudNimble team members and community members. It is strictly enforced on all official CloudNimble
repositories, websites, and resources. If you encounter someone violating
these terms, please let us know via DM on [Twitter][twitter-link] or via email at opensource@nimbleapps.cloud and we will address it as soon as possible.

## Contributors

Thank you to all the people who have contributed to the project: [Source code Contributors][contri-link]

Please visit our [Contribution](./.github/CONTRIBUTING.md) document to start contributing to our project.

<!-- Base Link References -->

[website-link]: https://nimbleapps.cloud/
[project-link]: https://github.com/CloudNimble/Breakdance/
[release-link]: https://github.com/CloudNimble/Breakdance/releases
[doc-link]: https://github.com/CloudNimble/Breakdance/tree/main/docs
[contri-link]: https://github.com/CloudNimble/Breakdance/graphs/contributors
[issues-link]: https://github.com/CloudNimble/Breakdance/issues

[twitter-link]: https://twitter.com/cloud_nimble
[twitter-intent]:https://twitter.com/intent/tweet?via=cloud_nimble&text=Check%20out%20Breakdance%2C%20the%20framework%20for%20reliable%2C%20distributed%2C%20scalable%2C%20cross-platform%20event%20processing%20on%20.NET.&hashtags=dotnetcore%2Cazure
[twitter-img]:https://img.shields.io/badge/share-on%20twitter-55acee.svg?style=for-the-badge&logo=twitter

<!-- CI/CD Link References -->

[devops-rtm-build]:https://dev.azure.com/cloudnimble/Breakdance/_build/latest?definitionId=10
[devops-rtm-release]:https://dev.azure.com/cloudnimble/Breakdance/_release?view=all&definitionId=2

[devops-rtm-build-img]:https://img.shields.io/azure-devops/build/cloudnimble/breakdance/10.svg?style=for-the-badge&logo=azuredevops
[devops-rtm-release-img]:https://img.shields.io/azure-devops/release/cloudnimble/7f9e2e9c-c38f-43dd-a5f2-0b909c883db2/2/2.svg?style=for-the-badge&logo=azuredevops

<!-- Ecosystem Link References -->

[bd-aspnetcore-nuget]: https://www.nuget.org/packages/Breakdance.AspNetCore
[bd-aspnetcore-sigr-nuget]: https://www.nuget.org/packages/Breakdance.AspNetCore.SignalR
[bd-assemblies-nuget]: https://www.nuget.org/packages/Breakdance.Assemblies
[bd-blazor-nuget]: https://www.nuget.org/packages/Breakdance.Blazor
[bd-mstest-nuget]: https://www.nuget.org/packages/Breakdance.Extensions.MSTest2
[bd-tools-nuget]: https://www.nuget.org/packages/Breakdance.Tools
[bd-webapi-nuget]: https://www.nuget.org/packages/Breakdance.WebApi

<!-- Badges -->
[bd-aspnetcore-rtm-nuget-img]: https://img.shields.io/nuget/v/Breakdance.AspNetCore?label=&logo=NuGet&style=for-the-badge
[bd-aspnetcore-sigr-rtm-nuget-img]: https://img.shields.io/nuget/v/Breakdance.AspNetCore.SignalR?label=&logo=NuGet&style=for-the-badge
[bd-assemblies-rtm-nuget-img]: https://img.shields.io/nuget/v/Breakdance.Assemblies?label=&logo=NuGet&style=for-the-badge
[bd-blazor-rtm-nuget-img]: https://img.shields.io/nuget/v/Breakdance.Blazor?label=&logo=NuGet&style=for-the-badge
[bd-mstest-rtm-nuget-img]: https://img.shields.io/nuget/v/Breakdance.Extensions.MSTest2?label=&logo=NuGet&style=for-the-badge
[bd-tools-rtm-nuget-img]: https://img.shields.io/nuget/v/Breakdance.Tools?label=&logo=NuGet&style=for-the-badge
[bd-webapi-rtm-nuget-img]: https://img.shields.io/nuget/v/Breakdance.WebApi?label=&logo=NuGet&style=for-the-badge

[bd-aspnetcore-ci-nuget-img]: https://img.shields.io/nuget/vpre/Breakdance.AspNetCore?label=&logo=NuGet&style=for-the-badge
[bd-aspnetcore-sigr-ci-nuget-img]: https://img.shields.io/nuget/vpre/Breakdance.AspNetCore.SignalR?label=&logo=NuGet&style=for-the-badge
[bd-assemblies-ci-nuget-img]: https://img.shields.io/nuget/vpre/Breakdance.Assemblies?label=&logo=NuGet&style=for-the-badge
[bd-blazor-ci-nuget-img]: https://img.shields.io/nuget/vpre/Breakdance.Blazor?label=&logo=NuGet&style=for-the-badge
[bd-mstest-ci-nuget-img]: https://img.shields.io/nuget/vpre/Breakdance.Extensions.MSTest2?label=&logo=NuGet&style=for-the-badge
[bd-tools-ci-nuget-img]: https://img.shields.io/nuget/vpre/Breakdance.Tools?label=&logo=NuGet&style=for-the-badge
[bd-webapi-ci-nuget-img]: https://img.shields.io/nuget/vpre/Breakdance.WebApi?label=&logo=NuGet&style=for-the-badge
