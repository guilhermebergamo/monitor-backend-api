-- Script de criação da base de dados PostgreSQL (Neon)
-- Execute este script no console SQL do Neon

-- Criar tabela de registros
CREATE TABLE IF NOT EXISTS registros (
    id SERIAL PRIMARY KEY,
    observacao VARCHAR(500) NOT NULL,
    data_hora TIMESTAMP NOT NULL DEFAULT NOW(),
    quantidade INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Criar índice para otimizar consultas por data
CREATE INDEX IF NOT EXISTS idx_registros_data_hora ON registros(data_hora DESC);

-- Inserir dados de exemplo para testes
INSERT INTO registros (observacao, data_hora, quantidade) VALUES
    ('Sistema iniciado com sucesso', NOW(), 1),
    ('Processamento de dados concluído', NOW() - INTERVAL '30 minutes', 150),
    ('Backup automático realizado', NOW() - INTERVAL '1 hour', 1),
    ('Atualização de cache executada', NOW() - INTERVAL '2 hours', 2500),
    ('Sincronização com API externa', NOW() - INTERVAL '3 hours', 45),
    ('Limpeza de logs antigos', NOW() - INTERVAL '6 hours', 1200),
    ('Verificação de integridade dos dados', NOW() - INTERVAL '12 hours', 1),
    ('Geração de relatório diário', NOW() - INTERVAL '24 hours', 1);

-- Verificar dados inseridos
SELECT 
    id,
    observacao,
    data_hora,
    quantidade
FROM registros
ORDER BY data_hora DESC;

-- Query de contagem
SELECT COUNT(*) as total_registros FROM registros;
