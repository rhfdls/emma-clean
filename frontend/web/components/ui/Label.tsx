"use client";
import React from "react";

export default function Label({ className = "", ...rest }: React.LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label {...rest} className={`block text-sm font-medium text-gray-700 ${className}`} />
  );
}
