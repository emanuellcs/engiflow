locals {
  secret_arns = [
    var.db_connection_secret_arn,
    var.jwt_signing_key_secret_arn,
    var.ses_smtp_username_secret_arn,
    var.ses_smtp_password_secret_arn
  ]

  api_endpoint = try(aws_ecs_express_gateway_service.api.ingress_paths[0].endpoint, "")
  web_endpoint = try(aws_ecs_express_gateway_service.web.ingress_paths[0].endpoint, "")

  api_service_url = local.api_endpoint == "" ? "" : (
    startswith(local.api_endpoint, "http://") || startswith(local.api_endpoint, "https://")
    ? local.api_endpoint
    : "https://${local.api_endpoint}"
  )

  web_service_url = local.web_endpoint == "" ? "" : (
    startswith(local.web_endpoint, "http://") || startswith(local.web_endpoint, "https://")
    ? local.web_endpoint
    : "https://${local.web_endpoint}"
  )

  api_environment = {
    ASPNETCORE_ENVIRONMENT                            = "Production"
    ASPNETCORE_URLS                                   = "http://+:8080"
    App__FrontendBaseUrl                              = var.frontend_public_base_url
    App__EmailDomain                                  = replace(var.ses_from_email, "/^[^@]+@/", "")
    EngiFlow__Authentication__Jwt__Issuer             = var.jwt_issuer
    EngiFlow__Authentication__Jwt__Audience           = var.jwt_audience
    EngiFlow__Authentication__Jwt__AccessTokenMinutes = tostring(var.jwt_access_token_minutes)
    EngiFlow__Database__MigrateOnStartup              = "true"
    EngiFlow__Storage__S3__BucketName                 = var.s3_bucket_name
    EngiFlow__Storage__S3__Region                     = var.aws_region
    EngiFlow__Storage__S3__ForcePathStyle             = "false"
    EngiFlow__Email__Smtp__Host                       = "email-smtp.${var.aws_region}.amazonaws.com"
    EngiFlow__Email__Smtp__Port                       = "587"
    EngiFlow__Email__Smtp__UseStartTls                = "true"
    EngiFlow__Email__Smtp__FromEmail                  = var.ses_from_email
    EngiFlow__Email__Smtp__FromName                   = var.ses_from_name
  }

  api_secrets = {
    ConnectionStrings__DefaultConnection      = var.db_connection_secret_arn
    EngiFlow__Authentication__Jwt__SigningKey = var.jwt_signing_key_secret_arn
    EngiFlow__Email__Smtp__Username           = var.ses_smtp_username_secret_arn
    EngiFlow__Email__Smtp__Password           = var.ses_smtp_password_secret_arn
  }

  web_environment = {
    API_INTERNAL_BASE_URL    = local.api_service_url
    HOSTNAME                 = "0.0.0.0"
    NEXT_PUBLIC_API_BASE_URL = local.api_service_url
    NEXT_TELEMETRY_DISABLED  = "1"
    NODE_ENV                 = "production"
    PORT                     = "3000"
  }
}

resource "aws_ecs_cluster" "this" {
  name = "${var.name_prefix}-cluster"

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-cluster"
  })
}

data "aws_iam_policy_document" "ecs_tasks_assume" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

