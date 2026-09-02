import { NavLink } from 'react-router-dom';

export default function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="logo">TRAFFIC<span>HUNT</span></div>
      <nav>
        <NavLink to="/" end>Dashboard</NavLink>
        <NavLink to="/campaigns">Campaigns</NavLink>
        <NavLink to="/prospects">Prospects</NavLink>
      </nav>
      <div className="sidebar-footer">Private growth tool</div>
    </aside>
  );
}
