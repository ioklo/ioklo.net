// src/app/page.tsx
import { getImagePosts } from '@/lib/db';
import Image from 'next/image';

export const revalidate = 0; // 개발 중 캐시 끄기

export default function HomePage() {
  const posts = getImagePosts(); // sync 호출

  return (
    <main className="mx-auto max-w-xl p-4 space-y-4">
      <h1 className="text-2xl font-bold mb-4">My Insta-like Feed</h1>

      {posts.length === 0 && (
        <p className="text-gray-500">아직 게시물이 없습니다.</p>
      )}

      <div className="space-y-8">
        {posts.map((post) => (
          <article
            key={post.id}
            className="border rounded-lg overflow-hidden bg-white"
          >
            <div className="bg-black">
              <div className="relative w-full aspect-[4/3]">
                <Image
                  src={post.image_url}
                  alt={post.description ?? ''}
                  fill
                  className="object-cover"
                />
              </div>
            </div>
            <div className="p-3">
              {post.description && (
                <p className="text-sm whitespace-pre-wrap">
                  {post.description}
                </p>
              )}
            </div>
          </article>
        ))}
      </div>
    </main>
  );
}