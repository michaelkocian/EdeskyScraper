 
import https from 'https';
import fs from 'fs';

const URL = 'https://egov.opava-city.cz/Uredni_deska/SeznamDokumentu.aspx';
const WEBHOOK_URL = 'https://discord.com/api/webhooks/1365045974917058672/PsJhdkjYRAXzPanuNeFWqnR3MOWRhGziGTQAZWtc2iSRRGeq6jUymq63K_7mUi37QeQx';
const LAST_FILE = 'last.txt';

function fetchPage(url) {
  return new Promise((resolve, reject) => {
    https.get(url, (res) => {
      let data = '';

      res.on('data', (chunk) => data += chunk);
      res.on('end', () => resolve(data));
    }).on('error', reject);
  });
}

function sendDiscordNotification(message) {
  const payload = JSON.stringify({ content: message });

  const req = https.request(
    new URL(WEBHOOK_URL),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(payload),
      },
    },
    (res) => {
      res.on('data', () => {}); // noop
    }
  );

  req.on('error', console.error);
  req.write(payload);
  req.end();
}

async function main() {
  try {
    const html = await fetchPage(URL);

    // Jednoduché vyčištění (volitelné): odstranění skriptů, stylů atd.
    const cleaned = html
      .replace(/<script[\s\S]*?<\/script>/gi, '')
      .replace(/<style[\s\S]*?<\/style>/gi, '')
      .replace(/<[^>]+>/g, '') // vyhodí všechny HTML tagy
      .replace(/\s+/g, ' ')    // normalizuje mezery
      .trim();

    let last = '';
    if (fs.existsSync(LAST_FILE)) {
      last = fs.readFileSync(LAST_FILE, 'utf8');
    }

    if (cleaned !== last) {
      sendDiscordNotification(`🔔 Změna na webu detekována:\n\n${cleaned.slice(0, 300)}...`);
      fs.writeFileSync(LAST_FILE, cleaned, 'utf8');
    }
  } catch (err) {
    console.error('❌ Chyba:', err.message);
    sendDiscordNotification(`❗ Chyba při scrapování: ${err.message}`);
  }
}

main();
