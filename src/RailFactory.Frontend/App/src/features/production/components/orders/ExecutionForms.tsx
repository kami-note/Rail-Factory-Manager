import React, { useState } from 'react';
import { Tab, Tabs } from '@mui/material';
import { CheckCircle, FlaskConical, Trash2 } from 'lucide-react';
import { MaterialExecutionForm } from './MaterialExecutionForm';
import { InspectionForm } from './InspectionForm';

export function ExecutionForms({ tenantCode, orderId, onRecorded }: {
  tenantCode: string;
  orderId: string;
  onRecorded: () => void;
}) {
  const [subTab, setSubTab] = useState(0);

  return (
    <>
      <Tabs value={subTab} onChange={(_, v) => setSubTab(v as number)} variant="fullWidth" sx={{ mb: 2 }}>
        <Tab label="Consumo" icon={<FlaskConical size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
        <Tab label="Scrap" icon={<Trash2 size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
        <Tab label="Inspeção" icon={<CheckCircle size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
      </Tabs>
      {subTab === 0 && <MaterialExecutionForm tenantCode={tenantCode} orderId={orderId} mode="consumption" onRecorded={onRecorded} />}
      {subTab === 1 && <MaterialExecutionForm tenantCode={tenantCode} orderId={orderId} mode="scrap" onRecorded={onRecorded} />}
      {subTab === 2 && <InspectionForm tenantCode={tenantCode} orderId={orderId} onRecorded={onRecorded} />}
    </>
  );
}
