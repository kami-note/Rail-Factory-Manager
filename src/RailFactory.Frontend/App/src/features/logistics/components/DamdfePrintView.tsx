import React, { useEffect } from 'react';
import { createPortal } from 'react-dom';
import type { Dispatch, ShipmentOrder, TenantFiscalProfile } from '../types';
import type { Vehicle } from '../../fleet/types';
import type { Person } from '../../hr/types';

type Props = {
  dispatch: Dispatch;
  order: ShipmentOrder;
  vehicle: Vehicle | undefined;
  driver: Person | undefined;
  fiscalProfile: TenantFiscalProfile | undefined;
  onClose: () => void;
};

const CSS = `
@media print {
  body > * { display: none !important; }
  body > .damdfe-root { display: block !important; }
  @page { size: A4 portrait; margin: 10mm 10mm; }
}
@media screen {
  .damdfe-root { display: none; }
}
.damdfe-root {
  font-family: Arial, Helvetica, sans-serif;
  font-size: 10px;
  color: #111;
  line-height: 1.35;
  max-width: 190mm;
}
.damdfe-header {
  display: flex;
  border: 1.5px solid #111;
  margin-bottom: 0;
}
.damdfe-header-left {
  flex: 1;
  padding: 6px 8px;
  border-right: 1px solid #111;
}
.damdfe-header-center {
  width: 110px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border-right: 1px solid #111;
  text-align: center;
}
.damdfe-header-right {
  width: 130px;
  padding: 6px 8px;
  font-size: 9px;
}
.damdfe-company-name {
  font-size: 13px;
  font-weight: bold;
  text-transform: uppercase;
  margin-bottom: 2px;
}
.damdfe-company-details { font-size: 9px; color: #333; }
.damdfe-doc-title {
  font-size: 11px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 2px;
}
.damdfe-doc-subtitle { font-size: 8px; color: #555; margin-bottom: 4px; }
.damdfe-section {
  border: 1px solid #111;
  border-top: none;
}
.damdfe-section-title {
  background: #ddd;
  padding: 2px 6px;
  font-size: 8px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.4px;
}
.damdfe-section-body { padding: 4px 6px; }
.damdfe-row {
  display: flex;
  gap: 12px;
  margin-bottom: 2px;
}
.damdfe-field { flex: 1; }
.damdfe-label {
  font-size: 7.5px;
  color: #555;
  text-transform: uppercase;
  letter-spacing: 0.2px;
  margin-bottom: 1px;
}
.damdfe-value { font-size: 10px; font-weight: 700; }
.damdfe-value-normal { font-size: 10px; font-weight: 400; }
.damdfe-percurso {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 0;
  flex-wrap: wrap;
}
.damdfe-uf-origin, .damdfe-uf-dest {
  background: #222;
  color: #fff;
  padding: 3px 10px;
  border-radius: 14px;
  font-size: 12px;
  font-weight: bold;
}
.damdfe-uf-arrow {
  font-size: 14px;
  color: #555;
  font-weight: bold;
}
.damdfe-key-box {
  border: 1px solid #333;
  padding: 2px 4px;
  background: #f9f9f9;
  font-family: 'Courier New', Courier, monospace;
  font-size: 8px;
  word-break: break-all;
  margin-top: 2px;
}
.damdfe-nfe-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 2px 0;
  border-bottom: 0.5px solid #eee;
}
.damdfe-nfe-seq {
  font-size: 9px;
  font-weight: bold;
  color: #555;
  min-width: 24px;
}
.damdfe-key-inline {
  font-family: 'Courier New', Courier, monospace;
  font-size: 8.5px;
  background: #f5f5f5;
  padding: 1px 4px;
  border: 0.5px solid #ccc;
  flex: 1;
  word-break: break-all;
}
.damdfe-protocol-box {
  border: 1px solid #111;
  border-top: none;
  padding: 4px 8px;
  font-size: 8px;
  background: #f0f0f0;
  text-align: center;
}
.damdfe-sig-row {
  display: flex;
  border: 1px solid #111;
  border-top: none;
}
.damdfe-sig-box {
  flex: 1;
  border-right: 1px solid #111;
  padding: 20px 8px 4px;
  font-size: 8.5px;
  color: #555;
  text-align: center;
}
.damdfe-sig-box:last-child { border-right: none; }
.damdfe-footer {
  margin-top: 4px;
  font-size: 8px;
  color: #777;
  text-align: center;
}
.damdfe-badge {
  display: inline-block;
  padding: 1px 7px;
  border: 1px solid #111;
  font-size: 9px;
  font-weight: bold;
  text-transform: uppercase;
}
`;

function fmtCnpj(v: string): string {
  const d = v.replace(/\D/g, '');
  if (d.length === 14) return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');
  if (d.length === 11) return d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
  return v;
}

function fmtKey(key: string): string {
  return key.replace(/(.{4})/g, '$1 ').trim();
}

