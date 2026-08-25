variable "cluster_name" {
  description = "Nome do cluster kind."
  type        = string
  default     = "oficina"
}

variable "namespace" {
  description = "Namespace onde os recursos serao criados."
  type        = string
  default     = "oficina"
}

variable "api_node_port" {
  description = "NodePort exposto no host para acessar a API."
  type        = number
  default     = 30080
}

variable "db_name" {
  description = "Nome do banco de dados."
  type        = string
  default     = "oficina"
}

variable "db_user" {
  description = "Usuario do banco."
  type        = string
  default     = "oficina"
}

variable "db_password" {
  description = "Senha do banco (sobrescreva em producao via TF_VAR_db_password)."
  type        = string
  default     = "oficina-db-pass-CHANGE-ME"
  sensitive   = true
}
