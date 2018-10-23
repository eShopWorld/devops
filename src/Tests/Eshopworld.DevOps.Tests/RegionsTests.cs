
// ReSharper disable once CheckNamespace

using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

public class RegionsTests
{
    [Fact, IsDev]
    public void WestEurope_StringChecks()
    {
        Regions we = Regions.WestEurope;
        we.ToRegionName().Should().Be("West Europe");
        we.ToRegionCode().Should().Be("WE");
    }

    [Fact, IsDev]
    public void EastUS_StringChecks()
    {
        Regions eus = Regions.EastUS;
        eus.ToRegionName().Should().Be("East US");
        eus.ToRegionCode().Should().Be("EUS");
    }
}

