terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }
}

# Credentials come from the environment or the CI role (AWS_ACCESS_KEY_ID, AWS_PROFILE, OIDC), never from this file.
provider "aws" {
  region = var.region
}

resource "aws_s3_bucket" "invoices" {
  bucket = "${var.project_name}-invoices-${var.environment}"

  tags = {
    Project     = var.project_name
    Environment = var.environment
  }
}

resource "aws_s3_bucket_versioning" "invoices" {
  bucket = aws_s3_bucket.invoices.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_db_instance" "kiosk" {
  identifier          = "${var.project_name}-${var.environment}"
  engine              = "postgres"
  engine_version      = "18"
  instance_class      = "db.t4g.medium"
  allocated_storage   = 50
  username            = "kiosk"
  password            = var.db_password
  skip_final_snapshot = true

  tags = {
    Project     = var.project_name
    Environment = var.environment
  }
}
