import { NavLink, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { DashboardPage } from './pages/DashboardPage'
import { HistoryPage } from './pages/HistoryPage'
import { SensorsPage } from './pages/SensorsPage'
import './App.css'

function Layout() {
  return (
    <div className="app-shell">
      <header className="top-nav">
        <div className="brand">Industrial RT</div>
        <nav className="nav-links" aria-label="Main">
          <NavLink to="/dashboard" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')} end>
            Dashboard
          </NavLink>
          <NavLink to="/sensors" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            Sensors
          </NavLink>
          <NavLink to="/history" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            History
          </NavLink>
        </nav>
      </header>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="sensors" element={<SensorsPage />} />
        <Route path="history" element={<HistoryPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
