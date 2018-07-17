using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WDTA2.Models;

namespace WDTA2.Data
{
    public class RoleSeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var roles = new[] { Constants.OwnerRole, Constants.FranchHolderRoleCbd, Constants.FranchHolderRoleNorth, Constants.CustomerRole};

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole
                    {
                        Name = role
                    });
                }
            }

            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            await EnsureUserHasRole(userManager, "peachlsy@gmail.com", Constants.OwnerRole);
            await EnsureUserHasRole(userManager, "owner@qq.com", Constants.OwnerRole);
            await EnsureUserHasRole(userManager, "s3590747@student.rmit.edu.au", Constants.FranchHolderRoleCbd);
            await EnsureUserHasRole(userManager, "s1234567@student.rmit.edu.au", Constants.FranchHolderRoleNorth);
            await EnsureUserHasRole(userManager, "414659976@qq.com", Constants.FranchHolderRoleCbd);
            await EnsureUserHasRole(userManager, "north@qq.com", Constants.FranchHolderRoleNorth);
            await EnsureUserHasRole(userManager, "south@qq.com", Constants.FranchHolderRoleSouth);
            await EnsureUserHasRole(userManager, "west@qq.com", Constants.FranchHolderRoleWest);
            await EnsureUserHasRole(userManager, "east@qq.com", Constants.FranchHolderRoleEast);
            await EnsureUserHasRole(userManager, "customer@qq.com", Constants.CustomerRole);

        }


        private static async Task EnsureUserHasRole(
            UserManager<ApplicationUser> userManager, string userName, string role)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user != null && !await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}

