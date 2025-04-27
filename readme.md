# Dynamic API Proxy Sample

After [DiscoveryViaKubernetesApiSample](https://github.com/joaofbantunes/DiscoveryViaKubernetesApiSample), felt like trying to quickly build an API proxy that would be configured through annotations on Kubernetes services.

## Trying out the sample

Start by creating the container image. There's a Docker compose file in the root just to make that easier, by running:

```bash
docker compose build
```

Then we can run the proxy application, as well as a couple of sample services (using Traefik's [whoami](https://github.com/traefik/whoami) sample).

```bash
kubectl apply -f ./k8s
```

We can curl the first service through the proxy, to see it working:

```bash
curl -v http://localhost:8080/sample-whoami-1/api
```

We can also curl the second service:

```bash
curl -v http://localhost:8080/sample-whoami-2/api
```