export function DamdfePrintView({ dispatch, order, vehicle, driver, fiscalProfile, onClose }: Props) {
  useEffect(() => {
    const handler = () => onClose();
    window.addEventListener('afterprint', handler);
    window.print();
    return () => window.removeEventListener('afterprint', handler);
  }, [onClose]);

  const fmtDate = (iso: string) => new Date(iso).toLocaleDateString('pt-BR');
  const fmtDateTime = (iso: string) =>
    new Date(iso).toLocaleString('pt-BR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });

  const emissionDate = dispatch.dispatchedAt ?? dispatch.createdAt;
  const vehiclePlate = dispatch.vehiclePlate ?? vehicle?.plate ?? '—';
  const vehicleRntrc = dispatch.vehicleRntrc ?? vehicle?.rntrc ?? '—';
  const vehicleRenavam = vehicle?.renavam ?? '—';
  const driverName = dispatch.driverName ?? driver?.name ?? '—';
  const driverCpf = dispatch.driverCpf ?? driver?.documentNumber ?? '—';

  const totalKg = order.items.reduce((s, i) => s + i.weightKg * i.quantity, 0);
  const totalValue = order.items.reduce((s, i) => s + (i.unitValue ?? 0) * i.quantity, 0);
  const nfeAccessKey = dispatch.mdfeLinkedNfeKey ?? dispatch.fiscalAccessKey;

  const mdfeKey = dispatch.mdfeAccessKey;
  const mdfeStatus = dispatch.mdfeStatus ?? 'Pendente';

  const ufCarregamento = dispatch.mdfeUfCarregamento ?? fiscalProfile?.ufOrigem ?? '—';
  const ufDescarregamento = dispatch.mdfeUfDescarregamento ?? order.recipientState ?? '—';

  const emitterName = fiscalProfile?.emitterName || '—';
  const emitterCnpj = fiscalProfile?.emitterCnpj ? fmtCnpj(fiscalProfile.emitterCnpj) : '—';
  const emitterIe = fiscalProfile?.emitterIe || '—';
  const emitterCity = fiscalProfile?.emitterCity || '—';
  const emitterState = fiscalProfile?.emitterState || fiscalProfile?.ufOrigem || '—';

  const html = `
<style>${CSS}</style>
<div class="damdfe-root">

  <!-- CABEÇALHO -->
  <div class="damdfe-header">
    <div class="damdfe-header-left">
      <div class="damdfe-company-name">${emitterName}</div>
      <div class="damdfe-company-details">
        CNPJ: ${emitterCnpj} &nbsp;|&nbsp; IE: ${emitterIe}
        <br/>${emitterCity} – ${emitterState}
      </div>
    </div>
    <div class="damdfe-header-center">
      <div class="damdfe-doc-title">DA-MDF-e</div>
      <div class="damdfe-doc-subtitle">Manifesto Eletrônico de<br/>Documentos Fiscais</div>
      <div style="font-size:9px;font-weight:bold">Modal: Rodoviário</div>
    </div>
    <div class="damdfe-header-right">
      <div style="margin-bottom:3px">
        <div class="damdfe-label">Status MDF-e</div>
        <span class="damdfe-badge">${mdfeStatus}</span>
      </div>
      <div style="margin-bottom:3px">
        <div class="damdfe-label">Data / Hora Emissão</div>
        <div style="font-size:9px;font-weight:700">${fmtDateTime(emissionDate)}</div>
      </div>
      <div>
        <div class="damdfe-label">Ordem de Expedição</div>
        <div style="font-size:9px;font-weight:700">${order.orderNumber}</div>
      </div>
    </div>
  </div>

  ${mdfeKey ? `
  <div class="damdfe-section" style="border-top:1px solid #111">
    <div class="damdfe-section-title">Chave de Acesso MDF-e (44 dígitos)</div>
    <div class="damdfe-section-body">
      <div class="damdfe-key-box">${fmtKey(mdfeKey)}</div>
    </div>
  </div>` : ''}

  <!-- PERCURSO -->
  <div class="damdfe-section">
    <div class="damdfe-section-title">Percurso</div>
    <div class="damdfe-section-body">
      <div class="damdfe-percurso">
        <span class="damdfe-uf-origin">${ufCarregamento}</span>
        <span class="damdfe-uf-arrow">&#8594;</span>
        <span style="font-size:8px;color:#777;font-style:italic">direto</span>
        <span class="damdfe-uf-arrow">&#8594;</span>
        <span class="damdfe-uf-dest">${ufDescarregamento}</span>
      </div>
      <div class="damdfe-row" style="margin-top:4px">
        <div class="damdfe-field">
          <div class="damdfe-label">UF de Carregamento</div>
          <div class="damdfe-value">${ufCarregamento}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">UFs Percorridas (intermediárias)</div>
          <div class="damdfe-value-normal" style="font-style:italic;color:#666">Nenhuma — transporte direto</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">UF de Descarregamento</div>
          <div class="damdfe-value">${ufDescarregamento}</div>
        </div>
      </div>
    </div>
  </div>

  <!-- VEÍCULO -->
  <div class="damdfe-section">
    <div class="damdfe-section-title">Veículo de Tração — Modal Rodoviário</div>
    <div class="damdfe-section-body">
      <div class="damdfe-row">
        <div class="damdfe-field">
          <div class="damdfe-label">Placa</div>
          <div class="damdfe-value">${vehiclePlate}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">RENAVAM</div>
          <div class="damdfe-value">${vehicleRenavam}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">RNTRC</div>
          <div class="damdfe-value">${vehicleRntrc}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">Cap. Máx. Carga (kg)</div>
          <div class="damdfe-value">${vehicle?.maxWeightKg?.toLocaleString('pt-BR') ?? '—'}</div>
        </div>
      </div>
    </div>
  </div>

  <!-- MOTORISTA -->
  <div class="damdfe-section">
    <div class="damdfe-section-title">Motorista</div>
    <div class="damdfe-section-body">
      <div class="damdfe-row">
        <div class="damdfe-field" style="flex:2">
          <div class="damdfe-label">Nome</div>
          <div class="damdfe-value">${driverName}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">CPF</div>
          <div class="damdfe-value">${driverCpf}</div>
        </div>
      </div>
    </div>
  </div>

  <!-- DESTINATÁRIO -->
  <div class="damdfe-section">
    <div class="damdfe-section-title">Destinatário / Local de Descarregamento</div>
    <div class="damdfe-section-body">
      <div class="damdfe-row">
        <div class="damdfe-field" style="flex:2">
          <div class="damdfe-label">Nome / Razão Social</div>
          <div class="damdfe-value">${order.recipientName ?? '—'}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">CNPJ / CPF</div>
          <div class="damdfe-value">${order.recipientCnpj ? fmtCnpj(order.recipientCnpj) : '—'}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">IE</div>
          <div class="damdfe-value-normal">${order.recipientIe ?? '—'}</div>
        </div>
      </div>
      <div class="damdfe-row">
        <div class="damdfe-field" style="flex:3">
          <div class="damdfe-label">Endereço</div>
          <div class="damdfe-value-normal">${[order.recipientStreet, order.recipientNumber, order.recipientDistrict].filter(Boolean).join(', ') || '—'}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">Município / UF</div>
          <div class="damdfe-value-normal">${[order.recipientCity, order.recipientState].filter(Boolean).join(' / ') || '—'}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">CEP</div>
          <div class="damdfe-value-normal">${order.recipientZipCode ?? '—'}</div>
        </div>
      </div>
    </div>
  </div>

  <!-- TOTAIS DA CARGA -->
  <div class="damdfe-section">
    <div class="damdfe-section-title">Produto Predominante / Totais da Carga</div>
    <div class="damdfe-section-body">
      <div class="damdfe-row">
        <div class="damdfe-field">
          <div class="damdfe-label">Qtd. Docs. Fiscais</div>
          <div class="damdfe-value">${nfeAccessKey ? 1 : 0}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">Qtd. de Itens</div>
          <div class="damdfe-value">${order.items.length}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">Peso Bruto Total (kg)</div>
          <div class="damdfe-value">${totalKg.toLocaleString('pt-BR', { minimumFractionDigits: 3, maximumFractionDigits: 3 })}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">Valor Total Carga (R$)</div>
          <div class="damdfe-value">${totalValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</div>
        </div>
        <div class="damdfe-field">
          <div class="damdfe-label">Valor do Frete (R$)</div>
          <div class="damdfe-value">${dispatch.freightValueBrl.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</div>
        </div>
      </div>
    </div>
  </div>

  <!-- DOCUMENTOS FISCAIS -->
  <div class="damdfe-section">
    <div class="damdfe-section-title">Documentos Fiscais Vinculados (NF-e)</div>
    <div class="damdfe-section-body">
      ${nfeAccessKey ? `
      <div class="damdfe-nfe-row">
        <span class="damdfe-nfe-seq">001</span>
        <span style="font-size:9px;font-weight:bold;min-width:28px">NF-e</span>
        <span class="damdfe-key-inline">${fmtKey(nfeAccessKey)}</span>
      </div>` : `
      <div style="color:#888;font-size:9px;padding:3px">
        Nenhum documento NF-e autorizado vinculado.
      </div>`}
    </div>
  </div>

  <!-- PROTOCOLO -->
  <div class="damdfe-protocol-box">
    <strong>Protocolo de Autorização SEFAZ:</strong>
    ${mdfeKey
      ? `MDF-e autorizado &nbsp;|&nbsp; Chave: ${fmtKey(mdfeKey)}`
      : 'Aguardando autorização SEFAZ'}
    &nbsp;&nbsp;|&nbsp;&nbsp;
    Emitido em: ${fmtDateTime(new Date().toISOString())}
    &nbsp;&nbsp;|&nbsp;&nbsp;
    Rastreio: ${dispatch.trackingCode}
  </div>

  <!-- ASSINATURAS -->
  <div class="damdfe-sig-row">
    <div class="damdfe-sig-box">Emitente / Remetente</div>
    <div class="damdfe-sig-box">Motorista</div>
    <div class="damdfe-sig-box">Destinatário</div>
  </div>

  <div class="damdfe-footer">
    Documento Auxiliar do Manifesto Eletrônico de Documentos Fiscais — valor informativo — ${fmtDate(new Date().toISOString())}
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