data "aws_iam_policy_document" "ecs_infrastructure_assume" {
  statement {
    sid     = "AllowAccessToECSForInfrastructureManagement"
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ecs.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "execution" {
  name               = "${var.name_prefix}-ecs-execution"
  assume_role_policy = data.aws_iam_policy_document.ecs_tasks_assume.json
  tags               = var.tags
}

resource "aws_iam_role_policy_attachment" "execution" {
  role       = aws_iam_role.execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role" "task" {
  name               = "${var.name_prefix}-ecs-task"
  assume_role_policy = data.aws_iam_policy_document.ecs_tasks_assume.json
  tags               = var.tags
}

resource "aws_iam_role" "infrastructure" {
  name               = "${var.name_prefix}-ecs-infrastructure"
  assume_role_policy = data.aws_iam_policy_document.ecs_infrastructure_assume.json
  tags               = var.tags
}

resource "aws_iam_role_policy_attachment" "infrastructure" {
  role       = aws_iam_role.infrastructure.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSInfrastructureRoleforExpressGatewayServices"
}

data "aws_iam_policy_document" "execution_secrets" {
  statement {
    effect    = "Allow"
    actions   = ["secretsmanager:GetSecretValue"]
    resources = local.secret_arns
  }
}

resource "aws_iam_role_policy" "execution_secrets" {
  name   = "${var.name_prefix}-execution-secrets"
  role   = aws_iam_role.execution.id
  policy = data.aws_iam_policy_document.execution_secrets.json
}

data "aws_iam_policy_document" "task_storage" {
  statement {
    effect = "Allow"
    actions = [
      "s3:ListBucket"
    ]
    resources = [var.s3_bucket_arn]
  }

  statement {
    effect = "Allow"
    actions = [
      "s3:DeleteObject",
      "s3:GetObject",
      "s3:PutObject"
    ]
    resources = ["${var.s3_bucket_arn}/*"]
  }
}

resource "aws_iam_role_policy" "task_storage" {
  name   = "${var.name_prefix}-task-storage"
  role   = aws_iam_role.task.id
  policy = data.aws_iam_policy_document.task_storage.json
}

resource "aws_cloudwatch_log_group" "api" {
  name              = "/aws/ecs/express/${var.name_prefix}/api"
  retention_in_days = 7
  tags              = var.tags
}

resource "aws_cloudwatch_log_group" "web" {
  name              = "/aws/ecs/express/${var.name_prefix}/web"
  retention_in_days = 7
  tags              = var.tags
}

resource "aws_ecs_express_gateway_service" "api" {
  service_name            = "${var.name_prefix}-api"
  cluster                 = aws_ecs_cluster.this.name
  execution_role_arn      = aws_iam_role.execution.arn
  infrastructure_role_arn = aws_iam_role.infrastructure.arn
  task_role_arn           = aws_iam_role.task.arn
  cpu                     = var.cpu
  memory                  = var.memory
  health_check_path       = "/healthz"
  wait_for_steady_state   = true

  primary_container {
    image          = var.api_image
    container_port = 8080

    aws_logs_configuration {
      log_group         = aws_cloudwatch_log_group.api.name
      log_stream_prefix = "api"
    }

    dynamic "environment" {
      for_each = local.api_environment
      content {
        name  = environment.key
        value = environment.value
      }
    }

    dynamic "secret" {
      for_each = local.api_secrets
      content {
        name       = secret.key
        value_from = secret.value
      }
    }
  }

  network_configuration {
    subnets         = var.service_subnet_ids
    security_groups = [var.api_security_group_id]
  }

  scaling_target {
    auto_scaling_metric       = "REQUEST_COUNT_PER_TARGET"
    auto_scaling_target_value = 1000
    min_task_count            = 1
    max_task_count            = 1
  }

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-api"
  })

  depends_on = [
    aws_iam_role_policy_attachment.execution,
    aws_iam_role_policy_attachment.infrastructure,
    aws_iam_role_policy.execution_secrets,
    aws_iam_role_policy.task_storage
  ]
}

resource "aws_ecs_express_gateway_service" "web" {
  service_name            = "${var.name_prefix}-web"
  cluster                 = aws_ecs_cluster.this.name
  execution_role_arn      = aws_iam_role.execution.arn
  infrastructure_role_arn = aws_iam_role.infrastructure.arn
  task_role_arn           = aws_iam_role.task.arn
  cpu                     = var.cpu
  memory                  = var.memory
  health_check_path       = "/auth"
  wait_for_steady_state   = true

  primary_container {
    image          = var.web_image
    container_port = 3000

    aws_logs_configuration {
      log_group         = aws_cloudwatch_log_group.web.name
      log_stream_prefix = "web"
    }

    dynamic "environment" {
      for_each = local.web_environment
      content {
        name  = environment.key
        value = environment.value
      }
    }
  }

  network_configuration {
    subnets         = var.service_subnet_ids
    security_groups = [var.web_security_group_id]
  }

  scaling_target {
    auto_scaling_metric       = "REQUEST_COUNT_PER_TARGET"
    auto_scaling_target_value = 1000
    min_task_count            = 1
    max_task_count            = 1
  }

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-web"
  })

  depends_on = [
    aws_ecs_express_gateway_service.api,
    aws_iam_role_policy_attachment.execution,
    aws_iam_role_policy_attachment.infrastructure,
    aws_iam_role_policy.execution_secrets,
    aws_iam_role_policy.task_storage
  ]
}
