import React, { useState } from 'react';
import { Search, Filter, User, Phone, Mail, MapPin, Tag, Star, Calendar } from 'lucide-react';

const Contacts = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [filterAgent, setFilterAgent] = useState('all');

  // Current user context (in real app, this would come from auth/context)
  const currentUser = {
    name: 'Mike Rodriguez',
    role: 'Broker/Owner',
    isOwner: true
  };

  // Team agents (in real app, this would come from API)
  const teamAgents = [
    { name: 'Mike Rodriguez', role: 'Broker/Owner' },
    { name: 'Sarah Johnson', role: 'Senior Agent' },
    { name: 'Jessica Chen', role: 'Agent' },
    { name: 'Amanda Foster', role: 'Luxury Specialist' },
    { name: 'David Kim', role: 'Assistant' }
  ];

  const contacts = [
    {
      id: 1,
      name: 'Emily Johnson',
      email: 'emily.johnson@email.com',
      phone: '(619) 555-0123',
      status: 'Client',
      tags: ['FirstTimeBuyer', 'CondoInterest', 'HOAConcerns'],
      lastInteraction: '2 hours ago',
      agent: 'Mike Rodriguez',
      priority: 'high',
      location: 'Hillcrest, San Diego',
      notes: 'Excited about Hillcrest condo, nervous about competing offers'
    },
    {
      id: 2,
      name: 'Robert Williams',
      email: 'robert.williams@email.com',
      phone: '(858) 555-0456',
      status: 'Client',
      tags: ['LuxuryBuyer', 'CashBuyer', 'LaJolla'],
      lastInteraction: '4 hours ago',
      agent: 'Amanda Foster',
      priority: 'high',
      location: 'La Jolla, San Diego',
      notes: 'Offer accepted on oceanview property, coordinating closing'
    },
    {
      id: 3,
      name: 'Kevin Brown',
      email: 'kevin.brown@email.com',
      phone: '(760) 555-0789',
      status: 'Prospect',
      tags: ['WebsiteInquiry', 'PreApproved', 'QuickMove'],
      lastInteraction: '6 hours ago',
      agent: 'Jessica Chen',
      priority: 'medium',
      location: 'Downtown, San Diego',
      notes: 'Inquired about 5th Avenue condo, pre-approved up to $500K'
    },
    {
      id: 4,
      name: 'James Wilson',
      email: 'james.wilson@email.com',
      phone: '(206) 555-0321',
      status: 'Prospect',
      tags: ['Relocation', 'TechWorker', 'TimelinePressure'],
      lastInteraction: '1 day ago',
      agent: 'Jessica Chen',
      priority: 'high',
      location: 'Seattle, WA (Relocating)',
      notes: 'Relocating from Seattle for Qualcomm job, needs 60-day close'
    },
    {
      id: 5,
      name: 'Maria Garcia',
      email: 'maria.garcia@email.com',
      phone: '(442) 555-0654',
      status: 'Prospect',
      tags: ['Seller', 'Relocation', 'StagingNeeded'],
      lastInteraction: '3 days ago',
      agent: 'Sarah Johnson',
      priority: 'medium',
      location: 'Encinitas, San Diego',
      notes: 'Listing 4BR home in Encinitas, relocating to Phoenix'
    },
    {
      id: 6,
      name: 'Thomas Anderson',
      email: 'thomas.anderson@email.com',
      phone: '(619) 555-0987',
      status: 'PastClient',
      tags: ['AnniversaryCall', 'ReferralSource', 'HappyClient'],
      lastInteraction: '5 days ago',
      agent: 'Mike Rodriguez',
      priority: 'low',
      location: 'Hillcrest, San Diego',
      notes: 'One-year anniversary, mentioned brother looking to buy'
    }
  ];

  const filteredContacts = contacts.filter(contact => {
    const matchesSearch = contact.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         contact.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         contact.tags.some(tag => tag.toLowerCase().includes(searchTerm.toLowerCase()));
    
    const matchesFilter = filterStatus === 'all' || contact.status === filterStatus;
    
    const matchesAgent = filterAgent === 'all' || contact.agent === filterAgent;

    return matchesSearch && matchesFilter && matchesAgent;
  });

  const getStatusColor = (status) => {
    switch (status) {
      case 'Client': return 'bg-green-100 text-green-800';
      case 'Prospect': return 'bg-yellow-100 text-yellow-800';
      case 'Lead': return 'bg-blue-100 text-blue-800';
      case 'PastClient': return 'bg-gray-100 text-gray-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getPriorityColor = (priority) => {
    switch (priority) {
      case 'high': return 'text-red-600';
      case 'medium': return 'text-yellow-600';
      case 'low': return 'text-green-600';
      default: return 'text-gray-600';
    }
  };

  const statusCounts = contacts.reduce((acc, contact) => {
    acc[contact.status] = (acc[contact.status] || 0) + 1;
    return acc;
  }, {});

  const agentCounts = contacts.reduce((acc, contact) => {
    acc[contact.agent] = (acc[contact.agent] || 0) + 1;
    return acc;
  }, {});

  const getActiveFiltersCount = () => {
    let count = 0;
    if (filterStatus !== 'all') count++;
    if (filterAgent !== 'all') count++;
    return count;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Contacts</h1>
          <p className="text-gray-600">
            Manage your client relationships and prospects
            {currentUser.isOwner && ' • Organization View'}
          </p>
        </div>
        <button className="bg-emma-blue text-white px-4 py-2 rounded-lg hover:bg-blue-600">
          Add Contact
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Total Contacts</p>
              <p className="text-2xl font-bold text-gray-900">{contacts.length}</p>
            </div>
            <User className="h-8 w-8 text-emma-blue" />
          </div>
        </div>
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Active Clients</p>
              <p className="text-2xl font-bold text-green-600">{statusCounts.Client || 0}</p>
            </div>
            <Star className="h-8 w-8 text-green-500" />
          </div>
        </div>
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Prospects</p>
              <p className="text-2xl font-bold text-yellow-600">{statusCounts.Prospect || 0}</p>
            </div>
            <Calendar className="h-8 w-8 text-yellow-500" />
          </div>
        </div>
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Past Clients</p>
              <p className="text-2xl font-bold text-gray-600">{statusCounts.PastClient || 0}</p>
            </div>
            <User className="h-8 w-8 text-gray-500" />
          </div>
        </div>
      </div>

      {/* Search and Filter */}
      <div className="bg-white p-6 rounded-lg card-shadow">
        <div className="flex flex-col md:flex-row gap-4">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-5 w-5" />
            <input
              type="text"
              placeholder="Search contacts by name, email, or tags..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-emma-blue focus:border-transparent"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <div className="flex items-center space-x-2">
            <Filter className="h-5 w-5 text-gray-400" />
            <select
              className="border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-emma-blue focus:border-transparent"
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value)}
            >
              <option value="all">All Status</option>
              <option value="Client">Clients</option>
              <option value="Prospect">Prospects</option>
              <option value="Lead">Leads</option>
              <option value="PastClient">Past Clients</option>
            </select>
          </div>
          <div className="flex items-center space-x-2">
            <Filter className="h-5 w-5 text-gray-400" />
            <select
              className="border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-emma-blue focus:border-transparent"
              value={filterAgent}
              onChange={(e) => setFilterAgent(e.target.value)}
            >
              <option value="all">All Agents ({contacts.length})</option>
              {teamAgents.map((agent, index) => (
                <option key={index} value={agent.name}>
                  {agent.name} ({agentCounts[agent.name] || 0})
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-center space-x-2">
            <p className="text-sm text-gray-600">Active Filters: {getActiveFiltersCount()}</p>
          </div>
          {getActiveFiltersCount() > 0 && (
            <button
              onClick={() => {
                setFilterStatus('all');
                setFilterAgent('all');
              }}
              className="px-3 py-2 text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Clear Filters
            </button>
          )}
        </div>
      </div>

      {/* Contacts List */}
      <div className="bg-white rounded-lg card-shadow overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">
            Contact List ({filteredContacts.length})
          </h2>
        </div>
        
        <div className="divide-y divide-gray-200">
          {filteredContacts.map((contact) => (
            <div key={contact.id} className="p-6 hover:bg-gray-50 transition-colors">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center space-x-3 mb-2">
                    <div className="w-10 h-10 bg-emma-blue rounded-full flex items-center justify-center">
                      <span className="text-white font-medium">
                        {contact.name.split(' ').map(n => n[0]).join('')}
                      </span>
                    </div>
                    <div>
                      <h3 className="text-lg font-medium text-gray-900">{contact.name}</h3>
                      <div className="flex items-center space-x-4 text-sm text-gray-600">
                        <span className="flex items-center">
                          <Mail className="h-4 w-4 mr-1" />
                          {contact.email}
                        </span>
                        <span className="flex items-center">
                          <Phone className="h-4 w-4 mr-1" />
                          {contact.phone}
                        </span>
                        <span className="flex items-center">
                          <MapPin className="h-4 w-4 mr-1" />
                          {contact.location}
                        </span>
                      </div>
                    </div>
                  </div>
                  
                  <div className="ml-13 space-y-2">
                    <div className="flex items-center space-x-2">
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(contact.status)}`}>
                        {contact.status}
                      </span>
                      <span className={`text-sm font-medium ${getPriorityColor(contact.priority)}`}>
                        {contact.priority.toUpperCase()} PRIORITY
                      </span>
                    </div>
                    
                    <div className="flex flex-wrap gap-1">
                      {contact.tags.map((tag, index) => (
                        <span key={index} className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-gray-100 text-gray-800">
                          <Tag className="h-3 w-3 mr-1" />
                          {tag}
                        </span>
                      ))}
                    </div>
                    
                    <p className="text-sm text-gray-700">{contact.notes}</p>
                  </div>
                </div>
                
                <div className="text-right space-y-2">
                  <div className="text-sm text-gray-600">
                    Agent: <span className="font-medium">{contact.agent}</span>
                  </div>
                  <div className="text-sm text-gray-500">
                    Last contact: {contact.lastInteraction}
                  </div>
                  <div className="space-x-2">
                    <button className="text-emma-blue hover:text-blue-600 text-sm font-medium">
                      View Details
                    </button>
                    <button className="text-gray-600 hover:text-gray-800 text-sm font-medium">
                      Add Note
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {filteredContacts.length === 0 && (
        <div className="text-center py-12">
          <User className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">No contacts found</h3>
          <p className="text-gray-600">Try adjusting your search or filter criteria.</p>
        </div>
      )}
    </div>
  );
};

export default Contacts;
