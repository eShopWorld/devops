using System;
using System.IO;
using System.Reflection;
using Eshopworld.DevOps.Configuration;
using Eshopworld.Tests.Core;
using Xunit;
using FluentAssertions;

namespace Eshopworld.DevOps.Tests
{
    /// <inheritdoc />
    /// <summary>
    /// tests for <see cref="T:Eshopworld.DevOps.OpusSDK" />
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class OpusSDKTests : IClassFixture<TestsFixture>
    {
        [Fact, IsIntegration]
        public void Test_ReadFromCoreAppSettings()
        {
            //arrange
            var sut = OpusSDK.BuildConfiguration(AssemblyDirectory);
            //assert
            sut["KeyRootAppSettings"].Should().BeEquivalentTo("AppSettingsValue");
        }


        [Fact, IsIntegration]
        public void Test_ReadFromTestAppSettings()

        {

            //arrange
            var sut = OpusSDK.BuildConfiguration(AssemblyDirectory, useTest: true);
            //assert
            sut["KeyTestAppSettings"].Should().BeEquivalentTo("TestAppSettingsValue");
        }



        [Fact, IsIntegration]
        public void Test_ReadFromEnvironmentalAppSettings()

        {
            //arrange
            var sut = OpusSDK.BuildConfiguration(AssemblyDirectory, "ENV1");
            //assert
            sut["KeyENV1AppSettings"].Should().BeEquivalentTo("ENV1AppSettingsValue");
        }


        [Fact, IsIntegration]
        public void Test_ReadFromEnvironmentalVariable()
        {
            //arrange
            var sut = OpusSDK.BuildConfiguration(AssemblyDirectory);
            //assert
            sut["PATH"].Should().NotBeNullOrEmpty();
        }


        [Fact, IsIntegration]
        public void Test_ReadFromKeyVault()
        {
            //arrange
            var sut = OpusSDK.BuildConfiguration(AssemblyDirectory);
            //assert
            sut["keyVaultItem"].Should().BeEquivalentTo("keyVaultItemValue");
        }



        /// <summary>
        /// the test runner will shadow copy the assemblies so to resolve the config files this is needed
        /// </summary>
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
