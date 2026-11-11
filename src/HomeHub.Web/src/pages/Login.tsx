import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { login } from '../api/client'

export default function Login() {
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(username, password)
      navigate('/')
    } catch {
      setError('Usuario o contrasena incorrectos.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900">
      <div className="bg-slate-800 border border-slate-700 p-8 w-full max-w-sm">
        <div className="mb-8 border-l-2 border-amber-400 pl-4">
          <p className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-1">
            Sistema de monitoreo
          </p>
          <h1 className="font-mono text-xl font-bold text-slate-100 tracking-wide">Home Hub</h1>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          <div>
            <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
              Usuario
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              className="w-full bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-2 text-sm focus:outline-none focus:border-amber-400 transition-colors"
            />
          </div>

          <div>
            <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
              Contrasena
            </label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              className="w-full bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-2 text-sm focus:outline-none focus:border-amber-400 transition-colors"
            />
          </div>

          {error && (
            <p className="font-mono text-xs text-amber-400 border border-amber-400/30 bg-amber-400/5 px-3 py-2 tracking-wide">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="bg-amber-400 hover:bg-amber-500 text-slate-900 font-mono font-bold py-2.5 text-xs tracking-widest uppercase transition-colors disabled:opacity-40 mt-1"
          >
            {loading ? 'Verificando...' : 'Acceder'}
          </button>
        </form>
      </div>
    </div>
  )
}
