# Dicionario de Dados - IAM (v2)

## Tabela: users

Formato resumido: `users(Id, Email, DisplayName, PictureUrl, ExternalId, Status, CreatedAtUtc, UpdatedAtUtc, ExpelledAtUtc)`


| Nome da Coluna | Tipo de Dados | Tamanho  | Restricoes       | Valor Padrao      | Descricao                              |
| -------------- | ------------- | -------- | ---------------- | ----------------- | -------------------------------------- |
| Id             | UUID          | 16 bytes | PK, NOT NULL     | gen_random_uuid() | Identificador unico do usuario         |
| Email          | VARCHAR(512)  | ate 512  | NOT NULL         | N/D               | Email de login                         |
| DisplayName    | VARCHAR(512)  | ate 512  | NULL             | N/D               | Nome de exibicao                       |
| PictureUrl     | VARCHAR(2048) | ate 2048 | NULL             | N/D               | URL de avatar                          |
| ExternalId     | VARCHAR(256)  | ate 256  | UNIQUE, NOT NULL | N/D               | Identificador externo (Google subject) |
| Status         | VARCHAR(32)   | ate 32   | NOT NULL         | active            | Estado do usuario                      |
| CreatedAtUtc   | TIMESTAMPTZ   | 8 bytes  | NOT NULL         | now()             | Data/hora de criacao                   |
| UpdatedAtUtc   | TIMESTAMPTZ   | 8 bytes  | NULL             | N/D               | Data/hora de atualizacao               |
| ExpelledAtUtc  | TIMESTAMPTZ   | 8 bytes  | NULL             | N/D               | Data/hora de expulsao                  |


## Tabela: roles

Formato resumido: `roles(Id, Name, Description)`


| Nome da Coluna | Tipo de Dados | Tamanho  | Restricoes       | Valor Padrao      | Descricao             |
| -------------- | ------------- | -------- | ---------------- | ----------------- | --------------------- |
| Id             | UUID          | 16 bytes | PK, NOT NULL     | gen_random_uuid() | Identificador da role |
| Name           | VARCHAR(256)  | ate 256  | UNIQUE, NOT NULL | N/D               | Nome da role          |
| Description    | VARCHAR(1024) | ate 1024 | NULL             | N/D               | Descricao da role     |


## Tabela: permissions

Formato resumido: `permissions(Id, Code, Description)`


| Nome da Coluna | Tipo de Dados | Tamanho  | Restricoes       | Valor Padrao      | Descricao                     |
| -------------- | ------------- | -------- | ---------------- | ----------------- | ----------------------------- |
| Id             | UUID          | 16 bytes | PK, NOT NULL     | gen_random_uuid() | Identificador da permissao    |
| Code           | VARCHAR(256)  | ate 256  | UNIQUE, NOT NULL | N/D               | Codigo no padrao dominio.acao |
| Description    | VARCHAR(1024) | ate 1024 | NULL             | N/D               | Descricao da permissao        |


## Tabela: user_roles

Formato resumido: `user_roles(Id, UserId, RoleId, TenantId)`


| Nome da Coluna | Tipo de Dados | Tamanho  | Restricoes   | Valor Padrao      | Descricao                 |
| -------------- | ------------- | -------- | ------------ | ----------------- | ------------------------- |
| Id             | UUID          | 16 bytes | PK, NOT NULL | gen_random_uuid() | Identificador do vinculo  |
| UserId         | UUID          | 16 bytes | FK, NOT NULL | N/D               | Usuario vinculado         |
| RoleId         | UUID          | 16 bytes | FK, NOT NULL | N/D               | Role vinculada            |
| TenantId       | UUID          | 16 bytes | NULL         | N/D               | Escopo de role por tenant |


## Tabela: role_permissions

Formato resumido: `role_permissions(RoleId, PermissionId)`


| Nome da Coluna | Tipo de Dados | Tamanho  | Restricoes      | Valor Padrao | Descricao           |
| -------------- | ------------- | -------- | --------------- | ------------ | ------------------- |
| RoleId         | UUID          | 16 bytes | PK/FK, NOT NULL | N/D          | Role vinculada      |
| PermissionId   | UUID          | 16 bytes | PK/FK, NOT NULL | N/D          | Permissao vinculada |


## Tabela: outbox_messages

Formato resumido: `outbox_messages(Id, MessageType, Payload, CreatedAtUtc, Processed, ProcessedAtUtc)`


| Nome da Coluna | Tipo de Dados | Tamanho  | Restricoes   | Valor Padrao      | Descricao                  |
| -------------- | ------------- | -------- | ------------ | ----------------- | -------------------------- |
| Id             | UUID          | 16 bytes | PK, NOT NULL | gen_random_uuid() | Identificador da mensagem  |
| MessageType    | VARCHAR(256)  | ate 256  | NOT NULL     | N/D               | Tipo da mensagem           |
| Payload        | TEXT          | variavel | NOT NULL     | N/D               | Conteudo serializado       |
| CreatedAtUtc   | TIMESTAMPTZ   | 8 bytes  | NOT NULL     | now()             | Data/hora de criacao       |
| Processed      | BOOLEAN       | 1 byte   | NOT NULL     | false             | Indicador de processamento |
| ProcessedAtUtc | TIMESTAMPTZ   | 8 bytes  | NULL         | N/D               | Data/hora de processamento |


