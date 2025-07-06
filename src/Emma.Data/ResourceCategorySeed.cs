using Emma.Models.Models;

namespace Emma.Data;

public static class ResourceCategorySeed
{
    // ResourceCategory has been removed from the codebase. This seed file is now obsolete and can be deleted.
    public static List<object> GetDefaultCategories()
    {
        Guid.TryParse("11111111-1111-1111-1111-111111111111", out var id1);
        Guid.TryParse("22222222-2222-2222-2222-222222222222", out var id2);
        Guid.TryParse("33333333-3333-3333-3333-333333333333", out var id3);
        Guid.TryParse("44444444-4444-4444-4444-444444444444", out var id4);
        Guid.TryParse("55555555-5555-5555-5555-555555555555", out var id5);
        Guid.TryParse("66666666-6666-6666-6666-666666666666", out var id6);

        return new List<object>
        {
            new 
            {
                Id = id1 != Guid.Empty ? id1 : Guid.Empty,
                Name = "Mortgage Lender/Broker",
                Description = "Mortgage lenders and brokers who help clients secure financing for real estate purchases",
                IconName = "bank",
                SortOrder = 1,
                IsActive = true
            },
            new 
            {
                Id = id2 != Guid.Empty ? id2 : Guid.Empty,
                Name = "Building Inspector",
                Description = "Professional inspectors who evaluate property conditions for buyers and sellers",
                IconName = "search",
                SortOrder = 2,
                IsActive = true
            },
        };
    }
}
