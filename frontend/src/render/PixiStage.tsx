import { useEffect, useImperativeHandle, useRef } from "react"
import type { Ref } from "react"
import type { StageState } from "../game/stageModel"
import { StageHeight, StageRenderer } from "./StageRenderer"
import type { StageHandle } from "./StageRenderer"

type Pending = { encounter?: Parameters<StageHandle["showEncounter"]>; state?: StageState }

const stageStyle = {
  position: "relative",
  height: StageHeight,
  overflow: "hidden",
  background:
    "radial-gradient(ellipse at 50% 100%, color-mix(in srgb, var(--ink) 12%, transparent), transparent 60%)",
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
    void StageRenderer.create(container).then((renderer) => {
      if (cancelled) {
        renderer.destroy()
        return
      }
      rendererRef.current = renderer
      const { encounter, state } = pendingRef.current
      if (encounter !== undefined) renderer.showEncounter(...encounter)
      if (state !== undefined) renderer.updateHealth(state)
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
