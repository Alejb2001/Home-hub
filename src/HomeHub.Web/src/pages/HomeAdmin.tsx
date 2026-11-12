import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addService,
  deleteService,
  getHome,
  toggleService,
  updateHomeName,
  updateService,
  type ServiceConfigDto,
} from '../api/client'
import Layout from '../components/Layout'

const SERVICE_TYPES = [{ value: 'growatt', label: 'Growatt (Paneles Solares)' }]

function ServiceTypeLabel({ type }: { type: string }) {
  const colors: Record<string, string> = {
    growatt: 'bg-amber-400/10 text-amber-400 border-amber-400/30',
  }
  const labels: Record<string, string> = { growatt: 'Growatt' }
  return (
    <span className={`font-mono text-xs px-2 py-0.5 border ${colors[type] ?? 'bg-slate-700 text-slate-400 border-slate-600'}`}>
      {labels[type] ?? type}
    </span>
  )
}

interface ServiceFormState {
  type: string
  displayName: string
  username: string
  password: string
}

const emptyForm: ServiceFormState = {
  type: 'growatt',
  displayName: 'Paneles Solares',
  username: '',
  password: '',
}

export default function HomeAdmin() {
  const qc = useQueryClient()
  const { data: home, isLoading } = useQuery({ queryKey: ['home'], queryFn: getHome })

  const [editingName, setEditingName] = useState(false)
  const [nameInput, setNameInput] = useState('')
  const [showAddModal, setShowAddModal] = useState(false)
  const [editingService, setEditingService] = useState<ServiceConfigDto | null>(null)
  const [form, setForm] = useState<ServiceFormState>(emptyForm)
  const [formError, setFormError] = useState('')

  const updateNameMut = useMutation({
    mutationFn: (name: string) => updateHomeName(name),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['home'] }); setEditingName(false) },
  })

  const addSvcMut = useMutation({
    mutationFn: () => addService(form.type, form.displayName, form.username, form.password),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['home'] }); closeModal() },
    onError: (e) => setFormError(e instanceof Error ? e.message : 'Error al guardar.'),
  })

  const updateSvcMut = useMutation({
    mutationFn: (id: number) => updateService(id, form.displayName, form.username, form.password),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['home'] }); closeModal() },
    onError: (e) => setFormError(e instanceof Error ? e.message : 'Error al guardar.'),
  })

  const toggleMut = useMutation({
    mutationFn: toggleService,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['home'] }),
  })

  const deleteMut = useMutation({
    mutationFn: deleteService,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['home'] }),
  })

  function openAdd() {
    setForm(emptyForm)
    setFormError('')
    setEditingService(null)
    setShowAddModal(true)
  }

  function openEdit(svc: ServiceConfigDto) {
    setForm({
      type: svc.type,
      displayName: svc.displayName,
      username: svc.credentialsUsername,
      password: '',
    })
    setFormError('')
    setEditingService(svc)
    setShowAddModal(true)
  }

  function closeModal() {
    setShowAddModal(false)
    setEditingService(null)
    setForm(emptyForm)
    setFormError('')
  }

  function handleFormSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormError('')
    if (!form.displayName.trim() || !form.username.trim()) {
      setFormError('Nombre y usuario son requeridos.')
      return
    }
    if (!editingService && !form.password) {
      setFormError('La contrasena es requerida.')
      return
    }
    if (editingService) {
      updateSvcMut.mutate(editingService.id)
    } else {
      addSvcMut.mutate()
    }
  }

  const isSaving = addSvcMut.isPending || updateSvcMut.isPending

  return (
    <Layout>
      <div className="max-w-2xl mx-auto space-y-8">
        {/* Home name */}
        <section>
          <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest mb-4">
            Mi Hogar
          </h2>
          {isLoading ? (
            <div className="h-8 w-40 bg-slate-800 animate-pulse" />
          ) : editingName ? (
            <form
              onSubmit={(e) => { e.preventDefault(); updateNameMut.mutate(nameInput) }}
              className="flex items-center gap-3"
            >
              <input
                value={nameInput}
                onChange={(e) => setNameInput(e.target.value)}
                className="bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-1.5 text-sm focus:outline-none focus:border-amber-400 transition-colors"
                autoFocus
              />
              <button
                type="submit"
                disabled={updateNameMut.isPending}
                className="font-mono text-xs text-amber-400 hover:text-amber-300 uppercase tracking-widest"
              >
                Guardar
              </button>
              <button
                type="button"
                onClick={() => setEditingName(false)}
                className="font-mono text-xs text-slate-500 hover:text-slate-300 uppercase tracking-widest"
              >
                Cancelar
              </button>
            </form>
          ) : (
            <div className="flex items-center gap-3">
              <span className="font-mono text-lg text-slate-100 font-bold">{home?.name}</span>
              <button
                onClick={() => { setNameInput(home?.name ?? ''); setEditingName(true) }}
                className="font-mono text-xs text-slate-500 hover:text-slate-300 uppercase tracking-widest"
              >
                Editar
              </button>
            </div>
          )}
        </section>

        {/* Services */}
        <section>
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-mono text-xs text-slate-500 uppercase tracking-widest">
              Servicios
            </h2>
            <button
              onClick={openAdd}
              className="font-mono text-xs text-amber-400 hover:text-amber-300 uppercase tracking-widest border border-amber-400/30 px-3 py-1 hover:border-amber-400/60 transition-colors"
            >
              + Agregar
            </button>
          </div>

          {isLoading ? (
            <div className="space-y-3">
              {[1, 2].map((i) => <div key={i} className="h-20 bg-slate-800 animate-pulse" />)}
            </div>
          ) : home?.services.length === 0 ? (
            <div className="border border-dashed border-slate-700 p-8 text-center">
              <p className="font-mono text-sm text-slate-500">
                No hay servicios configurados.
              </p>
              <p className="font-mono text-xs text-slate-600 mt-1">
                Agrega tu primer servicio para comenzar.
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {home?.services.map((svc) => (
                <div
                  key={svc.id}
                  className={`bg-slate-800 border p-4 ${svc.enabled ? 'border-slate-700' : 'border-slate-800 opacity-60'}`}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <ServiceTypeLabel type={svc.type} />
                        <span className="font-mono text-sm text-slate-100 font-semibold truncate">
                          {svc.displayName}
                        </span>
                      </div>
                      <p className="font-mono text-xs text-slate-500">
                        Usuario: {svc.credentialsUsername}
                      </p>
                      {svc.plantId && (
                        <p className="font-mono text-xs text-slate-600 mt-0.5">
                          Plant ID: {svc.plantId}
                        </p>
                      )}
                      {!svc.plantId && svc.enabled && (
                        <p className="font-mono text-xs text-amber-400/60 mt-0.5">
                          Sincronizando por primera vez...
                        </p>
                      )}
                    </div>
                    <div className="flex items-center gap-3 shrink-0">
                      <button
                        onClick={() => toggleMut.mutate(svc.id)}
                        disabled={toggleMut.isPending}
                        className={`font-mono text-xs uppercase tracking-widest transition-colors ${
                          svc.enabled
                            ? 'text-slate-400 hover:text-slate-200'
                            : 'text-amber-400 hover:text-amber-300'
                        }`}
                      >
                        {svc.enabled ? 'Desactivar' : 'Activar'}
                      </button>
                      <button
                        onClick={() => openEdit(svc)}
                        className="font-mono text-xs text-slate-400 hover:text-slate-200 uppercase tracking-widest transition-colors"
                      >
                        Editar
                      </button>
                      <button
                        onClick={() => {
                          if (confirm('Eliminar este servicio?')) deleteMut.mutate(svc.id)
                        }}
                        className="font-mono text-xs text-slate-600 hover:text-red-400 uppercase tracking-widest transition-colors"
                      >
                        Eliminar
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>

      {/* Add/Edit modal */}
      {showAddModal && (
        <div className="fixed inset-0 bg-slate-900/80 flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 border border-slate-700 w-full max-w-md p-6">
            <h3 className="font-mono text-sm font-bold text-slate-100 uppercase tracking-widest mb-6">
              {editingService ? 'Editar servicio' : 'Agregar servicio'}
            </h3>

            <form onSubmit={handleFormSubmit} className="flex flex-col gap-4">
              {!editingService && (
                <div>
                  <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
                    Tipo de servicio
                  </label>
                  <select
                    value={form.type}
                    onChange={(e) => setForm((f) => ({ ...f, type: e.target.value }))}
                    className="w-full bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-2 text-sm focus:outline-none focus:border-amber-400 transition-colors"
                  >
                    {SERVICE_TYPES.map((t) => (
                      <option key={t.value} value={t.value}>{t.label}</option>
                    ))}
                  </select>
                </div>
              )}

              <div>
                <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
                  Nombre
                </label>
                <input
                  type="text"
                  value={form.displayName}
                  onChange={(e) => setForm((f) => ({ ...f, displayName: e.target.value }))}
                  className="w-full bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-2 text-sm focus:outline-none focus:border-amber-400 transition-colors"
                  placeholder="Paneles Solares"
                />
              </div>

              <div>
                <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
                  Usuario Growatt (correo)
                </label>
                <input
                  type="email"
                  value={form.username}
                  onChange={(e) => setForm((f) => ({ ...f, username: e.target.value }))}
                  className="w-full bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-2 text-sm focus:outline-none focus:border-amber-400 transition-colors"
                  autoComplete="off"
                />
              </div>

              <div>
                <label className="block font-mono text-xs text-slate-500 uppercase tracking-widest mb-1.5">
                  Contrasena Growatt{editingService && ' (dejar vacio para mantener)'}
                </label>
                <input
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                  className="w-full bg-slate-900 border border-slate-600 text-slate-100 font-mono px-3 py-2 text-sm focus:outline-none focus:border-amber-400 transition-colors"
                  autoComplete="new-password"
                />
              </div>

              {formError && (
                <p className="font-mono text-xs text-amber-400 border border-amber-400/30 bg-amber-400/5 px-3 py-2">
                  {formError}
                </p>
              )}

              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={isSaving}
                  className="flex-1 bg-amber-400 hover:bg-amber-500 text-slate-900 font-mono font-bold py-2 text-xs tracking-widest uppercase transition-colors disabled:opacity-40"
                >
                  {isSaving ? 'Guardando...' : 'Guardar'}
                </button>
                <button
                  type="button"
                  onClick={closeModal}
                  className="font-mono text-xs text-slate-500 hover:text-slate-300 uppercase tracking-widest px-4 border border-slate-700 hover:border-slate-600 transition-colors"
                >
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </Layout>
  )
}
