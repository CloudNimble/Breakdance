# Testier for Microsoft's Restier Platform
A testing framework for the Restier OData platform.

## Introduction
Testier is a set of helpers that make it easier to build unit tests for your Restier-based APIs. Because Restier uses a set of built-in conventions to reflect over your API and _automagically_ find the methods it needs, it can be difficult to know what methods the system is expecting to find.

Testier uses a copy of the same convention lookup code, and the same method naming patterns, to generate a list of all possible method names. It can also generate a "Visibility Matrix" for your API, showing you which ones it expected, and which ones it found. That matrix looks something like this:

```
--------------------------------------------------
Function Name                            |   Found
--------------------------------------------------
CanFilterSport                           |   False
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
CanFilterTeam                            |   False
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
CanFilterPlayer                          |   False
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
CanExecuteTestMethod                     |   False
OnExecutingTestMethod                    |   False
OnExecutedTestMethod                     |   False
--------------------------------------------------
```

The included unit tests show how to take this information, and build tests that compare a baseline against your current version. That way, if something changes, your tests break, and you're forced to understand what changed.

## Installation

You can install Testier from NuGet by opening up Package Manager and typing `install-package AdvancedREI.Restier.Testier -pre`. Please note that the current version requires AdvancedREI's flavor of Restier, which we are currently shipping because Microsoft has not officially updated their NuGet packages in some time.

## Known Issues

 - Testier can't currently map OData functions that are bound to datasets. If you know how to do this, please feel free to submit a PR.