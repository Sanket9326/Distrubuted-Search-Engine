using Contracts;

namespace Common.Utilities;

public static class DepartmentParser
{
    public static Department Parse(string? commaSeparatedDepartments)
    {
        if (string.IsNullOrWhiteSpace(commaSeparatedDepartments))
        {
            return Department.None;
        }

        return Parse(commaSeparatedDepartments.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
    }

    public static Department Parse(IEnumerable<string>? departmentTokens)
    {
        if (departmentTokens is null)
        {
            return Department.None;
        }

        var result = Department.None;
        foreach (var token in departmentTokens)
        {
            if (Enum.TryParse<Department>(token, ignoreCase: true, out var department))
            {
                result |= department;
            }
        }

        return result;
    }
}
