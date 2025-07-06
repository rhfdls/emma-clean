// SPRINT1: CRM integration registry/factory for resolving integration services by provider key
using System;
using System.Collections.Generic;
using Emma.Core.Interfaces.Crm;
using Emma.Crm.Integrations.Fub;

namespace Emma.Crm.Integrations
{
    public static class CrmIntegrationRegistry
    {
        private static readonly Dictionary<string, ICrmIntegration> _integrations = new()
        {
            { "fub", new FubIntegrationService() }
            // Add more providers as needed
        };

        public static ICrmIntegration? Resolve(string provider)
        {
            _integrations.TryGetValue(provider.ToLowerInvariant(), out var integration);
            return integration;
        }
    }
}
