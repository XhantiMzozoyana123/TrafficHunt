import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import Dashboard from './pages/Dashboard';
import Campaigns from './pages/Campaigns';
import CampaignDetails from './pages/CampaignDetails';
import Prospects from './pages/Prospects';

export default function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <Sidebar />
        <main>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/campaigns" element={<Campaigns />} />
            <Route path="/campaigns/:id" element={<CampaignDetails />} />
            <Route path="/prospects" element={<Prospects />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}
