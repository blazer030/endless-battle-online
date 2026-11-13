import { fireEvent, render, screen } from "@testing-library/react"
import { expect, test } from "vitest"
import { ContentImage } from "./ContentImage"

// 寵物沒有怪物內容圖時用通用剪影，而不是怪物類的灰階佔位圖
test("ContentImage_UsesTheGivenPlaceholder", () => {
  render(
    <ContentImage
      category="monsters"
      assetKey="monster-9999"
      alt="寵物"
      placeholder="/pet-silhouette.webp"
    />,
  )

  expect(screen.getByRole("img", { name: "寵物" })).toHaveAttribute("src", "/pet-silhouette.webp")
})

// 有圖但載入失敗時也退回指定的佔位圖
test("ContentImage_FailedImageFallsBackToTheGivenPlaceholder", () => {
  render(
    <ContentImage category="items" assetKey="item-1304" alt="勳章" placeholder="/fallback.webp" />,
  )
  const image = screen.getByRole("img", { name: "勳章" })
  expect(image.getAttribute("src")).toContain("items/item-1304.")

  fireEvent.error(image)

  expect(image).toHaveAttribute("src", "/fallback.webp")
})
