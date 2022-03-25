const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://127.0.0.0:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://127.0.0.0:49119';

const PROXY_CONFIG = [
  {
    context: [
      "/api/**",
   ],
    target: target,
    secure: false
  }
]

module.exports = PROXY_CONFIG;
