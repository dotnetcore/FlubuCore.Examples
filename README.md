# README #

### Flubu example ###

* Example of flubu usage. Flubu is for building projects and executing deployment scripts using C# code.


### How do I get set up (.net 4.6)? ###

* Clone source code.
* Examine build script project in MVC_NET4.61 solution especialy buildscript.cs 

* In cmd run build.exe to run default build action. Build (example) will compile code, run unit tests, create iis web appilcation and package example application into zip for deployment.


* run build.exe help to see other available build actions.
* see other examples for diferent flubu use cases.
* see FlubuCore wiki for how to get started with flubu

### How do I get set up (.net core)? ###
#### .net core csproj ####
* Clone source code.
* Examine build script project in .Netcore_1.1.csproj folder especialy buildscript.cs 

* In cmd navigate to NetCore_csproj folder
* run dotnet restore buildscript.csproj
* run dotnet flubu to run default build action. run dotnet flubu to see all available targets
* see other examples for diferent flubu use cases.
* see FlubuCore wiki for how to get started with flubu
#### .net core xproj ####
* Clone source code.
* Examine build script project in .Netcore_1.1.xproj folder  especialy buildscript.cs 

* In cmd navigate to NetCore_xproj folder
* run dotnet restore
* run dotnet flubu to run default build action. run dotnet flubu to see all available targets
* see other examples for diferent flubu use cases.
* see FlubuCore wiki for how to get started with flubu
