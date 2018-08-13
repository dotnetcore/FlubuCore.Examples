using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Scripting;
using FlubuCore.Tasks;
using FlubuCore.Tasks.Iis;
using Newtonsoft.Json;
using Oracle.DataAccess.Client;

//#imp ./Scripts/DiffMatchPatch.cs
//#ass .\lib\System.Net.Http.dll
//#ass .\lib\System.Data.dll
//#ass .\lib\Oracle.DataAccess.dll
namespace BuildScript
{
    /// <summary>
    /// This is quite complex example of deploy script from real project. If u run the build script it won't work.
    /// It is here that u get an idea what can be done with FlubuCore.
    /// Some sensitive data have been masked.
    /// </summary>
    public class WebDeployScript : DefaultBuildScript
    {
        private const string TestDbConnectionString = "****";

        private const string ProdDbConnectionString = "***";

        private const string WebAppBaseUrlTest = "http://FlubuCore.tst";

        private const string WebAppBaseUrlPreProd = "http://FlubuCore.lan:1080";

        private const string WebAppBaseUrlProd = "http://FlubuCore.lan";

        private bool _anyDiffsInConfigs = false;

        private bool _failOnDiff = false;

        [FromArg("n2")]
        public string N2Table { get; set; } = null;

        [FromArg("oi")]
        public bool OnlyN2Import { get; set; } = false;

        [FromArg("Node")]
        public int Node { get; set; } = 1;
       
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
            if (context.ScriptArgs.ContainsKey("FailOnDiff"))
            {
                _failOnDiff = bool.Parse(context.ScriptArgs["FailOnDiff"]);
         
            }
        }

        protected override void ConfigureTargets(ITaskContext session)
        {
            //  var deleteN2Tables = session.CreateTarget("Delete.N2Tables").Do(DeleteN2Tables);

            session.CreateTarget("deployTest")
                .Do(Deploy, @"C:\Web\App", $"C:\\WebApp\\Backup\\web_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip",
                    "web_AppPool", "Test", true, TestDbConnectionString);

            session.CreateTarget("deployTest2")
                .Do(Deploy, @"C:\\WebApp", $"C:\\WebApp\\Backup\\web_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip",
                    "web_AppPool", "Test", false, TestDbConnectionString);

            session.CreateTarget("deployPreproduction")
                .Do(Deploy, @"C:\WebAppP", $"C:\\WebApp\\Backup\\web_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip",
                    "web_AppPool_Preprod", "Preproduction", true, ProdDbConnectionString);

            session.CreateTarget("deployPreProduction2")
                .Do(Deploy, @"C:\WebP", $"C:\\WebApp\\Backup\\web_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip",
                    "web_AppPool_preprod", "Preproduction", false, ProdDbConnectionString);

            session.CreateTarget("deployProduction")
                .Do(Deploy, @"C:\Web", $"C:\\WebApp\\Backup\\web_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip",
                    "web_AppPool", "Production", true, ProdDbConnectionString);

            session.CreateTarget("deployProduction2")
                .Do(Deploy, @"C:WebApp\", $"C:\\WebApp\\Backup\\web_{DateTime.Now:yyyy-M-dd--HH-mm-ss}.zip",
                    "web_AppPool", "Production", false, ProdDbConnectionString);
        }

