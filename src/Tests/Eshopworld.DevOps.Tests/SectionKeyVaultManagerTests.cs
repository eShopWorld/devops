using Eshopworld.DevOps.Configuration;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Azure.KeyVault.Models;
using Xunit;

namespace Eshopworld.DevOps.Tests
{
    public class SectionKeyVaultManagerTests
    {
        [Fact, IsUnit]
        public void Test_AllSecretsAreLoaded()
        {
            //arrange
            var sut = new SectionKeyVaultManager();
            //assert
            sut.Load(new SecretItem()).Should().BeTrue();
        }

        [Theory, IsUnit]
        [InlineData("https://rmtestkeyvault.vault.azure.net:443/secrets/a-b-c", "b:c")]
        [InlineData("https://rmtestkeyvault.vault.azure.net:443/secrets/a-b", "b")]
        [InlineData("https://rmtestkeyvault.vault.azure.net:443/secrets/a", "a")]
        [InlineData("https://rmtestkeyvault.vault.azure.net:443/secrets/a-", "a-")]
        public void Test_NamingStructure(string secretId, string expectedKey)
        {
            //arrange
            var sut = new SectionKeyVaultManager();
            //assert
            sut.GetKey(new SecretBundle("val", secretId))
                .Should().Be(expectedKey);
        }

    }
}
