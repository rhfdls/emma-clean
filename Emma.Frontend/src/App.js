import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import Dashboard from './pages/Dashboard';
import Contacts from './pages/Contacts';
import Interactions from './pages/Interactions';
import Analytics from './pages/Analytics';
import Demo from './pages/Demo';
import AskEmma from './pages/AskEmma';

function App() {
  return (
    <Router>
      <div className="min-h-screen bg-emma-light">
        <Navbar />
        <main className="container mx-auto px-4 py-8">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/contacts" element={<Contacts />} />
            <Route path="/interactions" element={<Interactions />} />
            <Route path="/analytics" element={<Analytics />} />
            <Route path="/demo" element={<Demo />} />
            <Route path="/ask-emma" element={<AskEmma />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;
