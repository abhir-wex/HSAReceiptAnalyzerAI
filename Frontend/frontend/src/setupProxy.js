const { createProxyMiddleware } = require('http-proxy-middleware');

module.exports = function(app) {
  app.use(
    '/Analyze',
    createProxyMiddleware({
      target: 'https://localhost:44395',
      changeOrigin: true,
      secure: false, // Allow self-signed certificates
      logLevel: 'debug',
      onError: (err, req, res) => {
        console.log('Proxy Error:', err);
      },
      onProxyReq: (proxyReq, req, res) => {
        console.log('Proxying request to:', proxyReq.getHeader('host') + proxyReq.path);
      }
    })
  );
};
