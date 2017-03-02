using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;

namespace FlubuExample.Tests
{
    public class TestExamples
    {
        [Test]
        public void TestExample()
        {
            var flName = typeof(XmlDocument).Assembly.FullName;
            Assert.AreEqual(1, 1);
        }
    }
}
