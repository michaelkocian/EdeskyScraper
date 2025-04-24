import axios from 'axios';
import cheerio from 'cheerio';
import fs from 'fs';

const URL = 'https://egov.opava-city.cz/Uredni_deska/SeznamDokumentu.aspx';
const WEBHOOK_URL = 'https://discord.com/api/webhooks/1365045974917058672/PsJhdkjYRAXzPanuNeFWqnR3MOWRhGziGTQAZWtc2iSRRGeq6jUymq63K_7mUi37QeQx';
const LAST_FILE = './last.txt';

async function scrapeAndNotify() {
  try {
    const response = await axios.get(URL);
    const $ = cheerio.load(response.data);
    const content = $('table.SeznamTable').text().trim(); // uprav si selektor

    let lastContent = '';
    if (fs.existsSync(LAST_FILE)) {
      lastContent = fs.readFileSync(LAST_FILE, 'utf8');
    }

    if (content !== lastContent) {
      await axios.post(WEBHOOK_URL, {
        content: `🔔 Změna na webu: ${content.slice(0, 200)}...`,
      });
      fs.writeFileSync(LAST_FILE, content, 'utf8');
    }

  } catch (err) {
    console.error('❌ Chyba při scrapování:', err.message);
  }
}

scrapeAndNotify();
