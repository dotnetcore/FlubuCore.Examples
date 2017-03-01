using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlubuCore.Context;
using FlubuCore.Scripting;

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
            .AddCoreTask(x => x.ExecuteDotnetTask("restore").WithArguments("FlubuExample"))
            .CoreTaskExtensions().DotnetBuild("FlubuExample")
            .BackToTarget();

        var package = context
            .CreateTarget("Package")
            .CoreTaskExtensions()
            .DotnetPublish("FlubuExample");

        var test = context.CreateTarget("test").AddCoreTask(x => x.Test().WorkingFolder("FlubuExample.Tests"));

        //// todo include compile and package into rebuild
        context.CreateTarget("Rebuild")
            .SetAsDefault()
            .DependsOn(compile);
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
}