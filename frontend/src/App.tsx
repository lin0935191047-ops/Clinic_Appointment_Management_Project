import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Navbar } from './components/layout/Navbar';
import { Footer } from './components/layout/Footer';
import { HomePage } from './pages/HomePage';
import { DoctorsPage } from './pages/DoctorsPage';
import { BookingPage } from './pages/BookingPage';
import { LookupPage } from './pages/LookupPage';
import { ReceptionistDashboardPage } from './pages/ReceptionistDashboardPage';

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <div className="min-h-screen flex flex-col bg-gray-50 text-gray-900">
        <Navbar />
        <main className="flex-1">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/doctors" element={<DoctorsPage />} />
            <Route path="/booking" element={<BookingPage />} />
            <Route path="/lookup" element={<LookupPage />} />
            <Route path="/receptionist" element={<ReceptionistDashboardPage />} />
            <Route
              path="*"
              element={
                <div className="text-center py-20">
                  <h2 className="text-2xl font-bold text-gray-800">404 - Không tìm thấy trang</h2>
                  <p className="text-gray-500 mt-2">Đường dẫn bạn yêu cầu không tồn tại.</p>
                </div>
              }
            />
          </Routes>
        </main>
        <Footer />
      </div>
    </BrowserRouter>
  );
};

export default App;
