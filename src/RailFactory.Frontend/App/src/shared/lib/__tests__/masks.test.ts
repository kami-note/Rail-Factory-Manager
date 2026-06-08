import { describe, expect, it } from 'vitest';
import { Masks, Validators } from '../utils/masks';

describe('Masks & Validators', () => {
  describe('Masks', () => {
    it('formats CPF correctly', () => {
      expect(Masks.cpf('12345678909')).toBe('123.456.789-09');
      expect(Masks.cpf('1234')).toBe('123.4');
    });

    it('formats CNPJ correctly', () => {
      expect(Masks.cnpj('12345678000190')).toBe('12.345.678/0001-90');
    });

    it('formats CEP correctly', () => {
      expect(Masks.cep('01311200')).toBe('01311-200');
    });

    it('formats Phone correctly', () => {
      expect(Masks.phone('11999999999')).toBe('(11) 99999-9999');
      expect(Masks.phone('1133334444')).toBe('(11) 3333-4444');
    });

    it('formats Plate correctly', () => {
      expect(Masks.plate('abc1234')).toBe('ABC-1234');
      expect(Masks.plate('ABC1A23')).toBe('ABC-1A23');
    });
  });

  describe('Validators', () => {
    it('validates Email', () => {
      expect(Validators.email('test@example.com')).toBe(true);
      expect(Validators.email('invalid-email')).toBe(false);
    });

    it('validates CPF', () => {
      // Valid CPF
      expect(Validators.cpf('28564032040')).toBe(true);
      // Invalid CPF digits
      expect(Validators.cpf('28564032043')).toBe(false);
      expect(Validators.cpf('11111111111')).toBe(false);
    });

    it('validates CNPJ', () => {
      // Valid CNPJ (Google Brasil)
      expect(Validators.cnpj('00623904000173')).toBe(true);
      expect(Validators.cnpj('00623904000174')).toBe(false);
    });

    it('validates CEP', () => {
      expect(Validators.cep('01311-200')).toBe(true);
      expect(Validators.cep('123')).toBe(false);
    });

    it('validates Plate', () => {
      expect(Validators.plate('ABC-1234')).toBe(true);
      expect(Validators.plate('ABC1A23')).toBe(true);
      expect(Validators.plate('AB1234')).toBe(false);
    });
  });
});
