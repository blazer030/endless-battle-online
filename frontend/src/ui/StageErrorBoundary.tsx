import { Component } from "react"
import type { ErrorInfo, ReactNode } from "react"
import { StageHeight } from "../render/stageLayout"

type Props = { children: ReactNode }
type State = { failed: boolean }

// 舞台 chunk 載入失敗時只留空白占位，戰報與頂欄照常運作
export class StageErrorBoundary extends Component<Props, State> {
  state: State = { failed: false }

  static getDerivedStateFromError(): State {
    return { failed: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("舞台載入失敗", error, info.componentStack)
  }

  render() {
    return this.state.failed ? (
      <div data-testid="stage-fallback" style={{ height: StageHeight }} />
    ) : (
      this.props.children
    )
  }
}
