# Provisiona um cluster Kubernetes local com kind e mapeia o NodePort da API para o host.
resource "kind_cluster" "this" {
  name           = var.cluster_name
  wait_for_ready = true

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    node {
      role = "control-plane"

      extra_port_mappings {
        container_port = var.api_node_port
        host_port      = var.api_node_port
      }
    }
  }
}

# Configura o provider kubernetes a partir das credenciais geradas pelo cluster kind.
provider "kubernetes" {
  host                   = kind_cluster.this.endpoint
  cluster_ca_certificate = kind_cluster.this.cluster_ca_certificate
  client_certificate     = kind_cluster.this.client_certificate
  client_key             = kind_cluster.this.client_key
}

resource "kubernetes_namespace" "oficina" {
  metadata {
    name = var.namespace
  }
}
