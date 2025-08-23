using System;
using System.Threading;
using System.Threading.Tasks;
using Emma.Api.Controllers;
using Emma.Api.Interfaces;
using Emma.Infrastructure.Data;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Api.UnitTests.Controllers
{
    public class InteractionControllerTests
    {
        private EmmaDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<EmmaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new EmmaDbContext(options);
        }

        [Fact]
        public async Task LogInteraction_MapsOccurredAtToStartedAt_AndSetsCreatedAtServerSide()
        {
            var db = CreateDb();
            var analysis = new Mock<IEmmaAnalysisService>();
            var queue = new Mock<IAnalysisQueue>();
            var cfg = new ConfigurationBuilder().Build();
            var logger = new Mock<ILogger<InteractionController>>();
            var controller = new InteractionController(db, analysis.Object, queue.Object, cfg, logger.Object);

            var contactId = Guid.NewGuid();
            var orgId = Guid.NewGuid();

            // Build a fake HttpContext with orgId claim
            var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("orgId", orgId.ToString())
            }, "TestAuth");
            httpCtx.User = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpCtx
            };

            var occurred = new DateTime(2025, 8, 20, 16, 30, 0, DateTimeKind.Utc);
            var body = new InteractionController.CreateInteractionRequest
            {
                Subject = "S",
                Content = "C",
                OccurredAt = occurred,
                ConsentGranted = false
            };

            var result = await controller.LogInteraction(contactId, body, CancellationToken.None);
            Assert.IsType<Microsoft.AspNetCore.Mvc.CreatedAtActionResult>(result);

            var saved = await db.Interactions.FirstOrDefaultAsync(i => i.ContactId == contactId);
            Assert.NotNull(saved);
            Assert.Equal(occurred, saved!.StartedAt);
            // CreatedAt should be set close to now; allow small delta
            Assert.True((DateTime.UtcNow - saved.CreatedAt).TotalMinutes < 1);
        }
    }
}
