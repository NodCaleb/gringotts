-- SQL script to create database tables for Gringotts entities (PostgreSQL)
-- Creates employee, customer and transaction tables with appropriate constraints

-- Enable uuid generation (uuid-ossp) if not already installed. Choose one based on your environment.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- employee table
CREATE TABLE IF NOT EXISTS public.employees (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 username VARCHAR(50) NOT NULL,
 accesscode INTEGER NOT NULL
);

-- customer table
CREATE TABLE IF NOT EXISTS public.customers (
 id BIGINT PRIMARY KEY,
 username VARCHAR(50) NOT NULL,
 personalname VARCHAR(100) NOT NULL,
 charactername VARCHAR(100) NOT NULL,
 balance NUMERIC(18,2) NOT NULL DEFAULT 0
);

-- transaction table
CREATE TABLE IF NOT EXISTS public.transactions (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 date TIMESTAMP WITHOUT TIME ZONE NOT NULL,
 senderid BIGINT NULL,
 recipientid BIGINT NOT NULL,
 employeeid UUID NULL,
 amount NUMERIC(18,2) NOT NULL,
 description VARCHAR(500) NOT NULL DEFAULT ''
);

-- Foreign keys: add constraints only if they do not already exist
DO $$
BEGIN
 IF NOT EXISTS (
 SELECT 1 FROM pg_constraint WHERE conname = 'fk_transaction_sender'
 ) THEN
 ALTER TABLE public.transactions
 ADD CONSTRAINT fk_transaction_sender FOREIGN KEY (senderid) REFERENCES public.customers(id) ON DELETE SET NULL;
 END IF;
END
$$;

DO $$
BEGIN
 IF NOT EXISTS (
 SELECT 1 FROM pg_constraint WHERE conname = 'fk_transaction_recipient'
 ) THEN
 ALTER TABLE public.transactions
 ADD CONSTRAINT fk_transaction_recipient FOREIGN KEY (recipientid) REFERENCES public.customers(id) ON DELETE RESTRICT;
 END IF;
END
$$;

DO $$
BEGIN
 IF NOT EXISTS (
 SELECT 1 FROM pg_constraint WHERE conname = 'fk_transaction_employee'
 ) THEN
 ALTER TABLE public.transactions
 ADD CONSTRAINT fk_transaction_employee FOREIGN KEY (employeeid) REFERENCES public.employees(id) ON DELETE SET NULL;
 END IF;
END
$$;

-- Optional indexes
CREATE INDEX IF NOT EXISTS idx_customer_username ON public.customers (username);
CREATE INDEX IF NOT EXISTS idx_employee_username ON public.employees (username);
CREATE INDEX IF NOT EXISTS idx_transaction_date ON public.transactions (date);

-- End of script
