variable "aws_region" {
  description = "AWS Region that hosts Terraform state and GitHub OIDC resources."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Short project identifier used in bootstrap resource names."
  type        = string
  default     = "engiflow"
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "prod"
}

variable "state_bucket_name" {
  description = "Optional globally unique S3 bucket name for Terraform remote state."
  type        = string
  default     = ""
}

variable "lock_table_name" {
  description = "Optional DynamoDB table name for Terraform state locking."
  type        = string
  default     = ""
}

variable "github_owner" {
  description = "GitHub organization or user that owns the EngiFlow repository."
  type        = string
}

variable "github_repo" {
  description = "GitHub repository name for EngiFlow."
  type        = string
}

variable "github_branch" {
  description = "Branch allowed to assume the GitHub Actions deployment role."
  type        = string
  default     = "main"
}

variable "github_oidc_provider_arn" {
  description = "Existing GitHub OIDC provider ARN. Leave empty to create one."
  type        = string
  default     = ""
}

variable "extra_tags" {
  description = "Additional tags for bootstrap resources."
  type        = map(string)
  default     = {}
}
