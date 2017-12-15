using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Iis;
using Newtonsoft.Json;

//#ref System.Xml.XmlDocument, System.Xml.XmlDocument, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//#ass ./Packages/Newtonsoft.Json.9.0.1/lib/netstandard1.0/Newtonsoft.Json.dll
//#imp ./BuildScript/BuildScriptHelper.cs

//// Examine build scripts in other projects(especialy mvc .net461 example) for more use cases. Also see FlubuCore buildscript on https://github.com/flubu-core/flubu.core/blob/master/BuildScript/BuildScript.cs
public class MyBuildScript : DefaultBuildScript
{
    protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
    {
        context.Properties.Set(BuildProps.CompanyName, "Flubu");
        context.Properties.Set(BuildProps.CompanyCopyright, "Copyright (C) 2010-2016 Flubu");
        context.Properties.Set(BuildProps.ProductId, "FlubuCoreExample");
        context.Properties.Set(BuildProps.ProductName, "FlubuCoreExample");
    }

    protected override void ConfigureTargets(ITaskContext context)
    {
        context.CreateTarget("Fetch.FlubuCore.Version")
            .Do(UpdateFlubuCoreNugetPackageToLatest);

        var compile = context
            .CreateTarget("compile")
            .SetDescription("Compiles the VS solution")
            .AddCoreTask(x => x.ExecuteDotnetTask("restore").WithArguments("FlubuExample.sln"))
            .CoreTaskExtensions().DotnetBuild("FlubuExample.sln")
            .BackToTarget();

        var package = context
            .CreateTarget("Package")
            .CoreTaskExtensions()
            .DotnetPublish("FlubuExample")
            .CreateZipPackageFromProjects("FlubuExample", "netstandard1.6", "FlubuExample")
            .BackToTarget();

        //// Can be used instead of CreateZipPackageFromProject. See MVC_NET4.61 project for full example of PackageTask
        //// context.CreateTarget("Package2").AddTask(x => x.PackageTask("FlubuExample"));

        var test = context.CreateTarget("test")
            .AddCoreTaskAsync(x => x.Test().WorkingFolder("FlubuExample.Tests"))
            .AddCoreTaskAsync(x => x.Test().WorkingFolder("FlubuExample.Tests2"));

        var doExample = context.CreateTarget("DoExample").Do(DoExample);
        var doExample2 = context.CreateTarget("DoExample2").Do(DoExample2);

        context.CreateTarget("iis.install").Do(IisInstall);

        //// todo include package into rebuild.
        context.CreateTarget("Rebuild")
            .SetAsDefault()
            .DependsOnAsync(doExample, doExample2)
            .DependsOn(compile, test, package);
    }

    public static void IisInstall(ITaskContext context)
    {
        context.Tasks().IisTasks()
            .CreateAppPoolTask("SomeAppPoolName")
            .ManagedRuntimeVersion("No Managed Code")
            .Mode(CreateApplicationPoolMode.DoNothingIfExists)
            .Execute(context);

        context.Tasks().IisTasks()
            .CreateWebsiteTask()
            .WebsiteName("SomeWebSiteName")
            .BindingProtocol("Http")
            .Port(2000)
            .PhysicalPath("SomePhysicalPath")
            .ApplicationPoolName("SomeAppPoolName")
            .WebsiteMode(CreateWebApplicationMode.DoNothingIfExists)
            .Execute(context);
    }

    private void UpdateFlubuCoreNugetPackageToLatest(ITaskContext context)
    {
        var fetchBuildVersionFromFileTask = context.Tasks().FetchBuildVersionFromFileTask();

        fetchBuildVersionFromFileTask.ProjectVersionFileName(@"..\FlubuCore.ProjectVersion.txt");
        var version = fetchBuildVersionFromFileTask.Execute(context);
        context.Tasks()
                .UpdateXmlFileTask("BuildScript.csproj")
                .UpdatePath("//DotNetCliToolReference[@Version]/@Version", version.ToString(3))
                .Execute(context);
    }

    private void DoExample(ITaskContext context)
    {
        XmlDocument xml = new XmlDocument(); //// Just an a example that external reference works.
        BuildScriptHelper.SomeMethod(); //// Just an a example that referencing other cs file works.

        ////Example of predefined propertie. Propertie are predefined by flubu.
        var osPlatform = context.Properties.Get<OSPlatform>(PredefinedBuildProperties.OsPlatform);

        if (osPlatform == OSPlatform.Windows)
        {
            context.LogInfo("Running on windows");
        }
        else if(osPlatform ==OSPlatform.Linux)
        {
            context.LogInfo("running on linux");
        }
    }

    private void DoExample2(ITaskContext context)
    {
        //// run 'dotnet flubu Rebuild -argName=SomeValue' to pass argument
        var example = context.ScriptArgs["argName"];
        if (string.IsNullOrEmpty(example))
        {
            example = "no vaule through script argument argName";
        }


        JsonConvert.SerializeObject(example);
    }
}