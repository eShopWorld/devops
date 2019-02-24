using System;
using System.Linq;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

// ReSharper disable once CheckNamespace
public class DeploymentRegionTests
{
    [Fact, IsUnit]
    public void WestEurope_StringChecks()
    {
        DeploymentRegion we = DeploymentRegion.WestEurope;
        we.ToRegionName().Should().Be("West Europe");
        we.ToRegionCode().Should().Be("WE");
    }

    [Fact, IsUnit]
    public void EastUS_StringChecks()
    {
        DeploymentRegion eus = DeploymentRegion.EastUS;
        eus.ToRegionName().Should().Be("East US");
        eus.ToRegionCode().Should().Be("EUS");
    }

    [Fact, IsUnit]
    public void UnrecognizedValue()
    {
        DeploymentRegion sut = (DeploymentRegion) 666;
        Assert.Throws<ArgumentException>(() => sut.ToRegionName());
        Assert.Throws<ArgumentException>(() => sut.ToRegionCode());
    }

    [Fact, IsUnit]
    public void EnsureAllItemsHaveDescriptor()
    {
        foreach (var field in typeof(DeploymentRegion).GetFields().Where(fi => !fi.IsSpecialName))
        {
            var regionDescriptor = (RegionDescriptorAttribute) field.GetCustomAttributes(
                typeof(RegionDescriptorAttribute),
                false).First();

            regionDescriptor.ToString().Should().NotBeNullOrWhiteSpace();
            regionDescriptor.ToShortString().Should().NotBeNullOrWhiteSpace();
        }
    }
}

