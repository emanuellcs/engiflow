# EngiFlow Terraform Bootstrap

This one-time stack creates the remote state bucket, DynamoDB lock table, and GitHub Actions OIDC deployment role.

Example:

```bash
terraform init
terraform apply \
  -var='github_owner=YOUR_ORG' \
  -var='github_repo=engiflow'
```

Copy the outputs into GitHub repository variables:

- `AWS_REGION`
- `AWS_ROLE_TO_ASSUME`
- `TF_STATE_BUCKET`
- `TF_STATE_LOCK_TABLE`

The application stack uses `TF_STATE_KEY=engiflow/prod.tfstate` unless you override it in GitHub variables.