        public void Deploy(ITarget target, string deployPath, string backupFullFilePath, string appPoolName, string prepConfEvniroment, bool importN2Table, string dbConnectionString)
        {
            var webConfigPath = Path.Combine(deployPath, "web.config");
            var webConfigOldBackupPath = @"Backup\Web\web.config";
            string oldN2TablePrefix = "N/A";
            oldN2TablePrefix = GetOldN2TablePrefix(target, webConfigPath, oldN2TablePrefix, webConfigOldBackupPath);

             target.Group(t =>
                {
                    t.Do(SwitchStatusForLb, prepConfEvniroment, options => { options.DoNotFailOnError(); })
                        .AddTask(x => x.Sleep(5000))
                        .AddTask(x => x.IisTasks().ControlAppPoolTask(appPoolName, ControlApplicationPoolAction.Stop))
                        .DoAsync((Action<ITaskContext>)UnzipPackage)
                        .DoAsync((Action<ITaskContext, string, string>)Backup, deployPath, backupFullFilePath)
                        .AddTask(x => x.RunProgramTask(@"Packages\WebA[[\prepconf.bat")
                            .WithArguments(prepConfEvniroment)
                            .WorkingFolder("Packages\\WebApp"))
                        .Do(DiffAllConfigs, deployPath)
                        .AddTask(x => x.CopyFileTask(webConfigPath, webConfigOldBackupPath, true).DoNotFailOnError()) //// web config must be backed up because next deploys might depend on it if current deploy fails.
                        .AddTask(x => x.DeleteDirectoryTask(deployPath, false).Retry(20, 5000))
                        .AddTask(x => x.CreateDirectoryTask(deployPath, false).DoNotFailOnError())
                        .AddTask(x => x.CopyDirectoryStructureTask(@"Packages\\Webapp", deployPath, true)
                                .Retry(5, 5000).NoLog());
                },
                onFinally: c =>
                {
                    c.Tasks().IisTasks().ControlAppPoolTask(appPoolName, ControlApplicationPoolAction.Start)
                        .Retry(10, 15000).Execute(c);
                    if (OnlyN2Import)
                    {
                        new DoTask2<string>(SwitchStatusForLb, prepConfEvniroment).Execute(c);
                    }
                    new DoTask2<string>(CheckLbStatus, prepConfEvniroment).Execute(c);
                },
                when: c => !OnlyN2Import);
          
            //// Package has to be unziped because we need n2 export file 
            target.Do(UnzipPackage).When(c => OnlyN2Import);
            

            if (N2Table == "NoImport")
            {
                importN2Table = false;
                N2Table = oldN2TablePrefix;
                target.Do(c => { c.LogInfo("No N2Import. N2Table prefix will be set to the value that the old scp had."); }).When((c) => oldN2TablePrefix != "N/A");
                target.Do(c => { c.LogInfo("WARNING: N2 Table prefix is set to N/A. "); }).When((c) => oldN2TablePrefix == "N/A");
            }

            if (N2Table == "DifferentFromCurrent")
            {
                switch (oldN2TablePrefix)
                {
                    case "N2":
                    {
                        N2Table = "N3";
                        break;
                    }
                    case "N3":
                    {
                        N2Table = "N2";
                        break;
                    }
                }
            }

            target.AddTask(x => x.UpdateXmlFileTask(webConfigPath).UpdatePath("//n2/database/@tablePrefix", N2Table));

            if (importN2Table)
            {
                var webBaseUrl = prepConfEvniroment == "Test" ? WebAppBaseUrlTest : WebAppBaseUrlProd;

                target.Do(DeleteN2Tables, dbConnectionString);
                target.Do(ImportN2DataFromXml, webBaseUrl);
            }
            else
            {
                target.Do((c) => c.LogInfo("Skipping n2 import"));
            }

            target.Do(UpdateWebConfig, dbConnectionString, deployPath);
            target.Do(c => { File.Delete(webConfigOldBackupPath); });
        }

        public void Backup(ITaskContext context, string sourceDir, string destinationFileName)
        {
            if (Directory.Exists(sourceDir))
            {
                context.LogInfo($"Zipping dir:{sourceDir} to: {destinationFileName}");
                using (var zip = new Ionic.Zip.ZipFile())
                {
                    zip.AddDirectory(sourceDir);
                    zip.Save(destinationFileName);
                }
            }
            else
            {
                context.LogInfo($"SourceDir {sourceDir} not found. Skiping backup.");
            }
        }

        public void DeleteN2Tables(ITaskContext context, string dbConnectionString)
        {
            var script = string.Format("BEGIN {0} END;", File.ReadAllText($"Scripts\\{N2Table}_CMS_DeleteData.sql"));
            script = script.Replace("\r\n", "\n");
            using (var oracleConnection = new OracleConnection(dbConnectionString))
            {
                oracleConnection.Open();

                using (var command = new OracleCommand(script) { Connection = oracleConnection })
                {
                    command.CommandType = CommandType.Text;
                    context.LogInfo($"Started deleting n2 tables with prefix {N2Table}.");
                    command.ExecuteNonQuery();
                    context.LogInfo("Deleted n2 tables.");
                }
            }
        }

        public void ImportN2DataFromXml(ITaskContext context, string WebAppBaseUrl)
        {
            CustomWebClient client = new CustomWebClient();
            var n2ExportFiles = Directory.GetFiles(@"Packages\WebApp\Content\N2Exports", "*.xml");
            client.BaseAddress = WebAppBaseUrl;
            context.LogInfo($"Found file {n2ExportFiles[0]}");
            var n2ExportFile = Path.GetFullPath(n2ExportFiles[0]);
            context.LogInfo($"Started importing n2 data to {N2Table} table.");
            var resposne = client.DownloadString($"/n2export/import?filename={n2ExportFile}");
            context.LogInfo("n2 data imported.");
        }

