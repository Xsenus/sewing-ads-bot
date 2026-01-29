import React from 'react';
import { Route, Routes } from 'react-router-dom';
import Login from './pages/Login';
import Home from './pages/Home';
import Categories from './pages/Categories';
import Channels from './pages/Channels';
import Moderation from './pages/Moderation';
import TelegramAdmins from './pages/TelegramAdmins';
import Settings from './pages/Settings';
import AdminAccounts from './pages/AdminAccounts';

/**
 * Роутер админки.
 */
export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/" element={<Home />} />
      <Route path="/categories" element={<Categories />} />
      <Route path="/channels" element={<Channels />} />
      <Route path="/moderation" element={<Moderation />} />
      <Route path="/telegram-admins" element={<TelegramAdmins />} />
      <Route path="/admin-accounts" element={<AdminAccounts />} />
      <Route path="/settings" element={<Settings />} />
    </Routes>
  );
}
