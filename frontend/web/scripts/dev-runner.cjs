const http = require('http');
const next = require('next');

const port = process.env.PORT ? Number(process.env.PORT) : 4000;
const dev = true;

async function main() {
  const app = next({ dev, port });
  const handle = app.getRequestHandler();
  await app.prepare();
  const server = http.createServer((req, res) => {
    handle(req, res);
  });
  server.listen(port, () => {
    console.log(`Next.js dev server running at http://localhost:${port}`);
  });
}

main().catch((err) => {
  console.error('Failed to start Next.js dev server:', err);
  process.exit(1);
});
