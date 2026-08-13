variable "name_prefix" {
  type = string
}

variable "aws_region" {
  type = string
}

variable "service_subnet_ids" {
  type = list(string)
}

variable "api_security_group_id" {
  type = string
}

variable "web_security_group_id" {
  type = string
}

variable "api_image" {
  type = string
}

variable "web_image" {
  type = string
}

variable "db_connection_secret_arn" {
  type = string
}

variable "jwt_signing_key_secret_arn" {
  type = string
}

variable "ses_smtp_username_secret_arn" {
  type = string
}

variable "ses_smtp_password_secret_arn" {
  type = string
}

variable "s3_bucket_name" {
  type = string
}

variable "s3_bucket_arn" {
  type = string
}

variable "frontend_public_base_url" {
  type = string
}

variable "jwt_issuer" {
  type = string
}

variable "jwt_audience" {
  type = string
}

variable "jwt_access_token_minutes" {
  type = number
}

variable "ses_from_email" {
  type = string
}

variable "ses_from_name" {
  type = string
}

variable "cpu" {
  type    = string
  default = "256"
}

variable "memory" {
  type    = string
  default = "512"
}

variable "tags" {
  type = map(string)
}
