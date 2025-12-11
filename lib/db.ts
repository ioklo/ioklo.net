// src/lib/db.ts
import Database from 'better-sqlite3';
import path from 'path';
import fs from 'fs';

let db: Database.Database | null = null;

function getDb() {
  if (!db) {
    const dbDir = path.join(process.cwd(), 'db');
    const dbPath = path.join(dbDir, 'app.db');

    if (!fs.existsSync(dbDir)) {
      fs.mkdirSync(dbDir, { recursive: true });
    }

    db = new Database(dbPath);
    db.pragma('journal_mode = WAL'); // 선택: 동시성/안정성 조금 좋아짐
  }
  return db;
}

// 타입 정의: ImagePost 레코드
export interface ImagePost {
  id: number;
  image_url: string;
  description: string | null;
  aspect_ratio: number | null;
  created_at: string;
}

// 전체 목록 가져오기
export function getImagePosts(): ImagePost[] {
  const db = getDb();
  const stmt = db.prepare<never, ImagePost>('SELECT * FROM image_posts ORDER BY created_at DESC');
  return stmt.all();
}

// 하나 추가하기
export function createImagePost(input: {
  imageUrl: string;
  description?: string;
  aspectRatio?: number;
}): number {
  const db = getDb();
  const stmt = db.prepare(
    `INSERT INTO image_posts (image_url, description, aspect_ratio)
     VALUES (@imageUrl, @description, @aspectRatio)`
  );
  const result = stmt.run({
    imageUrl: input.imageUrl,
    description: input.description ?? null,
    aspectRatio: input.aspectRatio ?? null,
  });

  return Number(result.lastInsertRowid);
}