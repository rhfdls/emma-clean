# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial implementation of EnvironmentValidator for startup config validation
- Secrets management documentation (`SECRETS_MANAGEMENT.md`)

### Changed
- Standardized all environment variable naming to UPPERCASE_DOUBLE_UNDERSCORE format

### Fixed
- Removed secrets from `docker-compose.yml`

### Security
- Enforced validation of secrets at app startup
