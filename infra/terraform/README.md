# EngiFlow AWS Infrastructure

This stack deploys EngiFlow to AWS ECS Express Mode, RDS PostgreSQL, S3, SES SMTP, ECR, and AWS Budgets.

## Why ECS Express Mode

AWS App Runner closed to new customers on April 30, 2026. For new AWS accounts, ECS Express Mode is the managed replacement that keeps the no-raw-IaaS operating model while exposing the underlying AWS resources when needed.

## Bootstrap

Run `infra/terraform/bootstrap` once, then set these GitHub repository variables from its outputs:

- `AWS_REGION`
- `AWS_ROLE_TO_ASSUME`
- `TF_STATE_BUCKET`
- `TF_STATE_LOCK_TABLE`
- `TF_STATE_KEY` (optional, default `engiflow/prod.tfstate`)
- `FRONTEND_PUBLIC_BASE_URL`
- `BUDGET_ALERT_EMAIL`

Set these GitHub repository secrets:

- `SES_SMTP_USERNAME`
- `SES_SMTP_PASSWORD`

## Network Cost Posture

The ECS Express services run in public service subnets so they can reach ECR, S3, and SES without a NAT gateway. Security groups only allow inbound container traffic from inside the VPC; RDS remains in private subnets and accepts PostgreSQL only from the API service security group.

## Deployment

The deploy workflow first applies only ECR repositories, pushes the API image, deploys the API service, captures its public URL, builds the web image with that URL for `NEXT_PUBLIC_API_BASE_URL`, then applies the full stack with both image tags.

Manual local commands:

```bash
terraform init \
  -backend-config="bucket=$TF_STATE_BUCKET" \
  -backend-config="key=engiflow/prod.tfstate" \
  -backend-config="region=$AWS_REGION" \
  -backend-config="dynamodb_table=$TF_STATE_LOCK_TABLE" \
  -backend-config="encrypt=true"
terraform plan
```
