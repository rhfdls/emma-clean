'use client';
// SPRINT1: /signup step in onboarding
import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useOnboarding } from '../../context/OnboardingContext';

interface FormState {
  email: string;
  password: string;
  confirmPassword: string;
}

const initialForm: FormState = {
  email: '',
  password: '',
  confirmPassword: '',
};

export default function SignupPage() {
  const router = useRouter();
  const { data, setData } = useOnboarding();
  const { plan, seatCount, stripeMetadata } = data;

  const [form, setForm] = useState<FormState>(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const validEmail = /^\S+@\S+\.\S+$/.test(form.email);
  const validPassword = form.password.length >= 8;
  const passwordsMatch = form.password === form.confirmPassword;
  const formValid = validEmail && validPassword && passwordsMatch;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await fetch('/api/onboarding/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          plan: plan,
          seatCount,
          email: form.email,
          password: form.password,
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
      <h1 className="text-3xl font-bold mb-6">Sign Up</h1>
      <div className="mb-4 p-4 bg-gray-50 rounded border">
        <div><span className="font-medium">Plan:</span> <span className="capitalize">{plan}</span></div>
        <div><span className="font-medium">Seats:</span> {seatCount}</div>
        <div><span className="font-medium">Total:</span> ${stripeMetadata?.total}/mo</div>
      </div>
      <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
        <label className="flex flex-col gap-1">
          Email
          <input
            type="email"
            name="email"
            value={form.email}
            onChange={handleChange}
            required
            className="border rounded px-3 py-2"
            autoComplete="email"
          />
        </label>
        <label className="flex flex-col gap-1">
          Password
          <input
            type="password"
            name="password"
            value={form.password}
            onChange={handleChange}
            required
            minLength={8}
            className="border rounded px-3 py-2"
            autoComplete="new-password"
          />
        </label>
        <label className="flex flex-col gap-1">
          Confirm Password
          <input
            type="password"
            name="confirmPassword"
            value={form.confirmPassword}
            onChange={handleChange}
            required
            minLength={8}
            className="border rounded px-3 py-2"
            autoComplete="new-password"
          />
        </label>
        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-2 rounded">
            {error}
          </div>
        )}
        <button
          type="submit"
          className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 transition disabled:opacity-50"
          disabled={!formValid || loading}
        >
          {loading ? 'Registering...' : 'Continue'}
        </button>
      </form>
    </main>
  );
}
