import React, { useEffect } from 'react';
import { createPortal } from 'react-dom';
import type { Dispatch, ShipmentOrder, Carrier } from '../types';
import type { Vehicle } from '../../fleet/types';
import type { Person } from '../../hr/types';

type Props = {
  dispatch: Dispatch;
  order: ShipmentOrder;
  vehicle: Vehicle | undefined;
  driver: Person | undefined;
  carrier: Carrier | undefined;
  onClose: () => void;
};

const CSS = `
@media print {
  body > * { display: none !important; }
  body > .rfp-root { display: block !important; }
  @page { margin: 15mm 12mm; }
}
@media screen {
  .rfp-root { display: none; }
}
.rfp-root {
  font-family: Arial, Helvetica, sans-serif;
  font-size: 11px;
  color: #111;
  line-height: 1.4;
}
.rfp-title {
  text-align: center;
  font-size: 16px;
  font-weight: bold;
  letter-spacing: 2px;
  text-transform: uppercase;
  margin-bottom: 2px;
}
.rfp-subtitle {
  text-align: center;
  font-size: 11px;
  color: #555;
  margin-bottom: 12px;
}
.rfp-divider {
  border: none;
  border-top: 1.5px solid #111;
  margin: 8px 0;
}
.rfp-divider-light {
  border: none;
  border-top: 0.5px solid #ccc;
  margin: 6px 0;
}
.rfp-row {
  display: flex;
  gap: 24px;
  margin-bottom: 8px;
}
.rfp-col {
  flex: 1;
}
.rfp-section-title {
  font-size: 9px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  color: #555;
  margin-bottom: 4px;
}
.rfp-field {
  margin-bottom: 3px;
}
.rfp-field-label {
  font-size: 9px;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.rfp-field-value {
  font-size: 12px;
  font-weight: bold;
}
.rfp-field-value.mono {
  font-family: 'Courier New', Courier, monospace;
  font-size: 13px;
}
.rfp-field-value.normal {
  font-weight: normal;
  font-size: 11px;
}
.rfp-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 6px;
}
.rfp-table th {
  font-size: 9px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #444;
  border-bottom: 1px solid #111;
  padding: 4px 4px 4px 0;
  text-align: left;
}
.rfp-table th.right { text-align: right; }
.rfp-table td {
  padding: 4px 4px 4px 0;
  border-bottom: 0.5px solid #ddd;
  font-size: 11px;
  vertical-align: middle;
}
.rfp-table td.mono {
  font-family: 'Courier New', Courier, monospace;
  font-size: 12px;
  font-weight: bold;
}
.rfp-table td.right { text-align: right; }
.rfp-table td.muted { color: #555; font-size: 10px; }
.rfp-totals {
  display: flex;
  justify-content: flex-end;
  gap: 24px;
  margin-top: 6px;
  padding-top: 6px;
  border-top: 1px solid #111;
}
.rfp-total-item {
  text-align: right;
}
.rfp-footer {
  margin-top: 16px;
  padding-top: 10px;
  border-top: 1.5px solid #111;
}
.rfp-nfe-key {
  font-family: 'Courier New', Courier, monospace;
  font-size: 10px;
  word-break: break-all;
  background: #f5f5f5;
  padding: 4px 6px;
  border: 0.5px solid #ccc;
  display: inline-block;
  margin-top: 2px;
}
.rfp-sig-row {
  display: flex;
  gap: 24px;
  margin-top: 20px;
}
.rfp-sig-box {
  flex: 1;
  border-top: 1px solid #111;
  padding-top: 4px;
  font-size: 9px;
  color: #555;
  text-align: center;
}
`;

