# Dotnet CQRS REPR pattern CRUD Templates

Auto-generates basic CRUD endpoints, Commands/Queries, Repository, and tests. This repository only works for my [Dotnet 8 CQRS Template project](https://github.com/AJax2012/Dotnet-8-CQRS-Template), but feel free to fork and adjust your paths and file templates according to your desires. Currently, there is an issue using it as a dotnet command, but you can run it from an IDE with a debugger.

## Usage

Currently, uses my local path, but eventually, will be a dotnet tool. Should be able to type `dotnet generateEndpointTemplates` in the desired directory and follow the instructions in your termainal. For a dry run to see your file output, run `dotnet generateEndpointTemplates -u`.
