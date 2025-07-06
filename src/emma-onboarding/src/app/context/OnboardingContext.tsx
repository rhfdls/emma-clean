'use client';
// SPRINT1: OnboardingContext for plan, seat, and Stripe metadata
import React, { createContext, useContext, useState, useEffect } from 'react';

export interface Plan {
  key: string;
  label: string;
  description: string;
  price: number;
  minSeats?: number;
  maxSeats?: number;
  [key: string]: any;
}

export interface StripeMetadata {
  planId: string;
  total: number;
}

export interface OnboardingData {
  plan: Plan | null;
  seatCount: number;
  stripeMetadata?: StripeMetadata;
  orgName?: string;
  email?: string;
  accountStatus?: string;
  orgGuid?: string;
}

const defaultData: OnboardingData = {
  plan: null,
  seatCount: 1,
};

const OnboardingContext = createContext<{
  data: OnboardingData;
  setData: React.Dispatch<React.SetStateAction<OnboardingData>>;
} | undefined>(undefined);

export const OnboardingProvider = ({ children }: { children: React.ReactNode }) => {
  const [data, setData] = useState<OnboardingData>(() => {
    if (typeof window !== 'undefined') {
      const stored = window.localStorage.getItem('onboarding');
      return stored ? JSON.parse(stored) : defaultData;
    }
    return defaultData;
  });

  useEffect(() => {
    window.localStorage.setItem('onboarding', JSON.stringify(data));
  }, [data]);

  return (
    <OnboardingContext.Provider value={{ data, setData }}>
      {children}
    </OnboardingContext.Provider>
  );
};

export const useOnboarding = () => {
  const ctx = useContext(OnboardingContext);
  if (!ctx) throw new Error('useOnboarding must be used within OnboardingProvider');
  return ctx;
};
