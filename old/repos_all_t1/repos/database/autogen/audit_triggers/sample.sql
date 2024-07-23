-- Table: public.test

-- DROP TABLE IF EXISTS public.test;

CREATE TABLE IF NOT EXISTS public.test
(
    id bigint NOT NULL DEFAULT nextval('test_id_seq'::regclass),
    data text COLLATE pg_catalog."default" NOT NULL,
    value numeric(18,6) NOT NULL,
    version bigint NOT NULL DEFAULT 0,
    db_time timestamp with time zone DEFAULT now(),
    CONSTRAINT id_version PRIMARY KEY (id, version)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.test
    OWNER to postgres;

-- Trigger: no_deletes

-- DROP TRIGGER IF EXISTS no_deletes ON public.test;

CREATE TRIGGER no_deletes
    BEFORE DELETE
    ON public.test
    FOR EACH ROW
    EXECUTE FUNCTION public.no_deletes();

-- Trigger: trigger_update

-- DROP TRIGGER IF EXISTS trigger_update ON public.test;

CREATE TRIGGER trigger_update
    BEFORE UPDATE 
    ON public.test
    FOR EACH ROW
    EXECUTE FUNCTION public.version_force();
