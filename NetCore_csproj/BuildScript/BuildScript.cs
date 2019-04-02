using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Context.FluentInterface.TaskExtensions;
using FlubuCore.Scripting;
using FlubuCore.Scripting.Attributes;
using FlubuCore.Tasks.Iis;
using FluentMigrator;
using Newtonsoft.Json;
using RestSharp;

//// Examine build scripts in other projects for more use cases.
[Include("./BuildScript/BuildScriptHelper.cs")]
public class MyBuildScript : DefaultBuildScript
{

    //// Exexcute 'dotnet flubu -ex={SomeValue}.'. to pass argument to property. You can also set 'ex' through config file or enviroment variable. See https://github.com/flubu-core/examples/tree/master/ArgumentAndConfigurationPassThroughToTasksExample
    [FromArg("ex", "Just an example." )]
    public string PassArgumentExample { get; set; }

    protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
    {
        context.Properties.Set(BuildProps.CompanyName, "Flubu");
        context.Properties.Set(BuildProps.CompanyCopyright, "Copyright (C) 2010-2016 Flubu");
        context.Properties.Set(BuildProps.ProductId, "FlubuExample");
        context.Properties.Set(BuildProps.ProductName, "FlubuExample");
        context.Properties.Set(BuildProps.SolutionFileName, "FlubuExample.sln");
		context.Properties.Set(BuildProps.BuildConfiguration, "Release");
    }

    protected override void ConfigureTargets(ITaskContext context)
    {
        var buildVersion = context.CreateTarget("buildVersion")
            .SetAsHidden()
            .SetDescription("Fetches flubu version from FlubuExample.ProjectVersion.txt file.")
            .AddTask(x => x.FetchBuildVersionFromFileTask());

        var compile = context
            .CreateTarget("compile")
            .SetDescription("Compiles the VS solution and sets version to FlubuExample.csproj")
            .AddCoreTask(x => x.UpdateNetCoreVersionTask("FlubuExample/FlubuExample.csproj"))
            .AddCoreTask(x => x.Restore())
            .AddCoreTask(x => x.Build())
            .DependsOn(buildVersion);

        var package = context
            .CreateTarget("Package")
            .AddCoreTask(x => x.Publish("FlubuExample"))
            .AddCoreTask(x => x.CreateZipPackageFromProjects("FlubuExample", "netstandard2.0", "FlubuExample"));

        //// Can be used instead of CreateZipPackageFromProject. See MVC_NET4.61 project for full example of PackageTask
        //// context.CreateTarget("Package2").AddTask(x => x.PackageTask("FlubuExample"));

        ///// Tasks are runned in parallel. You can do the same with DoAsync and DependsOnAsync and you can also mix Async and Sync tasks
        var test = context.CreateTarget("test")
            .AddCoreTaskAsync(x => x.Test().Project("FlubuExample.Tests"))
            .AddCoreTaskAsync(x => x.Test().Project("FlubuExample.Tests2"));

        var runExternalProgramExample = context.CreateTarget("run.libz")
            .AddTask(x => x.RunProgramTask(@"packages\LibZ.Tool\1.2.0\tools\libz.exe"));
        //// Pass any arguments...
        //// .WithArguments());

        var doExample = context.CreateTarget("DoExample").Do(DoExample);
        var doExample2 = context.CreateTarget("DoExample2").Do(DoExample2, "SomeValue");

        context.CreateTarget("ReuseSetOfTargetsExample")
            .AddTasks(ReuseSetOfTargetsExample, "Dir1", "Dir2")
            .AddTasks(ReuseSetOfTargetsExample, "Dir3", "Dir4");

        context.CreateTarget("iis.install").Do(IisInstall);
        
        context.CreateTarget("Rebuild")
            .SetAsDefault()
            .DependsOnAsync(doExample, doExample2)
            .DependsOn(compile, test, package);
    }

    private void DoExample(ITaskContext context)
    {
        BuildScriptHelper.SomeMethod(); //// Just an a example that referencing other cs file works.
    }

    private void DoExample2(ITaskContext context, string param)
    {
        //// run 'dotnet flubu Rebuild -ex=SomeValue' to pass argument
        string example = PassArgumentExample;
        if (string.IsNullOrEmpty(example))
        {
            example = "no vaule passed through script argument 'ex'.";
        }

        context.LogInfo(example);

        //// Just an a example that referencing nuget package works.
        JsonConvert.SerializeObject(example);
        var client = new RestClient("http://example.com");

        //// Just an a example that referencing by assmbly works (Fluent migrator)
        AddLogTable logTable = new AddLogTable();

    }

    //// See deployment example for real use case. You can also apply attribute Target on method. https://github.com/flubu-core/flubu.core/wiki/2-Build-script-fundamentals#Targets
    private void ReuseSetOfTargetsExample(ITarget target, string directoryName, string directoryName2)
    {
        //// Retry, When, OnError, Finally, ForMember, NoLog, DoNotFailOnError can be applied on all tasks.
        target.AddTask(x =>
                x.CreateDirectoryTask(directoryName, true).OnError((c, e) => c.LogInfo("Dummy example of onError.")))
            .When(c => true)
            .AddTask(x => x.CreateDirectoryTask(directoryName2, true).Finally(c => c.LogInfo("Dummy example of finally.")))
            ////You can group task and apply When, OnError, Finally on group of tasks. .
            .Group(
                t =>
                {
                    t.AddTask(x => x.DeleteDirectoryTask(directoryName, false).DoNotFailOnError().NoLog());
                    t.AddTask(x => x.DeleteDirectoryTask(directoryName2, true).Retry(3, 1000));
                },
                onFinally: c =>
                {
                    c.LogInfo("Dummy example of OnFinally and When on group of tasks.");
                },
                when: c => true
            );
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
            //// Example of ForMember. Can be used on any task method or property.
            //// execute 'dotnet flubu iis.install --appPool={SomeValue}'. If argument is not passed default value is used in this case 'DefaultAppPollName'
            .ForMember(x => x.ApplicationPoolName("DefaultAppPollName"), "appPool", "Name of the application pool.")
            .ApplicationPoolName("SomeAppPoolName")
            .WebsiteMode(CreateWebApplicationMode.DoNothingIfExists)
            .Execute(context);
    }

    [Migration(20180430121800)]
    public class AddLogTable : Migration
    {
        public override void Up()
        {
            Create.Table("Log")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Text").AsString();
        }

        public override void Down()
        {
            Delete.Table("Log");
        }
    }
}