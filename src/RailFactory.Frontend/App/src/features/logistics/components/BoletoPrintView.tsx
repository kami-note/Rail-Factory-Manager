import React, { useEffect, useMemo } from 'react';
import { createPortal } from 'react-dom';
import type { Dispatch, ShipmentOrder, TenantFiscalProfile } from '../types';
import { Masks } from '../../../shared/lib/utils/masks';

type Props = {
  dispatch: Dispatch;
  order: ShipmentOrder;
  fiscalProfile: TenantFiscalProfile | undefined;
  onClose: () => void;
};

/**
 * Generates a simple numeric barcode string from the dispatch tracking code.
 * Uses a hash-like approach to produce a 44-digit numeric sequence
 * that visually represents a Brazilian boleto barcode line.
 *
 * @remarks
 * This is a visual representation for internal use only.
 * Real boletos require integration with a payment gateway (Asaas, PagSeguro, etc.).
 * When paymentBoletoUrl is present, the user should be redirected there instead.
 */
function generateBoletoLine(trackingCode: string, amount: number, dueDate: string): string {
  // Produce a deterministic 44-char numeric string from inputs
  let hash = 0;
  const seed = `${trackingCode}${amount}${dueDate}`;
  for (let i = 0; i < seed.length; i++) {
    hash = ((hash << 5) - hash + seed.charCodeAt(i)) | 0;
  }
  const abs = Math.abs(hash);
  // Bank code: 341 (Itaú as placeholder), currency: 9 (BRL)
  const bankCurrency = '3419';
  // Amount: 10 digits (cents, zero-padded)
  const cents = Math.round(amount * 100).toString().padStart(10, '0');
  // Due factor: days since 07/10/1997
  const base = new Date('1997-10-07').getTime();
  const due = new Date(dueDate).getTime();
  const factor = Math.max(0, Math.round((due - base) / 86400000)).toString().padStart(4, '0');
  // Free field: generated from hash (25 digits)
  const free = (abs.toString() + '0000000000000000000000000').slice(0, 25);
  return `${bankCurrency}${factor}${cents}${free}`;
}

function formatBarcode(raw: string): string {
  // Split into groups of 10 for display
  return raw.replace(/(\d{10})/g, '$1 ').trim();
}

/**
 * Visual barcode renderer using CSS bars.
 * Alternates thick/thin bars based on binary representation of digits.
 */
function BarcodeVisual({ value }: { value: string }) {
  const bars = useMemo(() => {
    const chars = value.replace(/\D/g, '');
    const result: Array<{ width: number; color: string }> = [];
    for (let i = 0; i < chars.length; i++) {
      const digit = parseInt(chars[i], 10);
      // Alternate black/white bars with varying widths
      const isBlack = i % 2 === 0;
      const width = (digit % 3) + 1; // 1, 2, or 3 units
      result.push({ width: width * 1.5, color: isBlack ? '#111' : '#fff' });
    }
    // Always start with a narrow black guard bar
    result.unshift({ width: 1.5, color: '#111' });
    result.push({ width: 1.5, color: '#111' });
    return result;
  }, [value]);

  return (
    <div style={{
      display: 'flex',
      height: 50,
      alignItems: 'stretch',
      backgroundColor: '#fff',
      padding: '0 4px',
      border: '1px solid #eee',
    }}>
      {bars.map((bar, i) => (
        <div
          key={i}
          style={{
            width: `${bar.width}px`,
            backgroundColor: bar.color,
            flexShrink: 0,
          }}
        />
      ))}
    </div>
  );
}

