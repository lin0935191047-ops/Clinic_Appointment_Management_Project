const fs = require('fs');

const schema = JSON.parse(fs.readFileSync('render-schema.json', 'utf8'));

console.log('Top level properties:', Object.keys(schema.properties || {}));
const defs = schema.definitions || schema.$defs || {};
console.log('defs keys:', Object.keys(defs));

if (defs.staticService) {
  console.log('staticService properties:', Object.keys(defs.staticService.properties || {}));
}
if (defs.webService) {
  console.log('webService properties:', Object.keys(defs.webService.properties || {}));
}
if (defs.database) {
  console.log('database properties:', Object.keys(defs.database.properties || {}));
}
if (defs.envVar) {
  console.log('envVar properties:', Object.keys(defs.envVar.properties || {}));
}
if (defs.envVarFromService) {
  console.log('envVarFromService properties:', Object.keys(defs.envVarFromService.properties || {}));
}
