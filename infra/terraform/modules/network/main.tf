data "aws_availability_zones" "available" {
  state = "available"
}

resource "aws_vpc" "this" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-vpc"
  })
}

resource "aws_internet_gateway" "this" {
  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-igw"
  })
}

resource "aws_route_table" "service" {
  vpc_id = aws_vpc.this.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.this.id
  }

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-service-rt"
  })
}

resource "aws_subnet" "service" {
  count = length(var.service_subnet_cidrs)

  vpc_id                  = aws_vpc.this.id
  cidr_block              = var.service_subnet_cidrs[count.index]
  availability_zone       = data.aws_availability_zones.available.names[count.index]
  map_public_ip_on_launch = true

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-service-${count.index + 1}"
    Tier = "service"
  })
}

resource "aws_route_table_association" "service" {
  count = length(aws_subnet.service)

  subnet_id      = aws_subnet.service[count.index].id
  route_table_id = aws_route_table.service.id
}

resource "aws_route_table" "database" {
  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-database-rt"
  })
}

resource "aws_subnet" "database" {
  count = length(var.database_subnet_cidrs)

  vpc_id            = aws_vpc.this.id
  cidr_block        = var.database_subnet_cidrs[count.index]
  availability_zone = data.aws_availability_zones.available.names[count.index]

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-database-${count.index + 1}"
    Tier = "database"
  })
}

resource "aws_route_table_association" "database" {
  count = length(aws_subnet.database)

  subnet_id      = aws_subnet.database[count.index].id
  route_table_id = aws_route_table.database.id
}

resource "aws_security_group" "api_service" {
  name        = "${var.name_prefix}-api-svc"
  description = "API ECS Express task traffic"
  vpc_id      = aws_vpc.this.id

  ingress {
    description = "Allow managed load balancer traffic from inside the VPC"
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.this.cidr_block]
  }

  egress {
    description = "Allow outbound API dependencies"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-api-svc"
  })
}

resource "aws_security_group" "web_service" {
  name        = "${var.name_prefix}-web-svc"
  description = "Web ECS Express task traffic"
  vpc_id      = aws_vpc.this.id

  ingress {
    description = "Allow managed load balancer traffic from inside the VPC"
    from_port   = 3000
    to_port     = 3000
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.this.cidr_block]
  }

  egress {
    description = "Allow outbound web dependencies"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-web-svc"
  })
}

resource "aws_security_group" "database" {
  name        = "${var.name_prefix}-db"
  description = "RDS PostgreSQL traffic from the API service"
  vpc_id      = aws_vpc.this.id

  ingress {
    description     = "PostgreSQL from API tasks"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.api_service.id]
  }

  egress {
    description = "Allow database egress"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db"
  })
}
