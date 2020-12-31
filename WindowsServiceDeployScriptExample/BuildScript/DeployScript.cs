using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Utils;

namespace BuildScript
{
  public class TaskServiceDeployScript : DefaultBuildScript
    {
        private const string WindowsServiceName = "WindowsService.Example";

        /// <summary>
        /// Before you can deploy you have to run 'Rebuild' target in BuildScript.cs first so that the deployment package is created.
        /// </summary>
        /// <param name="context"></param>
        protected override void ConfigureTargets(ITaskContext context)
        {
            context.CreateTarget("deployTest")
                .AddTasks(Deploy, @"C:\WindowsService\Service", $"C:\\WindowsService\\Backup\\WindowsService_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip");
            
            context.CreateTarget("deployProduction")
                .AddTasks(Deploy, @"D:\WindowsService\Service", $"D:\\WindowsService\\Backup\\WindowsService_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip");
        }

        public void Deploy(ITarget target, string deployPath, string backupFullFilePath)
        {
            target.AddTask(x => x.ControlService(StandardServiceControlCommands.Stop, WindowsServiceName)
                            .DoNotFailOnError())
                        .Do(UnzipPackage)
                        .Do(Backup, deployPath, backupFullFilePath, x => { x.DoNotFailOnError(); })
                        .AddTask(x => x.DeleteDirectoryTask(deployPath, false).Retry(30, 5000))
                        .AddTask(x => x.CreateDirectoryTask(deployPath, false))
                        .AddTask(x => x.CopyDirectoryStructureTask(@"Deploy\\WindowsService", deployPath, true).NoLog()
                            .Retry(30, 5000))
                        .AddTask(x => x.ControlService(StandardServiceControlCommands.Start, WindowsServiceName)
                            .DoNotFailOnError());
        }


        public void UnzipPackage(ITaskContext context)
        {
            var files = Directory.EnumerateFiles("output", "*.zip").ToList();
            string zip = files[0];
            context.Tasks().UnzipTask(zip, "deploy\\WindowsService").NoLog().Execute(context);
        }

        public void Backup(ITaskContext context, string sourceDir, string destinationFileName)
        {
            ////if (Directory.Exists(sourceDir))
            ////{
            ////    context.LogInfo($"Zipping dir:{sourceDir} to: {destinationFileName}");
            ////    using (var zip = new Ionic.Zip.ZipFile())
            ////    {
            ////        zip.AddDirectory(sourceDir);
            ////        zip.Save(destinationFileName);
            ////    }
            ////}
            ////else
            ////{
            ////    context.LogInfo($"SourceDir {sourceDir} not found. Skiping backup.");
            ////}
        }
    }
}
