import React from 'react';
import { Users, MessageSquare, TrendingUp, Clock, Star, Phone, Mail, Calendar } from 'lucide-react';

const Dashboard = () => {
  const stats = [
    { label: 'Active Contacts', value: '47', icon: Users, color: 'bg-blue-500' },
    { label: 'This Week Interactions', value: '23', icon: MessageSquare, color: 'bg-green-500' },
    { label: 'Conversion Rate', value: '18%', icon: TrendingUp, color: 'bg-purple-500' },
    { label: 'Avg Response Time', value: '2.3h', icon: Clock, color: 'bg-orange-500' },
  ];

  const recentInteractions = [
    {
      id: 1,
      contact: 'Emily Johnson',
      type: 'call',
      content: 'Discussed offer strategy for Hillcrest condo. Emily is excited but nervous about competing offers.',
      time: '2 hours ago',
      agent: 'Mike Rodriguez',
      priority: 'high'
    },
    {
      id: 2,
      contact: 'Robert Williams',
      type: 'email',
      content: 'Offer accepted on La Jolla oceanview property! Coordinating inspection and financing.',
      time: '4 hours ago',
      agent: 'Amanda Foster',
      priority: 'high'
    },
    {
      id: 3,
      contact: 'Kevin Brown',
      type: 'email',
      content: 'New lead inquiry about 5th Avenue condo. Pre-approved up to $500K, wants quick showing.',
      time: '6 hours ago',
      agent: 'Jessica Chen',
      priority: 'medium'
    },
  ];

  const upcomingTasks = [
    { task: 'Follow up with Emily on offer decision', time: 'Today 3:00 PM', type: 'call' },
    { task: 'Send comps to Kevin Brown', time: 'Today 4:30 PM', type: 'email' },
    { task: 'Property showing - James Wilson', time: 'Tomorrow 6:00 PM', type: 'meeting' },
    { task: 'Inspection coordination - Robert Williams', time: 'Monday 10:00 AM', type: 'call' },
  ];

  const getTypeIcon = (type) => {
    switch (type) {
      case 'call': return <Phone className="h-4 w-4" />;
      case 'email': return <Mail className="h-4 w-4" />;
      case 'meeting': return <Calendar className="h-4 w-4" />;
      default: return <MessageSquare className="h-4 w-4" />;
    }
  };

  const getPriorityColor = (priority) => {
    switch (priority) {
      case 'high': return 'border-l-red-500 bg-red-50';
      case 'medium': return 'border-l-yellow-500 bg-yellow-50';
      case 'low': return 'border-l-green-500 bg-green-50';
      default: return 'border-l-gray-500 bg-gray-50';
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-emma-dark">Dashboard</h1>
          <p className="text-gray-600 mt-1">Welcome back, Mike! Here's what's happening today.</p>
        </div>
        <div className="bg-emma-blue text-white px-4 py-2 rounded-lg">
          <div className="text-sm">AI Insights Available</div>
          <div className="text-xs opacity-90">3 new recommendations</div>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {stats.map((stat, index) => (
          <div key={index} className="bg-white p-6 rounded-lg card-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">{stat.label}</p>
                <p className="text-3xl font-bold text-gray-900 mt-2">{stat.value}</p>
              </div>
              <div className={`${stat.color} p-3 rounded-lg`}>
                <stat.icon className="h-6 w-6 text-white" />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent Interactions */}
        <div className="lg:col-span-2 bg-white rounded-lg card-shadow">
          <div className="p-6 border-b border-gray-200">
            <h2 className="text-xl font-semibold text-gray-900">Recent Interactions</h2>
            <p className="text-sm text-gray-600">Latest activity across your contact network</p>
          </div>
          <div className="p-6 space-y-4">
            {recentInteractions.map((interaction) => (
              <div key={interaction.id} className={`p-4 rounded-lg border-l-4 ${getPriorityColor(interaction.priority)}`}>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-2 mb-2">
                      {getTypeIcon(interaction.type)}
                      <span className="font-medium text-gray-900">{interaction.contact}</span>
                      <span className="text-sm text-gray-500">• {interaction.agent}</span>
                    </div>
                    <p className="text-gray-700 text-sm">{interaction.content}</p>
                  </div>
                  <span className="text-xs text-gray-500 whitespace-nowrap ml-4">{interaction.time}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Upcoming Tasks */}
        <div className="bg-white rounded-lg card-shadow">
          <div className="p-6 border-b border-gray-200">
            <h2 className="text-xl font-semibold text-gray-900">Upcoming Tasks</h2>
            <p className="text-sm text-gray-600">AI-suggested follow-ups</p>
          </div>
          <div className="p-6 space-y-3">
            {upcomingTasks.map((task, index) => (
              <div key={index} className="flex items-start space-x-3 p-3 rounded-lg hover:bg-gray-50">
                <div className="flex-shrink-0 mt-1">
                  {getTypeIcon(task.type)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900">{task.task}</p>
                  <p className="text-xs text-gray-500">{task.time}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* AI Insights Section */}
      <div className="bg-gradient-to-r from-emma-blue to-purple-600 rounded-lg p-6 text-white">
        <div className="flex items-center space-x-3 mb-4">
          <Star className="h-6 w-6" />
          <h2 className="text-xl font-semibold">AI Insights</h2>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-white bg-opacity-20 rounded-lg p-4">
            <h3 className="font-medium mb-2">Hot Prospects</h3>
            <p className="text-sm opacity-90">Kevin Brown and James Wilson show high conversion signals</p>
          </div>
          <div className="bg-white bg-opacity-20 rounded-lg p-4">
            <h3 className="font-medium mb-2">Follow-up Needed</h3>
            <p className="text-sm opacity-90">3 contacts haven't been contacted in 5+ days</p>
          </div>
          <div className="bg-white bg-opacity-20 rounded-lg p-4">
            <h3 className="font-medium mb-2">Referral Opportunity</h3>
            <p className="text-sm opacity-90">Thomas Anderson mentioned his brother is looking to buy</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
