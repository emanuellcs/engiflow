variable "name_prefix" {
  type = string
}

variable "monthly_limit_usd" {
  type = number
}

variable "alert_email" {
  type = string
}

variable "tags" {
  type = map(string)
}
