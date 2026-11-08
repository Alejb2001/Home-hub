import { useQuery } from '@tanstack/react-query'
import {
  CartesianGrid,
  Line,
  LineChart,
  Bar,
  BarChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { getSummary, getHistory } from '../api/client'
import Layout from '../components/Layout'

function StatusBadge({ status }: { status: number }) {
  return (
    <span
      className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold ${status === 1 ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}
    >
      {status === 1 ? 'Online' : 'Offline'}
    </span>
  )
}

function SummaryCard({
  label,
  value,
  unit,
  children,
}: {
  label: string
  value: string
  unit: string
  children?: React.ReactNode
}) {
  return (
    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5 flex flex-col gap-1">
      <span className="text-sm text-gray-500">{label}</span>
      <div className="flex items-baseline gap-1">
        <span className="text-3xl font-bold text-gray-800">{value}</span>
        <span className="text-sm text-gray-400">{unit}</span>
      </div>
      {children}
    </div>
  )
}

export default function Dashboard() {
  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['solar-summary'],
    queryFn: getSummary,
    refetchInterval: 5 * 60 * 1000, // 5 minutes
  })

  const now = new Date()
  const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString()
  const sevenDaysAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString()

  const { data: todayReadings } = useQuery({
    queryKey: ['solar-history-today'],
    queryFn: () => getHistory(todayStart, now.toISOString()),
    refetchInterval: 5 * 60 * 1000,
  })

  const { data: weekReadings } = useQuery({
    queryKey: ['solar-history-week'],
    queryFn: () => getHistory(sevenDaysAgo, now.toISOString()),
  })

  // Aggregate energy per day for the last 7 days
  const dailyEnergy = (() => {
    if (!weekReadings) return []
    const byDay: Record<string, number> = {}
    for (const r of weekReadings) {
      const day = r.timestampUtc.slice(0, 10)
      byDay[day] = Math.max(byDay[day] ?? 0, r.energyTodayKwh)
    }
    return Object.entries(byDay)
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([date, kwh]) => ({ date: date.slice(5), kwh })) // "MM-DD"
  })()

  // Power curve today
  const powerCurve = (todayReadings ?? []).map((r) => ({
    time: new Date(r.timestampUtc).toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
    }),
    watts: r.powerW,
  }))

  if (summaryLoading) {
    return (
      <Layout>
        <div className="text-gray-400 text-center py-20">Cargando...</div>
      </Layout>
    )
  }

  return (
    <Layout>
      <h1 className="text-xl font-semibold text-gray-700 mb-4">Paneles Solares — Hoy</h1>

      {/* Summary cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <SummaryCard
          label="Potencia actual"
          value={summary ? summary.powerW.toFixed(0) : '—'}
          unit="W"
        />
        <SummaryCard
          label="Energía generada hoy"
          value={summary ? summary.energyTodayKwh.toFixed(2) : '—'}
          unit="kWh"
        />
        <SummaryCard label="Estado" value="" unit="">
          {summary ? (
            <StatusBadge status={summary.status} />
          ) : (
            <span className="text-gray-400">—</span>
          )}
        </SummaryCard>
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5">
          <h2 className="text-sm font-medium text-gray-500 mb-3">Energía últimos 7 días (kWh)</h2>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={dailyEnergy}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip formatter={(v) => [`${Number(v).toFixed(2)} kWh`, 'Energía']} />
              <Bar dataKey="kwh" fill="#facc15" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5">
          <h2 className="text-sm font-medium text-gray-500 mb-3">Potencia hoy (W)</h2>
          <ResponsiveContainer width="100%" height={200}>
            <LineChart data={powerCurve}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="time" tick={{ fontSize: 10 }} interval="preserveStartEnd" />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip formatter={(v) => [`${Number(v).toFixed(0)} W`, 'Potencia']} />
              <Line type="monotone" dataKey="watts" stroke="#f59e0b" dot={false} strokeWidth={2} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>

      {summary && (
        <p className="text-xs text-gray-400 mt-4 text-right">
          Última actualización: {new Date(summary.lastUpdated).toLocaleString('es-ES')}
        </p>
      )}
    </Layout>
  )
}
