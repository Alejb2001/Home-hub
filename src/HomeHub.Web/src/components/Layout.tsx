import { Link, useLocation, useNavigate } from 'react-router-dom'
import { clearToken } from '../api/client'

export default function Layout({ children }: { children: React.ReactNode }) {
  const location = useLocation()
  const navigate = useNavigate()

  function handleLogout() {
    clearToken()
    navigate('/login')
  }

  function navClass(path: string) {
    return location.pathname === path
      ? 'text-amber-400 border-b border-amber-400 pb-0.5 text-xs font-mono uppercase tracking-widest transition-colors'
      : 'text-slate-400 hover:text-slate-200 text-xs font-mono uppercase tracking-widest transition-colors'
  }

  return (
    <div className="min-h-screen bg-slate-900">
      <nav className="bg-slate-900 border-b border-slate-700 px-6 py-3 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="w-1 h-5 bg-amber-400 inline-block" />
          <span className="font-mono font-bold text-slate-100 tracking-widest text-sm uppercase">
            Home Hub
          </span>
        </div>
        <div className="flex items-center gap-6">
          <Link to="/" className={navClass('/')}>Dashboard</Link>
          <Link to="/history" className={navClass('/history')}>Historial</Link>
          <Link to="/home" className={navClass('/home')}>Mi Hogar</Link>
          <button
            onClick={handleLogout}
            className="text-xs font-mono text-slate-600 hover:text-red-400 uppercase tracking-widest transition-colors"
          >
            Salir
          </button>
        </div>
      </nav>
      <main className="max-w-5xl mx-auto px-4 py-6">{children}</main>
    </div>
  )
}
