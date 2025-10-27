-- SQL script to create database tables for Gringotts entities (PostgreSQL)
-- Creates Employee, Customer and Transaction tables with appropriate constraints

-- Enable uuid generation (uuid-ossp) if not already installed. Choose one based on your environment.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Employee table
CREATE TABLE IF NOT EXISTS public."Employee" (
 "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 "UserName" VARCHAR(50) NOT NULL,
 "AccessCode" INTEGER NOT NULL
);

-- Customer table
CREATE TABLE IF NOT EXISTS public."Customer" (
 "Id" BIGSERIAL PRIMARY KEY,
 "UserName" VARCHAR(50) NOT NULL,
 "PersonalName" VARCHAR(100) NOT NULL,
 "CharacterName" VARCHAR(100) NOT NULL,
 "Balance" NUMERIC(18,2) NOT NULL DEFAULT0
);

-- Transaction table
CREATE TABLE IF NOT EXISTS public."Transaction" (
 "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 "Date" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
 "SenderId" BIGINT NULL,
 "RecipientId" BIGINT NOT NULL,
 "EmployeeId" UUID NULL,
 "Amount" NUMERIC(18,2) NOT NULL,
 "Description" VARCHAR(500) NOT NULL DEFAULT ''
);

-- Foreign keys
ALTER TABLE public."Transaction"
 ADD CONSTRAINT fk_transaction_sender FOREIGN KEY ("SenderId") REFERENCES public."Customer"("Id") ON DELETE SET NULL;

ALTER TABLE public."Transaction"
 ADD CONSTRAINT fk_transaction_recipient FOREIGN KEY ("RecipientId") REFERENCES public."Customer"("Id") ON DELETE RESTRICT;

ALTER TABLE public."Transaction"
 ADD CONSTRAINT fk_transaction_employee FOREIGN KEY ("EmployeeId") REFERENCES public."Employee"("Id") ON DELETE SET NULL;

-- Optional indexes
CREATE INDEX IF NOT EXISTS idx_customer_username ON public."Customer" ("UserName");
CREATE INDEX IF NOT EXISTS idx_employee_username ON public."Employee" ("UserName");
CREATE INDEX IF NOT EXISTS idx_transaction_date ON public."Transaction" ("Date");

-- End of script
