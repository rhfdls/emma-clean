// SPRINT1: Dynamic plan provider API route
import { NextResponse } from 'next/server';

// Simulate dynamic plan config (replace with backend fetch in production)
const plans = [
  {
    key: 'basic',
    displayName: 'Basic',
    description: 'For small teams getting started',
    price: 250,
    features: ['Core CRM', 'Email Support'],
  },
  {
    key: 'pro',
    displayName: 'Pro',
    description: 'Best for growing organizations',
    price: 750,
    features: ['All Basic features', 'Advanced Analytics', 'Priority Support'],
  },
];

export async function GET() {
  // In production: fetch from dynamic enum/config provider
  return NextResponse.json(plans);
}
