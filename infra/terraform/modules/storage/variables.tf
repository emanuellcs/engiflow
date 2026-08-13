variable "name_prefix" {
  type = string
}

variable "bucket_name" {
  type    = string
  default = ""
}

variable "aws_region" {
  type = string
}

variable "account_id" {
  type = string
}

variable "tags" {
  type = map(string)
}
