# Dicionario de Dados - Production (v2)

## Tabela: production_orders

Formato resumido: `production_orders(id, tenant_id, number, product_id, bom_version_id, work_center_id, status, planned_qty, started_at_utc, finished_at_utc)`

| Nome da Coluna | Tipo de Dados | Tamanho | Restricoes | Valor Padrao | Descricao |
|---|---|---|---|---|---|
| id | UUID | 16 bytes | PK, NOT NULL | gen_random_uuid() | Identificador da OP |
| tenant_id | UUID | 16 bytes | NOT NULL | N/D | Tenant dono do registro |
| number | VARCHAR(64) | ate 64 | UNIQUE (por tenant), NOT NULL | N/D | Numero da ordem |
| product_id | UUID | 16 bytes | FK, NOT NULL | N/D | Produto da OP |
| bom_version_id | UUID | 16 bytes | FK, NOT NULL | N/D | BOM version aplicada |
| work_center_id | UUID | 16 bytes | FK, NOT NULL | N/D | Centro de trabalho |
| status | VARCHAR(32) | ate 32 | NOT NULL | Draft | Estado da OP |
| planned_qty | DECIMAL(18,4) | 18,4 | NOT NULL | 0 | Quantidade planejada |
| started_at_utc | TIMESTAMPTZ | 8 bytes | NULL | N/D | Inicio da execucao |
| finished_at_utc | TIMESTAMPTZ | 8 bytes | NULL | N/D | Fim da execucao |

## Tabela: material_reservations

Formato resumido: `material_reservations(id, tenant_id, production_order_id, lot_id, material_id, qty, status)`

| Nome da Coluna | Tipo de Dados | Tamanho | Restricoes | Valor Padrao | Descricao |
|---|---|---|---|---|---|
| id | UUID | 16 bytes | PK, NOT NULL | gen_random_uuid() | Identificador da reserva |
| tenant_id | UUID | 16 bytes | NOT NULL | N/D | Tenant dono do registro |
| production_order_id | UUID | 16 bytes | FK, NOT NULL | N/D | OP da reserva |
| lot_id | UUID | 16 bytes | FK, NULL | N/D | Lote reservado (quando definido) |
| material_id | UUID | 16 bytes | FK, NOT NULL | N/D | Material reservado |
| qty | DECIMAL(18,4) | 18,4 | NOT NULL | 0 | Quantidade reservada |
| status | VARCHAR(32) | ate 32 | NOT NULL | Reserved | Estado da reserva |

## Tabela: material_consumption

Formato resumido: `material_consumption(id, tenant_id, production_order_id, lot_id, material_id, qty, at_utc)`

| Nome da Coluna | Tipo de Dados | Tamanho | Restricoes | Valor Padrao | Descricao |
|---|---|---|---|---|---|
| id | UUID | 16 bytes | PK, NOT NULL | gen_random_uuid() | Identificador do consumo |
| tenant_id | UUID | 16 bytes | NOT NULL | N/D | Tenant dono do registro |
| production_order_id | UUID | 16 bytes | FK, NOT NULL | N/D | OP de consumo |
| lot_id | UUID | 16 bytes | FK, NOT NULL | N/D | Lote consumido |
| material_id | UUID | 16 bytes | FK, NOT NULL | N/D | Material consumido |
| qty | DECIMAL(18,4) | 18,4 | NOT NULL | 0 | Quantidade consumida |
| at_utc | TIMESTAMPTZ | 8 bytes | NOT NULL | now() | Data/hora do consumo |

## Tabela: scrap

Formato resumido: `scrap(id, tenant_id, production_order_id, material_id, qty, reason, at_utc)`

| Nome da Coluna | Tipo de Dados | Tamanho | Restricoes | Valor Padrao | Descricao |
|---|---|---|---|---|---|
| id | UUID | 16 bytes | PK, NOT NULL | gen_random_uuid() | Identificador de refugo |
| tenant_id | UUID | 16 bytes | NOT NULL | N/D | Tenant dono do registro |
| production_order_id | UUID | 16 bytes | FK, NOT NULL | N/D | OP associada |
| material_id | UUID | 16 bytes | FK, NOT NULL | N/D | Material refugado |
| qty | DECIMAL(18,4) | 18,4 | NOT NULL | 0 | Quantidade refugada |
| reason | VARCHAR(256) | ate 256 | NOT NULL | N/D | Motivo do refugo |
| at_utc | TIMESTAMPTZ | 8 bytes | NOT NULL | now() | Data/hora do refugo |

## Tabela: inspection

Formato resumido: `inspection(id, tenant_id, production_order_id, step, result, at_utc, notes)`

| Nome da Coluna | Tipo de Dados | Tamanho | Restricoes | Valor Padrao | Descricao |
|---|---|---|---|---|---|
| id | UUID | 16 bytes | PK, NOT NULL | gen_random_uuid() | Identificador da inspecao |
| tenant_id | UUID | 16 bytes | NOT NULL | N/D | Tenant dono do registro |
| production_order_id | UUID | 16 bytes | FK, NOT NULL | N/D | OP inspecionada |
| step | VARCHAR(128) | ate 128 | NOT NULL | N/D | Etapa de controle |
| result | VARCHAR(16) | ate 16 | NOT NULL | N/D | approved ou rejected |
| at_utc | TIMESTAMPTZ | 8 bytes | NOT NULL | now() | Data/hora da inspecao |
| notes | VARCHAR(1024) | ate 1024 | NULL | N/D | Observacoes |
