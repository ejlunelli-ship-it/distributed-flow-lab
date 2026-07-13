import { describe, it, expect } from 'vitest'
import { canConnect, validateConnection } from './connectionRules'

describe('connection rules', () => {
  it('accepts the canonical Producer→Exchange→Queue→Consumer chain', () => {
    expect(validateConnection('Producer', 'Exchange').valid).toBe(true)
    expect(validateConnection('Exchange', 'Queue').valid).toBe(true)
    expect(validateConnection('Queue', 'Consumer').valid).toBe(true)
  })

  it('rejects Consumer→Producer with an educational reason', () => {
    const result = validateConnection('Consumer', 'Producer')
    expect(result.valid).toBe(false)
    expect(result.reason).toMatch(/Consumer cannot connect to a Producer/)
  })

  it('treats storage nodes as terminal (no outgoing edges)', () => {
    const result = validateConnection('Database', 'Service')
    expect(result.valid).toBe(false)
    expect(result.reason).toMatch(/terminal node/)
    expect(canConnect('Cache', 'Service')).toBe(false)
  })

  it('accepts Kafka broker→topic→partition→consumer', () => {
    expect(canConnect('Broker', 'Topic')).toBe(true)
    expect(canConnect('Topic', 'Partition')).toBe(true)
    expect(canConnect('Partition', 'Consumer')).toBe(true)
  })

  it('lets a queue dead-letter to a DeadLetterQueue but not the reverse to a Producer', () => {
    expect(canConnect('Queue', 'DeadLetterQueue')).toBe(true)
    expect(canConnect('DeadLetterQueue', 'Producer')).toBe(false)
  })

  it('rejects an unrelated pairing with a specific message', () => {
    const result = validateConnection('Producer', 'Database')
    expect(result.valid).toBe(false)
    expect(result.reason).toBe('A Producer cannot connect to a Database.')
  })
})
