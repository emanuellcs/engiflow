resource "aws_ses_domain_identity" "this" {
  count = var.domain_identity != "" ? 1 : 0

  domain = var.domain_identity
}

resource "aws_ses_domain_dkim" "this" {
  count = var.domain_identity != "" ? 1 : 0

  domain = aws_ses_domain_identity.this[0].domain
}

resource "aws_ses_email_identity" "this" {
  count = var.email_identity != "" ? 1 : 0

  email = var.email_identity
}
