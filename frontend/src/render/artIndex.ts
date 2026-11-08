const artFiles: Record<string, string> = import.meta.glob("../assets/art/*.webp", {
  eager: true,
  query: "?url",
  import: "default",
})

const basename = (path: string) => (path.split("/").pop() ?? path).replace(/\.[^.]+$/, "")

export const createArtResolver = (globResult: Record<string, string>) => {
  const urls = new Map<string, string>()
  for (const [path, url] of Object.entries(globResult)) urls.set(basename(path), url)
  return (contentId: string): string | null => urls.get(contentId) ?? null
}

export const resolveArt: (contentId: string) => string | null = createArtResolver(artFiles)
