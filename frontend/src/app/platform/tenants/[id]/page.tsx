import { PlatformTenantDetailView } from "@/components/platform-admin";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <PlatformTenantDetailView tenantId={Number(id)} />;
}
