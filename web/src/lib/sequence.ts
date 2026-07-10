/**
 * Helpers for reasoning about the monotonic `sequence` that orders
 * SimulationEvents. Used by the realtime pipeline to detect gaps after a
 * reconnect and trigger backfill via GET /api/v1/simulations/{id}/events?fromSequence=.
 */

/**
 * Returns the missing sequence numbers between the last contiguous sequence the
 * client has and the newly received sequence. Returns an empty array when the
 * received sequence is the expected next one (or older/duplicate).
 *
 * @param lastContiguousSequence the highest sequence with no known gaps (-1 if none yet)
 * @param receivedSequence the sequence just received
 */
export function findSequenceGap(
  lastContiguousSequence: number,
  receivedSequence: number,
): number[] {
  const gap: number[] = []
  for (let s = lastContiguousSequence + 1; s < receivedSequence; s++) {
    gap.push(s)
  }
  return gap
}