const CSS = `
@media print {
  body > * { display: none !important; }
  body > .rfbl-root { display: block !important; }
  @page { size: A4 portrait; margin: 15mm 12mm; }
}
@media screen {
  .rfbl-root { display: none; }
}
.rfbl-root {
  font-family: Arial, Helvetica, sans-serif;
  font-size: 11px;
  color: #111;
  line-height: 1.4;
  max-width: 190mm;
}
.rfbl-bank-header {
  display: flex;
  align-items: center;
  border-bottom: 3px solid #111;
  padding-bottom: 4px;
  margin-bottom: 8px;
}
.rfbl-bank-name {
  font-size: 20px;
  font-weight: bold;
  letter-spacing: 2px;
  flex: 1;
}
.rfbl-bank-code {
  font-size: 20px;
  font-weight: bold;
  border-left: 3px solid #111;
  border-right: 3px solid #111;
  padding: 0 16px;
  margin: 0 12px;
}
.rfbl-linha-digitavel {
  font-family: 'Courier New', Courier, monospace;
  font-size: 13px;
  font-weight: bold;
  flex: 2;
  text-align: right;
}
.rfbl-section {
  border: 1px solid #888;
  margin-bottom: 0;
}
.rfbl-section + .rfbl-section { border-top: none; }
.rfbl-row {
  display: flex;
  border-bottom: 0.5px solid #ccc;
}
.rfbl-row:last-child { border-bottom: none; }
.rfbl-cell {
  flex: 1;
  padding: 3px 6px;
  border-right: 0.5px solid #ccc;
}
.rfbl-cell:last-child { border-right: none; }
.rfbl-cell-label {
  font-size: 8px;
  color: #555;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  margin-bottom: 1px;
}
.rfbl-cell-value {
  font-size: 11px;
  font-weight: bold;
}
.rfbl-cell-value-mono {
  font-family: 'Courier New', Courier, monospace;
  font-size: 12px;
  font-weight: bold;
}
.rfbl-cell-value-large {
  font-size: 14px;
  font-weight: bold;
}
.rfbl-cut { border-top: 1px dashed #888; margin: 12px 0; }
.rfbl-recibo-title {
  font-size: 9px;
  text-align: right;
  color: #555;
  margin-bottom: 4px;
  font-style: italic;
}
.rfbl-instructions {
  font-size: 9px;
  color: #444;
  margin-top: 8px;
  padding: 6px 8px;
  border: 0.5px solid #ccc;
  background: #fafafa;
}
.rfbl-footer {
  margin-top: 8px;
  font-size: 8px;
  color: #888;
  text-align: center;
}
.rfbl-disclaimer {
  background: #fff3e0;
  border: 1px solid #ff6f00;
  padding: 6px 10px;
  font-size: 9px;
  color: #e65100;
  margin-bottom: 10px;
  text-align: center;
  font-weight: bold;
}
`;

/**
 * BoletoPrintView
 *
 * Generates a print-ready boleto layout for freight payment.
 * If the dispatch has a real paymentBoletoUrl (from gateway),
 * it displays a notice to use the official URL instead.
 *
 * @remarks
 * The barcode and digitável line are generated locally for visual representation.
 * For production, always use the real boleto from the payment gateway.
 */
