using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Scripting;
using Newtonsoft.Json;

///This works
//#ref System.Xml.XmlDocument, System.Xml.XmlDocument, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//#ass .\packages\Newtonsoft.Json.9.0.1\lib\netstandard1.0\Newtonsoft.Json.dll

//// Exampine build scripts in other projects for more use cases
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
            .DotnetPublish("FlubuExample");

        var test = context.CreateTarget("test")
            .AddCoreTaskAsync(x => x.Test().WorkingFolder("FlubuExample.Tests"))
            .AddCoreTaskAsync(x => x.Test().WorkingFolder("FlubuExample.Tests2"));

        var doExample = context.CreateTarget("DoExample").Do(DoExample);
        var doExample2 = context.CreateTarget("DoExample2").Do(DoExample2);

        //// todo include package into rebuild.
        context.CreateTarget("Rebuild")
            .SetAsDefault()
            .DependsOnAsync(doExample, doExample2)
            .DependsOn(compile, test);
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
        XmlDocument xml = new XmlDocument();
    }

    private void DoExample2(ITaskContext context)
    {
        JsonConvert.SerializeObject("test");
    }
}