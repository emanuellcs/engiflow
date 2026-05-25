variable "domain_identity" {
  type    = string
  default = ""
}

variable "email_identity" {
  type    = string
  default = ""
}

variable "tags" {
  type = map(string)
}
