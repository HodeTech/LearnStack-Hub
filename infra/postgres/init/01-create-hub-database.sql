-- Hub Postgres bootstrap.
--
-- Idempotently creates the `learnstack_hub` database within the shared
-- Postgres instance that LearnStack core's compose stack runs.
--
-- Postgres lacks native `CREATE DATABASE IF NOT EXISTS`, so we use a
-- PL/pgSQL DO block + dynamic SQL to test catalogue.

DO $$
DECLARE
    target_db text := :'hub_db';
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname = target_db) THEN
        EXECUTE format('CREATE DATABASE %I', target_db);
        RAISE NOTICE 'Hub database % created.', target_db;
    ELSE
        RAISE NOTICE 'Hub database % already exists.', target_db;
    END IF;
END$$;
