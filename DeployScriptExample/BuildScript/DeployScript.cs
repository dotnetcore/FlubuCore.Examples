using System;
using System.IO;
using System.Linq;
using FlubuCore.Context;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Iis;

namespace BuildScript
{
   
    //// Deploy script can be executed with or without flubu web api. If u want to execute deploy script
    //// remotely execute it with flubu web api otherwise if u want to execute it locally execute it with flubu runner or dotnet cli tool.
    //// see wiki for executing script remotely with flubu web api https://github.com/flubu-core/flubu.core/wiki/6-Web-Api:-Getting-started
    public class DeployScript : DefaultBuildScript
    {
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
        }

        protected override void ConfigureTargets(ITaskContext session)
        {
            session.CreateTarget("deploy")
                .AddTask(x =>
                    x.IisTasks().CreateAppPoolTask("Example app pool")
                        .Mode(CreateApplicationPoolMode.DoNothingIfExists))
                .AddTask(x =>
                    x.IisTasks().ControlAppPoolTask("Example app pool", ControlApplicationPoolAction.Stop)
                        .DoNotFailOnError())
                .Do(UnzipPackage)
                .AddTask(x => x.CopyDirectoryStructureTask(@"Packages\ExampleApp", @"C:\ExampleApp", true).Retry(20, 5000))
                .Do(CreateWebSite)
                .AddTask(x => x.IisTasks().ControlAppPoolTask("Example app pool", ControlApplicationPoolAction.Start));


            session.CreateTarget("deployTest3");

        }

        public void UnzipPackage(ITaskContext context)
        {
            var files = Directory.EnumerateFiles("packages", "*.zip").ToList();
            string zip = files[0];
            context.Tasks().UnzipTask(zip, "packages").Execute(context);
        }

        public void CreateWebSite(ITaskContext context)
        {
            context.Tasks().IisTasks().CreateWebsiteTask()
                .ApplicationPoolName("Example app pool")
                .WebsiteName("Example web site")
                .BindingProtocol("Http")
                .Port(3080)
                .PhysicalPath("C:\\ExampleApp")
                .WebsiteMode(CreateWebApplicationMode.DoNothingIfExists).Execute(context);
        }
    }
}
