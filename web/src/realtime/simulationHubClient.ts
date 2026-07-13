import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import type { SimulationEvent, SimulationStateDto } from '@/domain'

/**
 * Thin wrapper over the `SimulationHub` connection (websocket-events.md,
 * ADR-002). Owns connect / subscribe / reconnect; contains NO domain logic —
 * received payloads are handed verbatim to the callbacks (the store owns
 * ordering and gap handling).
 */
export interface SimulationHubCallbacks {
  onEvent: (event: SimulationEvent) => void
  onStateChanged: (state: SimulationStateDto) => void
  /**
   * Fired after an automatic reconnect, once the subscription is restored.
   * The caller should backfill missed events via
   * GET /api/v1/simulations/{id}/events?fromSequence= (websocket-events.md §6).
   */
  onResubscribed?: (simulationId: string) => void
}

export class SimulationHubClient {
  private readonly connection: HubConnection
  private subscribedSimulationId: string | null = null

  constructor(callbacks: SimulationHubCallbacks, hubUrl = '/hubs/simulation') {
    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    this.connection.on('ReceiveSimulationEvent', callbacks.onEvent)
    this.connection.on('SimulationStateChanged', callbacks.onStateChanged)

    // Group membership does not survive a new connection: re-subscribe, then
    // let the caller reconcile missed events from the REST timeline.
    this.connection.onreconnected(() => {
      const simulationId = this.subscribedSimulationId
      if (simulationId !== null) {
        void this.connection
          .invoke('Subscribe', simulationId)
          .then(() => callbacks.onResubscribed?.(simulationId))
      }
    })
  }

  async start(): Promise<void> {
    if (this.connection.state === HubConnectionState.Disconnected) {
      await this.connection.start()
    }
  }

  async subscribe(simulationId: string): Promise<void> {
    if (this.subscribedSimulationId === simulationId) {
      return
    }
    if (this.subscribedSimulationId !== null) {
      await this.connection.invoke('Unsubscribe', this.subscribedSimulationId)
    }
    await this.connection.invoke('Subscribe', simulationId)
    this.subscribedSimulationId = simulationId
  }

  async stop(): Promise<void> {
    this.subscribedSimulationId = null
    await this.connection.stop()
  }
}
