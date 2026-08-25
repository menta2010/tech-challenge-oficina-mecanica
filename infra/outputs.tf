output "cluster_name" {
  description = "Nome do cluster kind criado."
  value       = kind_cluster.this.name
}

output "kubeconfig_path" {
  description = "Caminho do kubeconfig gerado pelo kind."
  value       = kind_cluster.this.kubeconfig_path
}

output "namespace" {
  description = "Namespace provisionado."
  value       = kubernetes_namespace.oficina.metadata[0].name
}

output "database_service" {
  description = "DNS interno do banco (host da connection string)."
  value       = "${kubernetes_service.postgres.metadata[0].name}.${var.namespace}.svc.cluster.local:5432"
}
