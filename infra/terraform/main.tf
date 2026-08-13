data "aws_caller_identity" "current" {}

data "aws_region" "current" {}

resource "random_password" "database" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false
}

module "network" {
  source = "./modules/network"

  name_prefix           = local.name_prefix
  vpc_cidr              = var.vpc_cidr
  service_subnet_cidrs  = var.service_subnet_cidrs
  database_subnet_cidrs = var.database_subnet_cidrs
  tags                  = local.tags
}

module "storage" {
  source = "./modules/storage"

  name_prefix = local.name_prefix
  bucket_name = var.attachments_bucket_name
  aws_region  = var.aws_region
  account_id  = data.aws_caller_identity.current.account_id
  tags        = local.tags
}

module "database" {
  source = "./modules/database"

  name_prefix           = local.name_prefix
  database_name         = var.database_name
  database_username     = var.database_username
  database_password     = random_password.database.result
  engine_version        = var.postgres_engine_version
  instance_class        = var.rds_instance_class
  subnet_ids            = module.network.database_subnet_ids
  security_group_ids    = [module.network.database_security_group_id]
  deletion_protection   = false
  skip_final_snapshot   = true
  backup_retention_days = 1
  tags                  = local.tags
}

resource "aws_secretsmanager_secret" "db_connection" {
  name                    = "${local.name_prefix}/api/ConnectionStrings__DefaultConnection"
  recovery_window_in_days = 0
  tags                    = local.tags
}

resource "aws_secretsmanager_secret_version" "db_connection" {
  secret_id = aws_secretsmanager_secret.db_connection.id
  secret_string = join(";", [
    "Host=${module.database.address}",
    "Port=${module.database.port}",
    "Database=${var.database_name}",
    "Username=${var.database_username}",
    "Password=${random_password.database.result}",
    "Ssl Mode=Require",
    "Trust Server Certificate=true"
  ])
}

resource "aws_secretsmanager_secret" "jwt_signing_key" {
  name                    = "${local.name_prefix}/api/EngiFlow__Authentication__Jwt__SigningKey"
  recovery_window_in_days = 0
  tags                    = local.tags
}

resource "aws_secretsmanager_secret_version" "jwt_signing_key" {
  secret_id     = aws_secretsmanager_secret.jwt_signing_key.id
  secret_string = random_password.jwt_signing_key.result
}

resource "aws_secretsmanager_secret" "ses_smtp_username" {
  name                    = "${local.name_prefix}/api/EngiFlow__Email__Smtp__Username"
  recovery_window_in_days = 0
  tags                    = local.tags
}

resource "aws_secretsmanager_secret_version" "ses_smtp_username" {
  secret_id     = aws_secretsmanager_secret.ses_smtp_username.id
  secret_string = var.ses_smtp_username
}

resource "aws_secretsmanager_secret" "ses_smtp_password" {
  name                    = "${local.name_prefix}/api/EngiFlow__Email__Smtp__Password"
  recovery_window_in_days = 0
  tags                    = local.tags
}

resource "aws_secretsmanager_secret_version" "ses_smtp_password" {
  secret_id     = aws_secretsmanager_secret.ses_smtp_password.id
  secret_string = var.ses_smtp_password
}

module "compute_registry" {
  source = "./modules/compute_registry"

  name_prefix = local.name_prefix
  tags        = local.tags
}

module "email" {
  source = "./modules/email"

  domain_identity = var.ses_domain_identity
  email_identity  = var.ses_email_identity
  tags            = local.tags
}

module "compute_services" {
  source = "./modules/compute_services"

  name_prefix                  = local.name_prefix
  aws_region                   = var.aws_region
  service_subnet_ids           = module.network.service_subnet_ids
  api_security_group_id        = module.network.api_service_security_group_id
  web_security_group_id        = module.network.web_service_security_group_id
  api_image                    = "${module.compute_registry.api_repository_url}:${var.api_image_tag}"
  web_image                    = "${module.compute_registry.web_repository_url}:${var.web_image_tag}"
  db_connection_secret_arn     = aws_secretsmanager_secret.db_connection.arn
  jwt_signing_key_secret_arn   = aws_secretsmanager_secret.jwt_signing_key.arn
  ses_smtp_username_secret_arn = aws_secretsmanager_secret.ses_smtp_username.arn
  ses_smtp_password_secret_arn = aws_secretsmanager_secret.ses_smtp_password.arn
  s3_bucket_name               = module.storage.bucket_name
  s3_bucket_arn                = module.storage.bucket_arn
  frontend_public_base_url     = var.frontend_public_base_url
  jwt_issuer                   = var.jwt_issuer
  jwt_audience                 = var.jwt_audience
  jwt_access_token_minutes     = var.jwt_access_token_minutes
  ses_from_email               = var.ses_from_email
  ses_from_name                = var.ses_from_name
  tags                         = local.tags
}

module "finops" {
  source = "./modules/finops"

  name_prefix       = local.name_prefix
  monthly_limit_usd = var.monthly_budget_limit_usd
  alert_email       = var.budget_alert_email
  tags              = local.tags
}
