-- Colunas das views (o parser dos scripts não deduz tipos de um SELECT).
-- Uso: mysql -u root -p -N --raw -D labcontrol_v2 < tools/gerador-csharp/colunas_views.sql > tools/gerador-csharp/views.json
SET SESSION group_concat_max_len = 1000000;
SELECT JSON_PRETTY(JSON_ARRAYAGG(JSON_OBJECT(
         'view', TABLE_NAME, 'col', COLUMN_NAME, 'pos', ORDINAL_POSITION, 'type', COLUMN_TYPE,
         'nullable', IS_NULLABLE = 'YES')))
  FROM (SELECT * FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME IN (SELECT TABLE_NAME FROM information_schema.VIEWS WHERE TABLE_SCHEMA = DATABASE())
         ORDER BY TABLE_NAME, ORDINAL_POSITION) C;
