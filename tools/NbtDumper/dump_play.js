const mc = require('minecraft-protocol');
const fs = require('fs');

console.log("Connecting to Vanilla Server to dump Play state chunks...");

const client = mc.createClient({
  host: '127.0.0.1',
  port: 25565,
  username: 'ChunkDumperBot5',
  version: '1.21.4'
});

let dumpedViewPos = false;
let dumpedChunk = false;

client.on('packet', (data, meta, buffer, fullBuffer) => {
  if (meta.state === 'play') {
    
    if (meta.name === 'update_view_position' && !dumpedViewPos) {
        fs.writeFileSync('raw_update_view_position.bin', fullBuffer);
        console.log(`Saved raw update_view_position: ${fullBuffer.length} bytes`);
        dumpedViewPos = true;
    }
    
    if (meta.name === 'map_chunk' && !dumpedChunk) {
        fs.writeFileSync('raw_map_chunk.bin', fullBuffer);
        console.log(`Saved raw map_chunk for ${data.x}, ${data.z}: ${fullBuffer.length} bytes`);
        dumpedChunk = true;
    }
    
    if (dumpedViewPos && dumpedChunk) {
        console.log("Dumped necessary packets! Exiting...");
        process.exit(0);
    }
  }
});

client.on('error', (err) => console.log('Error:', err));
