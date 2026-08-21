const fs = require('fs');
const proto = JSON.parse(fs.readFileSync('protocol.json', 'utf8'));

console.log("LOGIN TOCLIENT PACKETS:");
const loginToClient = proto.login.toClient.types;
for (let key in loginToClient) {
    if (key.startsWith('packet_')) {
        console.log(key, JSON.stringify(loginToClient[key]));
    }
}
