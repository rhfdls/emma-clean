// Dynamic enum API route for Next.js frontend
import { NextRequest, NextResponse } from 'next/server';
import path from 'path';
import { promises as fs } from 'fs';

export async function GET(req: NextRequest, { params }: { params: { type: string } }) {
  const type = params.type;
  const configPath = path.join(process.cwd(), '../../Emma.Api/Config/enum-config.json');
  try {
    const json = await fs.readFile(configPath, 'utf8');
    const config = JSON.parse(json);
    if (!config[type]) return NextResponse.json({ error: 'Not found' }, { status: 404 });
    return NextResponse.json(config[type]);
  } catch (err) {
    return NextResponse.json({ error: 'Server error', details: String(err) }, { status: 500 });
  }
}
