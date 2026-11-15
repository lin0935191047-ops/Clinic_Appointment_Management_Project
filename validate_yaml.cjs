const fs = require('fs');
const yaml = require('./frontend/node_modules/js-yaml');
const Ajv2020 = require('./frontend/node_modules/ajv/dist/2020');

const schema = JSON.parse(fs.readFileSync('render-schema.json', 'utf8'));
const fileContent = fs.readFileSync('render.yaml', 'utf8');
const doc = yaml.load(fileContent);

const ajv = new Ajv2020({ allErrors: true, strict: false });
const validate = ajv.compile(schema);
const valid = validate(doc);

if (valid) {
  console.log('SUCCESS: render.yaml is completely VALID according to Render JSON Schema!');
} else {
  console.log('VALIDATION ERRORS:');
  console.log(JSON.stringify(validate.errors, null, 2));
}
