variable "aws_region" {
  description = "AWS Region for the EngiFlow application stack."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Short project identifier used in resource names."
  type        = string
  default     = "engiflow"
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "prod"
}

variable "cost_center" {
  description = "FinOps cost allocation tag."
  type        = string
  default     = "engiflow-validation"
}

variable "extra_tags" {
  description = "Additional tags applied through the AWS provider default_tags block."
  type        = map(string)
  default     = {}
}

variable "vpc_cidr" {
  description = "CIDR block for the EngiFlow VPC."
  type        = string
  default     = "10.42.0.0/16"
}

variable "service_subnet_cidrs" {
  description = "Public service subnet CIDRs used by ECS Express Mode to avoid NAT gateway fixed costs."
  type        = list(string)
  default     = ["10.42.0.0/24", "10.42.1.0/24"]
}

variable "database_subnet_cidrs" {
  description = "Private RDS subnet CIDRs."
  type        = list(string)
  default     = ["10.42.10.0/24", "10.42.11.0/24"]
}

variable "database_name" {
  description = "PostgreSQL database name."
  type        = string
  default     = "engiflow"
}

variable "database_username" {
  description = "PostgreSQL master username."
  type        = string
  default     = "engiflow"
}

variable "postgres_engine_version" {
  description = "RDS PostgreSQL engine version. PostgreSQL 17 is selected for db.t4g.micro support."
  type        = string
  default     = "17"
}

variable "rds_instance_class" {
  description = "RDS instance class."
  type        = string
  default     = "db.t4g.micro"
}

variable "attachments_bucket_name" {
  description = "Optional globally unique S3 bucket name for ECO attachments."
  type        = string
  default     = ""
}

variable "api_image_tag" {
  description = "ECR image tag to deploy for the API service."
  type        = string
  default     = "latest"
}

variable "web_image_tag" {
  description = "ECR image tag to deploy for the web service."
  type        = string
  default     = "latest"
}

variable "frontend_public_base_url" {
  description = "Externally reachable web origin used in API-generated password setup/reset links."
  type        = string

  validation {
    condition     = startswith(var.frontend_public_base_url, "https://")
    error_message = "frontend_public_base_url must be an HTTPS origin, for example https://app.example.com."
  }
}

variable "jwt_issuer" {
  description = "JWT issuer expected by the API."
  type        = string
  default     = "EngiFlow.Api"
}

variable "jwt_audience" {
  description = "JWT audience expected by clients."
  type        = string
  default     = "EngiFlow.Clients"
}

variable "jwt_access_token_minutes" {
  description = "JWT access token lifetime in minutes."
  type        = number
  default     = 60
}

variable "ses_smtp_username" {
  description = "SES SMTP username stored in Secrets Manager for API runtime injection."
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.ses_smtp_username) > 0
    error_message = "ses_smtp_username must be provided through TF_VAR_ses_smtp_username."
  }
}

variable "ses_smtp_password" {
  description = "SES SMTP password stored in Secrets Manager for API runtime injection."
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.ses_smtp_password) > 0
    error_message = "ses_smtp_password must be provided through TF_VAR_ses_smtp_password."
  }
}

variable "ses_from_email" {
  description = "Verified SES sender email address used by EngiFlow."
  type        = string
  default     = "no-reply@example.com"
}

variable "ses_from_name" {
  description = "Display name for EngiFlow transactional email."
  type        = string
  default     = "EngiFlow"
}

variable "ses_domain_identity" {
  description = "Optional SES domain identity to create for verification."
  type        = string
  default     = ""
}

variable "ses_email_identity" {
  description = "Optional SES email identity to create for verification."
  type        = string
  default     = ""
}

variable "budget_alert_email" {
  description = "Email address that receives AWS Budgets alerts."
  type        = string

  validation {
    condition     = can(regex("^[^@]+@[^@]+\\.[^@]+$", var.budget_alert_email))
    error_message = "budget_alert_email must be a valid email address."
  }
}

variable "monthly_budget_limit_usd" {
  description = "Monthly AWS account spend ceiling for validation."
  type        = number
  default     = 10
}
