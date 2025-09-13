using System;
using System.Collections.Generic;
using Emma.Api.Authorization;
using Emma.Models.Models;
using Xunit;

namespace Emma.Api.UnitTests.Authorization
{
    public class ContactAccessServiceTests
    {
        [Fact]
        public void IsOwnerOrAdmin_Works()
        {
            Assert.True(ContactAccessService.IsOwnerOrAdmin(new[] { "Admin" }));
            Assert.True(ContactAccessService.IsOwnerOrAdmin(new[] { "Owner" }));
            Assert.False(ContactAccessService.IsOwnerOrAdmin(Array.Empty<string>()));
        }

        [Fact]
        public void IsCurrentOwner_Works()
        {
            var uid = Guid.NewGuid();
            var c = new Contact { OwnerId = uid };
            Assert.True(ContactAccessService.IsCurrentOwner(uid, c));
            Assert.False(ContactAccessService.IsCurrentOwner(Guid.NewGuid(), c));
        }

        [Fact]
        public void ValidateCollaboratorUpdate_AllBusinessFields_Ok()
        {
            var dto = new { CompanyName = "ACME", Website = "https://ac.me", LicenseNumber = "ON-1", IsPreferred = true, Notes = "ok" };
            var (ok, blocked) = ContactAccessService.ValidateCollaboratorUpdate(dto);
            Assert.True(ok);
            Assert.Empty(blocked);
        }

        [Fact]
        public void ValidateCollaboratorUpdate_BlockedFields_Forbidden()
        {
            var dto = new { OwnerId = Guid.NewGuid(), PrimaryEmail = "a@b.com", RelationshipState = "ServiceProvider" };
            var (ok, blocked) = ContactAccessService.ValidateCollaboratorUpdate(dto);
            Assert.False(ok);
            Assert.Contains("OwnerId", (IEnumerable<string>)blocked);
            Assert.Contains("PrimaryEmail", (IEnumerable<string>)blocked);
            Assert.Contains("RelationshipState", (IEnumerable<string>)blocked);
        }
    }
}
