import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { register } from '../api/client'

export default function Register() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (password !== confirm) {
      setError('Las contrasenas no coinciden.')
      return
    }
    if (password.length < 8) {
      setError('La contrasena debe tener al menos 8 caracteres.')
      return
    }
    setLoading(true)
    try {
      await register(email, password)
      navigate('/login', { state: { registered: true } })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al crear la cuenta.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900">
      <div className="bg-slate-800 border border-slate-700 p-8 w-full max-w-sm">
        <div className="mb-8 border-l-2 border-amber-400 pl-4">
          <p className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-1">
            Crear cuenta
          </p>
          <h1 className="font-mono text-xl font-bold text-slate-100 tracking-wide">Home Hub</h1>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          <div>
            <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
              Correo electronico
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
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
              autoComplete="new-password"
              className="w-full bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-2 text-sm focus:outline-none focus:border-amber-400 transition-colors"
            />
          </div>

          <div>
            <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
              Confirmar contrasena
            </label>
            <input
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              required
              autoComplete="new-password"
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
            {loading ? 'Creando cuenta...' : 'Crear cuenta'}
          </button>

          <p className="text-center font-mono text-xs text-slate-500">
            Ya tienes cuenta?{' '}
            <Link to="/login" className="text-amber-400 hover:text-amber-300 transition-colors">
              Iniciar sesion
            </Link>
          </p>
        </form>
      </div>
    </div>
  )
}
