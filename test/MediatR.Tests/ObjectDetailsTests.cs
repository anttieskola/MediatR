using MediatR.Internal;
using MediatR.Tests.ObjectDetailsTestAssets.NsCompare;
using MediatR.Tests.ObjectDetailsTestAssets.Other;
using MediatR.Tests.ObjectDetailsTestAssets.YetAnother;
using Shouldly;
using System;
using Xunit;

namespace MediatR.Tests;

public class ObjectDetailsTests
{
    // --- Constructor ---

    [Fact]
    public void ConstructorShouldStoreValueAndType()
    {
        NsCompareRequest value = new();

        ObjectDetails details = new(value);

        ReferenceEquals(details.Value, value).ShouldBeTrue();
        details.Type.ShouldBe(value.GetType());
    }

    [Fact]
    public void ConstructorShouldSetNameFromType()
    {
        ObjectDetails details = new(new NsCompareRequest());

        details.Name.ShouldBe(nameof(NsCompareRequest));
    }

    [Fact]
    public void ConstructorShouldSetAssemblyName()
    {
        ObjectDetails details = new(new NsCompareRequest());

        details.AssemblyName.ShouldBe(typeof(NsCompareRequest).Assembly.GetName().Name);
    }

    [Fact]
    public void ConstructorShouldSetLocationFromNamespace()
    {
        ObjectDetails details = new(new NsCompareRequest());

        details.Location.ShouldBe("ObjectDetailsTestAssets.NsCompare");
    }

    [Fact]
    public void ConstructorShouldSetNullLocation_WhenTypeHasNoNamespace()
    {
        ObjectDetails details = new(new GlobalNamespaceAsset());

        details.Location.ShouldBeNull();
    }

    [Fact]
    public void ConstructorShouldDefaultIsOverriddenToFalse()
    {
        ObjectDetails details = new(new NsCompareRequest());

        details.IsOverridden.ShouldBeFalse();
    }

    // --- Compare: null handling ---

    [Fact]
    public void CompareShouldReturnPositive_WhenXIsNull()
    {
        ObjectDetails comparer = new(new NsCompareRequest());
        ObjectDetails y = new(new NsCompareHandler());

        int result = comparer.Compare(null, y);

        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareShouldReturnNegative_WhenYIsNull()
    {
        ObjectDetails comparer = new(new NsCompareRequest());
        ObjectDetails x = new(new NsCompareHandler());

        int result = comparer.Compare(x, null);

        result.ShouldBeLessThan(0);
    }

    // --- Compare: equality ---

    [Fact]
    public void CompareShouldReturnZero_WhenComparingSameInstance()
    {
        ObjectDetails comparer = new(new NsCompareRequest());
        ObjectDetails x = new(new NsCompareHandler());

        comparer.Compare(x, x).ShouldBe(0);
    }

    [Fact]
    public void CompareShouldReturnZero_WhenComparingTwoInstancesOfSameType()
    {
        ObjectDetails comparer = new(new NsCompareRequest());
        ObjectDetails x = new(new NsCompareHandler());
        ObjectDetails y = new(new NsCompareHandler());

        comparer.Compare(x, y).ShouldBe(0);
    }

    [Fact]
    public void CompareShouldReturnZero_WhenLocationIsNull()
    {
        ObjectDetails comparer = new(new NsCompareRequest());
        ObjectDetails noNamespace = new(new GlobalNamespaceAsset());
        ObjectDetails withNamespace = new(new NsCompareHandler());

        // A handler whose type has no namespace (null Location) cannot be ordered by
        // namespace/location, so it compares as equal once assembly no longer distinguishes.
        comparer.Compare(noNamespace, withNamespace).ShouldBe(0);
        comparer.Compare(withNamespace, noNamespace).ShouldBe(0);
    }

    // --- Compare: assembly priority ---

    [Fact]
    public void CompareShouldPrioritizeHandlerInRequestAssembly()
    {
        ObjectDetails request = new(new NsCompareRequest());
        ObjectDetails sameAssembly = new(new NsCompareHandler());
        ObjectDetails otherAssembly = new(new Unit());

        typeof(NsCompareRequest).Assembly.GetName().Name
            .ShouldNotBe(typeof(Unit).Assembly.GetName().Name,
                "these types must live in different assemblies for the test to be meaningful");

        request.Compare(sameAssembly, otherAssembly).ShouldBeLessThan(0);
        request.Compare(otherAssembly, sameAssembly).ShouldBeGreaterThan(0);
    }

    // --- Compare: namespace priority ---

    [Fact]
    public void CompareShouldPrioritizeHandlerInRequestNamespace()
    {
        ObjectDetails request = new(new NsCompareRequest());
        ObjectDetails sameNamespace = new(new NsCompareHandler());
        ObjectDetails otherNamespace = new(new OtherNamespaceHandler());

        request.Compare(sameNamespace, otherNamespace).ShouldBeLessThan(0);
        request.Compare(otherNamespace, sameNamespace).ShouldBeGreaterThan(0);
    }

    // --- Compare: location fallback ---

    [Fact]
    public void CompareShouldFallBackToLocation_WhenNamespacesDoNotMatchRequest()
    {
        ObjectDetails request = new(new NsCompareRequest());
        ObjectDetails shorterLocation = new(new OtherNamespaceHandler());
        ObjectDetails longerLocation = new(new YetAnotherNamespaceHandler());

        // Neither namespace matches the request's namespace, so the comparison falls back to
        // location length: the longer (more specific) location has higher priority.
        request.Compare(shorterLocation, longerLocation).ShouldBeGreaterThan(0);
        request.Compare(longerLocation, shorterLocation).ShouldBeLessThan(0);
    }

    // --- Compare: comparer contract ---

    [Fact]
    public void CompareShouldBeAntisymmetric()
    {
        ObjectDetails comparer = new(new NsCompareRequest());
        ObjectDetails[] details =
        [
            new ObjectDetails(new NsCompareRequest()),
            new ObjectDetails(new NsCompareHandler()),
            new ObjectDetails(new OtherNamespaceHandler()),
            new ObjectDetails(new Unit()),
        ];

        for (int i = 0; i < details.Length; i++)
        {
            for (int j = 0; j < details.Length; j++)
            {
                int compareXy = comparer.Compare(details[i], details[j]);
                int compareYx = comparer.Compare(details[j], details[i]);

                compareXy.ShouldBe(-compareYx);
            }
        }
    }

    [Fact]
    public void CompareShouldDefineAConsistentOrderForSorting()
    {
        ObjectDetails comparer = new(new NsCompareRequest());
        ObjectDetails[] handlers =
        [
            new ObjectDetails(new OtherNamespaceHandler()),
            new ObjectDetails(new NsCompareHandler()),
            new ObjectDetails(new Unit()),
            new ObjectDetails(new NsCompareRequest()),
        ];

        Array.Sort(handlers, comparer);

        for (int i = 0; i < handlers.Length; i++)
        {
            for (int j = i + 1; j < handlers.Length; j++)
            {
                comparer.Compare(handlers[i], handlers[j]).ShouldBeLessThanOrEqualTo(0);
            }
        }
    }
}
