import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Brain, Home, Users, MessageSquare, BarChart3, Play, Sparkles } from 'lucide-react';

const Navbar = () => {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Dashboard', icon: Home },
    { path: '/contacts', label: 'Contacts', icon: Users },
    { path: '/interactions', label: 'Interactions', icon: MessageSquare },
    { path: '/analytics', label: 'Analytics', icon: BarChart3 },
    { path: '/ask-emma', label: 'Ask EMMA', icon: Sparkles },
    { path: '/demo', label: 'Live Demo', icon: Play },
  ];

  return (
    <nav className="bg-white shadow-lg border-b border-gray-200">
      <div className="container mx-auto px-4">
        <div className="flex justify-between items-center h-16">
          {/* Logo */}
          <Link to="/" className="flex items-center space-x-2">
            <Brain className="h-8 w-8 text-emma-blue" />
            <span className="text-xl font-bold text-emma-dark">EMMA AI</span>
            <span className="text-sm text-gray-500 bg-gray-100 px-2 py-1 rounded">Real Estate</span>
          </Link>

          {/* Navigation Links */}
          <div className="flex space-x-1">
            {navItems.map(({ path, label, icon: Icon }) => (
              <Link
                key={path}
                to={path}
                className={`flex items-center space-x-2 px-4 py-2 rounded-lg transition-colors ${
                  location.pathname === path
                    ? 'bg-emma-blue text-white'
                    : 'text-gray-600 hover:bg-gray-100 hover:text-emma-blue'
                }`}
              >
                <Icon className="h-4 w-4" />
                <span className="font-medium">{label}</span>
              </Link>
            ))}
          </div>

          {/* User Info */}
          <div className="flex items-center space-x-3">
            <div className="text-right">
              <div className="text-sm font-medium text-gray-900">Premier Realty</div>
              <div className="text-xs text-gray-500">Mike Rodriguez (Broker)</div>
            </div>
            <div className="w-8 h-8 bg-emma-blue rounded-full flex items-center justify-center">
              <span className="text-white text-sm font-medium">MR</span>
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
