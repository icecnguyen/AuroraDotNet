const mc = require('minecraft-protocol');
const fs = require('fs');

console.log("Connecting to Vanilla Server to dump RAW registry_data...");

const client = mc.createClient({
  host: '127.0.0.1',
  port: 25565,
  username: 'RawDumperBot',
  version: '1.21.4'
});

client.on('packet', (data, meta, buffer, fullBuffer) => {
  if (meta.name === 'registry_data') {
      const safeName = data.id.replace(':', '_').replace('/', '_');
      fs.writeFileSync(`raw_${safeName}.bin`, fullBuffer);
      console.log(`Saved raw buffer for ${data.id}: ${fullBuffer.length} bytes`);
  }
});

client.on('login', () => {
  console.log("Logged in! Exiting soon...");
  setTimeout(() => process.exit(0), 1000);
});

client.on('error', (err) => console.log('Error:', err));

client.on('error', (err) => console.log('Lỗi:', err));
