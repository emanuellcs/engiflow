output "api_service_url" {
  value = local.api_service_url
}

output "web_service_url" {
  value = local.web_service_url
}

output "api_service_arn" {
  value = aws_ecs_express_gateway_service.api.service_arn
}

output "web_service_arn" {
  value = aws_ecs_express_gateway_service.web.service_arn
}
