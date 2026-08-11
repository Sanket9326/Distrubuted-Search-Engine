export const environment = {
  production: true,
  // The browser reaches the cluster through the Web UI origin. Nginx proxies
  // these paths to the internal Kubernetes services, so localhost is not
  // used from the user's machine.
  uploadServiceUrl: '',
  searchServiceUrl: '',
  prometheusUrl: '/prometheus'
};
