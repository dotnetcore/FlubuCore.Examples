# README #

### Flubu examples ###

These examples will help you to get quickly start with FlubuCore:
* [.NET Framework build example](https://github.com/flubu-core/examples/blob/master/MVC_NET4.61/BuildScripts/BuildScript.cs
) - Example covers versioning, building the project, running tests, packaging application for deployment.

* [.NET Core build example](https://github.com/flubu-core/examples/blob/master/NetCore_csproj/BuildScript/BuildScript.cs
) - Example covers versioning, building the project, running tests, packaging application for deployment.

* [Deployment script example](https://github.com/flubu-core/examples/blob/master/DeployScriptExample/BuildScript/DeployScript.cs
) - Example shows how to write simple deployment script. 


### How do I get set up (.net 4.6)? ###

* Clone source code.
* Examine build script project in MVC_NET4.61 solution especialy buildscript.cs 

* In cmd run flubu.exe to run default build action. Build (example) will compile code, run unit tests, create iis web appilcation and package example application into zip for deployment.


* run flubu.exe help to see other available build actions.
* see other examples for diferent flubu use cases.
* see FlubuCore wiki for how to get started with flubu

### How do I get set up (.net core)? ###
#### .net core csproj ####
* Clone source code.
* Examine build script project in .Netcore.csproj folder especialy buildscript.cs 

* In cmd navigate to NetCore_csproj folder
* run dotnet restore buildscript.csproj
* run dotnet flubu to run default build action. run dotnet flubu to see all available targets
* see other examples for diferent flubu use cases.
* see FlubuCore wiki for how to get started with flubu
#### .net core xproj ####
* Clone source code.
* Examine build script project in .Netcore.xproj folder  especialy buildscript.cs 

* In cmd navigate to NetCore_xproj folder
* run dotnet restore
* run dotnet flubu to run default build action. run dotnet flubu to see all available targets
* see other examples for diferent flubu use cases.
* see FlubuCore wiki for how to get started with flubu
