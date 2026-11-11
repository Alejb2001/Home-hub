import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { getHistory, type SolarReading } from '../api/client'
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

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('es-ES', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function downloadCsv(readings: SolarReading[]) {
  const headers = ['Fecha UTC', 'Potencia (W)', 'Energia hoy (kWh)', 'Energia total (kWh)', 'Estado']
  const rows = readings.map((r) => [
    r.timestampUtc,
    r.powerW,
    r.energyTodayKwh,
    r.energyTotalKwh,
    r.status === 1 ? 'Online' : 'Offline',
  ])
  const csv = [headers, ...rows].map((row) => row.join(',')).join('\n')
  const blob = new Blob([csv], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `solar-historial-${new Date().toISOString().slice(0, 10)}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

export default function History() {
  const today = new Date().toISOString().slice(0, 10)
  const sevenDaysAgo = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)

  const [from, setFrom] = useState(sevenDaysAgo)
  const [to, setTo] = useState(today)

  const { data: readings = [], isLoading } = useQuery({
    queryKey: ['solar-history', from, to],
    queryFn: () =>
      getHistory(new Date(from).toISOString(), new Date(to + 'T23:59:59Z').toISOString()),
    enabled: !!from && !!to && from <= to,
  })

  const chartData = readings.map((r) => ({
    time: formatDate(r.timestampUtc),
    watts: r.powerW,
    kwh: r.energyTodayKwh,
  }))

  return (
    <Layout>
      {/* Header row */}
      <div className="flex flex-col sm:flex-row sm:items-center gap-4 mb-5">
        <div className="flex items-center gap-3">
          <span className="w-0.5 h-5 bg-amber-400 inline-block" />
          <h1 className="font-mono text-xs font-bold text-slate-300 uppercase tracking-widest">
            Historial Solar
          </h1>
        </div>

        <div className="flex items-center gap-3 sm:ml-auto">
          <label className="font-mono text-xs text-slate-500 uppercase tracking-widest">Desde</label>
          <input
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            className="bg-slate-800 border border-slate-600 text-slate-200 font-mono px-3 py-1.5 text-xs focus:outline-none focus:border-amber-400 transition-colors"
          />
          <label className="font-mono text-xs text-slate-500 uppercase tracking-widest">Hasta</label>
          <input
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            className="bg-slate-800 border border-slate-600 text-slate-200 font-mono px-3 py-1.5 text-xs focus:outline-none focus:border-amber-400 transition-colors"
          />
        </div>
      </div>

      {/* Chart */}
      <div className="bg-slate-800 border border-slate-700 p-5 mb-3">
        <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
          Potencia (W)
        </h2>
        {isLoading ? (
          <div className="font-mono text-slate-600 text-center py-10 text-xs uppercase tracking-widest">
            Cargando...
          </div>
        ) : chartData.length === 0 ? (
          <div className="font-mono text-slate-600 text-center py-10 text-xs uppercase tracking-widest">
            Sin datos para el periodo seleccionado.
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={chartData} margin={{ top: 0, right: 4, left: -16, bottom: 0 }}>
              <CartesianGrid strokeDasharray="2 4" stroke={CHART.grid} vertical={false} />
              <XAxis
                dataKey="time"
                tick={CHART.tick}
                axisLine={false}
                tickLine={false}
                interval={Math.floor(chartData.length / 8)}
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
        )}
      </div>

      {/* Table */}
      <div className="bg-slate-800 border border-slate-700 p-5">
        <div className="flex items-center justify-between mb-4">
          <span className="font-mono text-xs text-slate-500 uppercase tracking-widest">
            {readings.length} lecturas
          </span>
          {readings.length > 0 && (
            <button
              onClick={() => downloadCsv(readings)}
              className="font-mono text-xs bg-amber-400 hover:bg-amber-500 text-slate-900 font-bold px-4 py-1.5 uppercase tracking-widest transition-colors"
            >
              Exportar CSV
            </button>
          )}
        </div>

        {readings.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-xs font-mono text-left">
              <thead>
                <tr className="border-b border-slate-700">
                  <th className="py-2 pr-4 text-slate-500 uppercase tracking-widest font-medium">Fecha</th>
                  <th className="py-2 pr-4 text-slate-500 uppercase tracking-widest font-medium">Potencia (W)</th>
                  <th className="py-2 pr-4 text-slate-500 uppercase tracking-widest font-medium">Hoy (kWh)</th>
                  <th className="py-2 pr-4 text-slate-500 uppercase tracking-widest font-medium">Total (kWh)</th>
                  <th className="py-2 text-slate-500 uppercase tracking-widest font-medium">Estado</th>
                </tr>
              </thead>
              <tbody>
                {readings
                  .slice(-50)
                  .reverse()
                  .map((r) => (
                    <tr key={r.id} className="border-b border-slate-700/50 hover:bg-slate-700/30 transition-colors">
                      <td className="py-1.5 pr-4 text-slate-400">{formatDate(r.timestampUtc)}</td>
                      <td className="py-1.5 pr-4 text-slate-200">{r.powerW.toFixed(0)}</td>
                      <td className="py-1.5 pr-4 text-slate-200">{r.energyTodayKwh.toFixed(3)}</td>
                      <td className="py-1.5 pr-4 text-slate-200">{r.energyTotalKwh.toFixed(1)}</td>
                      <td className="py-1.5">
                        <span
                          className={`px-2 py-0.5 text-xs uppercase tracking-widest border ${
                            r.status === 1
                              ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-400'
                              : 'border-red-500/40 bg-red-500/10 text-red-400'
                          }`}
                        >
                          {r.status === 1 ? 'Online' : 'Offline'}
                        </span>
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Layout>
  )
}
