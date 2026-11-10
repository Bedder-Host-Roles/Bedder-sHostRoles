
using BHR.Roles.Core;
using System.Reflection;

namespace BHR.Tests.Roles;

public class GeneralRoleTests
{
    [Fact]
    public void All_Role_Ids_Are_Unique()
    {
        var ids = CustomRoleManager.RoleClass.Values
            .Select(role => role.GetType())
            .Distinct()
            .Select(type =>
            {
                var field = type.GetField(
                    "Id",
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

                Assert.NotNull(field);
                Assert.True(field.IsLiteral);
                return (int)field.GetRawConstantValue()!;
            })
            .ToArray();

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }
}