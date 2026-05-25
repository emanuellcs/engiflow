locals {
  name_prefix = "${var.project_name}-${var.environment}"
  tags = merge(
    {
      Project     = "EngiFlow"
      Application = "EngiFlow"
      Environment = var.environment
      ManagedBy   = "Terraform"
      CostCenter  = var.cost_center
    },
    var.extra_tags
  )
}
