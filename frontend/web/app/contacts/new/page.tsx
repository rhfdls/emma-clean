import ContactForm from "@/components/forms/ContactForm";

export const dynamic = "force-dynamic";

export default function NewContactPage() {
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold">New Contact</h1>
        <p className="text-sm text-muted-foreground">
          Create a contact in your organization.
        </p>
      </header>
      <ContactForm />
    </div>
  );
}
