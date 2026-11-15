const fs = require('fs');
const Ajv2020 = require('./frontend/node_modules/ajv/dist/2020');

const schema = JSON.parse(fs.readFileSync('render-schema.json', 'utf8'));

const testDoc = {
  services: [
    {
      type: 'web',
      name: 'clinic-backend-api',
      runtime: 'docker',
      dockerfilePath: 'backend/Dockerfile',
      dockerContext: '.',
      plan: 'free',
      region: 'singapore',
      envVars: [
        { key: 'ASPNETCORE_ENVIRONMENT', value: 'Production' },
        { key: 'AUTO_MIGRATE', value: 'true' },
        { key: 'ENABLE_SWAGGER', value: 'true' },
        { key: 'JWT_SECRET_KEY', generateValue: true },
        {
          key: 'ConnectionStrings__DefaultConnection',
          fromDatabase: {
            name: 'clinic-postgres',
            property: 'connectionString'
          }
        }
      ]
    },
    {
      type: 'web',
      name: 'clinic-frontend',
      runtime: 'static',
      rootDir: 'frontend',
      buildCommand: 'npm ci && npm run build',
      staticPublishPath: 'dist',
      routes: [
        {
          type: 'rewrite',
          source: '/*',
          destination: '/index.html'
        }
      ],
      envVars: [
        {
          key: 'VITE_API_BASE_URL',
          fromService: {
            type: 'web',
            name: 'clinic-backend-api',
            property: 'host'
          }
        }
      ]
    }
  ],
  databases: [
    {
      name: 'clinic-postgres',
      databaseName: 'clinicappointmentdb',
      user: 'postgres',
      plan: 'free',
      region: 'singapore'
    }
  ]
};

const ajv = new Ajv2020({ allErrors: true, strict: false });
const validate = ajv.compile(schema);
const valid = validate(testDoc);

if (valid) {
  console.log('SUCCESS: testDoc is completely VALID according to Render JSON Schema!');
} else {
  console.log('VALIDATION ERRORS:');
  console.log(JSON.stringify(validate.errors, null, 2));
}
