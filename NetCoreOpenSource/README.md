## Building open source projects with FlubuCore

This is a simple example of a build script with FlubuCore for a .NET Core open source project. Build script can be also used as a template.

It covers most common build steps that are needed in an open source project: Fetching build version from a file or using GitVersion, updating projects version,  building projects in the solution, running tests, packing and publishing a nuget package. It also covers how to setup build in Appveyor, Travis CI, Github Actions and Azure devops. 

- [Travis configuration file](https://github.com/dotnetcore/FlubuCore.Examples/blob/master/.travis.yml) Travis build sample also includes publishing of nuget package. 
- [AppVeyor configuration file](https://github.com/dotnetcore/FlubuCore.Examples/blob/master/appveyor.yml)
- [Github Actions](https://github.com/dotnetcore/FlubuCore.Examples/blob/master/.github/workflows/build.yml)
- [Azure DevOps](https://github.com/dotnetcore/FlubuCore.Examples/blob/master/azure-pipelines.yml)

### Running script locally

- .net core sdk 2.1 or greater is required
- Install FlubuCore global tool with command: 'dotnet tool install --global FlubuCore.GlobalTool --version 4.2.8'
- run 'Flubu Rebuild' in the folder where NetCoreOpenSource.sln is located.

For .net core 2.0 and lower use FlubuCore cli tool instead of FlubuCore global tool.

GitVersion - https://gitversion.readthedocs.io/en/latest/
