--
-- PostgreSQL database dump
--

-- Dumped from database version 17.2
-- Dumped by pg_dump version 17.2

-- Started on 2026-04-10 10:23:56

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 7 (class 2615 OID 16783)
-- Name: imported_dummy; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA imported_dummy;


--
-- TOC entry 6 (class 2615 OID 16782)
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


--
-- TOC entry 5740 (class 0 OID 0)
-- Dependencies: 6
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA public IS '';


--
-- TOC entry 2 (class 3079 OID 189605)
-- Name: pg_trgm; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;


--
-- TOC entry 5741 (class 0 OID 0)
-- Dependencies: 2
-- Name: EXTENSION pg_trgm; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION pg_trgm IS 'text similarity measurement and index searching based on trigrams';


--
-- TOC entry 398 (class 1255 OID 16784)
-- Name: accounts_balances_version_force(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.accounts_balances_version_force() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
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
UPDATE accounts_balances_audit SET version=version-1 WHERE id=available_id;
IF (TG_OP='UPDATE') THEN
INSERT INTO accounts_balances_audit values (NEW.*, 0,TG_OP,now(),DEFAULT);
ELSIF (TG_OP='INSERT') THEN
INSERT INTO accounts_balances_audit values (NEW.*, 0,TG_OP,now(),DEFAULT);
ELSIF (TG_OP='DELETE') THEN
INSERT INTO accounts_balances_audit values (OLD.*, 0,TG_OP,now(),DEFAULT);
ELSIF (TG_OP='TRUNCATE') THEN
INSERT INTO accounts_balances_audit values (OLD.*, 0,TG_OP,now(),DEFAULT);
END IF;
RETURN NEW;
END;
$$;


--
-- TOC entry 356 (class 1255 OID 99276)
-- Name: auto_increment_seq_no(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.auto_increment_seq_no() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Get the last sequence number for this itemcode
    SELECT COALESCE(MAX(seq_no), 0) + 1 INTO NEW.seq_no
    FROM cycle_count
    WHERE itemcode = NEW.itemcode;
    
    RETURN NEW;
END;
$$;


--
-- TOC entry 399 (class 1255 OID 16785)
-- Name: no_deletes(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.no_deletes() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
IF (TG_OP='DELETE') THEN
RAISE EXCEPTION 'YOU MAY NOT DELETE ANY ENTRY IN AN AUDIT TABLE';
END IF;
END;
$$;


--
-- TOC entry 219 (class 1259 OID 16786)
-- Name: sih; Type: TABLE; Schema: imported_dummy; Owner: -
--

CREATE TABLE imported_dummy.sih (
    itemcode bigint NOT NULL,
    "desc" text NOT NULL,
    sih real NOT NULL,
    cost real NOT NULL,
    sell real NOT NULL
);


--
-- TOC entry 220 (class 1259 OID 16791)
-- Name: sih_current; Type: TABLE; Schema: imported_dummy; Owner: -
--

CREATE TABLE imported_dummy.sih_current (
    itemcode integer NOT NULL,
    "desc" text,
    sih real,
    cost real,
    sell real
);


--
-- TOC entry 221 (class 1259 OID 16796)
-- Name: accounts_balances; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_balances (
    account_type integer NOT NULL,
    account_no bigint NOT NULL,
    amount double precision NOT NULL,
    time_tai timestamp with time zone DEFAULT now() NOT NULL,
    time_as_entered timestamp with time zone DEFAULT now() NOT NULL,
    done_with boolean DEFAULT false NOT NULL
);


--
-- TOC entry 5742 (class 0 OID 0)
-- Dependencies: 221
-- Name: TABLE accounts_balances; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.accounts_balances IS 'Positive is debit';


--
-- TOC entry 296 (class 1259 OID 74707)
-- Name: accounts_information; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_information (
    account_type integer NOT NULL,
    account_name text NOT NULL,
    account_pii bigint,
    account_i18n_label bigint,
    account_min double precision DEFAULT '-1000000000'::integer NOT NULL,
    account_max double precision DEFAULT 1000000000 NOT NULL,
    human_friendly_id text,
    allow_credit_on_pos boolean DEFAULT false NOT NULL,
    allow_debit_on_pos boolean DEFAULT false NOT NULL,
    is_bank boolean DEFAULT false NOT NULL,
    is_cash boolean DEFAULT false NOT NULL,
    is_reserve boolean DEFAULT false NOT NULL,
    is_reconcilable boolean DEFAULT false NOT NULL,
    is_inventory_tracked boolean DEFAULT false NOT NULL,
    is_default_cash_register boolean DEFAULT false NOT NULL,
    account_no bigint NOT NULL,
    loyalty_base_multiplicative_points_percentage double precision DEFAULT 0.0 NOT NULL,
    account_surcharges_multiplicative_percentage double precision DEFAULT 0 NOT NULL,
    account_surcharges_additive_fee double precision DEFAULT 0 NOT NULL,
    ifrs_category_id bigint DEFAULT 1,
    accounts_surcharges_transferred_to_during_sale_payment bigint DEFAULT 0 NOT NULL,
    accounts_usage_non_transparent_charge_percentage double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 297 (class 1259 OID 74734)
-- Name: accounts_information_2_account_no_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.accounts_information_2_account_no_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5743 (class 0 OID 0)
-- Dependencies: 297
-- Name: accounts_information_2_account_no_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.accounts_information_2_account_no_seq OWNED BY public.accounts_information.account_no;


--
-- TOC entry 222 (class 1259 OID 16808)
-- Name: accounts_journal_entries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_journal_entries (
    journal_no integer NOT NULL,
    ref_no text,
    amount double precision NOT NULL,
    debit_account_type integer NOT NULL,
    debit_account_no bigint NOT NULL,
    credit_account_type integer NOT NULL,
    credit_account_no bigint NOT NULL,
    description text,
    time_tai timestamp with time zone DEFAULT now() NOT NULL,
    time_as_entered timestamp with time zone NOT NULL,
    ref text,
    principal_id bigint NOT NULL,
    principal_name text NOT NULL,
    journal_univ_seq bigint NOT NULL,
    debit_account_name text DEFAULT ''::text NOT NULL,
    credit_account_name text DEFAULT ''::text NOT NULL,
    internal_reference text DEFAULT ''::text,
    extra_data text
);


--
-- TOC entry 286 (class 1259 OID 41865)
-- Name: accounts_journal_entries_journal_univ_seq_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.accounts_journal_entries ALTER COLUMN journal_univ_seq ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.accounts_journal_entries_journal_univ_seq_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 223 (class 1259 OID 16815)
-- Name: accounts_journal_information; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_journal_information (
    journal_id bigint NOT NULL,
    journal_name text NOT NULL,
    journal_i18n_label bigint
);


--
-- TOC entry 224 (class 1259 OID 16820)
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.accounts_journal_information_journal_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5744 (class 0 OID 0)
-- Dependencies: 224
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.accounts_journal_information_journal_id_seq OWNED BY public.accounts_journal_information.journal_id;


--
-- TOC entry 225 (class 1259 OID 16821)
-- Name: accounts_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_types (
    account_type integer NOT NULL,
    account_type_name text NOT NULL,
    account_type_i18n_label bigint
);


--
-- TOC entry 5745 (class 0 OID 0)
-- Dependencies: 225
-- Name: TABLE accounts_types; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.accounts_types IS 'Always these four _real_ accounts';


--
-- TOC entry 230 (class 1259 OID 16836)
-- Name: catalogue; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.catalogue (
    itemcode bigint NOT NULL,
    description text NOT NULL,
    active boolean DEFAULT true NOT NULL,
    created_on timestamp with time zone DEFAULT now() NOT NULL,
    description_pos text NOT NULL,
    description_web text NOT NULL,
    descriptions_other_languages bigint DEFAULT 0,
    default_vat_category bigint DEFAULT 0 NOT NULL,
    vat_depends_on_user boolean DEFAULT false NOT NULL,
    vat_category_adjustable boolean DEFAULT false NOT NULL,
    price_manual boolean DEFAULT false NOT NULL,
    enforce_above_cost boolean DEFAULT true NOT NULL,
    active_web boolean DEFAULT false NOT NULL,
    expiry_tracking_enabled boolean DEFAULT false NOT NULL,
    permissions_category bigint DEFAULT 0 NOT NULL,
    categories_bitmask bigint DEFAULT 1 NOT NULL,
    process_discounts boolean DEFAULT true NOT NULL,
    max_per_invoice double precision DEFAULT 1000000 NOT NULL,
    min_per_invoice double precision DEFAULT 0 NOT NULL,
    max_per_person double precision DEFAULT 1000000 NOT NULL,
    height_m double precision DEFAULT 0 NOT NULL,
    length_m double precision DEFAULT 0 NOT NULL,
    width_m double precision DEFAULT 0 NOT NULL,
    weight_per_unit_kg double precision DEFAULT 0 NOT NULL,
    allow_price_suggestions boolean DEFAULT true NOT NULL,
    remarks text DEFAULT ''::text NOT NULL,
    quota_per_quota_period double precision DEFAULT 0 NOT NULL,
    time_based_quota_enabled boolean DEFAULT false NOT NULL,
    quota_per_invoice double precision DEFAULT 0 NOT NULL,
    per_invoice_quota_enabled boolean DEFAULT false NOT NULL,
    discount_method_is_maximum boolean DEFAULT false NOT NULL,
    is_loss_leader boolean DEFAULT false NOT NULL,
    tags text DEFAULT ''::text NOT NULL,
    extra_structured text DEFAULT ''::text NOT NULL,
    ref_link text DEFAULT ''::text NOT NULL,
    ref_doc_id bigint
);


--
-- TOC entry 317 (class 1259 OID 132174)
-- Name: computed_tags; Type: MATERIALIZED VIEW; Schema: public; Owner: -
--

CREATE MATERIALIZED VIEW public.computed_tags AS
 SELECT t.itemcode,
    tag.tag
   FROM public.catalogue t,
    LATERAL unnest(string_to_array(TRIM(BOTH '|'::text FROM t.tags), '|'::text)) tag(tag)
  WITH NO DATA;


--
-- TOC entry 319 (class 1259 OID 132186)
-- Name: tags_implies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tags_implies (
    id bigint NOT NULL,
    tag text NOT NULL,
    implies text NOT NULL,
    recorded_at timestamp with time zone DEFAULT now() NOT NULL,
    description text DEFAULT ''::text NOT NULL,
    created_by bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 321 (class 1259 OID 132200)
-- Name: all_tags; Type: MATERIALIZED VIEW; Schema: public; Owner: -
--

CREATE MATERIALIZED VIEW public.all_tags AS
 SELECT tags_implies.tag
   FROM public.tags_implies
UNION ALL
 SELECT tags_implies.implies AS tag
   FROM public.tags_implies
UNION ALL
 SELECT computed_tags.tag
   FROM public.computed_tags
  WITH NO DATA;


--
-- TOC entry 354 (class 1259 OID 214148)
-- Name: allowed_keys; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.allowed_keys (
    id bigint NOT NULL,
    principal bigint NOT NULL,
    fingerprint_sha256 text NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    cert_contents text DEFAULT ''::text NOT NULL,
    name text DEFAULT ''::text NOT NULL,
    valid_until timestamp with time zone DEFAULT now() NOT NULL,
    terminal text DEFAULT '0'::text NOT NULL
);


--
-- TOC entry 353 (class 1259 OID 214147)
-- Name: allowed_keys_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.allowed_keys_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5746 (class 0 OID 0)
-- Dependencies: 353
-- Name: allowed_keys_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.allowed_keys_id_seq OWNED BY public.allowed_keys.id;


--
-- TOC entry 226 (class 1259 OID 16826)
-- Name: api_authorization; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.api_authorization (
    userid bigint NOT NULL,
    pubkey text,
    "authorization" text NOT NULL
);


--
-- TOC entry 227 (class 1259 OID 16831)
-- Name: authorized_terminals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.authorized_terminals (
    userid bigint NOT NULL,
    terminalid bigint NOT NULL
);


--
-- TOC entry 228 (class 1259 OID 16834)
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.authorized_terminals_terminalid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5747 (class 0 OID 0)
-- Dependencies: 228
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.authorized_terminals_terminalid_seq OWNED BY public.authorized_terminals.terminalid;


--
-- TOC entry 229 (class 1259 OID 16835)
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.authorized_terminals_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5748 (class 0 OID 0)
-- Dependencies: 229
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.authorized_terminals_userid_seq OWNED BY public.authorized_terminals.userid;


--
-- TOC entry 322 (class 1259 OID 140366)
-- Name: barcodes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.barcodes (
    code text NOT NULL,
    itemcode bigint,
    batchcode bigint,
    remarks text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    modified_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 323 (class 1259 OID 148558)
-- Name: barcodes_resolved; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.barcodes_resolved AS
 SELECT b.code AS barcode,
        CASE
            WHEN (c.itemcode IS NOT NULL) THEN ('itemcode:'::text || b.code)
            WHEN ((b.itemcode IS NOT NULL) AND (b.itemcode <> 0)) THEN ('itemcode:'::text || (b.itemcode)::text)
            WHEN ((b.batchcode IS NOT NULL) AND (b.batchcode <> 0)) THEN ('batchcode:'::text || (b.batchcode)::text)
            ELSE NULL::text
        END AS reference
   FROM (public.barcodes b
     LEFT JOIN public.catalogue c ON ((b.code = (c.itemcode)::text)));


--
-- TOC entry 276 (class 1259 OID 41699)
-- Name: bundled_pricing; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.bundled_pricing (
    bundle_id bigint NOT NULL,
    itemcode bigint NOT NULL,
    discount double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 275 (class 1259 OID 41698)
-- Name: bundled_pricing_bundle_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.bundled_pricing_bundle_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5749 (class 0 OID 0)
-- Dependencies: 275
-- Name: bundled_pricing_bundle_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.bundled_pricing_bundle_id_seq OWNED BY public.bundled_pricing.bundle_id;


--
-- TOC entry 298 (class 1259 OID 74749)
-- Name: inventory_batchcode_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.inventory_batchcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 236 (class 1259 OID 16872)
-- Name: inventory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory (
    itemcode bigint NOT NULL,
    batchcode bigint DEFAULT nextval('public.inventory_batchcode_seq'::regclass) NOT NULL,
    batch_enabled boolean DEFAULT true NOT NULL,
    mfg_date timestamp with time zone DEFAULT now(),
    exp_date timestamp with time zone,
    packed_size real DEFAULT 1 NOT NULL,
    units double precision DEFAULT 0 NOT NULL,
    measurement_unit text DEFAULT 'qty'::text NOT NULL,
    marked_price double precision NOT NULL,
    selling_price double precision NOT NULL,
    cost_price double precision NOT NULL,
    volume_discounts boolean DEFAULT false NOT NULL,
    suppliercode bigint DEFAULT 0 NOT NULL,
    user_discounts boolean DEFAULT false NOT NULL,
    last_counted_at timestamp with time zone DEFAULT now() NOT NULL,
    remarks text DEFAULT ''::text NOT NULL,
    min_price double precision DEFAULT 0.0 NOT NULL,
    multiplicative_discount_percentage double precision DEFAULT 0 NOT NULL,
    additive_discount_percentage double precision DEFAULT 0 NOT NULL,
    enforce_min_price boolean DEFAULT true NOT NULL,
    tags text DEFAULT ''::text NOT NULL,
    extra_structured text DEFAULT ''::text NOT NULL,
    ref_link text DEFAULT ''::text NOT NULL,
    ref_doc_id bigint
);


--
-- TOC entry 5750 (class 0 OID 0)
-- Dependencies: 236
-- Name: TABLE inventory; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.inventory IS 'Internal inventory management functions';


--
-- TOC entry 351 (class 1259 OID 189599)
-- Name: catalogue_inventory_view; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.catalogue_inventory_view AS
 SELECT c.itemcode,
    c.description,
    c.active,
    c.created_on,
    c.description_pos,
    c.description_web,
    c.descriptions_other_languages,
    c.default_vat_category,
    c.vat_depends_on_user,
    c.vat_category_adjustable,
    c.price_manual,
    c.enforce_above_cost,
    c.active_web,
    c.expiry_tracking_enabled,
    c.permissions_category,
    c.categories_bitmask,
    c.process_discounts,
    c.max_per_invoice,
    c.min_per_invoice,
    c.max_per_person,
    c.height_m,
    c.length_m,
    c.width_m,
    c.weight_per_unit_kg,
    c.allow_price_suggestions,
    c.remarks,
    c.quota_per_quota_period,
    c.time_based_quota_enabled,
    c.quota_per_invoice,
    c.per_invoice_quota_enabled,
    c.discount_method_is_maximum,
    c.is_loss_leader,
    c.tags,
    c.extra_structured,
    c.ref_link,
    c.ref_doc_id,
    COALESCE(inv_data.valid_stock_quantity, (0)::double precision) AS valid_stock_quantity,
    inv_data.lowest_available_price
   FROM (public.catalogue c
     LEFT JOIN LATERAL ( SELECT sum(i.units) AS valid_stock_quantity,
            min(LEAST(i.selling_price, i.marked_price)) AS lowest_available_price
           FROM public.inventory i
          WHERE ((i.itemcode = c.itemcode) AND (i.units > (0)::double precision) AND ((i.exp_date IS NULL) OR (i.exp_date > CURRENT_TIMESTAMP)))) inv_data ON (true));


--
-- TOC entry 231 (class 1259 OID 16853)
-- Name: catalogue_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.catalogue_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5751 (class 0 OID 0)
-- Dependencies: 231
-- Name: catalogue_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.catalogue_itemcode_seq OWNED BY public.catalogue.itemcode;


--
-- TOC entry 232 (class 1259 OID 16854)
-- Name: categories_bitmask; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.categories_bitmask (
    bitmask bigint NOT NULL,
    name text NOT NULL,
    i18n_label bigint
);


--
-- TOC entry 293 (class 1259 OID 50142)
-- Name: cheque_books; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cheque_books (
    id bigint NOT NULL,
    account_id bigint NOT NULL,
    start_number bigint NOT NULL,
    end_number bigint NOT NULL,
    next_number bigint NOT NULL,
    is_open boolean DEFAULT true NOT NULL,
    is_cancelled boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 292 (class 1259 OID 50141)
-- Name: cheque_books_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.cheque_books_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5752 (class 0 OID 0)
-- Dependencies: 292
-- Name: cheque_books_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.cheque_books_id_seq OWNED BY public.cheque_books.id;


--
-- TOC entry 326 (class 1259 OID 156804)
-- Name: cheque_leaves; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cheque_leaves (
    id bigint NOT NULL,
    cheque_book_id bigint NOT NULL,
    leaf_number bigint NOT NULL,
    status text DEFAULT 'unused'::text NOT NULL,
    payee_name text DEFAULT ''::text NOT NULL,
    amount double precision NOT NULL,
    issued_at timestamp with time zone DEFAULT now() NOT NULL,
    notes text,
    tx_id text,
    updated_at time with time zone DEFAULT now() NOT NULL,
    issued_by bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 325 (class 1259 OID 156803)
-- Name: cheque_leaves_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.cheque_leaves_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5753 (class 0 OID 0)
-- Dependencies: 325
-- Name: cheque_leaves_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.cheque_leaves_id_seq OWNED BY public.cheque_leaves.id;


--
-- TOC entry 285 (class 1259 OID 41817)
-- Name: codes_batches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.codes_batches (
    code text NOT NULL,
    itemcode bigint NOT NULL,
    batchcode bigint NOT NULL,
    created_at time with time zone DEFAULT now() NOT NULL,
    enabled boolean DEFAULT true NOT NULL
);


--
-- TOC entry 284 (class 1259 OID 41810)
-- Name: codes_catalogue; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.codes_catalogue (
    code text NOT NULL,
    itemcode bigint NOT NULL,
    created_at time with time zone DEFAULT now() NOT NULL,
    enabled boolean DEFAULT true NOT NULL
);


--
-- TOC entry 233 (class 1259 OID 16859)
-- Name: credentials; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.credentials (
    userid bigint NOT NULL,
    username text NOT NULL,
    valid_until timestamp with time zone NOT NULL,
    modified timestamp with time zone NOT NULL,
    pubkey text,
    password_pbkdf2 text NOT NULL,
    created_time timestamp with time zone DEFAULT now() NOT NULL,
    active boolean DEFAULT false NOT NULL
);


--
-- TOC entry 234 (class 1259 OID 16866)
-- Name: credentials_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.credentials_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5754 (class 0 OID 0)
-- Dependencies: 234
-- Name: credentials_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.credentials_userid_seq OWNED BY public.credentials.userid;


--
-- TOC entry 273 (class 1259 OID 41689)
-- Name: customer_discounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.customer_discounts (
    customer_id bigint NOT NULL,
    recommended_discount_percent double precision DEFAULT 0 NOT NULL,
    loyalty_rate double precision DEFAULT 0 NOT NULL,
    loyalty_paid_to_account_id double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 303 (class 1259 OID 99301)
-- Name: cycle_count; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cycle_count (
    id bigint NOT NULL,
    itemcode bigint NOT NULL,
    seq_no bigint NOT NULL,
    recorded_qty double precision NOT NULL,
    actual_qty double precision NOT NULL,
    count_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    principal_id bigint NOT NULL,
    principal_name character varying(100) NOT NULL,
    location character varying(100),
    notes text
);


--
-- TOC entry 301 (class 1259 OID 99299)
-- Name: cycle_count_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.cycle_count_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5755 (class 0 OID 0)
-- Dependencies: 301
-- Name: cycle_count_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.cycle_count_id_seq OWNED BY public.cycle_count.id;


--
-- TOC entry 302 (class 1259 OID 99300)
-- Name: cycle_count_seq_no_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.cycle_count_seq_no_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5756 (class 0 OID 0)
-- Dependencies: 302
-- Name: cycle_count_seq_no_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.cycle_count_seq_no_seq OWNED BY public.cycle_count.seq_no;


--
-- TOC entry 271 (class 1259 OID 41667)
-- Name: default_deny_fields; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.default_deny_fields (
    field text NOT NULL,
    created_at time with time zone DEFAULT now()
);


--
-- TOC entry 5757 (class 0 OID 0)
-- Dependencies: 271
-- Name: TABLE default_deny_fields; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.default_deny_fields IS 'Of the form [object].[field] or [field]';


--
-- TOC entry 235 (class 1259 OID 16867)
-- Name: descriptions_other_languages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.descriptions_other_languages (
    id bigint NOT NULL,
    language character varying(5) NOT NULL,
    description text NOT NULL,
    description_pos text NOT NULL,
    description_web text NOT NULL
);


--
-- TOC entry 257 (class 1259 OID 25189)
-- Name: i18n_labels; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.i18n_labels (
    id bigint NOT NULL,
    lang text NOT NULL,
    value text
);


--
-- TOC entry 256 (class 1259 OID 25182)
-- Name: idempotency; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.idempotency (
    key text NOT NULL,
    request text,
    time_tai time with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 346 (class 1259 OID 181449)
-- Name: ifrs_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ifrs_categories (
    id bigint NOT NULL,
    code text NOT NULL,
    name text NOT NULL,
    report_type text NOT NULL,
    is_current boolean DEFAULT false NOT NULL,
    valid_account_type integer NOT NULL,
    sort_order integer DEFAULT 0
);


--
-- TOC entry 345 (class 1259 OID 181448)
-- Name: ifrs_categories_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.ifrs_categories_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5758 (class 0 OID 0)
-- Dependencies: 345
-- Name: ifrs_categories_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.ifrs_categories_id_seq OWNED BY public.ifrs_categories.id;


--
-- TOC entry 300 (class 1259 OID 82863)
-- Name: inventory_adjustments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_adjustments (
    entry_id bigint NOT NULL,
    adjustment_batch bigint NOT NULL,
    itemcode bigint NOT NULL,
    batchcode bigint NOT NULL,
    count bigint NOT NULL,
    per_item_value double precision NOT NULL,
    net_value double precision NOT NULL,
    posted boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    edited_at time with time zone DEFAULT now() NOT NULL,
    reference_code text NOT NULL,
    processed_by bigint NOT NULL,
    created_by bigint NOT NULL,
    edited_by bigint NOT NULL,
    reason text DEFAULT ''::text NOT NULL,
    before_qty double precision NOT NULL,
    after_qty double precision NOT NULL
);


--
-- TOC entry 299 (class 1259 OID 82862)
-- Name: inventory_adjustments_entry_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.inventory_adjustments_entry_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5759 (class 0 OID 0)
-- Dependencies: 299
-- Name: inventory_adjustments_entry_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.inventory_adjustments_entry_id_seq OWNED BY public.inventory_adjustments.entry_id;


--
-- TOC entry 279 (class 1259 OID 41717)
-- Name: inventory_images; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_images (
    itemcode bigint NOT NULL,
    imageid bigint DEFAULT 0 NOT NULL,
    image_base64 text NOT NULL
);


--
-- TOC entry 237 (class 1259 OID 16885)
-- Name: inventory_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.inventory_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5760 (class 0 OID 0)
-- Dependencies: 237
-- Name: inventory_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.inventory_itemcode_seq OWNED BY public.inventory.itemcode;


--
-- TOC entry 287 (class 1259 OID 50074)
-- Name: inventory_movements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_movements (
    itemcode bigint DEFAULT nextval('public.inventory_itemcode_seq'::regclass) NOT NULL,
    batchcode bigint NOT NULL,
    batch_enabled boolean DEFAULT true NOT NULL,
    mfg_date timestamp with time zone DEFAULT now(),
    exp_date timestamp with time zone,
    packed_size real DEFAULT 1 NOT NULL,
    units double precision DEFAULT 0 NOT NULL,
    measurement_unit text DEFAULT 'qty'::text NOT NULL,
    marked_price double precision NOT NULL,
    selling_price double precision NOT NULL,
    cost_price double precision NOT NULL,
    volume_discounts boolean DEFAULT false NOT NULL,
    suppliercode bigint DEFAULT 0 NOT NULL,
    user_discounts boolean DEFAULT false NOT NULL,
    last_counted_at timestamp with time zone DEFAULT now() NOT NULL,
    remarks text DEFAULT ''::text NOT NULL,
    reference text NOT NULL,
    entered_time timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    from_units double precision DEFAULT 0 NOT NULL,
    to_units double precision DEFAULT 0 NOT NULL,
    is_one_off boolean DEFAULT false NOT NULL,
    action_type text GENERATED ALWAYS AS (split_part(reference, ':'::text, 1)) STORED
);


--
-- TOC entry 263 (class 1259 OID 25225)
-- Name: issued_invoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.issued_invoices (
    invoice_id bigint NOT NULL,
    invoice_time timestamp with time zone DEFAULT now() NOT NULL,
    customer bigint,
    issued_value double precision NOT NULL,
    is_settled boolean NOT NULL,
    paid_value double precision NOT NULL,
    invoice_human_friendly text,
    invoice_time_posted time with time zone DEFAULT now() NOT NULL,
    is_posted boolean DEFAULT false NOT NULL,
    sub_total double precision NOT NULL,
    discount_total double precision NOT NULL,
    effective_discount_percentage double precision NOT NULL,
    tax_total double precision NOT NULL,
    grand_total double precision NOT NULL,
    sales_person_id bigint NOT NULL,
    currency_code text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    extra_data text DEFAULT ''::text NOT NULL,
    ref_doc_id bigint
);


--
-- TOC entry 5761 (class 0 OID 0)
-- Dependencies: 263
-- Name: TABLE issued_invoices; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.issued_invoices IS 'Issued invoices only';


--
-- TOC entry 262 (class 1259 OID 25224)
-- Name: issued_invoices_invoice_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.issued_invoices_invoice_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5762 (class 0 OID 0)
-- Dependencies: 262
-- Name: issued_invoices_invoice_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.issued_invoices_invoice_id_seq OWNED BY public.issued_invoices.invoice_id;


--
-- TOC entry 320 (class 1259 OID 132195)
-- Name: tags_transitive_closure; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.tags_transitive_closure AS
 WITH RECURSIVE closure_tree AS (
         SELECT t.tag,
            t.implies,
            (((('|'::text || t.tag) || '|'::text) || t.implies) || '|'::text) AS path
           FROM public.tags_implies t
        UNION ALL
         SELECT ct.tag,
            t.implies,
            ((ct.path || t.implies) || '|'::text)
           FROM (closure_tree ct
             JOIN public.tags_implies t ON ((ct.implies = t.tag)))
          WHERE (ct.path !~~ (((('%'::text || '|'::text) || t.implies) || '|'::text) || '%'::text))
        )
 SELECT tag,
    implies AS implication,
    replace(TRIM(BOTH '|'::text FROM path), '|'::text, ' -> '::text) AS rule_chain
   FROM closure_tree;


--
-- TOC entry 324 (class 1259 OID 156776)
-- Name: item_tag_implications; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.item_tag_implications AS
 SELECT ct.itemcode,
    ct.tag AS source_tag,
    ct.tag AS transitive_tag,
    'Direct'::text AS rule_chain
   FROM public.computed_tags ct
UNION ALL
 SELECT ct.itemcode,
    ct.tag AS source_tag,
    ttc.implication AS transitive_tag,
    ttc.rule_chain
   FROM (public.computed_tags ct
     JOIN public.tags_transitive_closure ttc ON ((ct.tag = ttc.tag)));


--
-- TOC entry 304 (class 1259 OID 99315)
-- Name: latest_cycle_count; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.latest_cycle_count AS
 SELECT cc1.id,
    cc1.itemcode,
    cc1.seq_no,
    cc1.recorded_qty,
    cc1.actual_qty,
    cc1.count_date,
    cc1.principal_id,
    cc1.principal_name,
    cc1.location,
    cc1.notes
   FROM (public.cycle_count cc1
     JOIN ( SELECT cycle_count.itemcode,
            max(cycle_count.seq_no) AS max_seq_no
           FROM public.cycle_count
          GROUP BY cycle_count.itemcode) cc2 ON (((cc1.itemcode = cc2.itemcode) AND (cc1.seq_no = cc2.max_seq_no))));


--
-- TOC entry 265 (class 1259 OID 25237)
-- Name: loyalty_points; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.loyalty_points (
    points_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    valid_from timestamp with time zone DEFAULT now() NOT NULL,
    valid_until timestamp with time zone NOT NULL,
    cust_id bigint NOT NULL,
    amount double precision NOT NULL,
    source_type text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 264 (class 1259 OID 25236)
-- Name: loyality_points_points_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.loyality_points_points_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5763 (class 0 OID 0)
-- Dependencies: 264
-- Name: loyality_points_points_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.loyality_points_points_id_seq OWNED BY public.loyalty_points.points_id;


--
-- TOC entry 267 (class 1259 OID 25245)
-- Name: loyalty_points_redemption; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.loyalty_points_redemption (
    redemption_id bigint NOT NULL,
    cust_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    amount double precision NOT NULL,
    time_issued time with time zone DEFAULT now() NOT NULL,
    loyality_points_id bigint NOT NULL,
    redeemed_for text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 266 (class 1259 OID 25244)
-- Name: loyalty_points_redemption_redemption_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.loyalty_points_redemption_redemption_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5764 (class 0 OID 0)
-- Dependencies: 266
-- Name: loyalty_points_redemption_redemption_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.loyalty_points_redemption_redemption_id_seq OWNED BY public.loyalty_points_redemption.redemption_id;


--
-- TOC entry 314 (class 1259 OID 123943)
-- Name: mapped_location_item_placed_in; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mapped_location_item_placed_in (
    id bigint NOT NULL,
    mapped_location_id bigint NOT NULL,
    itemcode bigint NOT NULL
);


--
-- TOC entry 313 (class 1259 OID 123942)
-- Name: mapped_location_item_placed_in_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.mapped_location_item_placed_in_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5765 (class 0 OID 0)
-- Dependencies: 313
-- Name: mapped_location_item_placed_in_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.mapped_location_item_placed_in_id_seq OWNED BY public.mapped_location_item_placed_in.id;


--
-- TOC entry 312 (class 1259 OID 123934)
-- Name: mapped_locations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mapped_locations (
    id bigint NOT NULL,
    map_id bigint NOT NULL,
    name text NOT NULL,
    horizontal_section bigint NOT NULL,
    vertical_section bigint NOT NULL
);


--
-- TOC entry 311 (class 1259 OID 123933)
-- Name: mapped_locations_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.mapped_locations_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5766 (class 0 OID 0)
-- Dependencies: 311
-- Name: mapped_locations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.mapped_locations_id_seq OWNED BY public.mapped_locations.id;


--
-- TOC entry 255 (class 1259 OID 17013)
-- Name: notification_servicer_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notification_servicer_types (
    notification_servicer_type_id bigint NOT NULL,
    notification_servicer_name text
);


--
-- TOC entry 254 (class 1259 OID 17012)
-- Name: notification_servicer_types_notification_servicer_type_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notification_servicer_types_notification_servicer_type_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5767 (class 0 OID 0)
-- Dependencies: 254
-- Name: notification_servicer_types_notification_servicer_type_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notification_servicer_types_notification_servicer_type_id_seq OWNED BY public.notification_servicer_types.notification_servicer_type_id;


--
-- TOC entry 253 (class 1259 OID 17003)
-- Name: notification_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notification_types (
    notification_type_id bigint NOT NULL,
    notification_type_name text NOT NULL,
    notification_servicer_type integer NOT NULL,
    notification_service text NOT NULL,
    notification_service_other_args text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 252 (class 1259 OID 17002)
-- Name: notification_types_notification_type_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notification_types_notification_type_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5768 (class 0 OID 0)
-- Dependencies: 252
-- Name: notification_types_notification_type_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notification_types_notification_type_id_seq OWNED BY public.notification_types.notification_type_id;


--
-- TOC entry 251 (class 1259 OID 16991)
-- Name: notifications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notifications (
    notif_id bigint NOT NULL,
    notif_type text DEFAULT 'INTERNAL'::text NOT NULL,
    notif_other_status text NOT NULL,
    notif_is_done boolean DEFAULT false NOT NULL,
    notif_target text NOT NULL,
    time_tai timestamp with time zone NOT NULL,
    time_expires_tai timestamp with time zone,
    notif_contents text DEFAULT ''::text NOT NULL,
    notif_priority integer,
    notif_from text DEFAULT 'InvoicerBackend'::text NOT NULL,
    notif_source text DEFAULT 'InvoicerBackend'::text NOT NULL
);


--
-- TOC entry 250 (class 1259 OID 16990)
-- Name: notifications_notif_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notifications_notif_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5769 (class 0 OID 0)
-- Dependencies: 250
-- Name: notifications_notif_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notifications_notif_id_seq OWNED BY public.notifications.notif_id;


--
-- TOC entry 291 (class 1259 OID 50124)
-- Name: payments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.payments (
    id bigint NOT NULL,
    scheduled_payment_id bigint,
    company_id bigint NOT NULL,
    vendor_id bigint,
    invoice_id bigint,
    payment_reference text NOT NULL,
    description text,
    currency text NOT NULL,
    amount double precision NOT NULL,
    exchange_rate double precision DEFAULT 1.0 NOT NULL,
    debit_account_id bigint NOT NULL,
    credit_account_id bigint NOT NULL,
    bank_account_id bigint NOT NULL,
    beneficiary_name text,
    beneficiary_bank_name text,
    beneficiary_branch text,
    beneficiary_account_no text,
    beneficiary_routing_no text,
    payment_method text NOT NULL,
    payment_date date NOT NULL,
    external_payment_id text,
    fee_amount double precision,
    net_amount double precision,
    is_reconciled boolean DEFAULT false NOT NULL,
    is_excluded boolean DEFAULT false NOT NULL,
    reconciliation_date date,
    reconciliation_ref text,
    created_by bigint NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by bigint,
    updated_at timestamp with time zone,
    version_number bigint DEFAULT 0 NOT NULL,
    auto_apply boolean DEFAULT true NOT NULL
);


--
-- TOC entry 290 (class 1259 OID 50123)
-- Name: payments_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.payments_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5770 (class 0 OID 0)
-- Dependencies: 290
-- Name: payments_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.payments_id_seq OWNED BY public.payments.id;


--
-- TOC entry 258 (class 1259 OID 25196)
-- Name: permissions_extended_api_call; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_extended_api_call (
    user_id bigint NOT NULL,
    api_call text NOT NULL,
    allowed_attributes text
);


--
-- TOC entry 238 (class 1259 OID 16886)
-- Name: permissions_list; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list (
    "Permission" text NOT NULL
);


--
-- TOC entry 5771 (class 0 OID 0)
-- Dependencies: 238
-- Name: TABLE permissions_list; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.permissions_list IS 'Comma-separated, no spaces';


--
-- TOC entry 239 (class 1259 OID 16891)
-- Name: permissions_list_categories_names; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list_categories_names (
    category bigint NOT NULL,
    category_name text,
    label_i18n bigint
);


--
-- TOC entry 240 (class 1259 OID 16896)
-- Name: permissions_list_users_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list_users_categories (
    userid bigint NOT NULL,
    categories bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 310 (class 1259 OID 123924)
-- Name: physical_maps; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.physical_maps (
    map_id bigint NOT NULL,
    map_name text NOT NULL,
    map_type text DEFAULT 'BMP'::text NOT NULL,
    map text NOT NULL,
    vertical_gridlines bigint NOT NULL,
    horizontal_gridlines bigint NOT NULL
);


--
-- TOC entry 309 (class 1259 OID 123923)
-- Name: physical_maps_map_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.physical_maps_map_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5772 (class 0 OID 0)
-- Dependencies: 309
-- Name: physical_maps_map_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.physical_maps_map_id_seq OWNED BY public.physical_maps.map_id;


--
-- TOC entry 278 (class 1259 OID 41707)
-- Name: pii; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pii (
    id bigint NOT NULL,
    name text NOT NULL,
    is_company boolean DEFAULT false NOT NULL,
    email text,
    telephone text,
    mobile text,
    title text,
    address text,
    fax text,
    "IM" text,
    "SIP" text,
    gender text DEFAULT 'unspecified'::text,
    discount_rate_additive_percentage double precision DEFAULT 0 NOT NULL,
    discount_rate_multiplicative_percentage double precision DEFAULT 1 NOT NULL,
    loyalty_points_rate_multiplicative_percentage double precision DEFAULT 1 NOT NULL,
    loyalty_points_rate_additive_percentage double precision DEFAULT 0 NOT NULL,
    extra_data text
);


--
-- TOC entry 277 (class 1259 OID 41706)
-- Name: pii_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pii_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5773 (class 0 OID 0)
-- Dependencies: 277
-- Name: pii_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pii_id_seq OWNED BY public.pii.id;


--
-- TOC entry 280 (class 1259 OID 41725)
-- Name: pii_images; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pii_images (
    pii_id bigint NOT NULL,
    image_no bigint DEFAULT 0 NOT NULL,
    image text
);


--
-- TOC entry 5774 (class 0 OID 0)
-- Dependencies: 280
-- Name: TABLE pii_images; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.pii_images IS 'Photos of people, companies - any person, including non-natural persons.';


--
-- TOC entry 283 (class 1259 OID 41787)
-- Name: purchases; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.purchases (
    received_invoice_id bigint NOT NULL,
    itemcode bigint NOT NULL,
    pack_size bigint DEFAULT 0 NOT NULL,
    pack_quantity bigint DEFAULT 0 NOT NULL,
    received_as_unit_quantity double precision DEFAULT 0 NOT NULL,
    free_packs bigint DEFAULT 0 NOT NULL,
    free_units double precision DEFAULT 0 NOT NULL,
    expiry_date time with time zone,
    manufacturing_date time with time zone,
    manufacturer_batch_id text DEFAULT ''::text,
    product_name text DEFAULT ''::text NOT NULL,
    added_date time with time zone DEFAULT now() NOT NULL,
    discount_percentage double precision DEFAULT 0 NOT NULL,
    discount_absolute double precision DEFAULT 0 NOT NULL,
    cost_per_unit double precision DEFAULT 0 NOT NULL,
    cost_per_pack double precision DEFAULT 0 NOT NULL,
    gross_cost_per_unit double precision DEFAULT 0 NOT NULL,
    selling_price double precision DEFAULT 0 NOT NULL,
    "VAT_percentage" double precision DEFAULT 0 NOT NULL,
    "VAT_category" bigint DEFAULT 0 NOT NULL,
    "VAT_absolute" double precision DEFAULT 0 NOT NULL,
    "VAT_category_name" text DEFAULT ''::text NOT NULL,
    total_units double precision DEFAULT 0 NOT NULL,
    net_total_price double precision DEFAULT 0 NOT NULL,
    total_amount_due double precision DEFAULT 0 NOT NULL,
    gross_total double precision DEFAULT 0 NOT NULL,
    net_total_cost double precision DEFAULT 0 NOT NULL,
    gross_markup_percentage double precision DEFAULT 0 NOT NULL,
    gross_markup_absolute double precision DEFAULT 0 NOT NULL,
    is_vat_a_disallowed_input_tax boolean DEFAULT false NOT NULL,
    net_cost_per_unit double precision DEFAULT 0 NOT NULL,
    line_number bigint DEFAULT 0 NOT NULL,
    is_one_off boolean DEFAULT false NOT NULL,
    creates_new_batch boolean DEFAULT false NOT NULL
);


--
-- TOC entry 306 (class 1259 OID 107516)
-- Name: quota_usage_per_user_itemcode; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.quota_usage_per_user_itemcode (
    quota_id bigint NOT NULL,
    itemcode bigint NOT NULL,
    pii bigint NOT NULL,
    valid_from time with time zone DEFAULT now() NOT NULL,
    valid_until timestamp with time zone NOT NULL,
    quantity double precision NOT NULL
);


--
-- TOC entry 305 (class 1259 OID 107515)
-- Name: quota_usage_per_user_itemcode_quota_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.quota_usage_per_user_itemcode_quota_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5775 (class 0 OID 0)
-- Dependencies: 305
-- Name: quota_usage_per_user_itemcode_quota_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.quota_usage_per_user_itemcode_quota_id_seq OWNED BY public.quota_usage_per_user_itemcode.quota_id;


--
-- TOC entry 269 (class 1259 OID 25253)
-- Name: receipts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.receipts (
    receipt_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    account_id bigint NOT NULL,
    amount double precision NOT NULL,
    time_received time with time zone DEFAULT now() NOT NULL,
    extra_data text
);


--
-- TOC entry 268 (class 1259 OID 25252)
-- Name: receipts_receipt_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.receipts_receipt_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5776 (class 0 OID 0)
-- Dependencies: 268
-- Name: receipts_receipt_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.receipts_receipt_id_seq OWNED BY public.receipts.receipt_id;


--
-- TOC entry 282 (class 1259 OID 41770)
-- Name: received_invoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.received_invoices (
    received_invoice_no bigint NOT NULL,
    is_posted boolean DEFAULT false NOT NULL,
    supplier_id bigint NOT NULL,
    supplier_name text DEFAULT ''::text NOT NULL,
    remarks text DEFAULT ''::text NOT NULL,
    reference text NOT NULL,
    gross_total double precision DEFAULT 0 NOT NULL,
    transport_charges double precision DEFAULT 0 NOT NULL,
    effective_discount_absolute_total double precision DEFAULT 0 NOT NULL,
    "default_VAT_percentage" double precision DEFAULT 0 NOT NULL,
    "default_VAT_category" bigint DEFAULT 0 NOT NULL,
    effective_discount_percentage_total double precision DEFAULT 0 NOT NULL,
    is_settled boolean DEFAULT false NOT NULL,
    "default_VAT_category_name" text DEFAULT ''::text NOT NULL,
    whole_invoice_discount_absolute double precision DEFAULT 0 NOT NULL,
    whole_invoice_discount_percentage double precision DEFAULT 0 NOT NULL,
    effective_discount_percentage_from_entered_items double precision DEFAULT 0 NOT NULL,
    effective_discount_absolute_from_entered_items double precision DEFAULT 0 NOT NULL,
    vat_total double precision DEFAULT 0 NOT NULL,
    effective_vat_percentage double precision DEFAULT 0 NOT NULL,
    posted_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    last_saved_at timestamp with time zone DEFAULT now() NOT NULL,
    total_amount_due double precision DEFAULT 0 NOT NULL,
    invoice_time timestamp with time zone DEFAULT now() NOT NULL,
    extra_data text,
    ref_doc_id bigint
);


--
-- TOC entry 281 (class 1259 OID 41769)
-- Name: received_invoices_received_invoice_no_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.received_invoices_received_invoice_no_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5777 (class 0 OID 0)
-- Dependencies: 281
-- Name: received_invoices_received_invoice_no_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.received_invoices_received_invoice_no_seq OWNED BY public.received_invoices.received_invoice_no;


--
-- TOC entry 308 (class 1259 OID 115736)
-- Name: ref_docs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ref_docs (
    ref_id bigint NOT NULL,
    ref_text text DEFAULT ''::text NOT NULL,
    ref_image text DEFAULT ''::text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    authored_by bigint NOT NULL,
    ref_extra_data text DEFAULT ''::text NOT NULL,
    ref_url text DEFAULT ''::text NOT NULL,
    is_inventory_image boolean DEFAULT false NOT NULL
);


--
-- TOC entry 307 (class 1259 OID 115735)
-- Name: ref_docs_ref_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.ref_docs_ref_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5778 (class 0 OID 0)
-- Dependencies: 307
-- Name: ref_docs_ref_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.ref_docs_ref_id_seq OWNED BY public.ref_docs.ref_id;


--
-- TOC entry 316 (class 1259 OID 123954)
-- Name: ref_docs_transcriptions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ref_docs_transcriptions (
    id bigint NOT NULL,
    ref_doc bigint NOT NULL,
    transcribed_content text NOT NULL,
    transcriber_llm_name text NOT NULL,
    transcribed_at timestamp with time zone NOT NULL,
    transcription_structured text DEFAULT ''::text NOT NULL,
    transcription_structure_type text DEFAULT ''::text NOT NULL,
    ref_doc_issued_at timestamp with time zone,
    ref_doc_valid_from timestamp with time zone,
    ref_doc_not_valid_after timestamp with time zone,
    ref_doc_summary text DEFAULT ''::text NOT NULL,
    ref_doc_title text DEFAULT ''::text NOT NULL,
    transcription_cost_usdc double precision,
    input_audio_tokens bigint,
    input_text_tokens bigint,
    input_image_tokens bigint,
    input_video_tokens bigint,
    output_audio_tokens bigint,
    output_text_tokens bigint,
    output_image_tokens bigint,
    output_video_tokens bigint,
    request_output_as_is text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 315 (class 1259 OID 123953)
-- Name: ref_docs_transcriptions_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.ref_docs_transcriptions_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5779 (class 0 OID 0)
-- Dependencies: 315
-- Name: ref_docs_transcriptions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.ref_docs_transcriptions_id_seq OWNED BY public.ref_docs_transcriptions.id;


--
-- TOC entry 241 (class 1259 OID 16900)
-- Name: requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.requests (
    time_tai time with time zone DEFAULT now() NOT NULL,
    principal bigint NOT NULL,
    token text NOT NULL,
    request_body text NOT NULL,
    type text,
    requested_action text,
    requested_privilege_level text,
    endpoint text,
    provided_privilege_levels text,
    datetime_tai timestamp with time zone DEFAULT now() NOT NULL,
    req_reference bigint DEFAULT ((((EXTRACT(epoch FROM clock_timestamp()))::bigint << 20) | ((1)::bigint << 10)) | ((random() * (1024)::double precision))::bigint) NOT NULL
);


--
-- TOC entry 259 (class 1259 OID 25204)
-- Name: requests_bad; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.requests_bad (
    time_tai timestamp with time zone DEFAULT now() NOT NULL,
    principal bigint,
    token text NOT NULL,
    request_body text,
    type text,
    requested_action text,
    requested_privilege_level text,
    endpoint text,
    provided_privilege_levels text,
    req_reference bigint DEFAULT ((((EXTRACT(epoch FROM clock_timestamp()))::bigint << 20) | ((1)::bigint << 10)) | ((random() * (1024)::double precision))::bigint) NOT NULL
);


--
-- TOC entry 261 (class 1259 OID 25213)
-- Name: sales; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sales (
    sale_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    entered_at timestamp with time zone DEFAULT now() NOT NULL,
    itemcode bigint NOT NULL,
    batchcode bigint NOT NULL,
    quantity double precision NOT NULL,
    selling_price double precision NOT NULL,
    vat_category bigint NOT NULL,
    vat_rate_percentage double precision NOT NULL,
    discount_rate double precision NOT NULL,
    discount double precision NOT NULL,
    vat_as_charged double precision NOT NULL,
    total_effective_selling_price double precision NOT NULL,
    remarks text DEFAULT ''::text NOT NULL,
    client_recorded_time_opening timestamp with time zone DEFAULT now(),
    client_recorded_time_closing timestamp with time zone DEFAULT now(),
    sales_human_friendly text,
    loyality_points_percentage double precision DEFAULT 0 NOT NULL,
    loyality_points_issued double precision DEFAULT 0 NOT NULL,
    product_name text DEFAULT ''::text NOT NULL,
    is_one_off boolean DEFAULT false NOT NULL
);


--
-- TOC entry 5780 (class 0 OID 0)
-- Dependencies: 261
-- Name: TABLE sales; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.sales IS 'Sales data go here';


--
-- TOC entry 260 (class 1259 OID 25212)
-- Name: sales_sale_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.sales_sale_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5781 (class 0 OID 0)
-- Dependencies: 260
-- Name: sales_sale_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.sales_sale_id_seq OWNED BY public.sales.sale_id;


--
-- TOC entry 289 (class 1259 OID 50102)
-- Name: scheduled_payments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.scheduled_payments (
    id bigint NOT NULL,
    company_id bigint NOT NULL,
    vendor_id bigint,
    invoice_id bigint,
    batch_id bigint,
    payment_reference text NOT NULL,
    description text,
    currency text NOT NULL,
    amount double precision NOT NULL,
    exchange_rate double precision DEFAULT 1.0 NOT NULL,
    debit_account_id bigint NOT NULL,
    credit_account_id bigint NOT NULL,
    bank_account_id bigint NOT NULL,
    beneficiary_name text,
    beneficiary_bank_name text,
    beneficiary_branch text,
    beneficiary_account_no text,
    beneficiary_routing_no text,
    payment_method text NOT NULL,
    frequency text NOT NULL,
    interval_value integer,
    next_run_date date NOT NULL,
    last_run_date date,
    is_pending boolean DEFAULT true NOT NULL,
    is_processing boolean DEFAULT false NOT NULL,
    is_completed boolean DEFAULT false NOT NULL,
    is_failed boolean DEFAULT false NOT NULL,
    is_cancelled boolean DEFAULT false NOT NULL,
    external_payment_id text,
    fee_amount double precision,
    net_amount double precision,
    is_reconciled boolean DEFAULT false NOT NULL,
    is_excluded boolean DEFAULT false NOT NULL,
    reconciliation_date date,
    reconciliation_ref text,
    approved_by bigint,
    approved_at timestamp with time zone,
    created_by bigint NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by bigint,
    updated_at timestamp with time zone,
    version_number bigint DEFAULT 0,
    is_automatic_clear boolean DEFAULT true NOT NULL,
    debit_account_type bigint DEFAULT 3 NOT NULL,
    credit_account_type bigint DEFAULT 0 NOT NULL,
    journal_no integer NOT NULL
);


--
-- TOC entry 288 (class 1259 OID 50101)
-- Name: scheduled_payments_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.scheduled_payments_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5782 (class 0 OID 0)
-- Dependencies: 288
-- Name: scheduled_payments_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.scheduled_payments_id_seq OWNED BY public.scheduled_payments.id;


--
-- TOC entry 294 (class 1259 OID 58288)
-- Name: scheduled_receipts_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.scheduled_receipts_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 295 (class 1259 OID 58289)
-- Name: scheduled_receipts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.scheduled_receipts (
    id bigint DEFAULT nextval('public.scheduled_receipts_id_seq'::regclass) NOT NULL,
    company_id bigint NOT NULL,
    vendor_id bigint,
    invoice_id bigint,
    batch_id bigint,
    payment_reference text NOT NULL,
    description text,
    currency text NOT NULL,
    amount double precision NOT NULL,
    exchange_rate double precision DEFAULT 1.0 NOT NULL,
    debit_account_id bigint NOT NULL,
    credit_account_id bigint NOT NULL,
    bank_account_id bigint NOT NULL,
    beneficiary_name text,
    beneficiary_bank_name text,
    beneficiary_branch text,
    beneficiary_account_no text,
    beneficiary_routing_no text,
    payment_method text NOT NULL,
    frequency text NOT NULL,
    interval_value integer,
    next_run_date date NOT NULL,
    last_run_date date,
    is_pending boolean DEFAULT true NOT NULL,
    is_processing boolean DEFAULT false NOT NULL,
    is_completed boolean DEFAULT false NOT NULL,
    is_failed boolean DEFAULT false NOT NULL,
    is_cancelled boolean DEFAULT false NOT NULL,
    external_payment_id text,
    fee_amount double precision,
    net_amount double precision,
    is_reconciled boolean DEFAULT false NOT NULL,
    is_excluded boolean DEFAULT false NOT NULL,
    reconciliation_date date,
    reconciliation_ref text,
    approved_by bigint,
    approved_at timestamp with time zone,
    created_by bigint NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by bigint,
    updated_at timestamp with time zone,
    version_number bigint DEFAULT 0,
    is_automatic_clear boolean DEFAULT false NOT NULL,
    debit_account_type bigint DEFAULT 0 NOT NULL,
    credit_account_type bigint DEFAULT 2 NOT NULL,
    journal_no integer NOT NULL
);


--
-- TOC entry 328 (class 1259 OID 165057)
-- Name: suggested_prices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.suggested_prices (
    id bigint NOT NULL,
    itemcode bigint NOT NULL,
    price double precision NOT NULL,
    created_by bigint NOT NULL,
    request_id bigint NOT NULL,
    all_request_ids text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 327 (class 1259 OID 165056)
-- Name: suggested_prices_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.suggested_prices_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5783 (class 0 OID 0)
-- Dependencies: 327
-- Name: suggested_prices_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.suggested_prices_id_seq OWNED BY public.suggested_prices.id;


--
-- TOC entry 272 (class 1259 OID 41678)
-- Name: suggested_prices_to_be_removed; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.suggested_prices_to_be_removed (
    itemcode bigint NOT NULL,
    price double precision NOT NULL,
    added_at timestamp with time zone DEFAULT now() NOT NULL,
    added_by bigint NOT NULL
);


--
-- TOC entry 318 (class 1259 OID 132185)
-- Name: tags_implies_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.tags_implies_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5784 (class 0 OID 0)
-- Dependencies: 318
-- Name: tags_implies_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.tags_implies_id_seq OWNED BY public.tags_implies.id;


--
-- TOC entry 341 (class 1259 OID 181409)
-- Name: tax_jurisdictions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tax_jurisdictions (
    id bigint NOT NULL,
    code text NOT NULL,
    name text,
    is_default boolean DEFAULT false
);


--
-- TOC entry 340 (class 1259 OID 181408)
-- Name: tax_jurisdictions_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.tax_jurisdictions_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5785 (class 0 OID 0)
-- Dependencies: 340
-- Name: tax_jurisdictions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.tax_jurisdictions_id_seq OWNED BY public.tax_jurisdictions.id;


--
-- TOC entry 343 (class 1259 OID 181421)
-- Name: tax_rates; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tax_rates (
    id bigint NOT NULL,
    jurisdiction_code text NOT NULL,
    vat_category_id integer NOT NULL,
    rate_percentage double precision NOT NULL
);


--
-- TOC entry 342 (class 1259 OID 181420)
-- Name: tax_rates_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.tax_rates_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5786 (class 0 OID 0)
-- Dependencies: 342
-- Name: tax_rates_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.tax_rates_id_seq OWNED BY public.tax_rates.id;


--
-- TOC entry 348 (class 1259 OID 189572)
-- Name: temp_issued_invoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.temp_issued_invoices (
    temp_invoice_run_no bigint NOT NULL,
    invoice_contents text NOT NULL,
    posted boolean DEFAULT false NOT NULL,
    created_at time with time zone DEFAULT now() NOT NULL,
    modified_at timestamp with time zone,
    request_id bigint NOT NULL,
    request_ids text DEFAULT ''::text NOT NULL,
    user_id bigint NOT NULL
);


--
-- TOC entry 347 (class 1259 OID 189571)
-- Name: temp_issued_invoices_temp_invoice_run_no_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.temp_issued_invoices_temp_invoice_run_no_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5787 (class 0 OID 0)
-- Dependencies: 347
-- Name: temp_issued_invoices_temp_invoice_run_no_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.temp_issued_invoices_temp_invoice_run_no_seq OWNED BY public.temp_issued_invoices.temp_invoice_run_no;


--
-- TOC entry 352 (class 1259 OID 205964)
-- Name: temp_received_invoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.temp_received_invoices (
    temp_invoice_run_no bigint DEFAULT nextval('public.temp_issued_invoices_temp_invoice_run_no_seq'::regclass) NOT NULL,
    invoice_contents text NOT NULL,
    posted boolean DEFAULT false NOT NULL,
    created_at time with time zone DEFAULT now() NOT NULL,
    modified_at timestamp with time zone,
    request_id bigint NOT NULL,
    request_ids text DEFAULT ''::text NOT NULL,
    user_id bigint NOT NULL
);


--
-- TOC entry 349 (class 1259 OID 189583)
-- Name: terminals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.terminals (
    terminal_id text NOT NULL,
    default_bank bigint NOT NULL,
    default_cash bigint NOT NULL,
    run_id bigint NOT NULL
);


--
-- TOC entry 350 (class 1259 OID 189588)
-- Name: terminals_run_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.terminals_run_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5788 (class 0 OID 0)
-- Dependencies: 350
-- Name: terminals_run_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.terminals_run_id_seq OWNED BY public.terminals.run_id;


--
-- TOC entry 274 (class 1259 OID 41695)
-- Name: tiered_discounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tiered_discounts (
    itemcode bigint NOT NULL,
    min_qty double precision NOT NULL,
    discount_percentage double precision NOT NULL
);


--
-- TOC entry 242 (class 1259 OID 16906)
-- Name: tokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tokens (
    userid bigint NOT NULL,
    tokenvalue text NOT NULL,
    tokensecret text NOT NULL,
    not_valid_after timestamp with time zone NOT NULL,
    tokenid text DEFAULT (random())::text NOT NULL,
    active boolean DEFAULT true NOT NULL,
    privileges text DEFAULT ''::text NOT NULL,
    categories_bitmask bigint DEFAULT 0 NOT NULL,
    terminal text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 243 (class 1259 OID 16915)
-- Name: user_authorization; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_authorization (
    userid bigint NOT NULL,
    user_cap text NOT NULL,
    user_default_cap text DEFAULT ''::text,
    check_extended_authorization boolean DEFAULT false NOT NULL
);


--
-- TOC entry 5789 (class 0 OID 0)
-- Dependencies: 243
-- Name: TABLE user_authorization; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.user_authorization IS 'user_cap: Comma-separated
user_default_cap: Comma-separated';


--
-- TOC entry 244 (class 1259 OID 16921)
-- Name: user_authorization_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.user_authorization_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5790 (class 0 OID 0)
-- Dependencies: 244
-- Name: user_authorization_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.user_authorization_userid_seq OWNED BY public.user_authorization.userid;


--
-- TOC entry 245 (class 1259 OID 16922)
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    userid bigint NOT NULL,
    name text NOT NULL,
    address text,
    email text,
    phone text
);


--
-- TOC entry 270 (class 1259 OID 33453)
-- Name: users_field_level_access_controls_deny_list; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users_field_level_access_controls_deny_list (
    user_id bigint NOT NULL,
    denied_field text NOT NULL
);


--
-- TOC entry 246 (class 1259 OID 16927)
-- Name: users_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.users_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5791 (class 0 OID 0)
-- Dependencies: 246
-- Name: users_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.users_userid_seq OWNED BY public.users.userid;


--
-- TOC entry 339 (class 1259 OID 173187)
-- Name: v_batch_selection_window; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_batch_selection_window AS
 SELECT itemcode,
    batchcode,
    units,
    selling_price,
    min_price,
    mfg_date,
    exp_date,
    sum(units) OVER w AS cumulative_quantity,
    (sum(units) OVER w - units) AS prev_cumulative_quantity
   FROM public.inventory i
  WHERE ((units > (0)::double precision) AND ((exp_date IS NULL) OR (exp_date >= CURRENT_DATE)))
  WINDOW w AS (PARTITION BY itemcode ORDER BY exp_date, mfg_date, batchcode);


--
-- TOC entry 330 (class 1259 OID 165111)
-- Name: v_sales_step1_inherent; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_step1_inherent AS
 SELECT inv.itemcode,
    inv.batchcode,
    inv.selling_price AS i_selling_price,
    inv.min_price AS i_min_price,
    inv.multiplicative_discount_percentage AS i_inv_mult_rate,
    inv.additive_discount_percentage AS i_inv_add_rate,
    cat.process_discounts,
    cat.discount_method_is_maximum,
    inv.volume_discounts AS has_vol_disc_flag,
    inv.user_discounts AS has_user_disc_flag,
    NULL::double precision AS i_suggested_price,
    concat('Source: STANDARD; ', 'Input: Price=', inv.selling_price, ', Min=', inv.min_price, '; ', 'InvRates: Mult=', inv.multiplicative_discount_percentage, '%, Add=', inv.additive_discount_percentage, '%') AS explanation_step1
   FROM (public.inventory inv
     JOIN public.catalogue cat ON ((cat.itemcode = inv.itemcode)));


--
-- TOC entry 249 (class 1259 OID 16935)
-- Name: volume_discounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.volume_discounts (
    itemcode bigint NOT NULL,
    start_from bigint DEFAULT 1 NOT NULL,
    discount_percentage double precision DEFAULT 0 NOT NULL,
    id bigint NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    request_id bigint NOT NULL,
    all_request_ids text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 331 (class 1259 OID 165116)
-- Name: v_sales_step2_volume; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_step2_volume AS
 SELECT s1.itemcode,
    s1.batchcode,
    s1.i_selling_price,
    s1.i_min_price,
    s1.process_discounts,
    s1.discount_method_is_maximum,
    s1.has_vol_disc_flag,
    s1.has_user_disc_flag,
    s1.i_inv_mult_rate,
    s1.i_inv_add_rate,
    s1.i_suggested_price,
    vd.start_from AS vol_start_from,
    COALESCE(vd.discount_percentage, (0.0)::double precision) AS i_vol_disc_pct,
        CASE
            WHEN (vd.id IS NOT NULL) THEN true
            ELSE false
        END AS is_vol_disc_active,
    concat(s1.explanation_step1, '; ', 'VolTier: ', COALESCE(vd.discount_percentage, (0.0)::double precision), '%') AS explanation_step2
   FROM (public.v_sales_step1_inherent s1
     LEFT JOIN public.volume_discounts vd ON ((s1.itemcode = vd.itemcode)));


--
-- TOC entry 334 (class 1259 OID 165157)
-- Name: v_sales_final_matrix; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_final_matrix AS
 SELECT vol.batchcode,
    vol.itemcode,
    pii.id AS pii_id,
    vol.vol_start_from,
    vol.process_discounts,
    vol.discount_method_is_maximum,
    vol.has_vol_disc_flag,
    vol.has_user_disc_flag,
    vol.i_selling_price,
    vol.i_min_price,
    vol.i_vol_disc_pct,
    vol.i_inv_mult_rate,
    vol.i_inv_add_rate,
    pii.discount_rate_multiplicative_percentage AS i_pii_mult_rate,
    pii.discount_rate_additive_percentage AS i_pii_add_rate,
    vol.i_suggested_price,
        CASE
            WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN (pii.loyalty_points_rate_multiplicative_percentage + pii.loyalty_points_rate_additive_percentage)
            ELSE (0.0)::double precision
        END AS o_effective_lp_rate,
        CASE
            WHEN (vol.process_discounts = false) THEN (0.0)::double precision
            WHEN (vol.discount_method_is_maximum = true) THEN GREATEST(vol.i_vol_disc_pct, (vol.i_inv_mult_rate + vol.i_inv_add_rate),
            CASE
                WHEN vol.has_user_disc_flag THEN (pii.discount_rate_multiplicative_percentage + pii.discount_rate_additive_percentage)
                ELSE (0.0)::double precision
            END)
            ELSE ((vol.i_vol_disc_pct +
            CASE
                WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN (((1.0)::double precision - (((1.0)::double precision - (vol.i_inv_mult_rate / (100.0)::double precision)) * ((1.0)::double precision - (pii.discount_rate_multiplicative_percentage / (100.0)::double precision)))) * (100.0)::double precision)
                WHEN vol.process_discounts THEN vol.i_inv_mult_rate
                ELSE (0.0)::double precision
            END) +
            CASE
                WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN (vol.i_inv_add_rate + pii.discount_rate_additive_percentage)
                WHEN vol.process_discounts THEN vol.i_inv_add_rate
                ELSE (0.0)::double precision
            END)
        END AS o_raw_discount_percentage,
    (vol.i_min_price * ((1.0)::double precision +
        CASE
            WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN ((pii.loyalty_points_rate_multiplicative_percentage + pii.loyalty_points_rate_additive_percentage) / (100.0)::double precision)
            ELSE (0.0)::double precision
        END)) AS o_adjusted_min_price,
    concat(vol.explanation_step2, '; ', 'PiiRates: Mult=', pii.discount_rate_multiplicative_percentage, '%, Add=', pii.discount_rate_additive_percentage, '%; ', 'Branch: ',
        CASE
            WHEN (vol.process_discounts = false) THEN 'DISABLED'::text
            WHEN vol.discount_method_is_maximum THEN 'MAX'::text
            ELSE 'STACK'::text
        END) AS explanation_final
   FROM (public.v_sales_step2_volume vol
     CROSS JOIN public.pii pii)
  WHERE (vol.i_selling_price >= vol.i_min_price);


--
-- TOC entry 332 (class 1259 OID 165131)
-- Name: v_sales_step1_suggested; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_step1_suggested AS
 SELECT inv.itemcode,
    inv.batchcode,
    sp.price AS i_selling_price,
    inv.min_price AS i_min_price,
    inv.multiplicative_discount_percentage AS i_inv_mult_rate,
    inv.additive_discount_percentage AS i_inv_add_rate,
    cat.process_discounts,
    cat.discount_method_is_maximum,
    inv.volume_discounts AS has_vol_disc_flag,
    inv.user_discounts AS has_user_disc_flag,
    sp.price AS i_suggested_price,
    concat('Source: SUGGESTED; ', 'Input: Price=', sp.price, ', Min=', inv.min_price, '; ', 'InvRates: Mult=', inv.multiplicative_discount_percentage, '%, Add=', inv.additive_discount_percentage, '%') AS explanation_step1
   FROM ((public.inventory inv
     JOIN public.catalogue cat ON ((cat.itemcode = inv.itemcode)))
     JOIN public.suggested_prices sp ON ((sp.itemcode = inv.itemcode)));


--
-- TOC entry 333 (class 1259 OID 165136)
-- Name: v_sales_step2_suggested_volume; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_step2_suggested_volume AS
 SELECT s1.itemcode,
    s1.batchcode,
    s1.i_selling_price,
    s1.i_min_price,
    s1.process_discounts,
    s1.discount_method_is_maximum,
    s1.has_vol_disc_flag,
    s1.has_user_disc_flag,
    s1.i_inv_mult_rate,
    s1.i_inv_add_rate,
    s1.i_suggested_price,
    vd.start_from AS vol_start_from,
    COALESCE(vd.discount_percentage, (0.0)::double precision) AS i_vol_disc_pct,
        CASE
            WHEN (vd.id IS NOT NULL) THEN true
            ELSE false
        END AS is_vol_disc_active,
    concat(s1.explanation_step1, '; ', 'VolTier: ', COALESCE(vd.discount_percentage, (0.0)::double precision), '%') AS explanation_step2
   FROM (public.v_sales_step1_suggested s1
     LEFT JOIN public.volume_discounts vd ON ((s1.itemcode = vd.itemcode)));


--
-- TOC entry 336 (class 1259 OID 165167)
-- Name: v_sales_final_matrix_suggested; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_final_matrix_suggested AS
 SELECT vol.batchcode,
    vol.itemcode,
    pii.id AS pii_id,
    vol.vol_start_from,
    vol.process_discounts,
    vol.discount_method_is_maximum,
    vol.has_vol_disc_flag,
    vol.has_user_disc_flag,
    vol.i_selling_price,
    vol.i_min_price,
    vol.i_vol_disc_pct,
    vol.i_inv_mult_rate,
    vol.i_inv_add_rate,
    pii.discount_rate_multiplicative_percentage AS i_pii_mult_rate,
    pii.discount_rate_additive_percentage AS i_pii_add_rate,
    vol.i_suggested_price,
        CASE
            WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN (pii.loyalty_points_rate_multiplicative_percentage + pii.loyalty_points_rate_additive_percentage)
            ELSE (0.0)::double precision
        END AS o_effective_lp_rate,
        CASE
            WHEN (vol.process_discounts = false) THEN (0.0)::double precision
            WHEN (vol.discount_method_is_maximum = true) THEN GREATEST(vol.i_vol_disc_pct, (vol.i_inv_mult_rate + vol.i_inv_add_rate),
            CASE
                WHEN vol.has_user_disc_flag THEN (pii.discount_rate_multiplicative_percentage + pii.discount_rate_additive_percentage)
                ELSE (0.0)::double precision
            END)
            ELSE ((vol.i_vol_disc_pct +
            CASE
                WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN (((1.0)::double precision - (((1.0)::double precision - (vol.i_inv_mult_rate / (100.0)::double precision)) * ((1.0)::double precision - (pii.discount_rate_multiplicative_percentage / (100.0)::double precision)))) * (100.0)::double precision)
                WHEN vol.process_discounts THEN vol.i_inv_mult_rate
                ELSE (0.0)::double precision
            END) +
            CASE
                WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN (vol.i_inv_add_rate + pii.discount_rate_additive_percentage)
                WHEN vol.process_discounts THEN vol.i_inv_add_rate
                ELSE (0.0)::double precision
            END)
        END AS o_raw_discount_percentage,
    (vol.i_min_price * ((1.0)::double precision +
        CASE
            WHEN (vol.process_discounts AND vol.has_user_disc_flag) THEN ((pii.loyalty_points_rate_multiplicative_percentage + pii.loyalty_points_rate_additive_percentage) / (100.0)::double precision)
            ELSE (0.0)::double precision
        END)) AS o_adjusted_min_price,
    concat(vol.explanation_step2, '; ', 'PiiRates: Mult=', pii.discount_rate_multiplicative_percentage, '%, Add=', pii.discount_rate_additive_percentage, '%; ', 'Branch: ',
        CASE
            WHEN (vol.process_discounts = false) THEN 'DISABLED'::text
            WHEN vol.discount_method_is_maximum THEN 'MAX'::text
            ELSE 'STACK'::text
        END) AS explanation_final
   FROM (public.v_sales_step2_suggested_volume vol
     CROSS JOIN public.pii pii)
  WHERE (vol.i_suggested_price >= vol.i_min_price);


--
-- TOC entry 335 (class 1259 OID 165162)
-- Name: v_sales_final_output; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_final_output AS
 SELECT batchcode,
    itemcode,
    pii_id,
    i_suggested_price,
    i_selling_price,
    i_min_price,
    o_adjusted_min_price,
    o_raw_discount_percentage,
    o_effective_lp_rate,
    (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision)) AS o_raw_discount_amt,
    (i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))) AS o_raw_price,
    GREATEST((i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))), o_adjusted_min_price) AS o_effective_selling_price_per_unit,
    (i_selling_price - GREATEST((i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))), o_adjusted_min_price)) AS o_effective_discount_per_unit,
    ((i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))) < o_adjusted_min_price) AS is_clamped,
    explanation_final
   FROM public.v_sales_final_matrix;


--
-- TOC entry 337 (class 1259 OID 165172)
-- Name: v_sales_final_output_suggested; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_final_output_suggested AS
 SELECT batchcode,
    itemcode,
    pii_id,
    i_suggested_price,
    i_selling_price,
    i_min_price,
    o_adjusted_min_price,
    o_raw_discount_percentage,
    o_effective_lp_rate,
    (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision)) AS o_raw_discount_amt,
    (i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))) AS o_raw_price,
    GREATEST((i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))), o_adjusted_min_price) AS o_effective_selling_price_per_unit,
    (i_selling_price - GREATEST((i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))), o_adjusted_min_price)) AS o_effective_discount_per_unit,
    ((i_selling_price - (i_selling_price * (o_raw_discount_percentage / (100.0)::double precision))) < o_adjusted_min_price) AS is_clamped,
    explanation_final
   FROM public.v_sales_final_matrix_suggested;


--
-- TOC entry 338 (class 1259 OID 165177)
-- Name: v_comprehensive_sales_final_matrix; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_comprehensive_sales_final_matrix AS
 SELECT v_sales_final_output.batchcode,
    v_sales_final_output.itemcode,
    v_sales_final_output.pii_id,
    v_sales_final_output.i_suggested_price,
    v_sales_final_output.i_selling_price,
    v_sales_final_output.i_min_price,
    v_sales_final_output.o_adjusted_min_price,
    v_sales_final_output.o_raw_discount_percentage,
    v_sales_final_output.o_effective_lp_rate,
    v_sales_final_output.o_raw_discount_amt,
    v_sales_final_output.o_raw_price,
    v_sales_final_output.o_effective_selling_price_per_unit,
    v_sales_final_output.o_effective_discount_per_unit,
    v_sales_final_output.is_clamped,
    v_sales_final_output.explanation_final
   FROM public.v_sales_final_output
UNION ALL
 SELECT v_sales_final_output_suggested.batchcode,
    v_sales_final_output_suggested.itemcode,
    v_sales_final_output_suggested.pii_id,
    v_sales_final_output_suggested.i_suggested_price,
    v_sales_final_output_suggested.i_selling_price,
    v_sales_final_output_suggested.i_min_price,
    v_sales_final_output_suggested.o_adjusted_min_price,
    v_sales_final_output_suggested.o_raw_discount_percentage,
    v_sales_final_output_suggested.o_effective_lp_rate,
    v_sales_final_output_suggested.o_raw_discount_amt,
    v_sales_final_output_suggested.o_raw_price,
    v_sales_final_output_suggested.o_effective_selling_price_per_unit,
    v_sales_final_output_suggested.o_effective_discount_per_unit,
    v_sales_final_output_suggested.is_clamped,
    v_sales_final_output_suggested.explanation_final
   FROM public.v_sales_final_output_suggested;


--
-- TOC entry 247 (class 1259 OID 16928)
-- Name: vat_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.vat_categories (
    vat_category_id bigint NOT NULL,
    vat_percentage double precision NOT NULL,
    vat_name text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


--
-- TOC entry 344 (class 1259 OID 181431)
-- Name: v_tax_resolution; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_tax_resolution AS
 SELECT j.code AS jurisdiction_code,
    j.name AS jurisdiction_name,
    j.is_default,
    c.vat_category_id,
    c.vat_name,
    COALESCE(tr.rate_percentage, c.vat_percentage) AS effective_rate_percentage,
        CASE
            WHEN (tr.id IS NOT NULL) THEN 'OVERRIDE'::text
            ELSE 'SOURCE_DEFAULT'::text
        END AS rate_source
   FROM ((public.tax_jurisdictions j
     CROSS JOIN public.vat_categories c)
     LEFT JOIN public.tax_rates tr ON (((tr.jurisdiction_code = j.code) AND (tr.vat_category_id = c.vat_category_id))));


--
-- TOC entry 248 (class 1259 OID 16934)
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.vat_categories_vat_category_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5792 (class 0 OID 0)
-- Dependencies: 248
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.vat_categories_vat_category_id_seq OWNED BY public.vat_categories.vat_category_id;


--
-- TOC entry 329 (class 1259 OID 165066)
-- Name: volume_discounts_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.volume_discounts_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5793 (class 0 OID 0)
-- Dependencies: 329
-- Name: volume_discounts_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.volume_discounts_id_seq OWNED BY public.volume_discounts.id;


--
-- TOC entry 5351 (class 2604 OID 74735)
-- Name: accounts_information account_no; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information ALTER COLUMN account_no SET DEFAULT nextval('public.accounts_information_2_account_no_seq'::regclass);


--
-- TOC entry 5101 (class 2604 OID 16941)
-- Name: accounts_journal_information journal_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_information ALTER COLUMN journal_id SET DEFAULT nextval('public.accounts_journal_information_journal_id_seq'::regclass);


--
-- TOC entry 5414 (class 2604 OID 214151)
-- Name: allowed_keys id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.allowed_keys ALTER COLUMN id SET DEFAULT nextval('public.allowed_keys_id_seq'::regclass);


--
-- TOC entry 5102 (class 2604 OID 16942)
-- Name: authorized_terminals userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_terminals ALTER COLUMN userid SET DEFAULT nextval('public.authorized_terminals_userid_seq'::regclass);


--
-- TOC entry 5217 (class 2604 OID 41702)
-- Name: bundled_pricing bundle_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bundled_pricing ALTER COLUMN bundle_id SET DEFAULT nextval('public.bundled_pricing_bundle_id_seq'::regclass);


--
-- TOC entry 5103 (class 2604 OID 16943)
-- Name: catalogue itemcode; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue ALTER COLUMN itemcode SET DEFAULT nextval('public.catalogue_itemcode_seq'::regclass);


--
-- TOC entry 5321 (class 2604 OID 50145)
-- Name: cheque_books id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cheque_books ALTER COLUMN id SET DEFAULT nextval('public.cheque_books_id_seq'::regclass);


--
-- TOC entry 5391 (class 2604 OID 156807)
-- Name: cheque_leaves id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cheque_leaves ALTER COLUMN id SET DEFAULT nextval('public.cheque_leaves_id_seq'::regclass);


--
-- TOC entry 5135 (class 2604 OID 16944)
-- Name: credentials userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials ALTER COLUMN userid SET DEFAULT nextval('public.credentials_userid_seq'::regclass);


--
-- TOC entry 5363 (class 2604 OID 99304)
-- Name: cycle_count id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cycle_count ALTER COLUMN id SET DEFAULT nextval('public.cycle_count_id_seq'::regclass);


--
-- TOC entry 5364 (class 2604 OID 99305)
-- Name: cycle_count seq_no; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cycle_count ALTER COLUMN seq_no SET DEFAULT nextval('public.cycle_count_seq_no_seq'::regclass);


--
-- TOC entry 5402 (class 2604 OID 181452)
-- Name: ifrs_categories id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ifrs_categories ALTER COLUMN id SET DEFAULT nextval('public.ifrs_categories_id_seq'::regclass);


--
-- TOC entry 5138 (class 2604 OID 16945)
-- Name: inventory itemcode; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory ALTER COLUMN itemcode SET DEFAULT nextval('public.inventory_itemcode_seq'::regclass);


--
-- TOC entry 5358 (class 2604 OID 82866)
-- Name: inventory_adjustments entry_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_adjustments ALTER COLUMN entry_id SET DEFAULT nextval('public.inventory_adjustments_entry_id_seq'::regclass);


--
-- TOC entry 5198 (class 2604 OID 25228)
-- Name: issued_invoices invoice_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.issued_invoices ALTER COLUMN invoice_id SET DEFAULT nextval('public.issued_invoices_invoice_id_seq'::regclass);


--
-- TOC entry 5204 (class 2604 OID 25240)
-- Name: loyalty_points points_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points ALTER COLUMN points_id SET DEFAULT nextval('public.loyality_points_points_id_seq'::regclass);


--
-- TOC entry 5207 (class 2604 OID 25248)
-- Name: loyalty_points_redemption redemption_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points_redemption ALTER COLUMN redemption_id SET DEFAULT nextval('public.loyalty_points_redemption_redemption_id_seq'::regclass);


--
-- TOC entry 5378 (class 2604 OID 123946)
-- Name: mapped_location_item_placed_in id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mapped_location_item_placed_in ALTER COLUMN id SET DEFAULT nextval('public.mapped_location_item_placed_in_id_seq'::regclass);


--
-- TOC entry 5377 (class 2604 OID 123937)
-- Name: mapped_locations id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mapped_locations ALTER COLUMN id SET DEFAULT nextval('public.mapped_locations_id_seq'::regclass);


--
-- TOC entry 5185 (class 2604 OID 17016)
-- Name: notification_servicer_types notification_servicer_type_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_servicer_types ALTER COLUMN notification_servicer_type_id SET DEFAULT nextval('public.notification_servicer_types_notification_servicer_type_id_seq'::regclass);


--
-- TOC entry 5183 (class 2604 OID 17006)
-- Name: notification_types notification_type_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_types ALTER COLUMN notification_type_id SET DEFAULT nextval('public.notification_types_notification_type_id_seq'::regclass);


--
-- TOC entry 5177 (class 2604 OID 16994)
-- Name: notifications notif_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications ALTER COLUMN notif_id SET DEFAULT nextval('public.notifications_notif_id_seq'::regclass);


--
-- TOC entry 5314 (class 2604 OID 50127)
-- Name: payments id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments ALTER COLUMN id SET DEFAULT nextval('public.payments_id_seq'::regclass);


--
-- TOC entry 5375 (class 2604 OID 123927)
-- Name: physical_maps map_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.physical_maps ALTER COLUMN map_id SET DEFAULT nextval('public.physical_maps_map_id_seq'::regclass);


--
-- TOC entry 5219 (class 2604 OID 41710)
-- Name: pii id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pii ALTER COLUMN id SET DEFAULT nextval('public.pii_id_seq'::regclass);


--
-- TOC entry 5366 (class 2604 OID 107519)
-- Name: quota_usage_per_user_itemcode quota_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.quota_usage_per_user_itemcode ALTER COLUMN quota_id SET DEFAULT nextval('public.quota_usage_per_user_itemcode_quota_id_seq'::regclass);


--
-- TOC entry 5210 (class 2604 OID 25256)
-- Name: receipts receipt_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.receipts ALTER COLUMN receipt_id SET DEFAULT nextval('public.receipts_receipt_id_seq'::regclass);


--
-- TOC entry 5228 (class 2604 OID 41773)
-- Name: received_invoices received_invoice_no; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.received_invoices ALTER COLUMN received_invoice_no SET DEFAULT nextval('public.received_invoices_received_invoice_no_seq'::regclass);


--
-- TOC entry 5368 (class 2604 OID 115739)
-- Name: ref_docs ref_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ref_docs ALTER COLUMN ref_id SET DEFAULT nextval('public.ref_docs_ref_id_seq'::regclass);


--
-- TOC entry 5379 (class 2604 OID 123957)
-- Name: ref_docs_transcriptions id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ref_docs_transcriptions ALTER COLUMN id SET DEFAULT nextval('public.ref_docs_transcriptions_id_seq'::regclass);


--
-- TOC entry 5189 (class 2604 OID 25216)
-- Name: sales sale_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales ALTER COLUMN sale_id SET DEFAULT nextval('public.sales_sale_id_seq'::regclass);


--
-- TOC entry 5300 (class 2604 OID 50105)
-- Name: scheduled_payments id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.scheduled_payments ALTER COLUMN id SET DEFAULT nextval('public.scheduled_payments_id_seq'::regclass);


--
-- TOC entry 5397 (class 2604 OID 165060)
-- Name: suggested_prices id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.suggested_prices ALTER COLUMN id SET DEFAULT nextval('public.suggested_prices_id_seq'::regclass);


--
-- TOC entry 5385 (class 2604 OID 132189)
-- Name: tags_implies id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tags_implies ALTER COLUMN id SET DEFAULT nextval('public.tags_implies_id_seq'::regclass);


--
-- TOC entry 5399 (class 2604 OID 181412)
-- Name: tax_jurisdictions id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tax_jurisdictions ALTER COLUMN id SET DEFAULT nextval('public.tax_jurisdictions_id_seq'::regclass);


--
-- TOC entry 5401 (class 2604 OID 181424)
-- Name: tax_rates id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tax_rates ALTER COLUMN id SET DEFAULT nextval('public.tax_rates_id_seq'::regclass);


--
-- TOC entry 5405 (class 2604 OID 189575)
-- Name: temp_issued_invoices temp_invoice_run_no; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.temp_issued_invoices ALTER COLUMN temp_invoice_run_no SET DEFAULT nextval('public.temp_issued_invoices_temp_invoice_run_no_seq'::regclass);


--
-- TOC entry 5409 (class 2604 OID 189589)
-- Name: terminals run_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.terminals ALTER COLUMN run_id SET DEFAULT nextval('public.terminals_run_id_seq'::regclass);


--
-- TOC entry 5166 (class 2604 OID 16946)
-- Name: user_authorization userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_authorization ALTER COLUMN userid SET DEFAULT nextval('public.user_authorization_userid_seq'::regclass);


--
-- TOC entry 5169 (class 2604 OID 16947)
-- Name: users userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users ALTER COLUMN userid SET DEFAULT nextval('public.users_userid_seq'::regclass);


--
-- TOC entry 5170 (class 2604 OID 16948)
-- Name: vat_categories vat_category_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.vat_categories ALTER COLUMN vat_category_id SET DEFAULT nextval('public.vat_categories_vat_category_id_seq'::regclass);


--
-- TOC entry 5174 (class 2604 OID 165067)
-- Name: volume_discounts id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.volume_discounts ALTER COLUMN id SET DEFAULT nextval('public.volume_discounts_id_seq'::regclass);


--
-- TOC entry 5424 (class 2606 OID 16950)
-- Name: sih_current sih_current_pkey; Type: CONSTRAINT; Schema: imported_dummy; Owner: -
--

ALTER TABLE ONLY imported_dummy.sih_current
    ADD CONSTRAINT sih_current_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 5422 (class 2606 OID 16952)
-- Name: sih sih_pkey; Type: CONSTRAINT; Schema: imported_dummy; Owner: -
--

ALTER TABLE ONLY imported_dummy.sih
    ADD CONSTRAINT sih_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 5426 (class 2606 OID 41874)
-- Name: accounts_balances accounts_balances_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_balances
    ADD CONSTRAINT accounts_balances_pkey PRIMARY KEY (account_type, account_no);


--
-- TOC entry 5521 (class 2606 OID 74743)
-- Name: accounts_information accounts_information_2_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information
    ADD CONSTRAINT accounts_information_2_pkey PRIMARY KEY (account_no);


--
-- TOC entry 5428 (class 2606 OID 41872)
-- Name: accounts_journal_entries accounts_journal_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_entries
    ADD CONSTRAINT accounts_journal_entries_pkey PRIMARY KEY (journal_univ_seq);


--
-- TOC entry 5430 (class 2606 OID 16958)
-- Name: accounts_journal_information accounts_journal_information_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_information
    ADD CONSTRAINT accounts_journal_information_pkey PRIMARY KEY (journal_id);


--
-- TOC entry 5432 (class 2606 OID 16960)
-- Name: accounts_types accounts_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_types
    ADD CONSTRAINT accounts_types_pkey PRIMARY KEY (account_type);


--
-- TOC entry 5571 (class 2606 OID 214159)
-- Name: allowed_keys allowed_keys_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.allowed_keys
    ADD CONSTRAINT allowed_keys_pkey PRIMARY KEY (id);


--
-- TOC entry 5434 (class 2606 OID 16962)
-- Name: api_authorization api_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.api_authorization
    ADD CONSTRAINT api_authorization_pkey PRIMARY KEY (userid, "authorization");


--
-- TOC entry 5547 (class 2606 OID 140374)
-- Name: barcodes barcodes_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.barcodes
    ADD CONSTRAINT barcodes_pkey PRIMARY KEY (code);


--
-- TOC entry 5490 (class 2606 OID 41705)
-- Name: bundled_pricing bundled_pricing_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bundled_pricing
    ADD CONSTRAINT bundled_pricing_pkey PRIMARY KEY (bundle_id);


--
-- TOC entry 5436 (class 2606 OID 16964)
-- Name: catalogue catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT catalogue_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 5441 (class 2606 OID 16966)
-- Name: categories_bitmask categories_bitmask_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categories_bitmask
    ADD CONSTRAINT categories_bitmask_pkey PRIMARY KEY (bitmask);


--
-- TOC entry 5514 (class 2606 OID 50151)
-- Name: cheque_books cheque_books_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cheque_books
    ADD CONSTRAINT cheque_books_pkey PRIMARY KEY (id);


--
-- TOC entry 5549 (class 2606 OID 156815)
-- Name: cheque_leaves cheque_leaves_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cheque_leaves
    ADD CONSTRAINT cheque_leaves_pkey PRIMARY KEY (id);


--
-- TOC entry 5502 (class 2606 OID 41823)
-- Name: codes_batches codes_batches_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.codes_batches
    ADD CONSTRAINT codes_batches_pkey PRIMARY KEY (code, itemcode, batchcode);


--
-- TOC entry 5500 (class 2606 OID 41816)
-- Name: codes_catalogue codes_catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.codes_catalogue
    ADD CONSTRAINT codes_catalogue_pkey PRIMARY KEY (code, itemcode);


--
-- TOC entry 5443 (class 2606 OID 16968)
-- Name: credentials credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT credentials_pkey PRIMARY KEY (userid);


--
-- TOC entry 5488 (class 2606 OID 41694)
-- Name: customer_discounts customer_discounts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_discounts
    ADD CONSTRAINT customer_discounts_pkey PRIMARY KEY (customer_id);


--
-- TOC entry 5527 (class 2606 OID 99310)
-- Name: cycle_count cycle_count_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cycle_count
    ADD CONSTRAINT cycle_count_pkey PRIMARY KEY (id);


--
-- TOC entry 5484 (class 2606 OID 41674)
-- Name: default_deny_fields default_deny_fields_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.default_deny_fields
    ADD CONSTRAINT default_deny_fields_pkey PRIMARY KEY (field);


--
-- TOC entry 5523 (class 2606 OID 74725)
-- Name: accounts_information human_friendly_id_2; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information
    ADD CONSTRAINT human_friendly_id_2 UNIQUE (human_friendly_id);


--
-- TOC entry 5470 (class 2606 OID 25195)
-- Name: i18n_labels i18n_labels_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.i18n_labels
    ADD CONSTRAINT i18n_labels_pkey PRIMARY KEY (id, lang);


--
-- TOC entry 5468 (class 2606 OID 25188)
-- Name: idempotency idempotency_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.idempotency
    ADD CONSTRAINT idempotency_pkey PRIMARY KEY (key);


--
-- TOC entry 5561 (class 2606 OID 181460)
-- Name: ifrs_categories ifrs_categories_code_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ifrs_categories
    ADD CONSTRAINT ifrs_categories_code_key UNIQUE (code);


--
-- TOC entry 5563 (class 2606 OID 181458)
-- Name: ifrs_categories ifrs_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ifrs_categories
    ADD CONSTRAINT ifrs_categories_pkey PRIMARY KEY (id);


--
-- TOC entry 5525 (class 2606 OID 82873)
-- Name: inventory_adjustments inventory_adjustments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_adjustments
    ADD CONSTRAINT inventory_adjustments_pkey PRIMARY KEY (entry_id);


--
-- TOC entry 5494 (class 2606 OID 41724)
-- Name: inventory_images inventory_images_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_images
    ADD CONSTRAINT inventory_images_pkey PRIMARY KEY (imageid, itemcode);


--
-- TOC entry 5448 (class 2606 OID 74752)
-- Name: inventory inventory_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_pkey PRIMARY KEY (batchcode);


--
-- TOC entry 5476 (class 2606 OID 25233)
-- Name: issued_invoices issued_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.issued_invoices
    ADD CONSTRAINT issued_invoices_pkey PRIMARY KEY (invoice_id);


--
-- TOC entry 5478 (class 2606 OID 25243)
-- Name: loyalty_points loyality_points_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points
    ADD CONSTRAINT loyality_points_pkey PRIMARY KEY (points_id);


--
-- TOC entry 5480 (class 2606 OID 25251)
-- Name: loyalty_points_redemption loyalty_points_redemption_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points_redemption
    ADD CONSTRAINT loyalty_points_redemption_pkey PRIMARY KEY (redemption_id);


--
-- TOC entry 5541 (class 2606 OID 123948)
-- Name: mapped_location_item_placed_in mapped_location_item_placed_in_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mapped_location_item_placed_in
    ADD CONSTRAINT mapped_location_item_placed_in_pkey PRIMARY KEY (id);


--
-- TOC entry 5539 (class 2606 OID 123941)
-- Name: mapped_locations mapped_locations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mapped_locations
    ADD CONSTRAINT mapped_locations_pkey PRIMARY KEY (id);


--
-- TOC entry 5466 (class 2606 OID 17020)
-- Name: notification_servicer_types notification_servicer_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_servicer_types
    ADD CONSTRAINT notification_servicer_types_pkey PRIMARY KEY (notification_servicer_type_id);


--
-- TOC entry 5464 (class 2606 OID 17011)
-- Name: notification_types notification_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_types
    ADD CONSTRAINT notification_types_pkey PRIMARY KEY (notification_type_id);


--
-- TOC entry 5462 (class 2606 OID 17000)
-- Name: notifications notifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_pkey PRIMARY KEY (notif_id);


--
-- TOC entry 5512 (class 2606 OID 50136)
-- Name: payments payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_pkey PRIMARY KEY (id);


--
-- TOC entry 5472 (class 2606 OID 25202)
-- Name: permissions_extended_api_call permissions_extended_api_call_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_extended_api_call
    ADD CONSTRAINT permissions_extended_api_call_pkey PRIMARY KEY (user_id, api_call);


--
-- TOC entry 5452 (class 2606 OID 16972)
-- Name: permissions_list_categories_names permissions_list_categories_names_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list_categories_names
    ADD CONSTRAINT permissions_list_categories_names_pkey PRIMARY KEY (category);


--
-- TOC entry 5450 (class 2606 OID 16974)
-- Name: permissions_list permissions_list_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list
    ADD CONSTRAINT permissions_list_pkey PRIMARY KEY ("Permission");


--
-- TOC entry 5454 (class 2606 OID 16976)
-- Name: permissions_list_users_categories permissions_list_users_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list_users_categories
    ADD CONSTRAINT permissions_list_users_categories_pkey PRIMARY KEY (userid);


--
-- TOC entry 5537 (class 2606 OID 123932)
-- Name: physical_maps physical_maps_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.physical_maps
    ADD CONSTRAINT physical_maps_pkey PRIMARY KEY (map_id);


--
-- TOC entry 5496 (class 2606 OID 41732)
-- Name: pii_images pii_images_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pii_images
    ADD CONSTRAINT pii_images_pkey PRIMARY KEY (pii_id, image_no);


--
-- TOC entry 5492 (class 2606 OID 41716)
-- Name: pii pii_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pii
    ADD CONSTRAINT pii_pkey PRIMARY KEY (id);


--
-- TOC entry 5533 (class 2606 OID 107522)
-- Name: quota_usage_per_user_itemcode quota_usage_per_user_itemcode_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.quota_usage_per_user_itemcode
    ADD CONSTRAINT quota_usage_per_user_itemcode_pkey PRIMARY KEY (quota_id);


--
-- TOC entry 5482 (class 2606 OID 25259)
-- Name: receipts receipts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.receipts
    ADD CONSTRAINT receipts_pkey PRIMARY KEY (receipt_id);


--
-- TOC entry 5498 (class 2606 OID 41786)
-- Name: received_invoices received_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.received_invoices
    ADD CONSTRAINT received_invoices_pkey PRIMARY KEY (received_invoice_no);


--
-- TOC entry 5535 (class 2606 OID 115747)
-- Name: ref_docs ref_docs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ref_docs
    ADD CONSTRAINT ref_docs_pkey PRIMARY KEY (ref_id);


--
-- TOC entry 5543 (class 2606 OID 123963)
-- Name: ref_docs_transcriptions ref_docs_transcriptions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ref_docs_transcriptions
    ADD CONSTRAINT ref_docs_transcriptions_pkey PRIMARY KEY (id);


--
-- TOC entry 5474 (class 2606 OID 25223)
-- Name: sales sales_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales
    ADD CONSTRAINT sales_pkey PRIMARY KEY (sale_id);


--
-- TOC entry 5507 (class 2606 OID 50119)
-- Name: scheduled_payments scheduled_payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.scheduled_payments
    ADD CONSTRAINT scheduled_payments_pkey PRIMARY KEY (id);


--
-- TOC entry 5519 (class 2606 OID 58306)
-- Name: scheduled_receipts scheduled_receipts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.scheduled_receipts
    ADD CONSTRAINT scheduled_receipts_pkey PRIMARY KEY (id);


--
-- TOC entry 5486 (class 2606 OID 41682)
-- Name: suggested_prices_to_be_removed suggested_prices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.suggested_prices_to_be_removed
    ADD CONSTRAINT suggested_prices_pkey PRIMARY KEY (itemcode, price);


--
-- TOC entry 5551 (class 2606 OID 165065)
-- Name: suggested_prices suggested_prices_pkey1; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.suggested_prices
    ADD CONSTRAINT suggested_prices_pkey1 PRIMARY KEY (id);


--
-- TOC entry 5545 (class 2606 OID 132194)
-- Name: tags_implies tags_implies_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tags_implies
    ADD CONSTRAINT tags_implies_pkey PRIMARY KEY (id);


--
-- TOC entry 5553 (class 2606 OID 181419)
-- Name: tax_jurisdictions tax_jurisdictions_code_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tax_jurisdictions
    ADD CONSTRAINT tax_jurisdictions_code_key UNIQUE (code);


--
-- TOC entry 5555 (class 2606 OID 181417)
-- Name: tax_jurisdictions tax_jurisdictions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tax_jurisdictions
    ADD CONSTRAINT tax_jurisdictions_pkey PRIMARY KEY (id);


--
-- TOC entry 5557 (class 2606 OID 181428)
-- Name: tax_rates tax_rates_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tax_rates
    ADD CONSTRAINT tax_rates_pkey PRIMARY KEY (id);


--
-- TOC entry 5565 (class 2606 OID 189582)
-- Name: temp_issued_invoices temp_issued_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.temp_issued_invoices
    ADD CONSTRAINT temp_issued_invoices_pkey PRIMARY KEY (temp_invoice_run_no);


--
-- TOC entry 5569 (class 2606 OID 205974)
-- Name: temp_received_invoices temp_received_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.temp_received_invoices
    ADD CONSTRAINT temp_received_invoices_pkey PRIMARY KEY (temp_invoice_run_no);


--
-- TOC entry 5567 (class 2606 OID 189596)
-- Name: terminals terminals_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.terminals
    ADD CONSTRAINT terminals_pkey PRIMARY KEY (run_id);


--
-- TOC entry 5456 (class 2606 OID 16978)
-- Name: tokens tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT tokens_pkey PRIMARY KEY (tokenid);


--
-- TOC entry 5439 (class 2606 OID 16980)
-- Name: catalogue unique_desc; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT unique_desc UNIQUE (description);


--
-- TOC entry 5559 (class 2606 OID 181430)
-- Name: tax_rates unique_override; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tax_rates
    ADD CONSTRAINT unique_override UNIQUE (jurisdiction_code, vat_category_id);


--
-- TOC entry 5458 (class 2606 OID 16982)
-- Name: user_authorization user_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_authorization
    ADD CONSTRAINT user_authorization_pkey PRIMARY KEY (userid);


--
-- TOC entry 5445 (class 2606 OID 16984)
-- Name: credentials username_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT username_unique UNIQUE (username);


--
-- TOC entry 5460 (class 2606 OID 16986)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


--
-- TOC entry 5437 (class 1259 OID 189687)
-- Name: idx_catalogue_description_trgm; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_catalogue_description_trgm ON public.catalogue USING gin (lower(description) public.gin_trgm_ops);


--
-- TOC entry 5528 (class 1259 OID 99313)
-- Name: idx_cycle_count_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cycle_count_date ON public.cycle_count USING btree (count_date);


--
-- TOC entry 5529 (class 1259 OID 99311)
-- Name: idx_cycle_count_itemcode; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cycle_count_itemcode ON public.cycle_count USING btree (itemcode);


--
-- TOC entry 5530 (class 1259 OID 99314)
-- Name: idx_cycle_count_principal_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cycle_count_principal_id ON public.cycle_count USING btree (principal_id);


--
-- TOC entry 5531 (class 1259 OID 99312)
-- Name: idx_cycle_count_seq_no; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cycle_count_seq_no ON public.cycle_count USING btree (seq_no);


--
-- TOC entry 5446 (class 1259 OID 189604)
-- Name: idx_inventory_fast_view; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inventory_fast_view ON public.inventory USING btree (itemcode) INCLUDE (units, exp_date, selling_price, marked_price) WHERE (units > (0)::double precision);


--
-- TOC entry 5508 (class 1259 OID 50138)
-- Name: ix_payments_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_payments_date ON public.payments USING btree (payment_date);


--
-- TOC entry 5509 (class 1259 OID 50139)
-- Name: ix_payments_recon; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_payments_recon ON public.payments USING btree (company_id, bank_account_id, is_reconciled, is_excluded);


--
-- TOC entry 5510 (class 1259 OID 50137)
-- Name: ix_payments_scheduled; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_payments_scheduled ON public.payments USING btree (scheduled_payment_id);


--
-- TOC entry 5503 (class 1259 OID 50120)
-- Name: ix_scheduled_payments_next_run; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_payments_next_run ON public.scheduled_payments USING btree (next_run_date, is_pending);


--
-- TOC entry 5504 (class 1259 OID 50122)
-- Name: ix_scheduled_payments_recon; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_payments_recon ON public.scheduled_payments USING btree (company_id, bank_account_id, is_reconciled, is_excluded);


--
-- TOC entry 5505 (class 1259 OID 50121)
-- Name: ix_scheduled_payments_status_flags; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_payments_status_flags ON public.scheduled_payments USING btree (is_processing, is_completed, is_failed, is_cancelled);


--
-- TOC entry 5515 (class 1259 OID 58307)
-- Name: ix_scheduled_receipts_next_run; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_receipts_next_run ON public.scheduled_receipts USING btree (next_run_date, is_pending);


--
-- TOC entry 5516 (class 1259 OID 58308)
-- Name: ix_scheduled_receipts_recon; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_receipts_recon ON public.scheduled_receipts USING btree (company_id, bank_account_id, is_reconciled, is_excluded);


--
-- TOC entry 5517 (class 1259 OID 58309)
-- Name: ix_scheduled_receipts_status_flags; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_receipts_status_flags ON public.scheduled_receipts USING btree (is_processing, is_completed, is_failed, is_cancelled);


-- Completed on 2026-04-10 10:23:56

--
-- PostgreSQL database dump complete
--

