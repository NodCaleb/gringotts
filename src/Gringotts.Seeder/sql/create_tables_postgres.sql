-- SQL script to create database tables for Gringotts entities (PostgreSQL)
-- Creates employee, customer and transaction tables with appropriate constraints

-- Enable uuid generation (uuid-ossp) if not already installed. Choose one based on your environment.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- employee table
CREATE TABLE IF NOT EXISTS public.employee (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 username VARCHAR(50) NOT NULL,
 access_code INTEGER NOT NULL
);

-- customer table
CREATE TABLE IF NOT EXISTS public.customer (
 id BIGSERIAL PRIMARY KEY,
 username VARCHAR(50) NOT NULL,
 personal_name VARCHAR(100) NOT NULL,
 character_name VARCHAR(100) NOT NULL,
 balance NUMERIC(18,2) NOT NULL DEFAULT 0
);

-- transaction table
CREATE TABLE IF NOT EXISTS public.transaction (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 date TIMESTAMP WITHOUT TIME ZONE NOT NULL,
 sender_id BIGINT NULL,
 recipient_id BIGINT NOT NULL,
 employee_id UUID NULL,
 amount NUMERIC(18,2) NOT NULL,
 description VARCHAR(500) NOT NULL DEFAULT ''
);

-- Foreign keys
ALTER TABLE public.transaction
 ADD CONSTRAINT fk_transaction_sender FOREIGN KEY (sender_id) REFERENCES public.customer(id) ON DELETE SET NULL;

ALTER TABLE public.transaction
 ADD CONSTRAINT fk_transaction_recipient FOREIGN KEY (recipient_id) REFERENCES public.customer(id) ON DELETE RESTRICT;

ALTER TABLE public.transaction
 ADD CONSTRAINT fk_transaction_employee FOREIGN KEY (employee_id) REFERENCES public.employee(id) ON DELETE SET NULL;

-- Optional indexes
CREATE INDEX IF NOT EXISTS idx_customer_username ON public.customer (username);
CREATE INDEX IF NOT EXISTS idx_employee_username ON public.employee (username);
CREATE INDEX IF NOT EXISTS idx_transaction_date ON public.transaction (date);

-- End of script
