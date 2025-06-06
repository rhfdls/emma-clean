import React from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell } from 'recharts';
import { TrendingUp, Users, MessageSquare, Target, Clock, Star, Brain, Calendar } from 'lucide-react';

const Analytics = () => {
  const conversionData = [
    { month: 'Jul', leads: 12, prospects: 8, clients: 3 },
    { month: 'Aug', leads: 15, prospects: 11, clients: 4 },
    { month: 'Sep', leads: 18, prospects: 13, clients: 5 },
    { month: 'Oct', leads: 22, prospects: 16, clients: 6 },
    { month: 'Nov', leads: 25, prospects: 18, clients: 8 },
  ];

  const interactionData = [
    { day: 'Mon', calls: 8, emails: 12, meetings: 3 },
    { day: 'Tue', calls: 6, emails: 15, meetings: 2 },
    { day: 'Wed', calls: 10, emails: 18, meetings: 4 },
    { day: 'Thu', calls: 12, emails: 14, meetings: 5 },
    { day: 'Fri', calls: 9, emails: 16, meetings: 3 },
    { day: 'Sat', calls: 4, emails: 8, meetings: 2 },
    { day: 'Sun', calls: 2, emails: 5, meetings: 1 },
  ];

  const responseTimeData = [
    { agent: 'Mike R.', avgTime: 2.1, target: 4.0 },
    { agent: 'Sarah J.', avgTime: 1.8, target: 4.0 },
    { agent: 'Jessica C.', avgTime: 3.2, target: 4.0 },
    { agent: 'Amanda F.', avgTime: 2.7, target: 4.0 },
    { agent: 'David K.', avgTime: 1.5, target: 4.0 },
  ];

  const leadSourceData = [
    { name: 'Website', value: 35, color: '#3b82f6' },
    { name: 'Referrals', value: 28, color: '#10b981' },
    { name: 'Social Media', value: 20, color: '#f59e0b' },
    { name: 'Open Houses', value: 12, color: '#8b5cf6' },
    { name: 'Other', value: 5, color: '#6b7280' },
  ];

  const aiInsights = [
    {
      title: 'Conversion Optimization',
      insight: 'First-time buyers show 23% higher conversion when contacted within 2 hours',
      action: 'Implement priority routing for first-time buyer inquiries',
      impact: '+15% conversion rate'
    },
    {
      title: 'Follow-up Timing',
      insight: 'Tuesday-Thursday calls have 40% higher answer rates than Monday/Friday',
      action: 'Reschedule follow-up calls to mid-week slots',
      impact: '+12% contact rate'
    },
    {
      title: 'Content Personalization',
      insight: 'Luxury clients respond better to market analysis vs. property features',
      action: 'Customize email templates by client segment',
      impact: '+8% engagement'
    },
    {
      title: 'Referral Opportunities',
      insight: '3 past clients mentioned family/friends looking to buy in recent calls',
      action: 'Send referral request emails to identified opportunities',
      impact: '+3 potential leads'
    }
  ];

  const kpiMetrics = [
    { label: 'Conversion Rate', value: '18.2%', change: '+2.3%', trend: 'up', icon: Target },
    { label: 'Avg Response Time', value: '2.3h', change: '-0.8h', trend: 'up', icon: Clock },
    { label: 'Client Satisfaction', value: '4.8/5', change: '+0.2', trend: 'up', icon: Star },
    { label: 'Monthly Interactions', value: '247', change: '+18%', trend: 'up', icon: MessageSquare },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-emma-dark">Analytics</h1>
          <p className="text-gray-600 mt-1">AI-powered insights and performance metrics</p>
        </div>
        <div className="flex space-x-2">
          <button className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">
            Export Report
          </button>
          <button className="bg-emma-blue text-white px-4 py-2 rounded-lg hover:bg-blue-600">
            Schedule Report
          </button>
        </div>
      </div>

      {/* KPI Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {kpiMetrics.map((metric, index) => (
          <div key={index} className="bg-white p-6 rounded-lg card-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">{metric.label}</p>
                <p className="text-3xl font-bold text-gray-900 mt-2">{metric.value}</p>
                <div className="flex items-center mt-2">
                  <TrendingUp className={`h-4 w-4 mr-1 ${metric.trend === 'up' ? 'text-green-500' : 'text-red-500'}`} />
                  <span className={`text-sm font-medium ${metric.trend === 'up' ? 'text-green-600' : 'text-red-600'}`}>
                    {metric.change}
                  </span>
                  <span className="text-sm text-gray-500 ml-1">vs last month</span>
                </div>
              </div>
              <div className="bg-emma-blue bg-opacity-10 p-3 rounded-lg">
                <metric.icon className="h-6 w-6 text-emma-blue" />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Charts Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Conversion Funnel */}
        <div className="bg-white p-6 rounded-lg card-shadow">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Conversion Funnel</h2>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={conversionData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" />
              <YAxis />
              <Tooltip />
              <Bar dataKey="leads" fill="#3b82f6" name="Leads" />
              <Bar dataKey="prospects" fill="#10b981" name="Prospects" />
              <Bar dataKey="clients" fill="#f59e0b" name="Clients" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Weekly Interactions */}
        <div className="bg-white p-6 rounded-lg card-shadow">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Weekly Interactions</h2>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={interactionData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="day" />
              <YAxis />
              <Tooltip />
              <Line type="monotone" dataKey="calls" stroke="#3b82f6" name="Calls" />
              <Line type="monotone" dataKey="emails" stroke="#10b981" name="Emails" />
              <Line type="monotone" dataKey="meetings" stroke="#8b5cf6" name="Meetings" />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Response Time by Agent */}
        <div className="bg-white p-6 rounded-lg card-shadow">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Response Time by Agent</h2>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={responseTimeData} layout="horizontal">
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis type="number" domain={[0, 5]} />
              <YAxis dataKey="agent" type="category" />
              <Tooltip formatter={(value) => [`${value} hours`, 'Response Time']} />
              <Bar dataKey="avgTime" fill="#3b82f6" name="Avg Response Time" />
              <Bar dataKey="target" fill="#e5e7eb" name="Target (4h)" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Lead Sources */}
        <div className="bg-white p-6 rounded-lg card-shadow">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Lead Sources</h2>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={leadSourceData}
                cx="50%"
                cy="50%"
                outerRadius={100}
                fill="#8884d8"
                dataKey="value"
                label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
              >
                {leadSourceData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* AI Insights Section */}
      <div className="bg-gradient-to-r from-purple-600 to-blue-600 rounded-lg p-6 text-white">
        <div className="flex items-center space-x-3 mb-6">
          <Brain className="h-6 w-6" />
          <h2 className="text-xl font-semibold">AI-Powered Insights</h2>
          <span className="bg-white bg-opacity-20 px-2 py-1 rounded text-sm">Updated 2 hours ago</span>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {aiInsights.map((insight, index) => (
            <div key={index} className="bg-white bg-opacity-10 rounded-lg p-4">
              <h3 className="font-semibold mb-2">{insight.title}</h3>
              <p className="text-sm opacity-90 mb-3">{insight.insight}</p>
              <div className="space-y-2">
                <div className="text-sm">
                  <span className="font-medium">Recommended Action:</span>
                  <p className="opacity-90">{insight.action}</p>
                </div>
                <div className="text-sm">
                  <span className="font-medium">Expected Impact:</span>
                  <span className="ml-2 bg-green-500 bg-opacity-20 px-2 py-1 rounded text-green-100">
                    {insight.impact}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Performance Summary */}
      <div className="bg-white rounded-lg card-shadow p-6">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">Performance Summary</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="text-center">
            <div className="bg-green-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-3">
              <TrendingUp className="h-8 w-8 text-green-600" />
            </div>
            <h3 className="font-semibold text-gray-900">Conversion Rate</h3>
            <p className="text-2xl font-bold text-green-600 mt-1">18.2%</p>
            <p className="text-sm text-gray-600">Above industry average (15%)</p>
          </div>
          
          <div className="text-center">
            <div className="bg-blue-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-3">
              <Users className="h-8 w-8 text-blue-600" />
            </div>
            <h3 className="font-semibold text-gray-900">Active Pipeline</h3>
            <p className="text-2xl font-bold text-blue-600 mt-1">47</p>
            <p className="text-sm text-gray-600">Contacts across all stages</p>
          </div>
          
          <div className="text-center">
            <div className="bg-purple-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-3">
              <Calendar className="h-8 w-8 text-purple-600" />
            </div>
            <h3 className="font-semibold text-gray-900">Avg. Sales Cycle</h3>
            <p className="text-2xl font-bold text-purple-600 mt-1">42 days</p>
            <p className="text-sm text-gray-600">15% faster than last quarter</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Analytics;
