import React, { useState } from 'react';
import { MessageSquare, Phone, Mail, Calendar, User, Clock, Search, Filter, Brain } from 'lucide-react';

const Interactions = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState('all');

  const interactions = [
    {
      id: 1,
      contactName: 'Emily Johnson',
      agentName: 'Mike Rodriguez',
      type: 'call',
      direction: 'outbound',
      content: 'Emily called excited about the Hillcrest condo we saw yesterday. She wants to make an offer but is nervous about competing with other buyers. I discussed offer strategy with her - recommended full price ($525K) with quick close (21 days) and a personal letter to the seller.',
      timestamp: '2024-11-20T10:30:00Z',
      duration: '18 minutes',
      tags: ['OfferStrategy', 'FirstTimeBuyer', 'CompetitiveBid'],
      aiInsights: {
        emotion: 'excited-nervous',
        propertyAddress: '456 Hillcrest Ave, San Diego, CA',
        offerAmount: '$525,000',
        nextAction: 'Prepare offer documents and personal letter'
      }
    },
    {
      id: 2,
      contactName: 'Robert Williams',
      agentName: 'Amanda Foster',
      type: 'email',
      direction: 'outbound',
      content: 'Hi Jennifer, I have a client, Robert Williams, who needs financing for a $1.2M purchase in La Jolla. He\'s a cash buyer but wants to maintain liquidity for his business. Looking for a jumbo loan at approximately 70% LTV. He has excellent credit (780+) and significant assets.',
      timestamp: '2024-11-17T11:20:00Z',
      tags: ['LenderCoordination', 'JumboLoan', 'LuxuryClient'],
      aiInsights: {
        loanAmount: '$840,000',
        ltv: '70%',
        creditScore: '780+',
        nextAction: 'Schedule lender call with client'
      }
    },
    {
      id: 3,
      contactName: 'Kevin Brown',
      agentName: 'Jessica Chen',
      type: 'email',
      direction: 'inbound',
      content: 'Hi, I saw your listing for the condo on 5th Avenue. Is it still available? I\'m pre-approved up to $500K and looking to move quickly. Can we schedule a showing this weekend? Thanks, Kevin',
      timestamp: '2024-11-18T14:30:00Z',
      tags: ['NewLead', 'WebsiteInquiry', 'ShowingRequest'],
      aiInsights: {
        urgencyLevel: 'high',
        budget: '$500,000',
        leadSource: 'website',
        nextAction: 'Respond within 2 hours with showing availability'
      }
    },
    {
      id: 4,
      contactName: 'James Wilson',
      agentName: 'Jessica Chen',
      type: 'call',
      direction: 'inbound',
      content: 'James called about the Sorrento Valley townhome listing. He\'s relocating from Seattle for a tech job and needs to close quickly. Budget is $800K-$900K, pre-approved with his current lender but open to local recommendations.',
      timestamp: '2024-11-19T17:30:00Z',
      duration: '22 minutes',
      tags: ['HotProspect', 'Relocation', 'TechWorker'],
      aiInsights: {
        relocationTimeline: '60 days',
        budget: '$800K-$900K',
        employer: 'Qualcomm',
        nextAction: 'Send property recommendations and schedule virtual tour'
      }
    },
    {
      id: 5,
      contactName: 'Maria Garcia',
      agentName: 'Sarah Johnson',
      type: 'meeting',
      direction: 'system',
      content: 'Met with Maria at her Encinitas home to discuss listing strategy. Home is well-maintained 4BR/3BA built in 2018, approximately 2,400 sq ft. Comparable sales suggest listing price of $1.1M-$1.15M.',
      timestamp: '2024-11-16T14:00:00Z',
      duration: '90 minutes',
      tags: ['ListingConsultation', 'CMA', 'Relocation'],
      aiInsights: {
        propertyType: 'single-family',
        listingPriceRange: '$1.1M-$1.15M',
        condition: 'well-maintained',
        nextAction: 'Prepare listing agreement and marketing plan'
      }
    },
    {
      id: 6,
      contactName: 'Thomas Anderson',
      agentName: 'Mike Rodriguez',
      type: 'call',
      direction: 'outbound',
      content: 'Called Thomas for his one-year home anniversary check-in. He\'s very happy with his Hillcrest purchase and the neighborhood. Asked about home value appreciation - shared recent comps showing 8% increase since his purchase.',
      timestamp: '2024-11-15T10:30:00Z',
      duration: '15 minutes',
      tags: ['AnniversaryCall', 'PastClient', 'ReferralOpportunity'],
      aiInsights: {
        satisfactionLevel: 'very-high',
        propertyAppreciation: '8%',
        referralPotential: 'high',
        nextAction: 'Follow up on brother referral opportunity'
      }
    }
  ];

  const filteredInteractions = interactions.filter(interaction => {
    const matchesSearch = interaction.contactName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         interaction.content.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         interaction.tags.some(tag => tag.toLowerCase().includes(searchTerm.toLowerCase()));
    
    const matchesFilter = filterType === 'all' || interaction.type === filterType;
    
    return matchesSearch && matchesFilter;
  });

  const getTypeIcon = (type) => {
    switch (type) {
      case 'call': return <Phone className="h-5 w-5" />;
      case 'email': return <Mail className="h-5 w-5" />;
      case 'meeting': return <Calendar className="h-5 w-5" />;
      default: return <MessageSquare className="h-5 w-5" />;
    }
  };

  const getTypeColor = (type) => {
    switch (type) {
      case 'call': return 'bg-blue-100 text-blue-800';
      case 'email': return 'bg-green-100 text-green-800';
      case 'meeting': return 'bg-purple-100 text-purple-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getDirectionColor = (direction) => {
    switch (direction) {
      case 'inbound': return 'text-green-600';
      case 'outbound': return 'text-blue-600';
      default: return 'text-gray-600';
    }
  };

  const formatTimestamp = (timestamp) => {
    const date = new Date(timestamp);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const typeCounts = interactions.reduce((acc, interaction) => {
    acc[interaction.type] = (acc[interaction.type] || 0) + 1;
    return acc;
  }, {});

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-emma-dark">Interactions</h1>
          <p className="text-gray-600 mt-1">Track all communications and AI-powered insights</p>
        </div>
        <button className="bg-emma-blue text-white px-4 py-2 rounded-lg hover:bg-blue-600">
          Add Interaction
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Total Interactions</p>
              <p className="text-2xl font-bold text-gray-900">{interactions.length}</p>
            </div>
            <MessageSquare className="h-8 w-8 text-emma-blue" />
          </div>
        </div>
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Phone Calls</p>
              <p className="text-2xl font-bold text-blue-600">{typeCounts.call || 0}</p>
            </div>
            <Phone className="h-8 w-8 text-blue-500" />
          </div>
        </div>
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Emails</p>
              <p className="text-2xl font-bold text-green-600">{typeCounts.email || 0}</p>
            </div>
            <Mail className="h-8 w-8 text-green-500" />
          </div>
        </div>
        <div className="bg-white p-4 rounded-lg card-shadow">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Meetings</p>
              <p className="text-2xl font-bold text-purple-600">{typeCounts.meeting || 0}</p>
            </div>
            <Calendar className="h-8 w-8 text-purple-500" />
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
              placeholder="Search interactions by contact, content, or tags..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-emma-blue focus:border-transparent"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <div className="flex items-center space-x-2">
            <Filter className="h-5 w-5 text-gray-400" />
            <select
              className="border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-emma-blue focus:border-transparent"
              value={filterType}
              onChange={(e) => setFilterType(e.target.value)}
            >
              <option value="all">All Types</option>
              <option value="call">Phone Calls</option>
              <option value="email">Emails</option>
              <option value="meeting">Meetings</option>
            </select>
          </div>
        </div>
      </div>

      {/* Interactions List */}
      <div className="space-y-4">
        {filteredInteractions.map((interaction) => (
          <div key={interaction.id} className="bg-white rounded-lg card-shadow p-6">
            <div className="flex items-start justify-between mb-4">
              <div className="flex items-center space-x-3">
                <div className={`p-2 rounded-lg ${getTypeColor(interaction.type)}`}>
                  {getTypeIcon(interaction.type)}
                </div>
                <div>
                  <h3 className="text-lg font-medium text-gray-900">{interaction.contactName}</h3>
                  <div className="flex items-center space-x-4 text-sm text-gray-600">
                    <span className="flex items-center">
                      <User className="h-4 w-4 mr-1" />
                      {interaction.agentName}
                    </span>
                    <span className={`font-medium ${getDirectionColor(interaction.direction)}`}>
                      {interaction.direction}
                    </span>
                    {interaction.duration && (
                      <span className="flex items-center">
                        <Clock className="h-4 w-4 mr-1" />
                        {interaction.duration}
                      </span>
                    )}
                  </div>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm text-gray-500">{formatTimestamp(interaction.timestamp)}</p>
                <span className={`inline-block px-2 py-1 rounded-full text-xs font-medium ${getTypeColor(interaction.type)}`}>
                  {interaction.type.toUpperCase()}
                </span>
              </div>
            </div>

            <div className="mb-4">
              <p className="text-gray-700 leading-relaxed">{interaction.content}</p>
            </div>

            <div className="flex flex-wrap gap-2 mb-4">
              {interaction.tags.map((tag, index) => (
                <span key={index} className="inline-block px-2 py-1 rounded-full text-xs bg-gray-100 text-gray-700">
                  {tag}
                </span>
              ))}
            </div>

            {/* AI Insights */}
            <div className="bg-gradient-to-r from-purple-50 to-blue-50 border border-purple-200 rounded-lg p-4">
              <div className="flex items-center space-x-2 mb-3">
                <Brain className="h-5 w-5 text-purple-600" />
                <h4 className="font-medium text-purple-900">AI Insights</h4>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                {Object.entries(interaction.aiInsights).map(([key, value]) => (
                  <div key={key} className="text-sm">
                    <span className="font-medium text-purple-700 capitalize">
                      {key.replace(/([A-Z])/g, ' $1').trim()}:
                    </span>
                    <span className="ml-2 text-purple-600">{value}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>

      {filteredInteractions.length === 0 && (
        <div className="text-center py-12">
          <MessageSquare className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">No interactions found</h3>
          <p className="text-gray-600">Try adjusting your search or filter criteria.</p>
        </div>
      )}
    </div>
  );
};

export default Interactions;