export function BoletoPrintView({ dispatch, order, fiscalProfile, onClose }: Props) {
  useEffect(() => {
    const handler = () => onClose();
    window.addEventListener('afterprint', handler);
    window.print();
    return () => window.removeEventListener('afterprint', handler);
  }, [onClose]);

  const fmt = (iso: string) => new Date(iso).toLocaleDateString('pt-BR');

  // Due date: 30 days from creation
  const dueDate = new Date(dispatch.createdAt);
  dueDate.setDate(dueDate.getDate() + 30);
  const dueDateStr = dueDate.toISOString();

  const boletoLine = generateBoletoLine(dispatch.trackingCode, dispatch.freightValueBrl, dueDateStr);
  const formattedLine = formatBarcode(boletoLine);

  const emitterName = fiscalProfile?.emitterName ?? 'EMITENTE';
  const emitterCnpj = fiscalProfile?.emitterCnpj ? Masks.cpfCnpj(fiscalProfile.emitterCnpj) : '—';
  const emitterCity = [fiscalProfile?.emitterCity, fiscalProfile?.emitterState].filter(Boolean).join('/');

  const hasGatewayBoleto = !!dispatch.paymentBoletoUrl;

  return createPortal(
    <>
      <style>{CSS}</style>
      <div className="rfbl-root">

        {hasGatewayBoleto && (
          <div className="rfbl-disclaimer">
            ⚠ ATENÇÃO: Este boleto é uma representação visual. O boleto OFICIAL emitido pelo sistema de pagamento
            está disponível em: <span style={{ fontFamily: 'monospace', textDecoration: 'underline' }}>{dispatch.paymentBoletoUrl}</span>
          </div>
        )}

        {/* Recibo do sacado (superior — topo da folha) */}
        <div className="rfbl-recibo-title">Recibo do Sacado</div>

        {/* Cabeçalho do banco */}
        <div className="rfbl-bank-header">
          <div className="rfbl-bank-name">{emitterName}</div>
          <div className="rfbl-bank-code">341-7</div>
          <div className="rfbl-linha-digitavel">{formattedLine.slice(0, 30)}</div>
        </div>

        {/* Informações principais */}
        <div className="rfbl-section">
          <div className="rfbl-row">
            <div className="rfbl-cell" style={{ flex: 3 }}>
              <div className="rfbl-cell-label">Beneficiário (Cedente)</div>
              <div className="rfbl-cell-value">{emitterName}</div>
              <div style={{ fontSize: 9, color: '#555' }}>CNPJ: {emitterCnpj} · {emitterCity}</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Agência / Código do beneficiário</div>
              <div className="rfbl-cell-value-mono">0001 / {dispatch.trackingCode.replace('RF-', '')}</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Vencimento</div>
              <div className="rfbl-cell-value-large">{dueDate.toLocaleDateString('pt-BR')}</div>
            </div>
          </div>

          <div className="rfbl-row">
            <div className="rfbl-cell" style={{ flex: 3 }}>
              <div className="rfbl-cell-label">Pagador (Sacado)</div>
              <div className="rfbl-cell-value">{order.recipientName ?? '—'}</div>
              <div style={{ fontSize: 9, color: '#555' }}>
                {order.recipientCnpj ? `CNPJ/CPF: ${Masks.cpfCnpj(order.recipientCnpj)}` : ''}
                {order.recipientStreet ? ` · ${order.recipientStreet}, ${order.recipientNumber ?? ''} · ${order.recipientCity ?? ''}/${order.recipientState ?? ''}` : ''}
              </div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Nosso número</div>
              <div className="rfbl-cell-value-mono">{dispatch.trackingCode}</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Valor do documento</div>
              <div className="rfbl-cell-value-large">
                {dispatch.freightValueBrl.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
              </div>
            </div>
          </div>

          <div className="rfbl-row">
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Documento de origem</div>
              <div className="rfbl-cell-value-mono">{order.orderNumber}</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Data de emissão</div>
              <div className="rfbl-cell-value-mono">{fmt(dispatch.createdAt)}</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Espécie</div>
              <div className="rfbl-cell-value">DM</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Aceite</div>
              <div className="rfbl-cell-value">N</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Qtd. itens</div>
              <div className="rfbl-cell-value">{order.items.length}</div>
            </div>
          </div>
        </div>

        {/* Instruções */}
        <div className="rfbl-instructions">
          <strong>Instruções ao banco:</strong> Não receber após o vencimento. Cobrar multa de 2% após o vencimento.
          Juros de mora de 1% ao mês. Referente ao frete de transporte — Despacho {dispatch.trackingCode}.
          {dispatch.shippingTrackingCode ? ` Código de rastreio: ${dispatch.shippingTrackingCode}.` : ''}
        </div>

        {/* Linha digitável completa */}
        <div style={{ fontFamily: 'monospace', fontSize: 11, fontWeight: 'bold', textAlign: 'center', margin: '8px 0 4px' }}>
          {formattedLine}
        </div>

        {/* Código de barras visual */}
        <BarcodeVisual value={boletoLine} />

        {/* Linha de corte */}
        <div className="rfbl-cut" />

        {/* Ficha de compensação (inferior) */}
        <div className="rfbl-recibo-title" style={{ marginBottom: 4 }}>Ficha de Compensação</div>

        <div className="rfbl-bank-header">
          <div className="rfbl-bank-name">{emitterName}</div>
          <div className="rfbl-bank-code">341-7</div>
          <div className="rfbl-linha-digitavel">{formattedLine.slice(0, 30)}</div>
        </div>

        <div className="rfbl-section">
          <div className="rfbl-row">
            <div className="rfbl-cell" style={{ flex: 3 }}>
              <div className="rfbl-cell-label">Beneficiário</div>
              <div className="rfbl-cell-value">{emitterName} · CNPJ: {emitterCnpj}</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Vencimento</div>
              <div className="rfbl-cell-value-large">{dueDate.toLocaleDateString('pt-BR')}</div>
            </div>
          </div>
          <div className="rfbl-row">
            <div className="rfbl-cell" style={{ flex: 3 }}>
              <div className="rfbl-cell-label">Pagador</div>
              <div className="rfbl-cell-value">{order.recipientName ?? '—'}</div>
              <div style={{ fontSize: 9, color: '#555' }}>
                {order.recipientCnpj ? `CNPJ/CPF: ${Masks.cpfCnpj(order.recipientCnpj)}` : ''}
              </div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Nosso número</div>
              <div className="rfbl-cell-value-mono">{dispatch.trackingCode}</div>
            </div>
            <div className="rfbl-cell">
              <div className="rfbl-cell-label">Valor</div>
              <div className="rfbl-cell-value-large">
                {dispatch.freightValueBrl.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
              </div>
            </div>
          </div>
        </div>

        {/* Código de barras (ficha de compensação) */}
        <div style={{ fontFamily: 'monospace', fontSize: 11, fontWeight: 'bold', textAlign: 'center', margin: '6px 0 4px' }}>
          {formattedLine}
        </div>
        <BarcodeVisual value={boletoLine} />

        <div className="rfbl-footer">
          Boleto de Frete — Despacho {dispatch.trackingCode} — Emitido em {fmt(dispatch.createdAt)}
          {' · '}Este documento é uma representação visual. Para pagamento online, acesse o sistema.
        </div>
      </div>
    </>,
    document.body
  );
}
