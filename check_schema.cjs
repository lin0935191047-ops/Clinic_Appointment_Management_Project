const fs = require('fs');

const data = {
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
      plan: 'free',
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
            envVarKey: 'RENDER_EXTERNAL_URL'
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

const schema = JSON.parse(fs.readFileSync('render-schema.json', 'utf8'));

// Check staticService allowed keys
const staticProps = Object.keys(schema.definitions.staticService.properties);
const frontend = data.services[1];
const invalidKeys = Object.keys(frontend).filter(k => !staticProps.includes(k));
console.log('Invalid keys in clinic-frontend:', invalidKeys);

// Check serverService allowed keys
const serverProps = Object.keys(schema.definitions.serverService.properties);
const backend = data.services[0];
const invalidBackendKeys = Object.keys(backend).filter(k => !serverProps.includes(k));
console.log('Invalid keys in clinic-backend-api:', invalidBackendKeys);

// Check database allowed keys
const dbProps = Object.keys(schema.definitions.database.properties);
const db = data.databases[0];
const invalidDbKeys = Object.keys(db).filter(k => !dbProps.includes(k));
console.log('Invalid keys in database:', invalidDbKeys);
