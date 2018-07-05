using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FlubuCore.Scripting;
using Xunit;

namespace BuildScript.BuildScript
{
    /// <summary>
    /// You can debug scripts with tests.
    /// You can use FlubuEngine to use Flubu tasks in any other .net library or application.
    /// </summary>
    public class BuildScriptTests
    {
        [Fact]
        public void TestTargetExample()
        {
            if (File.Exists("test123.txt"))
            {
                File.Delete("test123.txt");
            }

            IFlubuEngine engine = new FlubuEngine();

            var session = engine.CreateTaskSession(new BuildScriptArguments
            {
                MainCommands = new List<string>()
                {
                    "TestAndDebugExample"
                }
            });

            BuildScriptForTestsAndDebugExample bs = new BuildScriptForTestsAndDebugExample();

            var result = bs.Run(session);
            Assert.Equal(0, result);
            Assert.True(File.Exists("test123.txt"));
        }

        [Fact]
        //// You can just test your code in do (Custom code)
        public void TestDoExample()
        {
            if (File.Exists("test123.txt"))
            {
                File.Delete("test123.txt");
            }

            BuildScriptForTestsAndDebugExample bs = new BuildScriptForTestsAndDebugExample();
            
            IFlubuEngine engine = new FlubuEngine();

            var session = engine.CreateTaskSession(new BuildScriptArguments());
            bs.CreateFile(session);
            Assert.True(File.Exists("test123.txt"));
        }
    }
}
