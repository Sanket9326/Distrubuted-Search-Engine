using Common.Utilities;
using Contracts;

namespace SearchService.Tests;

public sealed class DepartmentParserTests
{
    [Fact]
    public void Parse_CommaSeparatedString_CombinesFlags()
    {
        var result = DepartmentParser.Parse("Finance,Engineering");

        Assert.Equal(Department.Finance | Department.Engineering, result);
    }

    [Fact]
    public void Parse_CommaSeparatedString_IgnoresUnknownTokens()
    {
        var result = DepartmentParser.Parse("Finance,NotARealDepartment");

        Assert.Equal(Department.Finance, result);
    }

    [Fact]
    public void Parse_NullOrWhitespaceString_ReturnsNone()
    {
        Assert.Equal(Department.None, DepartmentParser.Parse((string?)null));
        Assert.Equal(Department.None, DepartmentParser.Parse("   "));
    }

    [Fact]
    public void Parse_StringEnumerable_CombinesFlags()
    {
        var result = DepartmentParser.Parse(new[] { "Finance", "Engineering" });

        Assert.Equal(Department.Finance | Department.Engineering, result);
    }

    [Fact]
    public void Parse_StringEnumerable_IgnoresGarbageTokens()
    {
        var result = DepartmentParser.Parse(new[] { "Finance", "garbage", "" });

        Assert.Equal(Department.Finance, result);
    }

    [Fact]
    public void Parse_NullEnumerable_ReturnsNone()
    {
        Assert.Equal(Department.None, DepartmentParser.Parse((IEnumerable<string>?)null));
    }
}