export function DispatchPrintView({ dispatch, order, vehicle, driver, carrier, onClose }: Props) {
  useEffect(() => {
    const handler = () => onClose();
    window.addEventListener('afterprint', handler);
    window.print();
    return () => window.removeEventListener('afterprint', handler);
  }, [onClose]);

  const totalWeight = order.items.reduce((s, i) => s + i.weightKg * i.quantity, 0);
  const totalVolume = order.items.reduce((s, i) => s + i.volumeCbm * i.quantity, 0);

  const fmt = (iso: string) => new Date(iso).toLocaleDateString('pt-BR');
  const fmtDateTime = (iso: string) =>
    new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

  const emissionDate = dispatch.dispatchedAt ?? dispatch.conferencedAt ?? dispatch.createdAt;

  const addressLine1 = [
    order.recipientStreet,
    order.recipientNumber ? `nº ${order.recipientNumber}` : null,
    order.recipientDistrict,
  ].filter(Boolean).join(', ');

  const addressLine2 = [
    order.recipientCity,
    order.recipientState,
    order.recipientZipCode ? `CEP ${order.recipientZipCode}` : null,
  ].filter(Boolean).join(' — ');

  return createPortal(
    <>
      <style>{CSS}</style>
      <div className="rfp-root">

        {/* Cabeçalho */}
        <div className="rfp-title">Romaneio de Carga</div>
        <div className="rfp-subtitle">
          Despacho {dispatch.trackingCode} · Emitido em {fmtDateTime(emissionDate)}
        </div>
        <hr className="rfp-divider" />

        {/* Bloco de informações: despacho + transporte */}
        <div className="rfp-row">
          <div className="rfp-col">
            <div className="rfp-section-title">Ordem de Expedição</div>
            <div className="rfp-field">
              <div className="rfp-field-label">Número</div>
              <div className="rfp-field-value mono">{order.orderNumber}</div>
            </div>
            {order.productionOrderRef && (
              <div className="rfp-field">
                <div className="rfp-field-label">Ordem de Produção</div>
                <div className="rfp-field-value normal">{order.productionOrderRef}</div>
              </div>
            )}
            {order.natureOfOperation && (
              <div className="rfp-field">
                <div className="rfp-field-label">Natureza da Operação</div>
                <div className="rfp-field-value normal">{order.natureOfOperation}</div>
              </div>
            )}
          </div>

          <div className="rfp-col">
            <div className="rfp-section-title">Transporte</div>
            {carrier && (
              <div className="rfp-field">
                <div className="rfp-field-label">Transportadora</div>
                <div className="rfp-field-value">{carrier.name}</div>
                {carrier.documentNumber && (
                  <div style={{ fontSize: 10, color: '#666' }}>CNPJ {carrier.documentNumber}</div>
                )}
              </div>
            )}
            {vehicle && (
              <div className="rfp-field">
                <div className="rfp-field-label">Veículo</div>
                <div className="rfp-field-value mono">{vehicle.plate}</div>
                <div style={{ fontSize: 10, color: '#666' }}>{vehicle.type.label}</div>
              </div>
            )}
            {driver && (
              <div className="rfp-field">
                <div className="rfp-field-label">Motorista</div>
                <div className="rfp-field-value">{driver.name}</div>
              </div>
            )}
          </div>
        </div>

        <hr className="rfp-divider-light" />

        {/* Destinatário */}
        {(order.recipientName || order.recipientCnpj) && (
          <>
            <div className="rfp-section-title">Destinatário</div>
            <div className="rfp-row" style={{ marginBottom: 4 }}>
              <div className="rfp-col">
                {order.recipientName && (
                  <div className="rfp-field">
                    <div className="rfp-field-label">Razão Social / Nome</div>
                    <div className="rfp-field-value">{order.recipientName}</div>
                  </div>
                )}
                {order.recipientCnpj && (
                  <div className="rfp-field">
                    <div className="rfp-field-label">CNPJ / CPF</div>
                    <div className="rfp-field-value normal">{order.recipientCnpj}</div>
                  </div>
                )}
              </div>
              {(addressLine1 || addressLine2) && (
                <div className="rfp-col">
                  <div className="rfp-field">
                    <div className="rfp-field-label">Endereço</div>
                    {addressLine1 && <div className="rfp-field-value normal">{addressLine1}</div>}
                    {addressLine2 && <div className="rfp-field-value normal">{addressLine2}</div>}
                  </div>
                </div>
              )}
            </div>
            <hr className="rfp-divider-light" />
          </>
        )}

        {/* Itens */}
        <div className="rfp-section-title" style={{ marginTop: 8 }}>
          Itens do Despacho ({order.items.length})
        </div>
        <table className="rfp-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Código do Material</th>
              <th className="right">Quantidade</th>
              <th>UM</th>
              <th className="right">Peso Unit. (kg)</th>
              <th className="right">Peso Total (kg)</th>
              <th className="right">Volume (m³)</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map((item, i) => (
              <tr key={item.id}>
                <td className="muted">{i + 1}</td>
                <td className="mono">{item.materialCode}</td>
                <td className="right">{item.quantity}</td>
                <td className="muted">{item.unitOfMeasure}</td>
                <td className="right muted">{item.weightKg.toFixed(3)}</td>
                <td className="right">{(item.weightKg * item.quantity).toFixed(3)}</td>
                <td className="right muted">{(item.volumeCbm * item.quantity).toFixed(4)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="rfp-totals">
          <div className="rfp-total-item">
            <div className="rfp-field-label">Total de Itens</div>
            <div className="rfp-field-value">{order.items.length}</div>
          </div>
          <div className="rfp-total-item">
            <div className="rfp-field-label">Peso Total (kg)</div>
            <div className="rfp-field-value">{totalWeight.toFixed(3)}</div>
          </div>
          <div className="rfp-total-item">
            <div className="rfp-field-label">Volume Total (m³)</div>
            <div className="rfp-field-value">{totalVolume.toFixed(4)}</div>
          </div>
          <div className="rfp-total-item">
            <div className="rfp-field-label">Frete (R$)</div>
            <div className="rfp-field-value">
              {dispatch.freightValueBrl.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}
            </div>
          </div>
        </div>

        {/* Rodapé */}
        <div className="rfp-footer">
          {dispatch.fiscalAccessKey ? (
            <div>
              <div className="rfp-field-label">Chave de Acesso NF-e</div>
              <div className="rfp-nfe-key">{dispatch.fiscalAccessKey}</div>
            </div>
          ) : (
            <div style={{ fontSize: 10, color: '#888' }}>NF-e não emitida ou chave não disponível.</div>
          )}

          <div className="rfp-sig-row">
            <div className="rfp-sig-box">
              Assinatura e carimbo do recebedor
            </div>
            <div className="rfp-sig-box">
              Data e hora do recebimento
            </div>
            <div className="rfp-sig-box">
              Documento do recebedor (CPF/RG)
            </div>
          </div>

          <div style={{ marginTop: 14, fontSize: 9, color: '#888', textAlign: 'center' }}>
            Emitido em {fmt(emissionDate)} · Rastreio: {dispatch.trackingCode}
            {dispatch.conferencedAt ? ` · Conferido em ${fmt(dispatch.conferencedAt)}` : ''}
          </div>
        </div>

      </div>
    </>,
    document.body
  );
}
