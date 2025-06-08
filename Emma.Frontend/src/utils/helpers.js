import { format, formatDistanceToNow, parseISO, isValid } from 'date-fns';

// Date formatting utilities
export const formatDate = (date, formatString = 'MMM dd, yyyy') => {
  if (!date) return '';
  
  const parsedDate = typeof date === 'string' ? parseISO(date) : date;
  if (!isValid(parsedDate)) return '';
  
  return format(parsedDate, formatString);
};

export const formatDateTime = (date) => {
  return formatDate(date, 'MMM dd, yyyy HH:mm');
};

export const formatTimeAgo = (date) => {
  if (!date) return '';
  
  const parsedDate = typeof date === 'string' ? parseISO(date) : date;
  if (!isValid(parsedDate)) return '';
  
  return formatDistanceToNow(parsedDate, { addSuffix: true });
};

// String utilities
export const truncateText = (text, maxLength = 100) => {
  if (!text || text.length <= maxLength) return text;
  return text.substring(0, maxLength).trim() + '...';
};

export const capitalizeFirst = (str) => {
  if (!str) return '';
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
};

export const formatPhoneNumber = (phone) => {
  if (!phone) return '';
  
  // Remove all non-digits
  const digits = phone.replace(/\D/g, '');
  
  // Format as (XXX) XXX-XXXX for 10-digit numbers
  if (digits.length === 10) {
    return `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`;
  }
  
  // Return original if not 10 digits
  return phone;
};

export const formatCurrency = (amount, currency = 'USD') => {
  if (amount === null || amount === undefined) return '';
  
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
};

// Contact status utilities
export const getContactStatusColor = (status) => {
  const statusColors = {
    'Lead': 'bg-blue-100 text-blue-800',
    'Prospect': 'bg-yellow-100 text-yellow-800',
    'Client': 'bg-green-100 text-green-800',
    'PastClient': 'bg-gray-100 text-gray-800',
    'Inactive': 'bg-red-100 text-red-800',
  };
  
  return statusColors[status] || 'bg-gray-100 text-gray-800';
};

export const getContactPriorityColor = (priority) => {
  const priorityColors = {
    'high': 'text-red-600',
    'medium': 'text-yellow-600',
    'low': 'text-green-600',
  };
  
  return priorityColors[priority] || 'text-gray-600';
};

// Interaction type utilities
export const getInteractionTypeIcon = (type) => {
  const typeIcons = {
    'call': '📞',
    'email': '📧',
    'meeting': '📅',
    'note': '📝',
    'sms': '💬',
    'social': '📱',
  };
  
  return typeIcons[type] || '💬';
};

export const getInteractionTypeColor = (type) => {
  const typeColors = {
    'call': 'bg-blue-100 text-blue-800',
    'email': 'bg-green-100 text-green-800',
    'meeting': 'bg-purple-100 text-purple-800',
    'note': 'bg-gray-100 text-gray-800',
    'sms': 'bg-yellow-100 text-yellow-800',
    'social': 'bg-pink-100 text-pink-800',
  };
  
  return typeColors[type] || 'bg-gray-100 text-gray-800';
};

// Data validation utilities
export const isValidEmail = (email) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

export const isValidPhone = (phone) => {
  const phoneRegex = /^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$/;
  return phoneRegex.test(phone);
};

// Array utilities
export const groupBy = (array, key) => {
  return array.reduce((groups, item) => {
    const group = item[key];
    if (!groups[group]) {
      groups[group] = [];
    }
    groups[group].push(item);
    return groups;
  }, {});
};

export const sortBy = (array, key, direction = 'asc') => {
  return [...array].sort((a, b) => {
    const aVal = a[key];
    const bVal = b[key];
    
    if (aVal < bVal) return direction === 'asc' ? -1 : 1;
    if (aVal > bVal) return direction === 'asc' ? 1 : -1;
    return 0;
  });
};

// Search utilities
export const fuzzySearch = (items, query, searchFields) => {
  if (!query) return items;
  
  const lowercaseQuery = query.toLowerCase();
  
  return items.filter(item => {
    return searchFields.some(field => {
      const value = getNestedValue(item, field);
      if (Array.isArray(value)) {
        return value.some(v => 
          String(v).toLowerCase().includes(lowercaseQuery)
        );
      }
      return String(value).toLowerCase().includes(lowercaseQuery);
    });
  });
};

const getNestedValue = (obj, path) => {
  return path.split('.').reduce((current, key) => current?.[key], obj);
};

// Local storage utilities
export const saveToLocalStorage = (key, data) => {
  try {
    localStorage.setItem(key, JSON.stringify(data));
    return true;
  } catch (error) {
    console.error('Error saving to localStorage:', error);
    return false;
  }
};

export const loadFromLocalStorage = (key, defaultValue = null) => {
  try {
    const item = localStorage.getItem(key);
    return item ? JSON.parse(item) : defaultValue;
  } catch (error) {
    console.error('Error loading from localStorage:', error);
    return defaultValue;
  }
};

export const removeFromLocalStorage = (key) => {
  try {
    localStorage.removeItem(key);
    return true;
  } catch (error) {
    console.error('Error removing from localStorage:', error);
    return false;
  }
};

// URL utilities
export const buildQueryString = (params) => {
  const searchParams = new URLSearchParams();
  
  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      searchParams.append(key, String(value));
    }
  });
  
  return searchParams.toString();
};

export const parseQueryString = (queryString) => {
  const params = new URLSearchParams(queryString);
  const result = {};
  
  for (const [key, value] of params.entries()) {
    result[key] = value;
  }
  
  return result;
};

// Demo utilities
export const generateMockData = (count, generator) => {
  return Array.from({ length: count }, (_, index) => generator(index));
};

export const simulateApiDelay = (ms = 1000) => {
  return new Promise(resolve => setTimeout(resolve, ms));
};

// Chart data utilities
export const prepareChartData = (data, xKey, yKey, groupKey = null) => {
  if (groupKey) {
    const grouped = groupBy(data, groupKey);
    return Object.entries(grouped).map(([group, items]) => ({
      [xKey]: group,
      [yKey]: items.reduce((sum, item) => sum + (item[yKey] || 0), 0),
    }));
  }
  
  return data.map(item => ({
    [xKey]: item[xKey],
    [yKey]: item[yKey],
  }));
};

// Error handling utilities
export const getErrorMessage = (error) => {
  if (typeof error === 'string') return error;
  if (error?.message) return error.message;
  if (error?.response?.data?.message) return error.response.data.message;
  return 'An unexpected error occurred';
};

export const logError = (error, context = '') => {
  console.error(`Error${context ? ` in ${context}` : ''}:`, error);
  
  // In production, you might want to send this to an error tracking service
  if (process.env.NODE_ENV === 'production') {
    // Example: Sentry.captureException(error, { extra: { context } });
  }
};

export default {
  formatDate,
  formatDateTime,
  formatTimeAgo,
  truncateText,
  capitalizeFirst,
  formatPhoneNumber,
  formatCurrency,
  getContactStatusColor,
  getContactPriorityColor,
  getInteractionTypeIcon,
  getInteractionTypeColor,
  isValidEmail,
  isValidPhone,
  groupBy,
  sortBy,
  fuzzySearch,
  saveToLocalStorage,
  loadFromLocalStorage,
  removeFromLocalStorage,
  buildQueryString,
  parseQueryString,
  generateMockData,
  simulateApiDelay,
  prepareChartData,
  getErrorMessage,
  logError,
};
