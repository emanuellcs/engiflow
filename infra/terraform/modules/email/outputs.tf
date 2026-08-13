output "domain_verification_token" {
  value = try(aws_ses_domain_identity.this[0].verification_token, null)
}

output "dkim_tokens" {
  value = try(aws_ses_domain_dkim.this[0].dkim_tokens, [])
}
