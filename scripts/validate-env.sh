#!/bin/bash
# Emma AI Platform Environment Variable Validation Script
# This script validates that all required environment variables are present

GREEN="\033[0;32m"
YELLOW="\033[1;33m"
RED="\033[0;31m"
CYAN="\033[0;36m"
NC="\033[0m" # No Color

# Import environment variables from .env and .env.local if they exist
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
    echo -e "${GREEN}✓ Loaded variables from .env${NC}"
else
    echo -e "${YELLOW}⚠ .env file not found${NC}"
fi

if [ -f .env.local ]; then
    export $(grep -v '^#' .env.local | xargs)
    echo -e "${GREEN}✓ Loaded variables from .env.local${NC}"
else
    echo -e "${YELLOW}⚠ .env.local file not found. Create it from .env.example to store your secrets.${NC}"
fi

# Define required environment variables
declare -A required_variables
required_variables=(
    ["CONNECTION_STRINGS__POSTGRESQL"]="PostgreSQL Connection String"
    ["COSMOSDB__ACCOUNTENDPOINT"]="CosmosDB Account Endpoint"
    ["COSMOSDB__ACCOUNTKEY"]="CosmosDB Account Key"
    ["COSMOSDB__DATABASENAME"]="CosmosDB Database Name"
    ["COSMOSDB__CONTAINERNAME"]="CosmosDB Container Name"
    ["AZUREAIFOUNDRY__ENDPOINT"]="Azure AI Foundry Endpoint"
    ["AZUREAIFOUNDRY__APIKEY"]="Azure AI Foundry API Key"
    ["AZUREAIFOUNDRY__DEPLOYMENTNAME"]="Azure AI Foundry Deployment Name"
)

missing_variables=()

for var_name in "${!required_variables[@]}"; do
    var_value="${!var_name}"
    var_description="${required_variables[$var_name]}"
    
    if [ -z "$var_value" ]; then
        missing_variables+=("$var_name ($var_description)")
    else
        # Mask sensitive values in output
        if [[ "$var_name" == *KEY* ]] || [[ "$var_name" == *PASSWORD* ]] || [[ "$var_name" == *SECRET* ]]; then
            var_length=${#var_value}
            if [ $var_length -le 8 ]; then
                masked_value="****"
            else
                start_chars="${var_value:0:4}"
                end_chars="${var_value: -4}"
                masked_value="$start_chars...$end_chars"
            fi
            echo -e "${GREEN}✓ $var_name = $masked_value${NC}"
        else
            echo -e "${GREEN}✓ $var_name = $var_value${NC}"
        fi
    fi
done

if [ ${#missing_variables[@]} -gt 0 ]; then
    echo -e "\n${RED}❌ Missing required environment variables:${NC}"
    for missing in "${missing_variables[@]}"; do
        echo -e "${RED}  - $missing${NC}"
    done
    echo -e "\n${YELLOW}Please set these variables in your .env.local file.${NC}"
    exit 1
else
    echo -e "\n${GREEN}✅ All required environment variables are present!${NC}"
fi

# Check for shadowing issues
declare -A variables_to_check
variables_to_check=(
    ["COSMOSDB__ACCOUNTENDPOINT"]="CosmosDb__AccountEndpoint"
    ["COSMOSDB__ACCOUNTKEY"]="CosmosDb__AccountKey"
    ["AZUREAIFOUNDRY__ENDPOINT"]="AzureAIFoundry__Endpoint"
    ["AZUREAIFOUNDRY__APIKEY"]="AzureAIFoundry__ApiKey"
)

shadowing_issues=()

for upper_var in "${!variables_to_check[@]}"; do
    pascal_var="${variables_to_check[$upper_var]}"
    
    upper_value="${!upper_var}"
    pascal_value="${!pascal_var}"
    
    if [ ! -z "$upper_value" ] && [ ! -z "$pascal_value" ] && [ "$upper_value" != "$pascal_value" ]; then
        shadowing_issues+=("Conflict between $upper_var and $pascal_var")
    fi
done

if [ ${#shadowing_issues[@]} -gt 0 ]; then
    echo -e "\n${YELLOW}⚠ Environment variable conflicts detected:${NC}"
    for conflict in "${shadowing_issues[@]}"; do
        echo -e "${YELLOW}  - $conflict${NC}"
    done
    echo -e "${YELLOW}The Emma AI Platform will use uppercase variable names if conflicting versions exist.${NC}"
fi

echo -e "\n${CYAN}Environment validation completed!${NC}"
