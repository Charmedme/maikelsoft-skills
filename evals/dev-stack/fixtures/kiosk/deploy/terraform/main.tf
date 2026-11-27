terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }
}

provider "aws" {
  region     = var.region
  access_key = "kiosk-test-only-access-key-id"
  secret_key = "kiosk-test-only-secret-access-key-0b7d3e9a51c4"
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
