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

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('es-ES', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function downloadCsv(readings: SolarReading[]) {
  const headers = [
    'Fecha UTC',
    'Potencia (W)',
    'Energía hoy (kWh)',
    'Energía total (kWh)',
    'Estado',
  ]
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
      <div className="flex flex-col sm:flex-row sm:items-end gap-4 mb-6">
        <h1 className="text-xl font-semibold text-gray-700">Historial Solar</h1>
        <div className="flex items-center gap-2 ml-auto">
          <label className="text-sm text-gray-500">Desde</label>
          <input
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-yellow-400"
          />
          <label className="text-sm text-gray-500">Hasta</label>
          <input
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-yellow-400"
          />
        </div>
      </div>

      {/* Chart */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5 mb-4">
        <h2 className="text-sm font-medium text-gray-500 mb-3">Potencia (W)</h2>
        {isLoading ? (
          <div className="text-gray-400 text-center py-10">Cargando...</div>
        ) : chartData.length === 0 ? (
          <div className="text-gray-400 text-center py-10">
            Sin datos para el período seleccionado.
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis
                dataKey="time"
                tick={{ fontSize: 10 }}
                interval={Math.floor(chartData.length / 8)}
              />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip formatter={(v) => [`${Number(v).toFixed(0)} W`, 'Potencia']} />
              <Line type="monotone" dataKey="watts" stroke="#f59e0b" dot={false} strokeWidth={2} />
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>

      {/* Table + export */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-medium text-gray-500">{readings.length} lecturas</h2>
          {readings.length > 0 && (
            <button
              onClick={() => downloadCsv(readings)}
              className="text-sm bg-yellow-400 hover:bg-yellow-500 text-gray-900 font-medium px-3 py-1.5 rounded-lg transition"
            >
              Exportar CSV
            </button>
          )}
        </div>
        {readings.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left text-gray-600">
              <thead className="text-xs text-gray-400 uppercase border-b">
                <tr>
                  <th className="py-2 pr-4">Fecha</th>
                  <th className="py-2 pr-4">Potencia (W)</th>
                  <th className="py-2 pr-4">Hoy (kWh)</th>
                  <th className="py-2 pr-4">Total (kWh)</th>
                  <th className="py-2">Estado</th>
                </tr>
              </thead>
              <tbody>
                {readings
                  .slice(-50)
                  .reverse()
                  .map((r) => (
                    <tr key={r.id} className="border-b border-gray-50 hover:bg-gray-50">
                      <td className="py-1.5 pr-4">{formatDate(r.timestampUtc)}</td>
                      <td className="py-1.5 pr-4">{r.powerW.toFixed(0)}</td>
                      <td className="py-1.5 pr-4">{r.energyTodayKwh.toFixed(3)}</td>
                      <td className="py-1.5 pr-4">{r.energyTotalKwh.toFixed(1)}</td>
                      <td className="py-1.5">
                        <span
                          className={`px-1.5 py-0.5 rounded text-xs font-medium ${r.status === 1 ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}
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
