import React, { useEffect } from 'react';
import { createPortal } from 'react-dom';
import type { Dispatch, ShipmentOrder, Carrier, TenantFiscalProfile } from '../types';
import { Masks } from '../../../shared/lib/utils/masks';

type Props = {
  dispatch: Dispatch;
  order: ShipmentOrder;
  carrier: Carrier | undefined;
  fiscalProfile: TenantFiscalProfile | undefined;
  onClose: () => void;
};

const CSS = `
@media print {
  body > * { display: none !important; }
  body > .rfcd-root { display: block !important; }
  @page { size: A4 portrait; margin: 15mm 12mm; }
}
@media screen {
  .rfcd-root { display: none; }
}
.rfcd-root {
  font-family: Arial, Helvetica, sans-serif;
  font-size: 11px;
  color: #111;
  line-height: 1.4;
  max-width: 190mm;
}
.rfcd-watermark {
  position: fixed;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%) rotate(-45deg);
  font-size: 80px;
  font-weight: bold;
  color: rgba(0,0,0,0.04);
  pointer-events: none;
  z-index: 0;
  white-space: nowrap;
  letter-spacing: 8px;
}
.rfcd-header {
  border: 2px solid #111;
  margin-bottom: 0;
  display: flex;
  align-items: stretch;
}
.rfcd-header-brand {
  flex: 1;
  padding: 10px 12px;
  border-right: 1px solid #111;
}
.rfcd-brand-name {
  font-size: 16px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 2px;
  margin-bottom: 2px;
}
.rfcd-brand-details { font-size: 9px; color: #444; }
.rfcd-header-doc {
  width: 170px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 8px;
  text-align: center;
}
.rfcd-doc-title {
  font-size: 13px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  margin-bottom: 3px;
  border: 1px solid #111;
  padding: 3px 10px;
}
.rfcd-doc-number {
  font-size: 10px;
  font-family: 'Courier New', Courier, monospace;
  font-weight: bold;
  color: #333;
}
.rfcd-section {
  border: 1px solid #111;
  border-top: none;
}
.rfcd-section-title {
  background: #f0f0f0;
  border-bottom: 1px solid #ccc;
  padding: 3px 8px;
  font-size: 8.5px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #333;
}
.rfcd-section-body {
  padding: 6px 8px;
}
.rfcd-row {
  display: flex;
  gap: 16px;
  margin-bottom: 4px;
}
.rfcd-field { flex: 1; }
.rfcd-label {
  font-size: 8px;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  margin-bottom: 1px;
}
.rfcd-value {
  font-size: 11px;
  font-weight: bold;
}
.rfcd-value-normal {
  font-size: 11px;
  font-weight: normal;
}
.rfcd-value-mono {
  font-family: 'Courier New', Courier, monospace;
  font-size: 12px;
  font-weight: bold;
}
.rfcd-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 4px;
}
.rfcd-table th {
  font-size: 8.5px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.4px;
  color: #333;
  border-bottom: 1.5px solid #111;
  padding: 4px 4px 4px 0;
  text-align: left;
}
.rfcd-table th.right { text-align: right; }
.rfcd-table td {
  padding: 4px 4px 4px 0;
  border-bottom: 0.5px solid #ddd;
  font-size: 11px;
  vertical-align: middle;
}
.rfcd-table td.mono {
  font-family: 'Courier New', Courier, monospace;
  font-size: 12px;
  font-weight: bold;
}
.rfcd-table td.right { text-align: right; }
.rfcd-table td.muted { color: #666; font-size: 10px; }
.rfcd-totals-row {
  display: flex;
  justify-content: flex-end;
  gap: 24px;
  margin-top: 6px;
  padding-top: 6px;
  border-top: 1.5px solid #111;
}
.rfcd-total-item { text-align: right; }
.rfcd-payment-box {
  border: 2px solid #333;
  padding: 8px 12px;
  margin-top: 6px;
  background: #fafafa;
}
.rfcd-payment-title {
  font-size: 10px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  margin-bottom: 6px;
  border-bottom: 1px solid #ccc;
  padding-bottom: 4px;
}
.rfcd-nfe-key {
  font-family: 'Courier New', Courier, monospace;
  font-size: 9.5px;
  word-break: break-all;
  background: #f5f5f5;
  padding: 3px 6px;
  border: 0.5px solid #ccc;
  display: inline-block;
  margin-top: 2px;
}
.rfcd-sig-row {
  display: flex;
  gap: 16px;
  margin-top: 24px;
}
.rfcd-sig-box {
  flex: 1;
  border-top: 1px solid #111;
  padding-top: 4px;
  font-size: 8.5px;
  color: #555;
  text-align: center;
}
.rfcd-footer {
  margin-top: 8px;
  font-size: 8px;
  color: #888;
  text-align: center;
  padding-top: 4px;
  border-top: 0.5px solid #ccc;
}
`;

