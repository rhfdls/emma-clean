'use client';
// SPRINT1: /signup step in onboarding
import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useOnboarding } from '../../context/OnboardingContext';

interface FormState {
  organizationName: string;
  email: string;
  password: string;
  confirmPassword: string;
  crm: string;
}

const initialForm: FormState = {
  organizationName: '',
  email: '',
  password: '',
  confirmPassword: '',
  crm: 'none',
};

import { useEffect } from 'react';
import Image from 'next/image';

export default function SignupPage() {
  const router = useRouter();
  const { data, setData } = useOnboarding();
  const { plan, seatCount, stripeMetadata } = data;

  const [form, setForm] = useState<FormState>(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [hydrated, setHydrated] = useState(false);

  // Parse plan and seats from URL, persist in localStorage and context
  useEffect(() => {
    if (typeof window === 'undefined') return;
    const params = new URLSearchParams(window.location.search);
    const urlPlan = params.get('plan') || 'basic';
    const urlSeats = parseInt(params.get('seats') || '1', 10);
    const planToUse = urlPlan;
    const seatCountToUse = isNaN(urlSeats) ? 1 : urlSeats;
    setData((prev: any) => ({ ...prev, plan: planToUse, seatCount: seatCountToUse }));
    localStorage.setItem('emma_onboarding_plan', planToUse);
    localStorage.setItem('emma_onboarding_seatCount', seatCountToUse.toString());
    setHydrated(true);
  }, [setData]);

  if (!hydrated) return null;

  const validEmail = /^\S+@\S+\.\S+$/.test(form.email);
  const validPassword = form.password.length >= 8;
  const passwordsMatch = form.password === form.confirmPassword;
  const formValid = validEmail && validPassword && passwordsMatch;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await fetch('http://localhost:5000/api/onboarding/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          OrganizationName: form.organizationName,
          Email: form.email,
          Password: form.password,
          PlanKey: plan,
          SeatCount: seatCount,
          Crm: form.crm !== 'none' ? form.crm : undefined,
        }),
      });
      if (!res.ok) throw new Error('Registration failed');
      setData((prev) => ({ ...prev, email: form.email }));
      router.push('/onboarding/verify');
    } catch (err: any) {
      setError(err.message || 'Registration failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="max-w-xl mx-auto py-12 px-4">
      <div className="flex flex-col items-center mb-8">
        {/* Use the provided GoG logo image for best branding fidelity */}
        <Image src="/gift-of-gab-logo.png" alt="Gift of Gab Logo" width={180} height={180} className="mb-4" priority />
      </div>
      <h1 className="text-2xl font-semibold text-center mb-6">Create Your Account</h1>
      <div className="mb-4 p-4 bg-gray-50 rounded border">
        <div><span className="font-medium">Plan:</span> <span className="capitalize">{plan}</span></div>
        <div><span className="font-medium">Seats:</span> {seatCount}</div>
        <div><span className="font-medium">Total:</span> ${stripeMetadata?.total}/mo</div>
      </div>
      <form className="flex flex-col gap-5" onSubmit={handleSubmit} noValidate>
        <label className="flex flex-col gap-1 font-medium">
          Organization Name <span className="text-red-500">*</span>
          <input
            type="text"
            name="organizationName"
            value={form.organizationName}
            onChange={handleChange}
            required
            minLength={2}
            className={`border px-3 py-2 rounded shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-300 ${!form.organizationName && form.organizationName !== undefined ? 'border-red-500' : 'border-gray-300'}`}
            autoComplete="organization"
          />
          {!form.organizationName && (
            <span className="text-red-500 text-sm mt-1">Organization name is required.</span>
          )}
        </label>
        {/* SPRINT1: CRM selection (optional) */}
        <label className="flex flex-col gap-1 font-medium">
          CRM Integration (optional)
          <select
            name="crm"
            value={form.crm}
            onChange={handleChange}
            className="border px-3 py-2 rounded shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-300"
          >
            <option value="none">None</option>
            <option value="fub">Follow Up Boss (FUB)</option>
          </select>
          <span className="text-xs text-gray-500 mt-1">You can connect a CRM now or later in settings.</span>
        </label>
        <label className="flex flex-col gap-1 font-medium">
          Email <span className="text-red-500">*</span>
          <input
            type="email"
            name="email"
            value={form.email}
            onChange={handleChange}
            required
            className={`border px-3 py-2 rounded shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-300 ${!validEmail && form.email ? 'border-red-500' : 'border-gray-300'}`}
            autoComplete="email"
          />
          {!validEmail && form.email && (
            <span className="text-red-500 text-sm mt-1">Please enter a valid email address.</span>
          )}
        </label>
        <label className="flex flex-col gap-1 font-medium">
          Password <span className="text-red-500">*</span>
          <input
            type="password"
            name="password"
            value={form.password}
            onChange={handleChange}
            required
            minLength={8}
            className={`border px-3 py-2 rounded shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-300 ${!validPassword && form.password ? 'border-red-500' : 'border-gray-300'}`}
            autoComplete="new-password"
          />
          {!validPassword && form.password && (
            <span className="text-red-500 text-sm mt-1">Password must be at least 8 characters.</span>
          )}
        </label>
        <label className="flex flex-col gap-1 font-medium">
          Confirm Password <span className="text-red-500">*</span>
          <input
            type="password"
            name="confirmPassword"
            value={form.confirmPassword}
            onChange={handleChange}
            required
            minLength={8}
            className={`border px-3 py-2 rounded shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-300 ${!passwordsMatch && form.confirmPassword ? 'border-red-500' : 'border-gray-300'}`}
            autoComplete="new-password"
          />
          {!passwordsMatch && form.confirmPassword && (
            <span className="text-red-500 text-sm mt-1">Passwords do not match.</span>
          )}
        </label>
        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-2 rounded mt-2">
            {error}
          </div>
        )}
        <button
          type="submit"
          className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 transition disabled:opacity-50 font-semibold tracking-wide shadow"
          disabled={!formValid || loading}
        >
          {loading ? 'Registering...' : 'Register'}
        </button>
      </form>
      <div className="mt-6 text-center text-gray-600">
        Already have an account?{' '}
        <a href="/login" className="text-blue-600 hover:underline font-medium">Login</a>
      </div>
    </main>
  );
}
