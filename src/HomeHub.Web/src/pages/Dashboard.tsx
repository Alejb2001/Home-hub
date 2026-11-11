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

const CHART = {
  grid: '#334155',
  tick: { fill: '#64748b', fontSize: 10, fontFamily: 'ui-monospace, monospace' },
  tooltip: {
    contentStyle: {
      backgroundColor: '#1e293b',
      border: '1px solid #475569',
      borderRadius: 0,
      color: '#f1f5f9',
      fontSize: 11,
      fontFamily: 'ui-monospace, monospace',
    },
  },
}

function StatusBadge({ status }: { status: number }) {
  const online = status === 1
  return (
    <span
      className={`inline-flex items-center gap-2 px-3 py-1.5 font-mono text-xs uppercase tracking-widest border ${
        online
          ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-400'
          : 'border-red-500/40 bg-red-500/10 text-red-400'
      }`}
    >
      <span className={`w-1.5 h-1.5 rounded-full ${online ? 'bg-emerald-400' : 'bg-red-400'}`} />
      {online ? 'Online' : 'Offline'}
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
    <div className="bg-slate-800 border border-slate-700 border-t-2 border-t-amber-400/70 p-5 flex flex-col gap-2">
      <span className="font-mono text-xs text-slate-500 uppercase tracking-widest">{label}</span>
      <div className="flex items-baseline gap-2">
        <span className="font-mono text-3xl font-bold text-slate-100">{value}</span>
        {unit && <span className="font-mono text-sm text-amber-400">{unit}</span>}
      </div>
      {children}
    </div>
  )
}

function SectionHeader({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex items-center gap-3 mb-4">
      <span className="w-0.5 h-5 bg-amber-400 inline-block" />
      <h1 className="font-mono text-xs font-bold text-slate-300 uppercase tracking-widest">
        {children}
      </h1>
    </div>
  )
}

export default function Dashboard() {
  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['solar-summary'],
    queryFn: getSummary,
    refetchInterval: 5 * 60 * 1000,
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

  const dailyEnergy = (() => {
    if (!weekReadings) return []
    const byDay: Record<string, number> = {}
    for (const r of weekReadings) {
      const day = r.timestampUtc.slice(0, 10)
      byDay[day] = Math.max(byDay[day] ?? 0, r.energyTodayKwh)
    }
    return Object.entries(byDay)
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([date, kwh]) => ({ date: date.slice(5), kwh }))
  })()

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
        <div className="font-mono text-slate-600 text-center py-20 tracking-widest text-xs uppercase">
          Cargando...
        </div>
      </Layout>
    )
  }

  return (
    <Layout>
      <SectionHeader>Paneles Solares — Hoy</SectionHeader>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-5">
        <SummaryCard
          label="Potencia actual"
          value={summary ? summary.powerW.toFixed(0) : '—'}
          unit="W"
        />
        <SummaryCard
          label="Energia generada hoy"
          value={summary ? summary.energyTodayKwh.toFixed(2) : '—'}
          unit="kWh"
        />
        <SummaryCard label="Estado del inversor" value="" unit="">
          {summary ? (
            <StatusBadge status={summary.status} />
          ) : (
            <span className="font-mono text-slate-600">—</span>
          )}
        </SummaryCard>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-3">
        <div className="bg-slate-800 border border-slate-700 p-5">
          <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
            Energia — ultimos 7 dias (kWh)
          </h2>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={dailyEnergy} margin={{ top: 0, right: 4, left: -16, bottom: 0 }}>
              <CartesianGrid strokeDasharray="2 4" stroke={CHART.grid} vertical={false} />
              <XAxis dataKey="date" tick={CHART.tick} axisLine={false} tickLine={false} />
              <YAxis tick={CHART.tick} axisLine={false} tickLine={false} />
              <Tooltip
                formatter={(v) => [`${Number(v).toFixed(2)} kWh`, 'Energia']}
                contentStyle={CHART.tooltip.contentStyle}
                cursor={{ fill: '#1e3a5f30' }}
              />
              <Bar dataKey="kwh" fill="#f59e0b" radius={[1, 1, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-slate-800 border border-slate-700 p-5">
          <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
            Potencia hoy (W)
          </h2>
          <ResponsiveContainer width="100%" height={200}>
            <LineChart data={powerCurve} margin={{ top: 0, right: 4, left: -16, bottom: 0 }}>
              <CartesianGrid strokeDasharray="2 4" stroke={CHART.grid} vertical={false} />
              <XAxis
                dataKey="time"
                tick={CHART.tick}
                axisLine={false}
                tickLine={false}
                interval="preserveStartEnd"
              />
              <YAxis tick={CHART.tick} axisLine={false} tickLine={false} />
              <Tooltip
                formatter={(v) => [`${Number(v).toFixed(0)} W`, 'Potencia']}
                contentStyle={CHART.tooltip.contentStyle}
              />
              <Line
                type="monotone"
                dataKey="watts"
                stroke="#f59e0b"
                dot={false}
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>

      {summary && (
        <p className="font-mono text-xs text-slate-700 mt-4 text-right tracking-widest uppercase">
          Sync: {new Date(summary.lastUpdated).toLocaleString('es-ES')}
        </p>
      )}
    </Layout>
  )
}
