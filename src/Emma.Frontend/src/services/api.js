import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || 'http://localhost:5000',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for adding auth tokens
api.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for handling errors
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized access
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Contact API endpoints
export const contactsAPI = {
  getAll: () => api.get('/api/contacts'),
  getById: (id) => api.get(`/api/contacts/${id}`),
  create: (contact) => api.post('/api/contacts', contact),
  update: (id, contact) => api.put(`/api/contacts/${id}`, contact),
  delete: (id) => api.delete(`/api/contacts/${id}`),
  search: (query) => api.get(`/api/contacts/search?q=${encodeURIComponent(query)}`),
};

// Interactions API endpoints
export const interactionsAPI = {
  getAll: () => api.get('/api/interactions'),
  getById: (id) => api.get(`/api/interactions/${id}`),
  getByContactId: (contactId) => api.get(`/api/interactions/contact/${contactId}`),
  create: (interaction) => api.post('/api/interactions', interaction),
  update: (id, interaction) => api.put(`/api/interactions/${id}`, interaction),
  delete: (id) => api.delete(`/api/interactions/${id}`),
  search: (query) => api.get(`/api/interactions/search?q=${encodeURIComponent(query)}`),
};

// AI API endpoints
export const aiAPI = {
  queryInteractions: (query) => api.post('/api/ai/query-interactions', { query }),
  generateInsights: (contactId) => api.post('/api/ai/insights', { contactId }),
  processMessage: (message, context) => api.post('/api/ai/process-message', { message, context }),
  getRecommendations: (contactId) => api.get(`/api/ai/recommendations/${contactId}`),
};

// Analytics API endpoints
export const analyticsAPI = {
  getDashboardStats: () => api.get('/api/analytics/dashboard'),
  getConversionData: (period = '6months') => api.get(`/api/analytics/conversion?period=${period}`),
  getInteractionStats: (period = '7days') => api.get(`/api/analytics/interactions?period=${period}`),
  getResponseTimes: () => api.get('/api/analytics/response-times'),
  getLeadSources: (period = '3months') => api.get(`/api/analytics/lead-sources?period=${period}`),
  getPerformanceMetrics: () => api.get('/api/analytics/performance'),
};

// Service Providers API endpoints
export const serviceProvidersAPI = {
  getAll: () => api.get('/api/service-providers'),
  getById: (id) => api.get(`/api/service-providers/${id}`),
  getByType: (type) => api.get(`/api/service-providers/type/${type}`),
  create: (provider) => api.post('/api/service-providers', provider),
  update: (id, provider) => api.put(`/api/service-providers/${id}`, provider),
  delete: (id) => api.delete(`/api/service-providers/${id}`),
};

// Contact Assignments API endpoints
export const assignmentsAPI = {
  getAll: () => api.get('/api/contact-assignments'),
  getByContactId: (contactId) => api.get(`/api/contact-assignments/contact/${contactId}`),
  getByProviderId: (providerId) => api.get(`/api/contact-assignments/provider/${providerId}`),
  create: (assignment) => api.post('/api/contact-assignments', assignment),
  update: (id, assignment) => api.put(`/api/contact-assignments/${id}`, assignment),
  updateStatus: (id, status) => api.patch(`/api/contact-assignments/${id}/status`, { status }),
  delete: (id) => api.delete(`/api/contact-assignments/${id}`),
};

// Demo API endpoints for presentation
export const demoAPI = {
  getScenarios: () => api.get('/api/demo/scenarios'),
  runScenario: (scenarioId) => api.post(`/api/demo/scenarios/${scenarioId}/run`),
  resetDemo: () => api.post('/api/demo/reset'),
  getSeedData: () => api.get('/api/demo/seed-data'),
};

// Ask EMMA - Conversational AI
export const askEmmaApi = {
  askQuestion: (question, context = {}) => api.post('/ask-emma/ask', {
    question,
    context,
    includeInsights: true
  }),
  askSimple: (question) => api.post('/ask-emma/ask-simple', {
    question
  }),
  getInteractionHistory: (sessionId) => api.get(`/ask-emma/interactions/${sessionId}`),
  clearInteraction: (sessionId) => api.delete(`/ask-emma/interactions/${sessionId}`)
};

// Utility functions
export const handleApiError = (error) => {
  console.error('API Error:', error);
  
  if (error.response) {
    // Server responded with error status
    const { status, data } = error.response;
    return {
      message: data.message || `Server error (${status})`,
      status,
      details: data.details || null,
    };
  } else if (error.request) {
    // Network error
    return {
      message: 'Network error - please check your connection',
      status: 0,
      details: 'No response from server',
    };
  } else {
    // Other error
    return {
      message: error.message || 'An unexpected error occurred',
      status: -1,
      details: null,
    };
  }
};

// Helper function to format API responses
export const formatResponse = (response) => {
  return {
    data: response.data,
    status: response.status,
    headers: response.headers,
  };
};

// Export the configured axios instance as default
export default api;
