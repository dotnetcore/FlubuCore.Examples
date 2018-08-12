using System;
using System.Threading.Tasks;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Packaging;
using FlubuCore.Packaging.Filters;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Iis;
using Newtonsoft.Json;
using RestSharp;

//#ass .\packages\Newtonsoft.Json.9.0.1\lib\net45\Newtonsoft.Json.dll
//#nuget RestSharp, 106.3.1
//#imp .\BuildScripts\BuildHelper.cs

/// <summary>
/// In this build script default targets(compile, generate common assembly info etc are included with  context.Properties.SetDefaultTargets(DefaultTargets.Dotnet);///
/// Type "build.exe help in cmd to see help
/// Examine build scripts in other projects for more use cases.
/// </summary>
public class BuildScriptSimple : DefaultBuildScript
{

    //// Exexcute 'build.exe -ex={SomeValue}.'. to pass argument to property. You can also set 'ex' through config file or enviroment variable. See https://github.com/flubu-core/examples/tree/master/ArgumentAndConfigurationPassThroughToTasksExample
    [FromArg("ex", "Just an example." )]
    public string PassArgumentExample { get; set; }

    protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
    {
        context.Properties.Set(BuildProps.NUnitConsolePath,
            @"packages\NUnit.ConsoleRunner.3.6.0\tools\nunit3-console.exe");
        context.Properties.Set(BuildProps.ProductId, "FlubuExample");
        context.Properties.Set(BuildProps.ProductName, "FlubuExample");
        context.Properties.Set(BuildProps.SolutionFileName, "FlubuExample.sln");
        context.Properties.Set(BuildProps.BuildConfiguration, "Release");
        //// Remove SetDefaultTarget's if u dont't want default targets to be included or if you want to define them by yourself.
        context.Properties.SetDefaultTargets(DefaultTargets.Dotnet);
    }

    protected override void ConfigureTargets(ITaskContext session)
    {
        var updateVersion = session.CreateTarget("update.version")
            .Do(TargetFetchBuildVersion)
            .SetAsHidden();

        var unitTest = session.CreateTarget("unit.tests")
            .SetDescription("Runs unit tests")
            .AddTask(x => x.NUnitTaskForNunitV3("FlubuExample.Tests"))
            .DependsOn("load.solution");

        var package = session.CreateTarget("Package")
            .SetDescription("Packages mvc example for deployment")
            .Do(TargetPackage);

        session.CreateTarget("Rebuild")
            .SetDescription("Rebuilds the solution.")
            .SetAsDefault()
            .DependsOn("compile") //// compile is included as one of the default targets.
            .DependsOn(unitTest, package);

        //// Below are dummy examples of what flubu can do.

        var runExternalProgramExample = session.CreateTarget("run.libz")
            .AddTask(x => x.RunProgramTask(@"packages\LibZ.Tool\1.2.0\tools\libz.exe"));
        //// Pass any arguments...
        //// .WithArguments());

        var refExample = session.CreateTarget("RefExample").Do(RefExample);

        session.CreateTarget("iis.install").Do(IisInstall);

        session.CreateTarget("ReuseSetOfTargetsExample")
            .AddTasks(ReuseSetOfTargetsExample, "Dir1", "Dir2")
            .AddTasks(ReuseSetOfTargetsExample, "Dir3", "Dir4");

        session.CreateTarget("AsyncExample")
            .AddTaskAsync(x => x.CreateDirectoryTask("Test", true))
            .AddTaskAsync(x => x.CreateDirectoryTask("Test2", true))
            .Do(RefExample)
            .DoAsync(AsyncExample, "exampleValue1")
            .DoAsync(AsyncExample, "examplevalue2")
            .DependsOnAsync(refExample, runExternalProgramExample);
    }

    public async Task AsyncExample(ITaskContext context, string exampleParamInDoTask)
    {
        await Task.Delay(100);
    }

    public void RefExample(ITaskContext context)
    {
        //// Just an example that using of other .cs files work.
        BuildHelper.SomeMethod();

        //// Just an example that referencing external assemblies work.
        var exampleSerialization = JsonConvert.SerializeObject("Example serialization");
        var client = new RestClient("http://example.com");
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

    public static void TargetFetchBuildVersion(ITaskContext context)
    {
        var version = context.Tasks().FetchBuildVersionFromFileTask().Execute(context);
     
        int svnRevisionNumber = 0; //in real scenario you would fetch revision number from subversion.
        int buildNumber = 0; // in real scenario you would fetch build version from build server.
        version = new Version(version.Major, version.Minor, buildNumber, svnRevisionNumber);
        context.Properties.Set(BuildProps.BuildVersion, version);
    }

    public static void TargetPackage(ITaskContext context)
    {
        FilterCollection installBinFilters = new FilterCollection();
        installBinFilters.Add(new RegexFileFilter(@".*\.xml$"));
        installBinFilters.Add(new RegexFileFilter(@".svn"));

        context.Tasks().PackageTask("builds")
            .AddDirectoryToPackage("FlubuExample", "FlubuExample", false, new RegexFileFilter(@"^.*\.(svc|asax|aspx|config|js|html|ico|bat|cgn)$").NegateFilter())
            .AddDirectoryToPackage("FlubuExample\\Bin", "FlubuExample\\Bin", false, installBinFilters)
            .AddDirectoryToPackage("FlubuExample\\Content", "FlubuExample\\Content", true)
            .AddDirectoryToPackage("FlubuExample\\Images", "FlubuExample\\Images", true)
            .AddDirectoryToPackage("FlubuExample\\Scripts", "FlubuExample\\Scripts", true)
            .AddDirectoryToPackage("FlubuExample\\Views", "FlubuExample\\Views", true)
            .ForMember(x => x .ZipPackage("FlubuExample.zip", true, 3), "-fn", "Zip package file name.")
            .Execute(context);
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
}
