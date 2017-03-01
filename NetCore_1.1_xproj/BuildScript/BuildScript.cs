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

        context
            .CreateTarget("rebuild")
            .SetDescription("Compiles the VS solution")
            .CoreTaskExtensions()
            .DotnetRestore(x => x.WithArguments("FlubuExample"))
            .DotnetBuild("FlubuExample")
            .DotnetPublish("FlubuExample")
            .CreateZipPackageFromProjects("FlubuExample", "netcoreapp1.0", "FlubuExample" );
    }

    private void UpdateFlubuCoreNugetPackageToLatest(ITaskContext context)
    {
        var FetchVersionTask = context.Tasks().FetchBuildVersionFromFileTask();

        FetchVersionTask.ProjectVersionFileName(@"..\FlubuCore.ProjectVersion.txt");
        var version = FetchVersionTask.Execute(context);
        context.Tasks().UpdateJsonFileTask("project.json")
            .Update(@"tools.dotnet-flubu.version", version.ToString(3))
            .Update(@"dependencies.FlubuCore", version.ToString(3))
            .Execute(context);

    }
}