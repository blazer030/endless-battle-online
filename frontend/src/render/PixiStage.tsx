import { useEffect, useImperativeHandle, useRef } from "react"
import type { Ref } from "react"
import type { StageState } from "../game/stageModel"
import { StageRenderer } from "./StageRenderer"
import type { StageHandle } from "./StageRenderer"
import { StageHeight } from "./stageLayout"

type Pending = { encounter?: Parameters<StageHandle["showEncounter"]>; state?: StageState }

const stageStyle = {
  position: "relative",
  height: StageHeight,
  overflow: "hidden",
  background:
    "radial-gradient(ellipse at 50% 100%, color-mix(in srgb, var(--cyan) 7%, transparent), transparent 60%)",
} as const

export function PixiStage({ ref }: { ref?: Ref<StageHandle> }) {
  const containerRef = useRef<HTMLDivElement>(null)
  const rendererRef = useRef<StageRenderer | null>(null)
  // 舞台初始化完成前收到的遭遇與血量，就緒後補上
  const pendingRef = useRef<Pending>({})

  useEffect(() => {
    const container = containerRef.current
    if (container === null) return
    let cancelled = false
    StageRenderer.create(container)
      .then((renderer) => {
        if (cancelled) {
          renderer.destroy()
          return
        }
        rendererRef.current = renderer
        const { encounter, state } = pendingRef.current
        if (encounter !== undefined) renderer.showEncounter(...encounter)
        if (state !== undefined) renderer.updateHealth(state)
      })
      .catch((error: unknown) => {
        // WebGL 無法初始化時舞台留白，戰報照常運作
        console.error("舞台初始化失敗", error)
      })
    return () => {
      cancelled = true
      rendererRef.current?.destroy()
      rendererRef.current = null
    }
  }, [])

  useImperativeHandle(
    ref,
    () => ({
      showEncounter: (...args) => {
        pendingRef.current = { encounter: args, state: args[2] }
        rendererRef.current?.showEncounter(...args)
      },
      updateHealth: (state) => {
        pendingRef.current.state = state
        rendererRef.current?.updateHealth(state)
      },
      play: (effects) => rendererRef.current?.play(effects),
    }),
    [],
  )

  return <div ref={containerRef} style={stageStyle} />
}