        public void UpdateWebConfig(ITaskContext context, string dbConnectionString, string deployPath)
        {
            string rootId = "0";
            string startPageId = "0";
            context.LogInfo("looking for RootId and StartPageId in database.");
            using (var oracleConnection = new OracleConnection(dbConnectionString))
            {
                oracleConnection.Open();

                using (var command = new OracleCommand() {Connection = oracleConnection})
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = $"select id from {N2Table}item where type = 'RootBase'";
                    var data = command.ExecuteReader();
                    while (data.Read())
                    {
                        rootId = data.GetValue(0).ToString();
                    }
                }

                using (var command = new OracleCommand() {Connection = oracleConnection})
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = $"select id from {N2Table}item where type = 'StartPage'";
                    var data = command.ExecuteReader();
                    while (data.Read())
                    {
                        startPageId = data.GetValue(0).ToString();
                    }
                }
            }

            context.LogInfo($"rootId {rootId} and StartPageId {startPageId} found.");

            context.Tasks().UpdateXmlFileTask(Path.Combine(deployPath, "web.config"))
                .UpdatePath("configuration/n2/host/@rootID", rootId)
                .UpdatePath("configuration/n2/host/@startPageID", startPageId).Execute(context);
        }

        public void UnzipPackage(ITaskContext context)
        {
            var files = Directory.EnumerateFiles("packages\\WebA[[", "*.zip").ToList();
            string zip = files[0];
            context.Tasks().UnzipTask(zip, "packages\\WebApp").NoLog().Execute(context);
        }

        public void DiffAllConfigs(ITaskContext context, string deployPath)
        {
            Diff(context, ".\\packages\\WebApp\\Windsor.Config",
                Path.Combine(deployPath, "Windsor.Config"),
                ".\\Reports\\WebReports\\Windsor.Config.html");

            Diff(context, ".\\packages\\WebApp\\web.config",
                Path.Combine(deployPath, "web.config"),
                ".\\Reports\\WebAppReports\\web.config.html");

            if (_failOnDiff && _anyDiffsInConfigs)
            {
                throw new TaskExecutionException("There were some changes in config(s). review changes", 99);
            }
        }

        public void Diff(ITaskContext context, string newConfigPath, string oldConfigPath, string htmlExportPath)
        {
            string newConfig = File.ReadAllText(newConfigPath);
            if (Contains(newConfig, "todo", StringComparison.InvariantCultureIgnoreCase) ||
                Contains(newConfig, "tbd", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new TaskExecutionException(
                    $"Config: '{newConfigPath}' contains todo or tbd! Check and fix config.", 99);
            }

            if (!File.Exists(oldConfigPath))
            {
                context.LogInfo($"old config not found '{oldConfigPath}'. Skipping compare.");
                return;
            }

            context.LogInfo($"Comparing {oldConfigPath}, {newConfigPath}");

            var oldCOnf = File.ReadAllText(oldConfigPath);
            diff_match_patch dmp = new diff_match_patch();
            dmp.Diff_EditCost = 4;
            List<Diff> diff = dmp.diff_main(oldCOnf, newConfig);

            if (diff.Count == 1)
            {
                if (diff[0].operation == Operation.EQUAL)
                {
                    context.LogInfo("Configs are the same");
                    return;
                }
            }
           
            _anyDiffsInConfigs = true;
            context.LogInfo($"Configs are not the same. Generating report {htmlExportPath}.");

            dmp.diff_cleanupSemantic(diff);
            var html = dmp.diff_prettyHtml(diff);

            File.WriteAllText(htmlExportPath, html);
        }

        public void SwitchStatusForLb(ITaskContext context, string enviroment)
        {
            if (enviroment != "Production" && enviroment != "Test")
            {
                context.LogInfo($"Switch healthcheck http status for enviroment {enviroment} is not turned on.");
                return;
            }

            WebClient client = new WebClient();
            var request = new {secretKey = "HRM.uL^aa!X;+-o=_/_3!x56}iyw)R" };
            var dataString = JsonConvert.SerializeObject(request);
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.BaseAddress = WebApiBaseUrl(enviroment);
            var resposne = client.UploadString("HealthCheck/Lb/SwitchStatus", dataString);
            context.LogInfo($"Switched healthcheck http status for enviroment '{enviroment}'.");
        }

        public void CheckLbStatus(ITaskContextInternal context, string enviroment)
        {
            if (enviroment != "Production" && enviroment != "Test")
            {
                context.LogInfo($"Switch healthcheck http status for enviroment {enviroment} is not turned on.");
                return;
            }

            WebClient client = new WebClient();
            client.BaseAddress = WebApiBaseUrl(enviroment);
            var resposne = client.DownloadString("HealthCheck/Lb");
        }

        private static string GetOldN2TablePrefix(ITarget target, string webConfig, string oldN2TablePrefix, string backupWebConfig)
        {
            if (File.Exists(webConfig))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(webConfig);
                oldN2TablePrefix = xml.SelectSingleNode("//n2/database/@tablePrefix")?.Value;
            }
            else if (File.Exists(backupWebConfig))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(backupWebConfig);
                oldN2TablePrefix = xml.SelectSingleNode("//n2/database/@tablePrefix")?.Value;
            }
            else
            {
                target.Do(c => { c.LogInfo("Warning: old web config not found!"); });
            }

            return oldN2TablePrefix;
        }

        public string WebApiBaseUrl(string enviroment)
        {
            switch (enviroment)
            {
                case "Production":
                {
                    if (Node == 2)
                    {
                        return "http://flubucore1.api.lan/";
                    }

                    return "http://flubucore2.api.lan/";
                }
                case "Test":
                {
                    if (Node == 2)
                    {
                        return "http://flubucore2.api.tst/";
                    }

                    return "http://flubucore2.api.tst/";
                }
                default:
                    throw new NotSupportedException($"Enviroment not supported.");
            }
        }

        public static bool Contains(string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public class CustomWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 20 * 60 * 1000;
                return w;
            }
        }
    }
}
