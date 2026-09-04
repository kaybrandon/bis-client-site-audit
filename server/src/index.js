import express from 'express';
import cors from 'cors';
import { auditSite } from './audit.js';

const PORT = process.env.PORT || 3001;
const app = express();

app.use(cors());
app.use(express.json());

app.get('/api/health', (_req, res) => {
  res.json({ status: 'ok', service: 'bis-client-site-audit', time: new Date().toISOString() });
});

app.post('/api/audit', async (req, res) => {
  const { url } = req.body || {};
  try {
    const report = await auditSite(url);
    res.json(report);
  } catch (err) {
    res.status(400).json({ error: err.message });
  }
});

app.listen(PORT, () => {
  console.log(`[audit-server] listening on http://localhost:${PORT}`);
});
