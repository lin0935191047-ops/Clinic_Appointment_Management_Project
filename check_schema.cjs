const fs = require('fs');
const schema = JSON.parse(fs.readFileSync('render-schema.json', 'utf8'));

console.log(JSON.stringify(schema.definitions.route, null, 2));
