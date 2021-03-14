using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Tests.Assemblies.SampleApis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Breakdance.Tests.Assemblies
{

    [TestClass]
    public class DependencyInjectionTestHelpersTests
    {

        [TestMethod]
        public void DependencyInjection_OutputsCorrectLog()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<SomeEventArgs>();
            collection.AddScoped<SomeStringList>();

            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(collection);
            result.Should().NotBeNullOrWhiteSpace();

            var baseline = File.ReadAllText("..//..//..//Baselines/SimpleDIContainer.txt");
            result.Should().Be(baseline);
        }

        [BreakdanceManifestGenerator]
        public void WriteDependencyInjectionOutputLog(string path)
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<SomeEventArgs>();
            collection.AddScoped<SomeStringList>();

            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(collection);
            var fullPath = Path.Combine(path, "Baselines//SimpleDIContainer.txt");
            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);

        }


    }
}
