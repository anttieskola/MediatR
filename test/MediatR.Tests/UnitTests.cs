using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class UnitTests
{
    [Fact]
    public async Task Should_be_equal_to_each_other()
    {
        Unit unit1 = Unit.Value;
        Unit unit2 = await Unit.Task;

        Assert.Equal(unit1, unit2);
        Assert.True(unit1 == unit2);
        Assert.False(unit1 != unit2);
    }

    [Fact]
    public void Should_be_equitable()
    {
        Dictionary<Unit, string> dictionary = new()
        {
            {new Unit(), "value"},
        };

        Assert.Equal("value", dictionary[default]);
    }

    [Fact]
    public void Should_tostring()
    {
        Unit unit = Unit.Value;
        Assert.Equal("()", unit.ToString());
    }

    [Fact]
    public void Should_compareto_as_zero()
    {
        Unit unit1 = new();
        Unit unit2 = new();

        Assert.Equal(0, unit1.CompareTo(unit2));
    }

    [Fact]
    public void Should_be_equal_as_true()
    {
        Unit unit1 = new();
        Unit unit2 = new();

        Assert.True(unit1 == unit2);
    }

    [Fact]
    public void Should_be_not_equal_as_false()
    {
        Unit unit1 = new();
        Unit unit2 = new();

        Assert.False(unit1 != unit2);
    }

    [Fact]
    public void Should_be_less_than_as_false()
    {
        Unit unit1 = new();
        Unit unit2 = new();

        Assert.False(unit1 < unit2);
    }

    [Fact]
    public void Should_be_less_or_equal_than_as_true()
    {
        Unit unit1 = new();
        Unit unit2 = new();

        Assert.True(unit1 <= unit2);
    }

    [Fact]
    public void Should_be_greater_than_as_false()
    {
        Unit unit1 = new();
        Unit unit2 = new();

        Assert.False(unit1 > unit2);
    }

    [Fact]
    public void Should_be_greater_or_equal_than_as_true()
    {
        Unit unit1 = new();
        Unit unit2 = new();

        Assert.True(unit1 >= unit2);
    }

    public static object[][] ValueData() =>
        [
            [new object(), false],
            ["", false],
            ["()", false],
            [null!, false],
            [new Uri("https://www.google.com"), false],
            [new Unit(), true],
            [Unit.Value, true],
            [Unit.Task.Result, true],
            [default(Unit), true],
        ];

    public static object[][] CompareToValueData()
        => [.. ValueData().Select(objects => new[] { objects[0] })];

    [Theory]
    [MemberData(nameof(ValueData))]
    public void Should_be_equal(object value, bool isEqual)
    {
        Unit unit1 = Unit.Value;

        if (isEqual)
        {
            Assert.True(unit1.Equals(value));
        }
        else
        {
            Assert.False(unit1.Equals(value));
        }
    }

    [Theory]
    [MemberData(nameof(CompareToValueData))]
    public void Should_compareto_value_as_zero(object value)
    {
        Unit unit1 = new();

        Assert.Equal(0, ((IComparable) unit1).CompareTo(value));
    }
}