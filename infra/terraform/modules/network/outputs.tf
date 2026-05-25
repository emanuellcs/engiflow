output "vpc_id" {
  value = aws_vpc.this.id
}

output "service_subnet_ids" {
  value = aws_subnet.service[*].id
}

output "database_subnet_ids" {
  value = aws_subnet.database[*].id
}

output "api_service_security_group_id" {
  value = aws_security_group.api_service.id
}

output "web_service_security_group_id" {
  value = aws_security_group.web_service.id
}

output "database_security_group_id" {
  value = aws_security_group.database.id
}
