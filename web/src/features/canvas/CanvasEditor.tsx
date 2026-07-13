import { useCallback, useEffect, type DragEvent } from 'react'
import {
  Background,
  Controls,
  MarkerType,
  MiniMap,
  ReactFlow,
  useReactFlow,
  type Connection,
  type EdgeTypes,
  type NodeTypes,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import {
  DFL_EDGE_TYPE,
  DFL_NODE_TYPE,
  useCanvasStore,
  type DflNode,
} from '@/state/canvasStore'
import { useUiStore } from '@/state/uiStore'
import { FlowNode } from './FlowNode'
import { FlowEdge } from './FlowEdge'
import { NODE_DND_MIME, nodeAccentVar } from './nodeAccent'
import { NODE_TYPES, type NodeType } from '@/domain'

// Registered once at module scope: React Flow warns and re-renders if these are re-created.
const nodeTypes: NodeTypes = { [DFL_NODE_TYPE]: FlowNode }
const edgeTypes: EdgeTypes = { [DFL_EDGE_TYPE]: FlowEdge }
const defaultEdgeOptions = {
  type: DFL_EDGE_TYPE,
  markerEnd: { type: MarkerType.ArrowClosed },
}

const isNodeType = (value: string): value is NodeType =>
  (NODE_TYPES as readonly string[]).includes(value)

/**
 * The React Flow editing surface (Epic 1). It is a *controlled* view over the `canvasStore`
 * (ADR-001): topology, selection, and validation all live in the store, and this component only
 * wires React Flow's callbacks to it. Illegal connections are rejected by the store and the
 * reason is surfaced inline (Epic 1 acceptance criteria). Drag from the palette or drop creates
 * nodes at the pointer.
 */
export function CanvasEditor() {
  const nodes = useCanvasStore((s) => s.nodes)
  const edges = useCanvasStore((s) => s.edges)
  const onNodesChange = useCanvasStore((s) => s.onNodesChange)
  const onEdgesChange = useCanvasStore((s) => s.onEdgesChange)
  const connect = useCanvasStore((s) => s.connect)
  const addNode = useCanvasStore((s) => s.addNode)
  const setSelection = useCanvasStore((s) => s.setSelection)

  const connectionError = useUiStore((s) => s.connectionError)
  const setConnectionError = useUiStore((s) => s.setConnectionError)

  const { screenToFlowPosition } = useReactFlow()

  const onConnect = useCallback(
    (connection: Connection) => {
      const result = connect(connection)
      setConnectionError(
        result.ok ? null : (result.reason ?? 'Invalid connection.'),
      )
    },
    [connect, setConnectionError],
  )

  const onDragOver = useCallback((event: DragEvent) => {
    event.preventDefault()
    event.dataTransfer.dropEffect = 'move'
  }, [])

  const onDrop = useCallback(
    (event: DragEvent) => {
      event.preventDefault()
      const raw = event.dataTransfer.getData(NODE_DND_MIME)
      if (!raw || !isNodeType(raw)) return
      const position = screenToFlowPosition({
        x: event.clientX,
        y: event.clientY,
      })
      addNode(raw, position)
    },
    [addNode, screenToFlowPosition],
  )

  // Auto-dismiss the inline validation message.
  useEffect(() => {
    if (!connectionError) return
    const timer = setTimeout(() => setConnectionError(null), 3500)
    return () => clearTimeout(timer)
  }, [connectionError, setConnectionError])

  return (
    <div
      className="relative h-full w-full"
      onDrop={onDrop}
      onDragOver={onDragOver}
    >
      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        defaultEdgeOptions={defaultEdgeOptions}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onSelectionChange={({ nodes: selNodes, edges: selEdges }) =>
          setSelection({
            nodeId: (selNodes[0] as DflNode | undefined)?.id ?? null,
            edgeId: selEdges[0]?.id ?? null,
          })
        }
        deleteKeyCode={['Delete', 'Backspace']}
        connectionRadius={30}
        fitView
        proOptions={{ hideAttribution: false }}
      >
        <Background />
        <MiniMap
          pannable
          zoomable
          nodeColor={(n) => nodeAccentVar((n as DflNode).data.type)}
        />
        <Controls />
      </ReactFlow>

      {connectionError && (
        <div
          role="alert"
          className="absolute left-1/2 top-4 z-10 -translate-x-1/2 rounded-md border border-danger bg-surface px-3 py-2 text-sm text-danger shadow-md"
        >
          {connectionError}
        </div>
      )}
    </div>
  )
}
