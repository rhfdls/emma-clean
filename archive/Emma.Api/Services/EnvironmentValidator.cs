using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emma.Api.Services
{
    /// <summary>
    /// Validates required environment variables for the Emma AI Platform
    /// </summary>
    public class EnvironmentValidator
    {
        private readonly ILogger<EnvironmentValidator> _logger;
        
        public EnvironmentValidator(ILogger<EnvironmentValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates that all required environment variables are present and not empty
        /// During transition period: Logs warnings instead of throwing exceptions for missing variables
        /// </summary>
        public void ValidateRequiredVariables()
        {
            var requiredVariables = new Dictionary<string, string>
            {
                // Database connection
                { "CONNECTION_STRINGS__POSTGRESQL", "PostgreSQL Connection String" },
                
                // CosmosDB (required for AI workflows)
                { "COSMOSDB__ACCOUNTENDPOINT", "CosmosDB Account Endpoint" },
                { "COSMOSDB__ACCOUNTKEY", "CosmosDB Account Key" },
                { "COSMOSDB__DATABASENAME", "CosmosDB Database Name" },
                { "COSMOSDB__CONTAINERNAME", "CosmosDB Container Name" },
                
                // Azure AI Foundry
                { "AZUREAIFOUNDRY__ENDPOINT", "Azure AI Foundry Endpoint" },
                { "AZUREAIFOUNDRY__APIKEY", "Azure AI Foundry API Key" },
                { "AZUREAIFOUNDRY__DEPLOYMENTNAME", "Azure AI Foundry Deployment Name" },
                
                // Azure OpenAI (if using legacy integration)
                { "AZUREOPENAI__ENDPOINT", "Azure OpenAI Endpoint" },
                { "AZUREOPENAI__APIKEY", "Azure OpenAI API Key" },
                { "AZUREOPENAI__DEPLOYMENTNAME", "Azure OpenAI Deployment Name" },
            };
            
            var missingVariables = new List<string>();
            
            foreach (var variable in requiredVariables)
            {
                var value = Environment.GetEnvironmentVariable(variable.Key);
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingVariables.Add($"{variable.Key} ({variable.Value})");
                }
            }
            
            if (missingVariables.Any())
            {
                // HYBRID APPROACH: Log warnings instead of failing during transition period
                var message = $"TRANSITION PERIOD WARNING: Environment variables not set in .env/.env.local: {string.Join(", ", missingVariables)}";
                _logger.LogWarning(message);
                _logger.LogWarning("Using hardcoded values from docker-compose.yml during transition period. Please migrate to .env.local for improved security.");

                // NOTE: Once the transition period is complete, uncomment the following lines to enforce required variables:
                // _logger.LogCritical(message);
                // throw new InvalidOperationException(message);
            }
            else
            {
                _logger.LogInformation("Environment validation completed successfully. All required variables are present.");
            }
        }
        
        /// <summary>
        /// Validates optional environment variables and logs warnings if they are missing
        /// </summary>
        public void ValidateOptionalVariables()
        {
            var optionalVariables = new Dictionary<string, string>
            {
                { "AZURESTORAGE__ACCOUNTNAME", "Azure Storage Account Name" },
                { "AZURESTORAGE__ACCOUNTKEY", "Azure Storage Account Key" },
                { "AZURESEARCH__SERVICENAME", "Azure Search Service Name" },
                { "AZURESEARCH__APIKEY", "Azure Search API Key" },
                { "AZURESEARCH__ENDPOINT", "Azure Search Endpoint" },
                { "KEYVAULT__NAME", "Azure Key Vault Name" },
            };
            
            foreach (var variable in optionalVariables)
            {
                var value = Environment.GetEnvironmentVariable(variable.Key);
                if (string.IsNullOrWhiteSpace(value))
                {
                    _logger.LogWarning("Optional environment variable not set: {VariableName} ({VariableDescription})", 
                        variable.Key, variable.Value);
                }
            }
        }
        
        /// <summary>
        /// Checks for potential environment variable conflicts or shadowing
        /// </summary>
        public void CheckForConflicts()
        {
            // Check for problematic double naming patterns
            var variablesToCheck = new List<(string, string)>
            {
                ("COSMOSDB__ACCOUNTENDPOINT", "CosmosDb__AccountEndpoint"),
                ("COSMOSDB__ACCOUNTKEY", "CosmosDb__AccountKey"),
                ("COSMOSDB__DATABASENAME", "CosmosDb__DatabaseName"),
                ("COSMOSDB__CONTAINERNAME", "CosmosDb__ContainerName"),
                
                // Check Azure AI Foundry lowercase variants
                ("AZUREAIFOUNDRY__ENDPOINT", "AzureAIFoundry__Endpoint"),
                ("AZUREAIFOUNDRY__APIKEY", "AzureAIFoundry__ApiKey"),
                
                // Check Azure OpenAI lowercase variants
                ("AZUREOPENAI__ENDPOINT", "AzureOpenAI__Endpoint"),
                ("AZUREOPENAI__APIKEY", "AzureOpenAI__ApiKey"),
            };
            
            foreach (var (upperVar, pascalVar) in variablesToCheck)
            {
                var upperValue = Environment.GetEnvironmentVariable(upperVar);
                var pascalValue = Environment.GetEnvironmentVariable(pascalVar);
                
                if (upperValue != null && pascalValue != null && upperValue != pascalValue)
                {
                    _logger.LogWarning(
                        "Environment variable conflict detected! Both {UpperVar} and {PascalVar} are defined with different values. " +
                        "The Emma AI Platform will use {UpperVar}={UpperValue} (uppercase takes precedence).",
                        upperVar, pascalVar, upperVar, upperValue);
                }
            }
        }
    }
}
