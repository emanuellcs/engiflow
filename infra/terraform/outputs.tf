output "api_ecr_repository_url" {
  description = "API ECR repository URL."
  value       = module.compute_registry.api_repository_url
}

output "web_ecr_repository_url" {
  description = "Web ECR repository URL."
  value       = module.compute_registry.web_repository_url
}

output "api_service_url" {
  description = "Public API service URL."
  value       = module.compute_services.api_service_url
}

output "web_service_url" {
  description = "Public web service URL."
  value       = module.compute_services.web_service_url
}

output "attachments_bucket_name" {
  description = "Private S3 bucket for ECO attachments."
  value       = module.storage.bucket_name
}

output "database_endpoint" {
  description = "RDS PostgreSQL endpoint."
  value       = module.database.address
}

output "ses_domain_verification_token" {
  description = "SES domain verification token when ses_domain_identity is set."
  value       = module.email.domain_verification_token
}

output "ses_dkim_tokens" {
  description = "SES DKIM tokens when ses_domain_identity is set."
  value       = module.email.dkim_tokens
}
