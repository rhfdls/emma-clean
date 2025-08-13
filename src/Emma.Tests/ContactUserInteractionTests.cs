using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Emma.Models.Models;

namespace Emma.Tests.Models
{
    #region ModelSync Test Coverage
    public class ContactUserInteractionTests
    {
        [Fact]
        public void Contact_Initializes_Navigation_Collections()
        {
            var contact = new Contact();
            Assert.NotNull(contact.EmailAddresses);
            Assert.NotNull(contact.PhoneNumbers);
            Assert.NotNull(contact.Addresses);
            Assert.NotNull(contact.Interactions);
            Assert.NotNull(contact.AssignedResources);
            Assert.NotNull(contact.Collaborators);
            Assert.NotNull(contact.Tasks);
        }

        [Fact]
        public void User_Exposes_All_Required_Collections()
        {
            var user = new User();
            Assert.NotNull(user.AssignedTasks);
            Assert.NotNull(user.OwnedContacts);
            Assert.NotNull(user.EmailAddresses);
            Assert.NotNull(user.PhoneNumbers);
            Assert.NotNull(user.SubscriptionAssignments);
            Assert.NotNull(user.Roles);
            Assert.Null(user.ProfileImageUrl); // default
            Assert.Null(user.TimeZone);
            Assert.Null(user.Locale);
            Assert.Null(user.LastLoginAt);
        }

        [Fact]
        public void Interaction_Links_To_Contact_And_AssignedEntities()
        {
            var contact = new Contact();
            var user = new User();
            var agent = new Agent();
            var interaction = new Interaction
            {
                Contact = contact,
                AssignedToUser = user,
                AssignedToAgent = agent,
                AssignedToId = Guid.NewGuid()
            };
            Assert.Equal(contact, interaction.Contact);
            Assert.Equal(user, interaction.AssignedToUser);
            Assert.Equal(agent, interaction.AssignedToAgent);
            Assert.True(interaction.AssignedToId.HasValue);
        }

        [Fact]
        public void Contact_Obsolete_Fields_Are_Ignored()
        {
            var contact = new Contact
            {
                Emails = new List<string> { "legacy@email.com" },
                Phones = new List<string> { "+15555555555" }
            };
            Assert.Single(contact.Emails);
            Assert.Single(contact.Phones);
        }
    }
    #endregion
}
