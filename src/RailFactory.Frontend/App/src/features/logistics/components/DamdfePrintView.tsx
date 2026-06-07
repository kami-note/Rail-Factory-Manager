import React, { useEffect } from 'react';
import { createPortal } from 'react-dom';
import type { Dispatch, ShipmentOrder } from '../types';
import type { Vehicle } from '../../fleet/types';
import type { Person } from '../../hr/types';

type Props = {
  dispatch: Dispatch;
  order: ShipmentOrder;
  vehicle: Vehicle | undefined;
  driver: Person | undefined;
  onClose: () => void;
};

const CSS = `
@media print {
  body > * { display: none !important; }
  body > .damdfe-root { display: block !important; }
  @page { size: A4; margin: 12mm 10mm; }
}
@media screen {
  .damdfe-root { display: none; }
}
.damdfe-root {
  font-family: Arial, Helvetica, sans-serif;
  font-size: 11px;
  color: #111;
  line-height: 1.4;
  max-width: 190mm;
}
.damdfe-title {
  text-align: center;
  font-size: 15px;
  font-weight: bold;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  margin-bottom: 2px;
}
.damdfe-subtitle {
  text-align: center;
  font-size: 10px;
  color: #555;
  margin-bottom: 10px;
}
.damdfe-divider {
  border: none;
  border-top: 1.5px solid #111;
  margin: 7px 0;
}
.damdfe-section-title {
  font-weight: bold;
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  background: #e8e8e8;
  padding: 2px 5px;
  margin-bottom: 5px;
}
.damdfe-grid {
  display: grid;
  gap: 4px 16px;
}
.damdfe-grid-2 { grid-template-columns: 1fr 1fr; }
.damdfe-grid-3 { grid-template-columns: 1fr 1fr 1fr; }
.damdfe-grid-4 { grid-template-columns: 1fr 1fr 1fr 1fr; }
.damdfe-field { margin-bottom: 3px; }
.damdfe-label { font-size: 9px; color: #555; text-transform: uppercase; letter-spacing: 0.3px; }
.damdfe-value { font-size: 11px; font-weight: bold; }
.damdfe-key {
  font-family: 'Courier New', Courier, monospace;
  font-size: 9px;
  word-break: break-all;
  background: #f5f5f5;
  padding: 3px 5px;
  border: 0.5px solid #ccc;
  display: block;
  margin-top: 2px;
}
.damdfe-ufs {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-top: 4px;
}
.damdfe-uf-chip {
  background: #222;
  color: #fff;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: bold;
}
.damdfe-nfe-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 2px 0;
  border-bottom: 0.5px solid #eee;
}
.damdfe-footer {
  margin-top: 16px;
  padding-top: 8px;
  border-top: 1.5px solid #111;
  display: flex;
  justify-content: space-between;
  font-size: 9px;
  color: #666;
}
.damdfe-sig-row {
  display: flex;
  gap: 20px;
  margin-top: 24px;
}
.damdfe-sig-box {
  flex: 1;
  border-top: 1px solid #111;
  padding-top: 4px;
  font-size: 9px;
  color: #555;
  text-align: center;
}
.damdfe-badge {
  display: inline-block;
  padding: 1px 8px;
  border: 1px solid #111;
  font-size: 10px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
`;

