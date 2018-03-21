using System;
using System.IO;

namespace Eshopworld.DevOps.Tests
{
    public class TestsFixture : IDisposable
    {
        public TestsFixture()
        {
            var secret = Environment.GetEnvironmentVariable("DEVOPSFLEX-TESTS-KVSECRET",EnvironmentVariableTarget.Machine);
            File.WriteAllText(Path.Combine(EswDevOpsSdkTests.AssemblyDirectory, "appsettings.KV.json"), $"{{\"KeyVaultName\": \"devopsflex-tests\",  \"KeyVaultClientId\": \"848c5ccc-8dad-4f0a-885d-1c50ab17f611\",\"KeyVaultClientSecret\": \"{secret}\"}}");
        }

        public void Dispose()
        {
            File.Delete(Path.Combine(Path.GetDirectoryName(EswDevOpsSdkTests.AssemblyDirectory) ?? throw new InvalidOperationException(), "appsettings.KV.json"));
        }
    }
}
