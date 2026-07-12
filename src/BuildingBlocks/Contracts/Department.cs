namespace Contracts;

[Flags]
public enum Department
{
    None = 0,
    HumanResources = 1 << 0,
    Finance = 1 << 1,
    Engineering = 1 << 2,
    Legal = 1 << 3,
    Sales = 1 << 4,
    Marketing = 1 << 5,
    Operations = 1 << 6,
    ExecutiveManagement = 1 << 7,
    All = HumanResources | Finance | Engineering | Legal | Sales | Marketing | Operations | ExecutiveManagement
}
