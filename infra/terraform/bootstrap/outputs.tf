output "state_bucket" {
  description = "S3 bucket for the application Terraform remote state."
  value       = aws_s3_bucket.state.bucket
}

output "lock_table" {
  description = "DynamoDB table for Terraform state locking."
  value       = aws_dynamodb_table.locks.name
}

output "github_actions_role_arn" {
  description = "GitHub Actions OIDC role ARN for deployment workflows."
  value       = aws_iam_role.github_actions_deploy.arn
}

output "github_oidc_provider_arn" {
  description = "GitHub OIDC provider ARN used by the deployment role."
  value       = local.github_oidc_provider_arn
}