export function DamdfePrintView({ dispatch, order, vehicle, driver, onClose }: Props) {
  useEffect(() => {
    const handler = () => onClose();
    window.addEventListener('afterprint', handler);
    window.print();
    return () => window.removeEventListener('afterprint', handler);
  }, [onClose]);

  const fmtDate = (iso: string) => new Date(iso).toLocaleDateString('pt-BR');
  const fmtDateTime = (iso: string) =>
    new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

  const emissionDate = dispatch.dispatchedAt ?? dispatch.createdAt;
  const vehiclePlate = dispatch.vehiclePlate ?? vehicle?.plate ?? '—';
  const vehicleRntrc = dispatch.vehicleRntrc ?? vehicle?.rntrc ?? '—';
  const driverName = dispatch.driverName ?? driver?.name ?? '—';
  const driverCpf = dispatch.driverCpf ?? driver?.documentNumber ?? '—';

  const totalKg = order.items.reduce((s, i) => s + i.weightKg * i.quantity, 0);
  const totalValue = order.items.reduce((s, i) => s + (i.unitValue ?? 0) * i.quantity, 0);
  const nfeAccessKey = dispatch.fiscalAccessKey;

  const mdfeStatus = dispatch.mdfeStatus;
  const mdfeKey = dispatch.mdfeAccessKey;

  const html = `
<style>${CSS}</style>
<div class="damdfe-root">

  <div class="damdfe-title">DA-MDF-e</div>
  <div class="damdfe-subtitle">Documento Auxiliar do Manifesto Eletrônico de Documentos Fiscais</div>

  <hr class="damdfe-divider" />

  <div class="damdfe-section-title">Identificação do Manifesto</div>
  <div class="damdfe-grid damdfe-grid-4">
    <div class="damdfe-field">
      <div class="damdfe-label">Rastreio</div>
      <div class="damdfe-value">${dispatch.trackingCode}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Data Emissão</div>
      <div class="damdfe-value">${fmtDateTime(emissionDate)}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Status MDF-e</div>
      <div class="damdfe-value">
        <span class="damdfe-badge">${mdfeStatus ?? 'Pendente'}</span>
      </div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Ordem de Expedição</div>
      <div class="damdfe-value">${order.orderNumber}</div>
    </div>
  </div>

  ${mdfeKey ? `
  <div class="damdfe-field" style="margin-top:6px">
    <div class="damdfe-label">Chave de Acesso MDF-e</div>
    <span class="damdfe-key">${mdfeKey}</span>
  </div>` : ''}

  <hr class="damdfe-divider" />

  <div class="damdfe-section-title">Veículo e Motorista</div>
  <div class="damdfe-grid damdfe-grid-4">
    <div class="damdfe-field">
      <div class="damdfe-label">Placa do Veículo</div>
      <div class="damdfe-value">${vehiclePlate}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">RNTRC</div>
      <div class="damdfe-value">${vehicleRntrc}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Motorista</div>
      <div class="damdfe-value">${driverName}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">CPF Motorista</div>
      <div class="damdfe-value">${driverCpf}</div>
    </div>
  </div>

  <hr class="damdfe-divider" />

  <div class="damdfe-section-title">Destinatário</div>
  <div class="damdfe-grid damdfe-grid-2">
    <div class="damdfe-field">
      <div class="damdfe-label">Nome / Razão Social</div>
      <div class="damdfe-value">${order.recipientName ?? '—'}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">CNPJ / CPF</div>
      <div class="damdfe-value">${order.recipientCnpj ?? '—'}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Endereço</div>
      <div class="damdfe-value">${[order.recipientStreet, order.recipientNumber, order.recipientDistrict].filter(Boolean).join(', ') || '—'}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Município / UF</div>
      <div class="damdfe-value">${[order.recipientCity, order.recipientState].filter(Boolean).join(' / ') || '—'}</div>
    </div>
  </div>

  <hr class="damdfe-divider" />

  <div class="damdfe-section-title">Totais da Carga</div>
  <div class="damdfe-grid damdfe-grid-4">
    <div class="damdfe-field">
      <div class="damdfe-label">Qtd. de Itens</div>
      <div class="damdfe-value">${order.items.length}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Peso Bruto Total (kg)</div>
      <div class="damdfe-value">${totalKg.toFixed(3)}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Valor Total (R$)</div>
      <div class="damdfe-value">${totalValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</div>
    </div>
    <div class="damdfe-field">
      <div class="damdfe-label">Frete (R$)</div>
      <div class="damdfe-value">${dispatch.freightValueBrl.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</div>
    </div>
  </div>

  <hr class="damdfe-divider" />

  <div class="damdfe-section-title">Documentos Fiscais Vinculados</div>
  ${nfeAccessKey ? `
  <div class="damdfe-nfe-row">
    <span style="font-size:10px;font-weight:bold;min-width:40px">NF-e</span>
    <span class="damdfe-key" style="flex:1">${nfeAccessKey}</span>
  </div>` : '<div style="color:#888;font-size:10px;padding:4px">Nenhum documento NF-e autorizado vinculado.</div>'}

  <hr class="damdfe-divider" />

  <div class="damdfe-sig-row">
    <div class="damdfe-sig-box">Emitente / Remetente</div>
    <div class="damdfe-sig-box">Motorista</div>
    <div class="damdfe-sig-box">Destinatário</div>
  </div>

  <div class="damdfe-footer">
    <span>Rail Factory — Emitido em ${fmtDateTime(new Date().toISOString())}</span>
    <span>Documento auxiliar — valor informativo</span>
  </div>

</div>
  `;

  return createPortal(
    <div
      className="damdfe-root"
      dangerouslySetInnerHTML={{ __html: html }}
      style={{ position: 'fixed', top: 0, left: 0, zIndex: -1 }}
    />,
    document.body
  );
}
