import Database from 'better-sqlite3';
import fs from 'fs';
import path from 'path';

const dbDir = path.join(process.cwd(), 'db');
const dbPath = path.join(dbDir, 'app.db');
const schemaPath = path.join(dbDir, 'schema.sql');

if (!fs.existsSync(dbDir)) {
  fs.mkdirSync(dbDir, { recursive: true });
}

const schema = fs.readFileSync(schemaPath, 'utf8');

const db = new Database(dbPath);
db.exec(schema);
db.close();

console.log('SQLite DB initialized at', dbPath);