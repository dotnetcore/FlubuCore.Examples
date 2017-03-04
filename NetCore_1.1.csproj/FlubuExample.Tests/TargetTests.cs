using System.Xml;
using Xunit;
using NUnit;
namespace Flubu.Tests
{
    public class TargetTests
    {
             [Fact]
        public void Test()
        {
            var flName = typeof(XmlDocument).AssemblyQualifiedName;
        }

        [Fact]
        public void Test2()
        {
            Assert.Equal(1, 1);
            var flName = typeof(NUnit.Framework.Assert).AssemblyQualifiedName;
        }

        [Fact]
        public void Test3()
        {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void Test4()
        {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void Test5()
        {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void Test6()
        {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void Test7()
        {
            Assert.Equal(1, 1);
        }
    }
}