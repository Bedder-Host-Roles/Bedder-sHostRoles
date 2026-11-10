
using BHR.Roles.Core;

namespace BHR.Tests.Roles;

public class GeneralRoleTests
{
    [Fact]
    public void All_Role_Ids_Are_Unqiue()
    {
        var roles = CustomRoleManager.RoleClass.Values;
        HashSet<int> seen = new();

        foreach (var role in roles)
        {
            Assert.DoesNotContain<int>(role.Id);
            seen.Add(role.Id);
        } 
    }
}