--BEGIN TRANSACTION;
CREATE SCHEMA IF NOT EXISTS audits;
DROP TABLE audits.table_name_audit;
--DROP TABLE IF EMPTY audits.table_name_audit;
create table audits.table_name_audit as (SELECT * FROM public.table_name);

ALTER TABLE audits.table_name_audit ADD COLUMN version bigint;
ALTER TABLE audits.table_name_audit ADD TG_OP text;
ALTER TABLE audits.table_name_audit ADD db_timestamp timestamp with time zone;
ALTER TABLE audits.table_name_audit ALTER COLUMN db_timestamp SET DEFAULT now();
ALTER TABLE audits.table_name_audit ADD COLUMN sequence bigserial;
--CREATE SEQUENCE audits.table_name_audit_seq;
--ALTER TABLE audits.table_name_audit ALTER COLUMN sequence SET DEFAULT nextval('audits.table_name_audit_seq');
--COMMIT;
-- FUNCTION: public.table_name.version_force()

-- DROP FUNCTION IF EXISTS public.table_name.version_force();

CREATE OR REPLACE FUNCTION public.table_name_version_force()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
available_id bigint;
BEGIN
--IF (NEW.version != 0) AND (NEW.version IS NOT NULL) THEN
--RAISE EXCEPTION 'AUDIT EXCEPTION. VERSION SHOULD ALWAYS BE ZERO';
--END IF;
--NEW.db_time:=now();
--NEW.version:=0;

IF (NEW.id = NULL) THEN
	available_id:=OLD.id;
ELSE
	available_id:=NEW.id;
END IF;
IF available_id IS NULL THEN
RAISE EXCEPTION 'ID CAN NOT BE NULL.';
END IF;
UPDATE audits.table_name_audit SET version=version-1 WHERE id=available_id;
IF (TG_OP='UPDATE') THEN
INSERT INTO audits.table_name_audit values (NEW.*, 0,TG_OP,now(),DEFAULT);
ELSIF (TG_OP='INSERT') THEN
INSERT INTO audits.table_name_audit values (NEW.*, 0,TG_OP,now(),DEFAULT);
ELSIF (TG_OP='DELETE') THEN
INSERT INTO audits.table_name_audit values (OLD.*, 0,TG_OP,now(),DEFAULT);
ELSIF (TG_OP='TRUNCATE') THEN
INSERT INTO audits.table_name_audit values (OLD.*, 0,TG_OP,now(),DEFAULT);
END IF;
RETURN NEW;
END;
$BODY$;

CREATE OR REPLACE FUNCTION public.no_deletes()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
IF (TG_OP='DELETE') THEN
RAISE EXCEPTION 'YOU MAY NOT DELETE ANY ENTRY IN AN AUDIT TABLE';
END IF;
END;
$BODY$;
ALTER FUNCTION public.table_name_version_force()
    OWNER TO postgres;
ALTER FUNCTION public.no_deletes()
    OWNER TO postgres;

DROP TRIGGER  table_name_audit_trigger ON table_name;
CREATE TRIGGER table_name_audit_trigger BEFORE INSERT OR UPDATE OR DELETE ON table_name
FOR EACH ROW EXECUTE FUNCTION
public.table_name_version_force();
