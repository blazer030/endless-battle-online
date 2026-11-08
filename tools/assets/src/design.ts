import sharp from "sharp"
import { AssetError } from "./process.ts"

const DesignQuality = 85
const DesignMaxBytes = 307_200

// 設計素材：不裁切、保留透明，只在比 maxWidth 寬時等比縮小
export const convertDesignAsset = async (
  source: Buffer,
  maxWidth: number | undefined,
): Promise<Buffer> => {
  const image = sharp(source)
  const resized =
    maxWidth === undefined ? image : image.resize({ width: maxWidth, withoutEnlargement: true })
  const webp = await resized
    .webp({ quality: DesignQuality, alphaQuality: 100, effort: 6 })
    .toBuffer()
  if (webp.length > DesignMaxBytes)
    throw new AssetError(
      `輸出 ${webp.length.toLocaleString()} bytes，超過上限 ${DesignMaxBytes} bytes；試試 --max-width`,
    )
  return webp
}
