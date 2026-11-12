const API_BASE = '/api'

function getToken(): string | null {
  return localStorage.getItem('token')
}

export function setToken(token: string): void {
  localStorage.setItem('token', token)
}

export function clearToken(): void {
  localStorage.removeItem('token')
}

export function isAuthenticated(): boolean {
  return !!getToken()
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })

  if (res.status === 401) {
    clearToken()
    window.location.href = '/login'
    throw new Error('Unauthorized')
  }

  if (!res.ok) {
    throw new Error(`API error ${res.status}`)
  }

  return res.json() as Promise<T>
}

// Auth
export async function login(username: string, password: string): Promise<void> {
  const data = await request<{ token: string }>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
  setToken(data.token)
}

// Solar
export interface SolarSummary {
  powerW: number
  energyTodayKwh: number
  energyMonthKwh: number
  energyTotalKwh: number
  status: number
  lastUpdated: string
  deviceModel: string
  nominalPowerW: number
  plantName: string
  co2Kg: number
  treesCount: number
}

export interface SolarReading {
  id: number
  plantId: string
  inverterSn: string
  timestampUtc: string
  powerW: number
  energyTodayKwh: number
  energyTotalKwh: number
  status: number
}

export interface SolarStats {
  period: string
  totalEnergyKwh: number
  maxPowerW: number
  from: string
  to: string
}

export async function getSummary(): Promise<SolarSummary | null> {
  const data = await request<SolarSummary | { message: string }>('/solar/summary')
  if ('message' in data) return null
  return data
}

export function getHistory(from: string, to: string): Promise<SolarReading[]> {
  return request<SolarReading[]>(
    `/solar/history?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`
  )
}

export function getStats(period: string): Promise<SolarStats> {
  return request<SolarStats>(`/solar/stats?period=${period}`)
}
