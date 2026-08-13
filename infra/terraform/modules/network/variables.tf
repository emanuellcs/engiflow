variable "name_prefix" {
  type = string
}

variable "vpc_cidr" {
  type = string
}

variable "service_subnet_cidrs" {
  type = list(string)
}

variable "database_subnet_cidrs" {
  type = list(string)
}

variable "tags" {
  type = map(string)
}
