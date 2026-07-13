import { create } from 'zustand'
import {
  applyEdgeChanges,
  applyNodeChanges,
  type Connection,
  type Edge,
  type EdgeChange,
  type Node,
  type NodeChange,
  type XYPosition,
} from '@xyflow/react'
import {
  defaultConfigFor,
  validateConnection,
  type FlowEdgeData,
  type FlowNodeData,
  type NodeConfig,
  type NodeType,
} from '@/domain'

/** React Flow node/edge specialized with the DFL domain payloads. */
export type DflNode = Node<FlowNodeData>
export type DflEdge = Edge<FlowEdgeData>

/** React Flow custom type keys (registered in CanvasEditor's `nodeTypes`/`edgeTypes`). */
export const DFL_NODE_TYPE = 'dflNode'
export const DFL_EDGE_TYPE = 'dflEdge'

export interface ConnectResult {
  ok: boolean
  /** Educational reason a connection was rejected (surfaced inline). */
  reason?: string
}

/**
 * Authoritative canvas state (ADR-001, ADR-016). React Flow is a *controlled* renderer over this
 * Zustand store — it never becomes a second source of truth. The store owns the topology
 * (`nodes`/`edges`), the current selection, and a `dirty` flag; connection legality is enforced
 * here via the domain rules (`validateConnection`) plus identity checks (self-loop, duplicate).
 */
interface CanvasState {
  nodes: DflNode[]
  edges: DflEdge[]
  selectedNodeId: string | null
  selectedEdgeId: string | null
  /** Unsaved-changes flag; drives the save affordance and the dirty-navigation guard. */
  dirty: boolean

  /** Monotonic counters producing stable, readable ids without relying on wall-clock/random. */
  nodeSeq: number
  edgeSeq: number

  /** Creates a node of `type` at `position` with its default config, and selects it. */
  addNode: (type: NodeType, position: XYPosition) => void
  /** Applies React Flow node changes (drag, select, remove) to the store. */
  onNodesChange: (changes: NodeChange<DflNode>[]) => void
  /** Applies React Flow edge changes (select, remove) to the store. */
  onEdgesChange: (changes: EdgeChange<DflEdge>[]) => void
  /** Validates and, if legal, creates a directed edge. Returns the verdict for inline feedback. */
  connect: (connection: Connection) => ConnectResult
  updateNodeConfig: (nodeId: string, patch: NodeConfig) => void
  updateNodeLabel: (nodeId: string, label: string) => void
  updateEdgeLabel: (edgeId: string, label: string) => void
  setSelection: (selection: {
    nodeId?: string | null
    edgeId?: string | null
  }) => void
  /** Removes the selected node (and incident edges) or the selected edge. */
  removeSelected: () => void
  reset: () => void
}

const initial = {
  nodes: [] as DflNode[],
  edges: [] as DflEdge[],
  selectedNodeId: null as string | null,
  selectedEdgeId: null as string | null,
  dirty: false,
  nodeSeq: 0,
  edgeSeq: 0,
}

export const useCanvasStore = create<CanvasState>((set, get) => ({
  ...initial,

  addNode: (type, position) => {
    const seq = get().nodeSeq + 1
    const id = `node-${type.toLowerCase()}-${seq}`
    const node: DflNode = {
      id,
      type: DFL_NODE_TYPE,
      position,
      selected: true,
      data: { type, label: type, config: defaultConfigFor(type) },
    }
    set((s) => ({
      nodes: [...s.nodes.map((n) => ({ ...n, selected: false })), node],
      nodeSeq: seq,
      selectedNodeId: id,
      selectedEdgeId: null,
      dirty: true,
    }))
  },

  onNodesChange: (changes) => {
    const next = applyNodeChanges(changes, get().nodes)
    const structural = changes.some(
      (c) => c.type === 'position' || c.type === 'remove' || c.type === 'add',
    )
    set({ nodes: next, dirty: get().dirty || structural })
  },

  onEdgesChange: (changes) => {
    const next = applyEdgeChanges(changes, get().edges)
    const structural = changes.some(
      (c) => c.type === 'remove' || c.type === 'add',
    )
    set({ edges: next, dirty: get().dirty || structural })
  },

  connect: (connection) => {
    const { source, target } = connection
    if (!source || !target) {
      return { ok: false, reason: 'A connection needs a source and a target.' }
    }
    if (source === target) {
      return { ok: false, reason: 'A node cannot connect to itself.' }
    }

    const { nodes, edges, edgeSeq } = get()
    const sourceNode = nodes.find((n) => n.id === source)
    const targetNode = nodes.find((n) => n.id === target)
    if (!sourceNode || !targetNode) {
      return { ok: false, reason: 'Unknown source or target node.' }
    }

    if (edges.some((e) => e.source === source && e.target === target)) {
      return { ok: false, reason: 'These nodes are already connected.' }
    }

    const verdict = validateConnection(
      sourceNode.data.type,
      targetNode.data.type,
    )
    if (!verdict.valid) {
      return { ok: false, reason: verdict.reason }
    }

    const seq = edgeSeq + 1
    const edge: DflEdge = {
      id: `edge-${seq}`,
      source,
      target,
      type: DFL_EDGE_TYPE,
      data: { label: '', config: {} },
    }
    set({ edges: [...edges, edge], edgeSeq: seq, dirty: true })
    return { ok: true }
  },

  updateNodeConfig: (nodeId, patch) => {
    set((s) => ({
      nodes: s.nodes.map((n) =>
        n.id === nodeId
          ? {
              ...n,
              data: { ...n.data, config: { ...n.data.config, ...patch } },
            }
          : n,
      ),
      dirty: true,
    }))
  },

  updateNodeLabel: (nodeId, label) => {
    set((s) => ({
      nodes: s.nodes.map((n) =>
        n.id === nodeId ? { ...n, data: { ...n.data, label } } : n,
      ),
      dirty: true,
    }))
  },

  updateEdgeLabel: (edgeId, label) => {
    set((s) => ({
      edges: s.edges.map((e) =>
        e.id === edgeId
          ? { ...e, label, data: { ...(e.data ?? { config: {} }), label } }
          : e,
      ),
      dirty: true,
    }))
  },

  setSelection: ({ nodeId, edgeId }) => {
    set({
      selectedNodeId: nodeId ?? null,
      selectedEdgeId: edgeId ?? null,
    })
  },

  removeSelected: () => {
    const { selectedNodeId, selectedEdgeId, nodes, edges } = get()
    if (selectedNodeId) {
      set({
        nodes: nodes.filter((n) => n.id !== selectedNodeId),
        edges: edges.filter(
          (e) => e.source !== selectedNodeId && e.target !== selectedNodeId,
        ),
        selectedNodeId: null,
        dirty: true,
      })
      return
    }
    if (selectedEdgeId) {
      set({
        edges: edges.filter((e) => e.id !== selectedEdgeId),
        selectedEdgeId: null,
        dirty: true,
      })
    }
  },

  reset: () => set({ ...initial }),
}))
