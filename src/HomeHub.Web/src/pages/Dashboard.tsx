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

function StatusDot({ status }: { status: number }) {
  const online = status === 1
  return (
    <span
      className={`inline-flex items-center gap-2 px-3 py-1 font-mono text-xs uppercase tracking-widest border ${
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

function StatCard({
  label,
  value,
  unit,
  sub,
}: {
  label: string
  value: string
  unit: string
  sub?: string
}) {
  return (
    <div className="bg-slate-800 border border-slate-700 border-t-2 border-t-amber-400/70 p-4 flex flex-col gap-1.5">
      <span className="font-mono text-xs text-slate-500 uppercase tracking-widest">{label}</span>
      <div className="flex items-baseline gap-2">
        <span className="font-mono text-2xl font-bold text-slate-100">{value}</span>
        {unit && <span className="font-mono text-sm text-amber-400">{unit}</span>}
      </div>
      {sub && <span className="font-mono text-xs text-slate-600">{sub}</span>}
    </div>
  )
}

function ImpactRow({ label, value, unit }: { label: string; value: string; unit: string }) {
  return (
    <div className="flex items-center justify-between py-2 border-b border-slate-700/50 last:border-0">
      <span className="font-mono text-xs text-slate-500 uppercase tracking-widest">{label}</span>
      <span className="font-mono text-sm font-bold text-slate-200">
        {value} <span className="text-amber-400 font-normal text-xs">{unit}</span>
      </span>
    </div>
  )
}

function EquipRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 border-b border-slate-700/50 last:border-0">
      <span className="font-mono text-xs text-slate-500 uppercase tracking-widest">{label}</span>
      <span className="font-mono text-xs text-slate-200 text-right max-w-[55%] truncate">{value}</span>
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
      {/* Header */}
      <div className="flex items-center justify-between mb-5">
        <div className="flex items-center gap-3">
          <span className="w-0.5 h-5 bg-amber-400 inline-block" />
          <h1 className="font-mono text-xs font-bold text-slate-300 uppercase tracking-widest">
            {summary?.plantName || 'Paneles Solares'} — Hoy
          </h1>
        </div>
        {summary && <StatusDot status={summary.status} />}
      </div>

      {/* Stat cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-5">
        <StatCard
          label="Potencia actual"
          value={summary ? summary.powerW.toFixed(0) : '—'}
          unit="W"
        />
        <StatCard
          label="Energia hoy"
          value={summary ? summary.energyTodayKwh.toFixed(2) : '—'}
          unit="kWh"
        />
        <StatCard
          label="Este mes"
          value={summary ? summary.energyMonthKwh.toFixed(1) : '—'}
          unit="kWh"
        />
        <StatCard
          label="Total acumulado"
          value={summary ? summary.energyTotalKwh.toFixed(1) : '—'}
          unit="kWh"
          sub={summary ? `${summary.nominalPowerW.toFixed(0)} W nominales` : undefined}
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-3 mb-3">
        <div className="bg-slate-800 border border-slate-700 p-5">
          <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
            Potencia hoy (W)
          </h2>
          <ResponsiveContainer width="100%" height={180}>
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
              <Line type="monotone" dataKey="watts" stroke="#f59e0b" dot={false} strokeWidth={2} />
            </LineChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-slate-800 border border-slate-700 p-5">
          <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
            Energia — ultimos 7 dias (kWh)
          </h2>
          <ResponsiveContainer width="100%" height={180}>
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
      </div>

      {/* Equipment + Impact */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-3">
        {/* Equipment */}
        <div className="bg-slate-800 border border-slate-700 p-5">
          <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
            Instalacion
          </h2>
          {summary ? (
            <>
              <EquipRow label="Planta" value={summary.plantName || '—'} />
              <EquipRow label="Inversor" value={summary.deviceModel || '—'} />
              <EquipRow label="Capacidad nominal" value={`${summary.nominalPowerW.toFixed(0)} W`} />
              <EquipRow label="Numero de serie" value={
                (todayReadings?.[0]?.inverterSn) || '—'
              } />
            </>
          ) : (
            <span className="font-mono text-xs text-slate-600">Sin datos</span>
          )}
        </div>

        {/* Environmental impact */}
        <div className="bg-slate-800 border border-slate-700 p-5">
          <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
            Impacto ambiental
          </h2>
          {summary ? (
            <>
              <ImpactRow
                label="CO2 evitado"
                value={summary.co2Kg.toFixed(1)}
                unit="kg"
              />
              <ImpactRow
                label="Arboles equivalentes"
                value={summary.treesCount.toFixed(0)}
                unit="arboles"
              />
              <ImpactRow
                label="Carbon evitado"
                value={(summary.energyTotalKwh * 0.4).toFixed(1)}
                unit="kg"
              />
            </>
          ) : (
            <span className="font-mono text-xs text-slate-600">Sin datos</span>
          )}
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
