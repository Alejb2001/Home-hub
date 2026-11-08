import { Link, useLocation, useNavigate } from 'react-router-dom'
import { clearToken } from '../api/client'

export default function Layout({ children }: { children: React.ReactNode }) {
  const location = useLocation()
  const navigate = useNavigate()

  function handleLogout() {
    clearToken()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between">
        <span className="font-semibold text-gray-800 text-lg">Home Hub</span>
        <div className="flex items-center gap-4">
          <Link
            to="/"
            className={`text-sm font-medium ${location.pathname === '/' ? 'text-yellow-600' : 'text-gray-600 hover:text-gray-900'}`}
          >
            Dashboard
          </Link>
          <Link
            to="/history"
            className={`text-sm font-medium ${location.pathname === '/history' ? 'text-yellow-600' : 'text-gray-600 hover:text-gray-900'}`}
          >
            Historial
          </Link>
          <button
            onClick={handleLogout}
            className="text-sm text-gray-500 hover:text-red-600 transition"
          >
            Salir
          </button>
        </div>
      </nav>
      <main className="max-w-5xl mx-auto px-4 py-6">{children}</main>
    </div>
  )
}
