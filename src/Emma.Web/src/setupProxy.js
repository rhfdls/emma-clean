const { createProxyMiddleware } = require('http-proxy-middleware');

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5262';

module.exports = function(app) {
  app.use(
    '/api',
    createProxyMiddleware({
      target: API_URL,
      changeOrigin: true,
      pathRewrite: {
        '^/api': '' // Remove the /api prefix when forwarding to the backend
      },
      logLevel: 'debug',
      onProxyReq: (proxyReq, req, res) => {
        // Add any custom headers if needed
        proxyReq.setHeader('x-added', 'foobar');
      },
      onError: (err, req, res) => {
        console.error('Proxy error:', err);
        res.status(500).json({ error: 'Proxy error', details: err.message });
      }
    })
  );
};
