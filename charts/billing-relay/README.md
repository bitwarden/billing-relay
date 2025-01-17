# Billing Relay

This chart deploys Billing Relay on Kubernetes for local or ephemeral environment development.

## Install Chart

**Important:** only helm3 is supported

```console
helm install [RELEASE_NAME] .
```

## Uninstall Chart

```console
helm uninstall [RELEASE_NAME]
```

This removes all the Kubernetes components associated with the chart and deletes the release.

_See [helm uninstall](https://helm.sh/docs/helm/helm_uninstall/) for command documentation._

## Upgrading Chart

```console
helm upgrade [RELEASE_NAME] . --install
```

_See [helm upgrade](https://helm.sh/docs/helm/helm_upgrade/) for command documentation._

## Configuration

See [Customizing the Chart Before Installing](https://helm.sh/docs/intro/using_helm/#customizing-the-chart-before-installing). To see all configurable options with detailed comments, visit the chart's [values.yaml](./values.yaml), or run these configuration commands:

```console
helm show values .
```

### Billing Relay Configuration

Customize the configuration of of the billing relay service. Values here are mounted into the container as environment variables.

```yaml
config:
  ASPNETCORE_ENVIRONMENT: Production
  Logging__Console__FormatterName: json
  Logging__Console__FormatterOptions__SingleLine: true
  Logging__Console__FormatterOptions__IncludeScopes: true
  Logging__LogLevel__Default: Debug
  Logging__LogLevel__Microsoft_AspNetCore: Debug
  globalSettings__euBillingBaseAddress: https://billing.bitwarden.eu
  globalSettings__usBillingBaseAddress: https://billing.bitwarden.com
```

### Set Billing Relay Tag

Deploy a specific tag:

```yaml
image:
  tag: "0.1.52"
```

Deploy a specific SHA:

```yaml
image:
  sha256: "2c193c56effda92efb9023b4d7743663980f6567db99e8433fb81ea37e4658fe"
```

## Local Development

- Install [KIND](https://kind.sigs.k8s.io/docs/user/quick-start/)

```console
kind create cluster --config ci/kind.yaml
kubectl apply -f https://kind.sigs.k8s.io/examples/ingress/deploy-ingress-nginx.yaml
helm install billing-relay . --create-namespace --namespace billing-relay --set service.port=5000
```

To use locally build images, see [Local Registry](https://kind.sigs.k8s.io/docs/user/local-registry/)
For ACR images, see [Load an image into your cluster](https://kind.sigs.k8s.io/docs/user/quick-start/#loading-an-image-into-your-cluster)

## Values

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| replicaCount | int | `1` | This will set the replicaset count more information can be found here: https://kubernetes.io/docs/concepts/workloads/controllers/replicaset/ |
| image.repository | string | `"bitwardenprod.azurecr.io/billing-relay"` | This sets the container image more information can be found here: https://kubernetes.io/docs/concepts/containers/images/ |
| image.pullPolicy | string | `"IfNotPresent"` | This sets the container image more information can be found here: https://kubernetes.io/docs/concepts/containers/images/ |
| image.tag | string | `""` | Define a tag OR sha256 hash of the image |
| image.sha256 | string | `""` | Define a tag OR sha256 hash of the image |
| nameOverride | string | `""` | This is to override the chart name. |
| fullnameOverride | string | `""` | This is to override the chart name. |
| config | object | `{"ASPNETCORE_ENVIRONMENT":"Production","Logging__Console__FormatterName":"json","Logging__Console__FormatterOptions__IncludeScopes":true,"Logging__Console__FormatterOptions__SingleLine":true,"Logging__LogLevel__Default":"Debug","Logging__LogLevel__Microsoft_AspNetCore":"Debug","globalSettings__euBillingBaseAddress":"https://billing.bitwarden.eu","globalSettings__usBillingBaseAddress":"https://billing.bitwarden.com"}` | ConfigMap values that are mounted into the container as environment variables |
| serviceAccount.create | bool | `true` | Specifies whether a service account should be created |
| serviceAccount.automount | bool | `true` | Automatically mount a ServiceAccount's API credentials? |
| serviceAccount.annotations | object | `{}` | Annotations to add to the service account |
| serviceAccount.name | string | `""` | The name of the service account to use. If not set and create is true, a name is generated using the fullname template |
| podAnnotations | object | `{}` | This is for setting Kubernetes Annotations to a Pod. For more information checkout: https://kubernetes.io/docs/concepts/overview/working-with-objects/annotations/ |
| podLabels | object | `{}` | This is for setting Kubernetes Labels to a Pod. For more information checkout: https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/ |
| podSecurityContext | object | `{}` |  |
| securityContext | object | `{}` |  |
| service.type | string | `"ClusterIP"` | This sets the service type more information can be found here: https://kubernetes.io/docs/concepts/services-networking/service/#publishing-services-service-types |
| service.port | int | `80` | This sets the ports more information can be found here: https://kubernetes.io/docs/concepts/services-networking/service/#field-spec-ports |
| service.targetPort | int | `5000` | This sets the ports more information can be found here: https://kubernetes.io/docs/concepts/services-networking/service/#field-spec-ports |
| ingress.enabled | bool | `true` | Enable 'ingress' or not |
| ingress.className | string | `"nginx"` |  |
| ingress.annotations | object | `{}` |  |
| ingress.hosts[0].host | string | `"localhost"` |  |
| ingress.hosts[0].paths[0].path | string | `"/"` |  |
| ingress.hosts[0].paths[0].pathType | string | `"ImplementationSpecific"` |  |
| ingress.tls | list | `[]` |  |
| resources.limits.memory | string | `"512Mi"` |  |
| resources.limits.cpu | string | `"500m"` |  |
| resources.requests.memory | string | `"256Mi"` |  |
| resources.requests.cpu | string | `"100m"` |  |
| livenessProbe | list | `[]` | This is to setup the liveness and readiness probes more information can be found here: https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/ |
| readinessProbe | list | `[]` | This is to setup the liveness and readiness probes more information can be found here: https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/ |
| autoscaling.enabled | bool | `true` | Enable 'autoscaling' or not |
| autoscaling.minReplicas | int | `1` |  |
| autoscaling.maxReplicas | int | `3` |  |
| autoscaling.targetCPUUtilizationPercentage | int | `400` |  |
| volumes | list | `[]` | Additional volumes on the output Deployment definition. |
| volumeMounts | list | `[]` | Additional volumeMounts on the output Deployment definition. |
| nodeSelector | object | `{}` |  |
| tolerations | list | `[]` |  |
| affinity | object | `{}` |  |
