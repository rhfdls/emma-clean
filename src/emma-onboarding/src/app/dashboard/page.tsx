// SPRINT1: OrgDashboard for post-onboarding
'use client';
import React, { useEffect, useState } from 'react';
import { useOnboarding } from '../context/OnboardingContext';

interface OrgProfile {
  orgName: string;
  plan: {
    key: string;
    label: string;
    description: string;
    price: number;
    [key: string]: any;
  };
  email: string;
  accountStatus: string;
  orgGuid?: string;
}

const isDev = process.env.NODE_ENV !== 'production';

export default function OrgDashboard() {
  const { data } = useOnboarding();
  const [profile, setProfile] = useState<OrgProfile | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // If onboarding context is missing, fetch from API
    if (!data?.plan || !data?.orgName) {
      setLoading(true);
      fetch('/api/account/profile')
        .then((res) => res.json())
        .then((profile) => setProfile(profile))
        .finally(() => setLoading(false));
    } else {
      setProfile({
        orgName: data.orgName,
        plan: data.plan,
        email: data.email,
        accountStatus: data.accountStatus,
        orgGuid: data.orgGuid,
      });
    }
  }, [data]);

  if (loading) return <div>Loading organization data...</div>;
  if (!profile) return <div>No organization data available.</div>;

  return (
    <main className="max-w-xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold mb-2">{profile.orgName}</h1>
      {isDev && profile.orgGuid && (
        <div className="text-xs text-gray-500 mb-2">
          <strong>Org GUID (Internal use only):</strong> {profile.orgGuid}
        </div>
      )}
      <div className="mb-4">
        <span className="font-medium">Plan:</span> {profile.plan.label} <span className="text-gray-500">(${profile.plan.price}/mo)</span>
      </div>
      <div className="mb-8">
        <span className="font-medium">Account Status:</span> {profile.accountStatus}
      </div>
      <div className="p-4 bg-blue-50 border border-blue-200 rounded">
        <span className="font-semibold text-blue-800">Agent setup coming soon.</span>
      </div>
    </main>
  );
}
