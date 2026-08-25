# Banco de dados PostgreSQL provisionado pelo Terraform (requisito Fase 2: cluster + banco).
resource "kubernetes_secret" "postgres" {
  metadata {
    name      = "oficina-postgres-secret"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }
  data = {
    POSTGRES_DB       = var.db_name
    POSTGRES_USER     = var.db_user
    POSTGRES_PASSWORD = var.db_password
  }
  type = "Opaque"
}

resource "kubernetes_persistent_volume_claim" "postgres" {
  metadata {
    name      = "oficina-postgres-pvc"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }
  spec {
    access_modes = ["ReadWriteOnce"]
    resources {
      requests = {
        storage = "1Gi"
      }
    }
  }
  wait_until_bound = false
}

resource "kubernetes_deployment" "postgres" {
  metadata {
    name      = "oficina-postgres"
    namespace = kubernetes_namespace.oficina.metadata[0].name
    labels    = { app = "oficina-postgres" }
  }
  spec {
    replicas = 1
    selector {
      match_labels = { app = "oficina-postgres" }
    }
    strategy {
      type = "Recreate"
    }
    template {
      metadata {
        labels = { app = "oficina-postgres" }
      }
      spec {
        container {
          name  = "postgres"
          image = "postgres:16-alpine"

          port {
            container_port = 5432
          }
          env_from {
            secret_ref {
              name = kubernetes_secret.postgres.metadata[0].name
            }
          }
          env {
            name  = "PGDATA"
            value = "/var/lib/postgresql/data/pgdata"
          }
          volume_mount {
            name       = "pgdata"
            mount_path = "/var/lib/postgresql/data"
          }
          resources {
            requests = { cpu = "100m", memory = "128Mi" }
            limits   = { cpu = "500m", memory = "512Mi" }
          }
          readiness_probe {
            exec {
              command = ["sh", "-c", "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB"]
            }
            initial_delay_seconds = 10
            period_seconds        = 10
          }
        }
        volume {
          name = "pgdata"
          persistent_volume_claim {
            claim_name = kubernetes_persistent_volume_claim.postgres.metadata[0].name
          }
        }
      }
    }
  }
}

resource "kubernetes_service" "postgres" {
  metadata {
    name      = "oficina-postgres"
    namespace = kubernetes_namespace.oficina.metadata[0].name
    labels    = { app = "oficina-postgres" }
  }
  spec {
    selector = { app = "oficina-postgres" }
    port {
      port        = 5432
      target_port = 5432
    }
    type = "ClusterIP"
  }
}
