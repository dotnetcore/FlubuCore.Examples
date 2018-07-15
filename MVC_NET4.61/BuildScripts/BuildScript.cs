using System;
using System.Threading.Tasks;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Packaging;
using FlubuCore.Packaging.Filters;
using FlubuCore.Scripting;
using Newtonsoft.Json;
using RestSharp;

//#ass .\packages\Newtonsoft.Json.9.0.1\lib\net45\Newtonsoft.Json.dll
//#nuget RestSharp, 106.3.1
//#imp .\BuildScripts\BuildHelper.cs

/// <summary>
/// In this build script default targets(compile, generate common assembly info etc are included with  context.Properties.SetDefaultTargets(DefaultTargets.Dotnet);///
/// Type "build.exe help -s=BuildScriptSimple.cs  in cmd to see help
/// See other examples at https://github.com/flubu-core/examples
/// </summary>
public class BuildScriptSimple : DefaultBuildScript
{

    //// Exexcute 'build.exe -ex={SomeValue}.'. to pass argument to property. You can also set 'ex' through config file or enviroment variable.
    [FromArg("ex", "Just and example" )]
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

       var test = session.CreateTarget("test").Do(ReuseOfTargetExample, "FlubuExample.Tests");
       var test2 = session.CreateTarget("test2").Do(ReuseOfTargetExample, "FlubuExample.Tests2");

        var runExternalProgramExample = session.CreateTarget("run.libz")
            .AddTask(x => x.RunProgramTask(@"packages\LibZ.Tool\1.2.0\tools\libz.exe"));
        //// Pass any arguments...
        //// .WithArguments());

        var refExample = session.CreateTarget("RefExample").Do(RefExample);

        session.CreateTarget("AsyncExample")
            .AddTaskAsync(x => x.CreateDirectoryTask("Test", true)
                .Retry(3)
                .Finally((c) =>
                {
                    c.LogInfo("Do something on finally ");
                })
                .OnError((c, e) =>
                {
                    c.LogInfo("Do something on error");
                }))
            .AddTaskAsync(x => x.CreateDirectoryTask("Test2", true)
                .DoNotFailOnError())
            .Do(RefExample).When((c) => true)
            .DoAsync(AsyncExample)
            .DoAsync(AsyncExample)
            .DependsOnAsync(test, test2);
    }
    
    /// See deployment example for real scenario example
    public void ReuseOfTargetExample(ITarget target, string projectToTest)
    {
        target
            .AddTask(x => x.CompileSolutionTask())
            .AddTask(x => x.NUnitTaskForNunitV3(projectToTest));
    }

    public async Task AsyncExample(ITaskContext context)
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
}