/**
 * CustomerDocumentPrintView
 *
 * Official customer-facing document for cargo dispatch.
 * Contains all service information needed by the recipient,
 * including payment reference and NF-e data.
 *
 * @remarks
 * Rendered into a print portal (display:none on screen, visible only on print).
 * Automatically triggers window.print() on mount.
 */
export function CustomerDocumentPrintView({ dispatch, order, carrier, fiscalProfile, onClose }: Props) {
  useEffect(() => {
    const handler = () => onClose();
    window.addEventListener('afterprint', handler);
    window.print();
    return () => window.removeEventListener('afterprint', handler);
  }, [onClose]);

  const fmt = (iso: string) => new Date(iso).toLocaleDateString('pt-BR');
  const fmtDateTime = (iso: string) =>
    new Date(iso).toLocaleString('pt-BR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });

  const emissionDate = dispatch.dispatchedAt ?? dispatch.conferencedAt ?? dispatch.createdAt;

  // Due date: 30 days from creation
  const dueDate = new Date(dispatch.createdAt);
  dueDate.setDate(dueDate.getDate() + 30);

  const totalWeight = order.items.reduce((s, i) => s + i.weightKg * i.quantity, 0);
  const totalValue = order.items.reduce((s, i) => s + (i.unitValue ?? 0) * i.quantity, 0);

  const addressLine1 = [
    order.recipientStreet,
    order.recipientNumber ? `nº ${order.recipientNumber}` : null,
    order.recipientDistrict,
  ].filter(Boolean).join(', ');

  const addressLine2 = [
    order.recipientCity,
    order.recipientState,
    order.recipientZipCode ? `CEP ${Masks.cep(order.recipientZipCode)}` : null,
  ].filter(Boolean).join(' — ');

  const emitterName = fiscalProfile?.emitterName ?? 'EMITENTE NÃO CONFIGURADO';
  const emitterCnpj = fiscalProfile?.emitterCnpj ? Masks.cpfCnpj(fiscalProfile.emitterCnpj) : '—';
  const emitterCity = fiscalProfile?.emitterCity ?? '';
  const emitterState = fiscalProfile?.emitterState ?? '';

  const hasPayment = !!(dispatch.paymentBoletoUrl || dispatch.paymentPixUrl);

  return createPortal(
    <>
      <style>{CSS}</style>
      <div className="rfcd-root">
        <div className="rfcd-watermark">DOCUMENTO PARA CLIENTE</div>

        {/* Cabeçalho */}
        <div className="rfcd-header">
          <div className="rfcd-header-brand">
            <div className="rfcd-brand-name">{emitterName}</div>
            <div className="rfcd-brand-details">
              CNPJ: {emitterCnpj}{emitterCity ? ` · ${emitterCity}` : ''}{emitterState ? `/${emitterState}` : ''}
            </div>
          </div>
          <div className="rfcd-header-doc">
            <div className="rfcd-doc-title">Documento para Cliente</div>
            <div className="rfcd-doc-number">{dispatch.trackingCode}</div>
            <div style={{ fontSize: 9, color: '#666', marginTop: 2 }}>Emitido em {fmt(emissionDate)}</div>
          </div>
        </div>

        {/* Destinatário */}
        <div className="rfcd-section">
          <div className="rfcd-section-title">Destinatário</div>
          <div className="rfcd-section-body">
            <div className="rfcd-row">
              <div className="rfcd-field" style={{ flex: 2 }}>
                <div className="rfcd-label">Razão Social / Nome</div>
                <div className="rfcd-value">{order.recipientName ?? '—'}</div>
              </div>
              <div className="rfcd-field">
                <div className="rfcd-label">CNPJ / CPF</div>
                <div className="rfcd-value-mono">{order.recipientCnpj ? Masks.cpfCnpj(order.recipientCnpj) : '—'}</div>
              </div>
              {order.recipientEmail && (
                <div className="rfcd-field">
                  <div className="rfcd-label">E-mail</div>
                  <div className="rfcd-value-normal">{order.recipientEmail}</div>
                </div>
              )}
            </div>
            {(addressLine1 || addressLine2) && (
              <div className="rfcd-row">
                <div className="rfcd-field">
                  <div className="rfcd-label">Endereço</div>
                  {addressLine1 && <div className="rfcd-value-normal">{addressLine1}</div>}
                  {addressLine2 && <div className="rfcd-value-normal">{addressLine2}</div>}
                </div>
                {order.recipientIe && (
                  <div className="rfcd-field">
                    <div className="rfcd-label">Inscrição Estadual</div>
                    <div className="rfcd-value-normal">{order.recipientIe}</div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Informações do Serviço */}
        <div className="rfcd-section">
          <div className="rfcd-section-title">Informações do Serviço de Transporte</div>
          <div className="rfcd-section-body">
            <div className="rfcd-row">
              <div className="rfcd-field">
                <div className="rfcd-label">Ordem de Expedição</div>
                <div className="rfcd-value-mono">{order.orderNumber}</div>
              </div>
              <div className="rfcd-field">
                <div className="rfcd-label">Código de Rastreio</div>
                <div className="rfcd-value-mono">{dispatch.trackingCode}</div>
              </div>
              <div className="rfcd-field">
                <div className="rfcd-label">Data de Emissão</div>
                <div className="rfcd-value-normal">{fmtDateTime(emissionDate)}</div>
              </div>
              <div className="rfcd-field">
                <div className="rfcd-label">Status</div>
                <div className="rfcd-value-normal">{{
                  Pending: 'Pendente',
                  InTransit: 'Em Trânsito',
                  Delivered: 'Entregue',
                  Returned: 'Devolvido',
                }[dispatch.status as string] ?? dispatch.status}</div>
              </div>
            </div>
            <div className="rfcd-row">
              {carrier && (
                <div className="rfcd-field">
                  <div className="rfcd-label">Transportadora</div>
                  <div className="rfcd-value">{carrier.name}</div>
                  {carrier.documentNumber && (
                    <div style={{ fontSize: 9, color: '#666' }}>CNPJ {Masks.cpfCnpj(carrier.documentNumber)}</div>
                  )}
                </div>
              )}
              {order.productionOrderRef && (
                <div className="rfcd-field">
                  <div className="rfcd-label">Ordem de Produção</div>
                  <div className="rfcd-value-normal">{order.productionOrderRef}</div>
                </div>
              )}
              {order.natureOfOperation && (
                <div className="rfcd-field">
                  <div className="rfcd-label">Natureza da Operação</div>
                  <div className="rfcd-value-normal">{order.natureOfOperation}</div>
                </div>
              )}
              {dispatch.shippingTrackingCode && (
                <div className="rfcd-field">
                  <div className="rfcd-label">Rastreio Transportadora</div>
                  <div className="rfcd-value-mono">{dispatch.shippingTrackingCode}</div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Itens */}
        <div className="rfcd-section">
          <div className="rfcd-section-title">Relação de Itens Despachados ({order.items.length} {order.items.length === 1 ? 'item' : 'itens'})</div>
          <div className="rfcd-section-body" style={{ paddingTop: 2 }}>
            <table className="rfcd-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Código do Material</th>
                  <th className="right">Qtd.</th>
                  <th>UM</th>
                  <th className="right">Valor Unit.</th>
                  <th className="right">Valor Total</th>
                  <th className="right">Peso (kg)</th>
                </tr>
              </thead>
              <tbody>
                {order.items.map((item, i) => (
                  <tr key={item.id}>
                    <td className="muted">{i + 1}</td>
                    <td className="mono">{item.materialCode}</td>
                    <td className="right">{item.quantity}</td>
                    <td className="muted">{item.unitOfMeasure}</td>
                    <td className="right muted">
                      {item.unitValue != null
                        ? item.unitValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
                        : '—'}
                    </td>
                    <td className="right">
                      {item.unitValue != null
                        ? (item.unitValue * item.quantity).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
                        : '—'}
                    </td>
                    <td className="right muted">{(item.weightKg * item.quantity).toFixed(3)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="rfcd-totals-row">
              <div className="rfcd-total-item">
                <div className="rfcd-label">Peso Total (kg)</div>
                <div className="rfcd-value">{totalWeight.toFixed(3)}</div>
              </div>
              <div className="rfcd-total-item">
                <div className="rfcd-label">Valor da Mercadoria</div>
                <div className="rfcd-value">
                  {totalValue > 0
                    ? totalValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
                    : '—'}
                </div>
              </div>
              <div className="rfcd-total-item">
                <div className="rfcd-label">Frete (R$)</div>
                <div className="rfcd-value">
                  {dispatch.freightValueBrl.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Pagamento */}
        <div className="rfcd-payment-box">
          <div className="rfcd-payment-title">Condições de Pagamento</div>
          <div className="rfcd-row">
            <div className="rfcd-field">
              <div className="rfcd-label">Valor Total a Pagar (Frete)</div>
              <div className="rfcd-value" style={{ fontSize: 14 }}>
                {dispatch.freightValueBrl.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
              </div>
            </div>
            <div className="rfcd-field">
              <div className="rfcd-label">Vencimento</div>
              <div className="rfcd-value">{dueDate.toLocaleDateString('pt-BR')}</div>
            </div>
            <div className="rfcd-field">
              <div className="rfcd-label">Referência</div>
              <div className="rfcd-value-mono">{dispatch.trackingCode}</div>
            </div>
            <div className="rfcd-field">
              <div className="rfcd-label">Formas de Pagamento</div>
              <div className="rfcd-value-normal">
                {hasPayment ? 'Boleto Bancário / PIX' : 'A combinar'}
              </div>
            </div>
          </div>
          {dispatch.paymentBoletoUrl && (
            <div style={{ marginTop: 6 }}>
              <div className="rfcd-label">Boleto disponível em:</div>
              <div style={{ fontFamily: 'monospace', fontSize: 9, wordBreak: 'break-all', color: '#1565c0', marginTop: 1 }}>
                {dispatch.paymentBoletoUrl}
              </div>
            </div>
          )}
          {dispatch.paymentPixUrl && (
            <div style={{ marginTop: 4 }}>
              <div className="rfcd-label">Chave / Link PIX:</div>
              <div style={{ fontFamily: 'monospace', fontSize: 9, wordBreak: 'break-all', color: '#2e7d32', marginTop: 1 }}>
                {dispatch.paymentPixUrl}
              </div>
            </div>
          )}
        </div>

        {/* Documento Fiscal */}
        {dispatch.fiscalAccessKey && (
          <div className="rfcd-section" style={{ marginTop: 6, borderTop: '1px solid #111' }}>
            <div className="rfcd-section-title">Documento Fiscal Eletrônico (NF-e)</div>
            <div className="rfcd-section-body">
              <div className="rfcd-label">Chave de Acesso</div>
              <div className="rfcd-nfe-key">{dispatch.fiscalAccessKey}</div>
            </div>
          </div>
        )}

        {/* Assinaturas */}
        <div className="rfcd-sig-row">
          <div className="rfcd-sig-box">Assinatura e carimbo do emitente</div>
          <div className="rfcd-sig-box">Assinatura e carimbo do recebedor</div>
          <div className="rfcd-sig-box">Data e hora do recebimento</div>
          <div className="rfcd-sig-box">Documento do recebedor (CPF/RG)</div>
        </div>

        <div className="rfcd-footer">
          Documento para Cliente — Emitido em {fmtDateTime(emissionDate)} · Rastreio: {dispatch.trackingCode}
          {dispatch.conferencedAt ? ` · Conferido em ${fmt(dispatch.conferencedAt)}` : ''}
          {' · '}Este documento é de caráter informativo e não substitui a Nota Fiscal Eletrônica.
        </div>
      </div>
    </>,
    document.body
  );
}
