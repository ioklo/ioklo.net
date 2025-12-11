CREATE TABLE IF NOT EXISTS image_posts (
  id           INTEGER PRIMARY KEY AUTOINCREMENT,
  image_url    TEXT NOT NULL,
  description  TEXT,
  aspect_ratio REAL,
  created_at   TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

