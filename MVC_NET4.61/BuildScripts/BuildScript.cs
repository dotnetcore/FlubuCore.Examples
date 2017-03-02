using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Packaging;
using FlubuCore.Packaging.Filters;
using FlubuCore.Scripting;
//#ref System.Xml.XmlDocument, System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089

/// <summary>
/// Flubu build script example for .net. Flubu Default targets for .net are not included. 
/// Most of them are created in this buildScipt tho. (load.solution, generate,commonAssinfo, compile) as a build script example
/// With Default targets included see BuildScriptWithDt.cs.
/// </summary>
public class BuildScript : DefaultBuildScript
{
    protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
    {
        context.Properties.Set(BuildProps.NUnitConsolePath,
            @"packages\NUnit.ConsoleRunner.3.6.0\tools\nunit3-console.exe");
        context.Properties.Set(BuildProps.ProductId, "FlubuExample");
        context.Properties.Set(BuildProps.ProductName, "FlubuExample");
        context.Properties.Set(BuildProps.SolutionFileName, "FlubuExample.sln");
        context.Properties.Set(BuildProps.BuildConfiguration, "Release");
    }

    protected override void ConfigureTargets(ITaskContext session)
    {
        var loadSolution = session.CreateTarget("load.solution")
            .SetAsHidden()
            .AddTask(x => x.LoadSolutionTask());

        var updateVersion = session.CreateTarget("update.version")
            .DependsOn(loadSolution)
            .SetAsHidden()
            .Do(TargetFetchBuildVersion);

        session.CreateTarget("generate.commonassinfo")
           .SetDescription("Generates common assembly info")
            .DependsOn(updateVersion)
           .TaskExtensions().GenerateCommonAssemblyInfo();

        var compile = session.CreateTarget("compile")
            .SetDescription("Compiles the solution.")
            .AddTask(x => x.CompileSolutionTask())
            .DependsOn("generate.commonassinfo");
        
        var unitTest = session.CreateTarget("unit.tests")
            .SetDescription("Runs unit tests")
            .DependsOn(loadSolution)
            .AddTask(x => x.NUnitTaskForNunitV3("FlubuExample.Tests"));

        var runExternalProgramExample = session.CreateTarget("abc").AddTask(x => x.RunProgramTask(@"packages\LibZ.Tool\1.2.0\tools\libz.exe"));

        var refAssemblyExample = session.CreateTarget("Referenced.Assembly.Example").Do(TargetReferenceAssemblyExample);

        var package = session.CreateTarget("Package")
            .SetDescription("Packages mvc example for deployment")
            .Do(TargetPackage);

        session.CreateTarget("Rebuild")
            .SetDescription("Rebuilds the solution.")
            .SetAsDefault()
            .DependsOn(compile, unitTest, package, refAssemblyExample);
    }

    public static void TargetFetchBuildVersion(ITaskContext context)
    {
        var version = context.Tasks().FetchBuildVersionFromFileTask().Execute(context);
        int svnRevisionNumber = 0; //in real scenario you would fetch revision number from subversion.
        int buildNumber = 0; // in real scenario you would fetch build version from build server.
        version = new System.Version(version.Major, version.Minor, buildNumber, svnRevisionNumber);
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
            .ZipPackage("FlubuExample.zip")
            .Execute(context);
    }

    public void TargetReferenceAssemblyExample(ITaskContext context)
    {
        XmlDocument xml = new XmlDocument();
    }
}
