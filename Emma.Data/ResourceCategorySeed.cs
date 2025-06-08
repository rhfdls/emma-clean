using Emma.Data.Models;

namespace Emma.Data;

public static class ResourceCategorySeed
{
    public static List<ResourceCategory> GetDefaultCategories()
    {
        return new List<ResourceCategory>
        {
            new ResourceCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Mortgage Lender/Broker",
                Description = "Mortgage lenders and brokers who help clients secure financing for real estate purchases",
                IconName = "bank",
                SortOrder = 1,
                IsActive = true
            },
            new ResourceCategory
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Building Inspector",
                Description = "Professional inspectors who evaluate property conditions for buyers and sellers",
                IconName = "search",
                SortOrder = 2,
                IsActive = true
            },
            new ResourceCategory
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Real Estate Lawyer",
                Description = "Legal professionals specializing in real estate transactions and property law",
                IconName = "gavel",
                SortOrder = 3,
                IsActive = true
            },
            new ResourceCategory
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Collaborator",
                Description = "Team members and other agents who assist with client services and transactions",
                IconName = "users",
                SortOrder = 4,
                IsActive = true
            },
            new ResourceCategory
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Title Company",
                Description = "Title companies that handle property title searches, insurance, and closing services",
                IconName = "file-text",
                SortOrder = 5,
                IsActive = true
            },
            new ResourceCategory
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "Appraiser",
                Description = "Licensed appraisers who determine property values for lending and insurance purposes",
                IconName = "calculator",
                SortOrder = 6,
                IsActive = true
            }
        };
    }
}
