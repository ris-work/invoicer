--
-- PostgreSQL database dump
--

-- Dumped from database version 17.2
-- Dumped by pg_dump version 17.2

-- Started on 2025-09-18 13:51:05

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
-- TOC entry 6 (class 2615 OID 16783)
-- Name: imported_dummy; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA imported_dummy;


--
-- TOC entry 5 (class 2615 OID 16782)
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


--
-- TOC entry 5345 (class 0 OID 0)
-- Dependencies: 5
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA public IS '';


--
-- TOC entry 308 (class 1255 OID 16784)
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
-- TOC entry 309 (class 1255 OID 16785)
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
-- TOC entry 218 (class 1259 OID 16786)
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
-- TOC entry 219 (class 1259 OID 16791)
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
-- TOC entry 220 (class 1259 OID 16796)
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
-- TOC entry 5346 (class 0 OID 0)
-- Dependencies: 220
-- Name: TABLE accounts_balances; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.accounts_balances IS 'Positive is debit';


--
-- TOC entry 295 (class 1259 OID 74707)
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
    account_no bigint NOT NULL
);


--
-- TOC entry 296 (class 1259 OID 74734)
-- Name: accounts_information_2_account_no_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.accounts_information_2_account_no_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5347 (class 0 OID 0)
-- Dependencies: 296
-- Name: accounts_information_2_account_no_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.accounts_information_2_account_no_seq OWNED BY public.accounts_information.account_no;


--
-- TOC entry 221 (class 1259 OID 16808)
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
    internal_reference text DEFAULT ''::text
);


--
-- TOC entry 285 (class 1259 OID 41865)
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
-- TOC entry 222 (class 1259 OID 16815)
-- Name: accounts_journal_information; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_journal_information (
    journal_id bigint NOT NULL,
    journal_name text NOT NULL,
    journal_i18n_label bigint
);


--
-- TOC entry 223 (class 1259 OID 16820)
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.accounts_journal_information_journal_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5348 (class 0 OID 0)
-- Dependencies: 223
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.accounts_journal_information_journal_id_seq OWNED BY public.accounts_journal_information.journal_id;


--
-- TOC entry 224 (class 1259 OID 16821)
-- Name: accounts_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_types (
    account_type integer NOT NULL,
    account_type_name text NOT NULL,
    account_type_i18n_label bigint
);


--
-- TOC entry 5349 (class 0 OID 0)
-- Dependencies: 224
-- Name: TABLE accounts_types; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.accounts_types IS 'Always these four _real_ accounts';


--
-- TOC entry 225 (class 1259 OID 16826)
-- Name: api_authorization; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.api_authorization (
    userid bigint NOT NULL,
    pubkey text,
    "authorization" text NOT NULL
);


--
-- TOC entry 226 (class 1259 OID 16831)
-- Name: authorized_terminals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.authorized_terminals (
    userid bigint NOT NULL,
    terminalid bigint NOT NULL
);


--
-- TOC entry 227 (class 1259 OID 16834)
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.authorized_terminals_terminalid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5350 (class 0 OID 0)
-- Dependencies: 227
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.authorized_terminals_terminalid_seq OWNED BY public.authorized_terminals.terminalid;


--
-- TOC entry 228 (class 1259 OID 16835)
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.authorized_terminals_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5351 (class 0 OID 0)
-- Dependencies: 228
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.authorized_terminals_userid_seq OWNED BY public.authorized_terminals.userid;


--
-- TOC entry 275 (class 1259 OID 41699)
-- Name: bundled_pricing; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.bundled_pricing (
    bundle_id bigint NOT NULL,
    itemcode bigint NOT NULL,
    discount double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 274 (class 1259 OID 41698)
-- Name: bundled_pricing_bundle_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.bundled_pricing_bundle_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5352 (class 0 OID 0)
-- Dependencies: 274
-- Name: bundled_pricing_bundle_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.bundled_pricing_bundle_id_seq OWNED BY public.bundled_pricing.bundle_id;


--
-- TOC entry 229 (class 1259 OID 16836)
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
    remarks text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 230 (class 1259 OID 16853)
-- Name: catalogue_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.catalogue_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5353 (class 0 OID 0)
-- Dependencies: 230
-- Name: catalogue_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.catalogue_itemcode_seq OWNED BY public.catalogue.itemcode;


--
-- TOC entry 231 (class 1259 OID 16854)
-- Name: categories_bitmask; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.categories_bitmask (
    bitmask bigint NOT NULL,
    name text NOT NULL,
    i18n_label bigint
);


--
-- TOC entry 292 (class 1259 OID 50142)
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
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 291 (class 1259 OID 50141)
-- Name: cheque_books_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.cheque_books_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5354 (class 0 OID 0)
-- Dependencies: 291
-- Name: cheque_books_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.cheque_books_id_seq OWNED BY public.cheque_books.id;


--
-- TOC entry 284 (class 1259 OID 41817)
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
-- TOC entry 283 (class 1259 OID 41810)
-- Name: codes_catalogue; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.codes_catalogue (
    code text NOT NULL,
    itemcode bigint NOT NULL,
    created_at time with time zone DEFAULT now() NOT NULL,
    enabled boolean DEFAULT true NOT NULL
);


--
-- TOC entry 232 (class 1259 OID 16859)
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
-- TOC entry 233 (class 1259 OID 16866)
-- Name: credentials_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.credentials_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5355 (class 0 OID 0)
-- Dependencies: 233
-- Name: credentials_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.credentials_userid_seq OWNED BY public.credentials.userid;


--
-- TOC entry 272 (class 1259 OID 41689)
-- Name: customer_discounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.customer_discounts (
    customer_id bigint NOT NULL,
    recommended_discount_percent double precision DEFAULT 0 NOT NULL,
    loyalty_rate double precision DEFAULT 0 NOT NULL,
    loyalty_paid_to_account_id double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 270 (class 1259 OID 41667)
-- Name: default_deny_fields; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.default_deny_fields (
    field text NOT NULL,
    created_at time with time zone DEFAULT now()
);


--
-- TOC entry 5356 (class 0 OID 0)
-- Dependencies: 270
-- Name: TABLE default_deny_fields; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.default_deny_fields IS 'Of the form [object].[field] or [field]';


--
-- TOC entry 234 (class 1259 OID 16867)
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
-- TOC entry 256 (class 1259 OID 25189)
-- Name: i18n_labels; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.i18n_labels (
    id bigint NOT NULL,
    lang text NOT NULL,
    value text
);


--
-- TOC entry 255 (class 1259 OID 25182)
-- Name: idempotency; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.idempotency (
    key text NOT NULL,
    request text,
    time_tai time with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 235 (class 1259 OID 16872)
-- Name: inventory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory (
    itemcode bigint NOT NULL,
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
    remarks text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 5357 (class 0 OID 0)
-- Dependencies: 235
-- Name: TABLE inventory; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.inventory IS 'Internal inventory management functions';


--
-- TOC entry 278 (class 1259 OID 41717)
-- Name: inventory_images; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_images (
    itemcode bigint NOT NULL,
    imageid bigint DEFAULT 0 NOT NULL,
    image_base64 text NOT NULL
);


--
-- TOC entry 236 (class 1259 OID 16885)
-- Name: inventory_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.inventory_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5358 (class 0 OID 0)
-- Dependencies: 236
-- Name: inventory_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.inventory_itemcode_seq OWNED BY public.inventory.itemcode;


--
-- TOC entry 286 (class 1259 OID 50074)
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
    to_units double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 262 (class 1259 OID 25225)
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
    currency_code text
);


--
-- TOC entry 5359 (class 0 OID 0)
-- Dependencies: 262
-- Name: TABLE issued_invoices; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.issued_invoices IS 'Issued invoices only';


--
-- TOC entry 261 (class 1259 OID 25224)
-- Name: issued_invoices_invoice_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.issued_invoices_invoice_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5360 (class 0 OID 0)
-- Dependencies: 261
-- Name: issued_invoices_invoice_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.issued_invoices_invoice_id_seq OWNED BY public.issued_invoices.invoice_id;


--
-- TOC entry 264 (class 1259 OID 25237)
-- Name: loyalty_points; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.loyalty_points (
    points_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    valid_from timestamp with time zone DEFAULT now() NOT NULL,
    valid_until timestamp with time zone NOT NULL,
    cust_id bigint NOT NULL
);


--
-- TOC entry 263 (class 1259 OID 25236)
-- Name: loyality_points_points_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.loyality_points_points_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5361 (class 0 OID 0)
-- Dependencies: 263
-- Name: loyality_points_points_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.loyality_points_points_id_seq OWNED BY public.loyalty_points.points_id;


--
-- TOC entry 266 (class 1259 OID 25245)
-- Name: loyalty_points_redemption; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.loyalty_points_redemption (
    redemption_id bigint NOT NULL,
    cust_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    amount double precision NOT NULL,
    time_issued time with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 265 (class 1259 OID 25244)
-- Name: loyalty_points_redemption_redemption_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.loyalty_points_redemption_redemption_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5362 (class 0 OID 0)
-- Dependencies: 265
-- Name: loyalty_points_redemption_redemption_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.loyalty_points_redemption_redemption_id_seq OWNED BY public.loyalty_points_redemption.redemption_id;


--
-- TOC entry 254 (class 1259 OID 17013)
-- Name: notification_servicer_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notification_servicer_types (
    notification_servicer_type_id bigint NOT NULL,
    notification_servicer_name text
);


--
-- TOC entry 253 (class 1259 OID 17012)
-- Name: notification_servicer_types_notification_servicer_type_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notification_servicer_types_notification_servicer_type_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5363 (class 0 OID 0)
-- Dependencies: 253
-- Name: notification_servicer_types_notification_servicer_type_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notification_servicer_types_notification_servicer_type_id_seq OWNED BY public.notification_servicer_types.notification_servicer_type_id;


--
-- TOC entry 252 (class 1259 OID 17003)
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
-- TOC entry 251 (class 1259 OID 17002)
-- Name: notification_types_notification_type_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notification_types_notification_type_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5364 (class 0 OID 0)
-- Dependencies: 251
-- Name: notification_types_notification_type_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notification_types_notification_type_id_seq OWNED BY public.notification_types.notification_type_id;


--
-- TOC entry 250 (class 1259 OID 16991)
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
-- TOC entry 249 (class 1259 OID 16990)
-- Name: notifications_notif_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notifications_notif_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5365 (class 0 OID 0)
-- Dependencies: 249
-- Name: notifications_notif_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notifications_notif_id_seq OWNED BY public.notifications.notif_id;


--
-- TOC entry 290 (class 1259 OID 50124)
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
-- TOC entry 289 (class 1259 OID 50123)
-- Name: payments_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.payments_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5366 (class 0 OID 0)
-- Dependencies: 289
-- Name: payments_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.payments_id_seq OWNED BY public.payments.id;


--
-- TOC entry 257 (class 1259 OID 25196)
-- Name: permissions_extended_api_call; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_extended_api_call (
    user_id bigint NOT NULL,
    api_call text NOT NULL,
    allowed_attributes text
);


--
-- TOC entry 237 (class 1259 OID 16886)
-- Name: permissions_list; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list (
    "Permission" text NOT NULL
);


--
-- TOC entry 5367 (class 0 OID 0)
-- Dependencies: 237
-- Name: TABLE permissions_list; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.permissions_list IS 'Comma-separated, no spaces';


--
-- TOC entry 238 (class 1259 OID 16891)
-- Name: permissions_list_categories_names; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list_categories_names (
    category bigint NOT NULL,
    category_name text,
    label_i18n bigint
);


--
-- TOC entry 239 (class 1259 OID 16896)
-- Name: permissions_list_users_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list_users_categories (
    userid bigint NOT NULL,
    categories bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 277 (class 1259 OID 41707)
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
    gender text DEFAULT 'unspecified'::text
);


--
-- TOC entry 276 (class 1259 OID 41706)
-- Name: pii_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.pii_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5368 (class 0 OID 0)
-- Dependencies: 276
-- Name: pii_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.pii_id_seq OWNED BY public.pii.id;


--
-- TOC entry 279 (class 1259 OID 41725)
-- Name: pii_images; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pii_images (
    pii_id bigint NOT NULL,
    image_no bigint DEFAULT 0 NOT NULL,
    image text
);


--
-- TOC entry 5369 (class 0 OID 0)
-- Dependencies: 279
-- Name: TABLE pii_images; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.pii_images IS 'Photos of people, companies - any person, including non-natural persons.';


--
-- TOC entry 282 (class 1259 OID 41787)
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
    expiry_date time with time zone NOT NULL,
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
    line_number bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 268 (class 1259 OID 25253)
-- Name: receipts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.receipts (
    receipt_id bigint NOT NULL,
    invoice_id bigint NOT NULL,
    account_id bigint NOT NULL,
    amount double precision NOT NULL,
    time_received time with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 267 (class 1259 OID 25252)
-- Name: receipts_receipt_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.receipts_receipt_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5370 (class 0 OID 0)
-- Dependencies: 267
-- Name: receipts_receipt_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.receipts_receipt_id_seq OWNED BY public.receipts.receipt_id;


--
-- TOC entry 281 (class 1259 OID 41770)
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
    total_amount_due double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 280 (class 1259 OID 41769)
-- Name: received_invoices_received_invoice_no_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.received_invoices_received_invoice_no_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5371 (class 0 OID 0)
-- Dependencies: 280
-- Name: received_invoices_received_invoice_no_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.received_invoices_received_invoice_no_seq OWNED BY public.received_invoices.received_invoice_no;


--
-- TOC entry 240 (class 1259 OID 16900)
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
    provided_privilege_levels text
);


--
-- TOC entry 258 (class 1259 OID 25204)
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
    provided_privilege_levels text
);


--
-- TOC entry 260 (class 1259 OID 25213)
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
    remarks text NOT NULL,
    client_recorded_time_opening timestamp with time zone DEFAULT now(),
    client_recorded_time_closing timestamp with time zone DEFAULT now(),
    sales_human_friendly text,
    loyality_points_percentage double precision DEFAULT 0 NOT NULL,
    loyality_points_issued double precision DEFAULT 0 NOT NULL,
    product_name text DEFAULT ''::text NOT NULL
);


--
-- TOC entry 5372 (class 0 OID 0)
-- Dependencies: 260
-- Name: TABLE sales; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.sales IS 'Sales data go here';


--
-- TOC entry 259 (class 1259 OID 25212)
-- Name: sales_sale_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.sales_sale_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5373 (class 0 OID 0)
-- Dependencies: 259
-- Name: sales_sale_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.sales_sale_id_seq OWNED BY public.sales.sale_id;


--
-- TOC entry 288 (class 1259 OID 50102)
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
-- TOC entry 287 (class 1259 OID 50101)
-- Name: scheduled_payments_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.scheduled_payments_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5374 (class 0 OID 0)
-- Dependencies: 287
-- Name: scheduled_payments_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.scheduled_payments_id_seq OWNED BY public.scheduled_payments.id;


--
-- TOC entry 293 (class 1259 OID 58288)
-- Name: scheduled_receipts_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.scheduled_receipts_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 294 (class 1259 OID 58289)
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
-- TOC entry 271 (class 1259 OID 41678)
-- Name: suggested_prices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.suggested_prices (
    itemcode bigint NOT NULL,
    price double precision NOT NULL
);


--
-- TOC entry 273 (class 1259 OID 41695)
-- Name: tiered_discounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tiered_discounts (
    itemcode bigint NOT NULL,
    min_qty double precision NOT NULL,
    discount_percentage double precision NOT NULL
);


--
-- TOC entry 241 (class 1259 OID 16906)
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
    categories_bitmask bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 242 (class 1259 OID 16915)
-- Name: user_authorization; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_authorization (
    userid bigint NOT NULL,
    user_cap text NOT NULL,
    user_default_cap text DEFAULT ''::text,
    check_extended_authorization boolean DEFAULT false NOT NULL
);


--
-- TOC entry 5375 (class 0 OID 0)
-- Dependencies: 242
-- Name: TABLE user_authorization; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.user_authorization IS 'user_cap: Comma-separated
user_default_cap: Comma-separated';


--
-- TOC entry 243 (class 1259 OID 16921)
-- Name: user_authorization_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.user_authorization_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5376 (class 0 OID 0)
-- Dependencies: 243
-- Name: user_authorization_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.user_authorization_userid_seq OWNED BY public.user_authorization.userid;


--
-- TOC entry 244 (class 1259 OID 16922)
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
-- TOC entry 269 (class 1259 OID 33453)
-- Name: users_field_level_access_controls_deny_list; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users_field_level_access_controls_deny_list (
    user_id bigint NOT NULL,
    denied_field text NOT NULL
);


--
-- TOC entry 245 (class 1259 OID 16927)
-- Name: users_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.users_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5377 (class 0 OID 0)
-- Dependencies: 245
-- Name: users_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.users_userid_seq OWNED BY public.users.userid;


--
-- TOC entry 246 (class 1259 OID 16928)
-- Name: vat_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.vat_categories (
    vat_category_id bigint NOT NULL,
    vat_percentage double precision NOT NULL,
    vat_name text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


--
-- TOC entry 247 (class 1259 OID 16934)
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.vat_categories_vat_category_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5378 (class 0 OID 0)
-- Dependencies: 247
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.vat_categories_vat_category_id_seq OWNED BY public.vat_categories.vat_category_id;


--
-- TOC entry 248 (class 1259 OID 16935)
-- Name: volume_discounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.volume_discounts (
    itemcode bigint NOT NULL,
    start_from bigint DEFAULT 1 NOT NULL,
    discount_per_unit double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 5093 (class 2604 OID 74735)
-- Name: accounts_information account_no; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information ALTER COLUMN account_no SET DEFAULT nextval('public.accounts_information_2_account_no_seq'::regclass);


--
-- TOC entry 4884 (class 2604 OID 16941)
-- Name: accounts_journal_information journal_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_information ALTER COLUMN journal_id SET DEFAULT nextval('public.accounts_journal_information_journal_id_seq'::regclass);


--
-- TOC entry 4885 (class 2604 OID 16942)
-- Name: authorized_terminals userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_terminals ALTER COLUMN userid SET DEFAULT nextval('public.authorized_terminals_userid_seq'::regclass);


--
-- TOC entry 4969 (class 2604 OID 41702)
-- Name: bundled_pricing bundle_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bundled_pricing ALTER COLUMN bundle_id SET DEFAULT nextval('public.bundled_pricing_bundle_id_seq'::regclass);


--
-- TOC entry 4886 (class 2604 OID 16943)
-- Name: catalogue itemcode; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue ALTER COLUMN itemcode SET DEFAULT nextval('public.catalogue_itemcode_seq'::regclass);


--
-- TOC entry 5064 (class 2604 OID 50145)
-- Name: cheque_books id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cheque_books ALTER COLUMN id SET DEFAULT nextval('public.cheque_books_id_seq'::regclass);


--
-- TOC entry 4909 (class 2604 OID 16944)
-- Name: credentials userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials ALTER COLUMN userid SET DEFAULT nextval('public.credentials_userid_seq'::regclass);


--
-- TOC entry 4912 (class 2604 OID 16945)
-- Name: inventory itemcode; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory ALTER COLUMN itemcode SET DEFAULT nextval('public.inventory_itemcode_seq'::regclass);


--
-- TOC entry 4955 (class 2604 OID 25228)
-- Name: issued_invoices invoice_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.issued_invoices ALTER COLUMN invoice_id SET DEFAULT nextval('public.issued_invoices_invoice_id_seq'::regclass);


--
-- TOC entry 4959 (class 2604 OID 25240)
-- Name: loyalty_points points_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points ALTER COLUMN points_id SET DEFAULT nextval('public.loyality_points_points_id_seq'::regclass);


--
-- TOC entry 4961 (class 2604 OID 25248)
-- Name: loyalty_points_redemption redemption_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points_redemption ALTER COLUMN redemption_id SET DEFAULT nextval('public.loyalty_points_redemption_redemption_id_seq'::regclass);


--
-- TOC entry 4945 (class 2604 OID 17016)
-- Name: notification_servicer_types notification_servicer_type_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_servicer_types ALTER COLUMN notification_servicer_type_id SET DEFAULT nextval('public.notification_servicer_types_notification_servicer_type_id_seq'::regclass);


--
-- TOC entry 4943 (class 2604 OID 17006)
-- Name: notification_types notification_type_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_types ALTER COLUMN notification_type_id SET DEFAULT nextval('public.notification_types_notification_type_id_seq'::regclass);


--
-- TOC entry 4937 (class 2604 OID 16994)
-- Name: notifications notif_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications ALTER COLUMN notif_id SET DEFAULT nextval('public.notifications_notif_id_seq'::regclass);


--
-- TOC entry 5057 (class 2604 OID 50127)
-- Name: payments id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments ALTER COLUMN id SET DEFAULT nextval('public.payments_id_seq'::regclass);


--
-- TOC entry 4971 (class 2604 OID 41710)
-- Name: pii id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pii ALTER COLUMN id SET DEFAULT nextval('public.pii_id_seq'::regclass);


--
-- TOC entry 4963 (class 2604 OID 25256)
-- Name: receipts receipt_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.receipts ALTER COLUMN receipt_id SET DEFAULT nextval('public.receipts_receipt_id_seq'::regclass);


--
-- TOC entry 4976 (class 2604 OID 41773)
-- Name: received_invoices received_invoice_no; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.received_invoices ALTER COLUMN received_invoice_no SET DEFAULT nextval('public.received_invoices_received_invoice_no_seq'::regclass);


--
-- TOC entry 4948 (class 2604 OID 25216)
-- Name: sales sale_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales ALTER COLUMN sale_id SET DEFAULT nextval('public.sales_sale_id_seq'::regclass);


--
-- TOC entry 5043 (class 2604 OID 50105)
-- Name: scheduled_payments id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.scheduled_payments ALTER COLUMN id SET DEFAULT nextval('public.scheduled_payments_id_seq'::regclass);


--
-- TOC entry 4929 (class 2604 OID 16946)
-- Name: user_authorization userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_authorization ALTER COLUMN userid SET DEFAULT nextval('public.user_authorization_userid_seq'::regclass);


--
-- TOC entry 4932 (class 2604 OID 16947)
-- Name: users userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users ALTER COLUMN userid SET DEFAULT nextval('public.users_userid_seq'::regclass);


--
-- TOC entry 4933 (class 2604 OID 16948)
-- Name: vat_categories vat_category_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.vat_categories ALTER COLUMN vat_category_id SET DEFAULT nextval('public.vat_categories_vat_category_id_seq'::regclass);


--
-- TOC entry 5097 (class 2606 OID 16950)
-- Name: sih_current sih_current_pkey; Type: CONSTRAINT; Schema: imported_dummy; Owner: -
--

ALTER TABLE ONLY imported_dummy.sih_current
    ADD CONSTRAINT sih_current_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 5095 (class 2606 OID 16952)
-- Name: sih sih_pkey; Type: CONSTRAINT; Schema: imported_dummy; Owner: -
--

ALTER TABLE ONLY imported_dummy.sih
    ADD CONSTRAINT sih_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 5099 (class 2606 OID 41874)
-- Name: accounts_balances accounts_balances_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_balances
    ADD CONSTRAINT accounts_balances_pkey PRIMARY KEY (account_type, account_no);


--
-- TOC entry 5192 (class 2606 OID 74743)
-- Name: accounts_information accounts_information_2_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information
    ADD CONSTRAINT accounts_information_2_pkey PRIMARY KEY (account_no);


--
-- TOC entry 5101 (class 2606 OID 41872)
-- Name: accounts_journal_entries accounts_journal_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_entries
    ADD CONSTRAINT accounts_journal_entries_pkey PRIMARY KEY (journal_univ_seq);


--
-- TOC entry 5103 (class 2606 OID 16958)
-- Name: accounts_journal_information accounts_journal_information_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_information
    ADD CONSTRAINT accounts_journal_information_pkey PRIMARY KEY (journal_id);


--
-- TOC entry 5105 (class 2606 OID 16960)
-- Name: accounts_types accounts_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_types
    ADD CONSTRAINT accounts_types_pkey PRIMARY KEY (account_type);


--
-- TOC entry 5107 (class 2606 OID 16962)
-- Name: api_authorization api_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.api_authorization
    ADD CONSTRAINT api_authorization_pkey PRIMARY KEY (userid, "authorization");


--
-- TOC entry 5161 (class 2606 OID 41705)
-- Name: bundled_pricing bundled_pricing_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bundled_pricing
    ADD CONSTRAINT bundled_pricing_pkey PRIMARY KEY (bundle_id);


--
-- TOC entry 5109 (class 2606 OID 16964)
-- Name: catalogue catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT catalogue_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 5113 (class 2606 OID 16966)
-- Name: categories_bitmask categories_bitmask_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categories_bitmask
    ADD CONSTRAINT categories_bitmask_pkey PRIMARY KEY (bitmask);


--
-- TOC entry 5185 (class 2606 OID 50151)
-- Name: cheque_books cheque_books_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cheque_books
    ADD CONSTRAINT cheque_books_pkey PRIMARY KEY (id);


--
-- TOC entry 5173 (class 2606 OID 41823)
-- Name: codes_batches codes_batches_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.codes_batches
    ADD CONSTRAINT codes_batches_pkey PRIMARY KEY (code, itemcode, batchcode);


--
-- TOC entry 5171 (class 2606 OID 41816)
-- Name: codes_catalogue codes_catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.codes_catalogue
    ADD CONSTRAINT codes_catalogue_pkey PRIMARY KEY (code, itemcode);


--
-- TOC entry 5115 (class 2606 OID 16968)
-- Name: credentials credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT credentials_pkey PRIMARY KEY (userid);


--
-- TOC entry 5159 (class 2606 OID 41694)
-- Name: customer_discounts customer_discounts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_discounts
    ADD CONSTRAINT customer_discounts_pkey PRIMARY KEY (customer_id);


--
-- TOC entry 5155 (class 2606 OID 41674)
-- Name: default_deny_fields default_deny_fields_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.default_deny_fields
    ADD CONSTRAINT default_deny_fields_pkey PRIMARY KEY (field);


--
-- TOC entry 5194 (class 2606 OID 74725)
-- Name: accounts_information human_friendly_id_2; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information
    ADD CONSTRAINT human_friendly_id_2 UNIQUE (human_friendly_id);


--
-- TOC entry 5141 (class 2606 OID 25195)
-- Name: i18n_labels i18n_labels_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.i18n_labels
    ADD CONSTRAINT i18n_labels_pkey PRIMARY KEY (id, lang);


--
-- TOC entry 5139 (class 2606 OID 25188)
-- Name: idempotency idempotency_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.idempotency
    ADD CONSTRAINT idempotency_pkey PRIMARY KEY (key);


--
-- TOC entry 5165 (class 2606 OID 41724)
-- Name: inventory_images inventory_images_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_images
    ADD CONSTRAINT inventory_images_pkey PRIMARY KEY (imageid, itemcode);


--
-- TOC entry 5119 (class 2606 OID 16970)
-- Name: inventory inventory_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_pkey PRIMARY KEY (itemcode, batchcode);


--
-- TOC entry 5147 (class 2606 OID 25233)
-- Name: issued_invoices issued_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.issued_invoices
    ADD CONSTRAINT issued_invoices_pkey PRIMARY KEY (invoice_id);


--
-- TOC entry 5149 (class 2606 OID 25243)
-- Name: loyalty_points loyality_points_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points
    ADD CONSTRAINT loyality_points_pkey PRIMARY KEY (points_id);


--
-- TOC entry 5151 (class 2606 OID 25251)
-- Name: loyalty_points_redemption loyalty_points_redemption_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.loyalty_points_redemption
    ADD CONSTRAINT loyalty_points_redemption_pkey PRIMARY KEY (redemption_id);


--
-- TOC entry 5137 (class 2606 OID 17020)
-- Name: notification_servicer_types notification_servicer_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_servicer_types
    ADD CONSTRAINT notification_servicer_types_pkey PRIMARY KEY (notification_servicer_type_id);


--
-- TOC entry 5135 (class 2606 OID 17011)
-- Name: notification_types notification_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_types
    ADD CONSTRAINT notification_types_pkey PRIMARY KEY (notification_type_id);


--
-- TOC entry 5133 (class 2606 OID 17000)
-- Name: notifications notifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_pkey PRIMARY KEY (notif_id);


--
-- TOC entry 5183 (class 2606 OID 50136)
-- Name: payments payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_pkey PRIMARY KEY (id);


--
-- TOC entry 5143 (class 2606 OID 25202)
-- Name: permissions_extended_api_call permissions_extended_api_call_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_extended_api_call
    ADD CONSTRAINT permissions_extended_api_call_pkey PRIMARY KEY (user_id, api_call);


--
-- TOC entry 5123 (class 2606 OID 16972)
-- Name: permissions_list_categories_names permissions_list_categories_names_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list_categories_names
    ADD CONSTRAINT permissions_list_categories_names_pkey PRIMARY KEY (category);


--
-- TOC entry 5121 (class 2606 OID 16974)
-- Name: permissions_list permissions_list_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list
    ADD CONSTRAINT permissions_list_pkey PRIMARY KEY ("Permission");


--
-- TOC entry 5125 (class 2606 OID 16976)
-- Name: permissions_list_users_categories permissions_list_users_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list_users_categories
    ADD CONSTRAINT permissions_list_users_categories_pkey PRIMARY KEY (userid);


--
-- TOC entry 5167 (class 2606 OID 41732)
-- Name: pii_images pii_images_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pii_images
    ADD CONSTRAINT pii_images_pkey PRIMARY KEY (pii_id, image_no);


--
-- TOC entry 5163 (class 2606 OID 41716)
-- Name: pii pii_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pii
    ADD CONSTRAINT pii_pkey PRIMARY KEY (id);


--
-- TOC entry 5153 (class 2606 OID 25259)
-- Name: receipts receipts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.receipts
    ADD CONSTRAINT receipts_pkey PRIMARY KEY (receipt_id);


--
-- TOC entry 5169 (class 2606 OID 41786)
-- Name: received_invoices received_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.received_invoices
    ADD CONSTRAINT received_invoices_pkey PRIMARY KEY (received_invoice_no);


--
-- TOC entry 5145 (class 2606 OID 25223)
-- Name: sales sales_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales
    ADD CONSTRAINT sales_pkey PRIMARY KEY (sale_id);


--
-- TOC entry 5178 (class 2606 OID 50119)
-- Name: scheduled_payments scheduled_payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.scheduled_payments
    ADD CONSTRAINT scheduled_payments_pkey PRIMARY KEY (id);


--
-- TOC entry 5190 (class 2606 OID 58306)
-- Name: scheduled_receipts scheduled_receipts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.scheduled_receipts
    ADD CONSTRAINT scheduled_receipts_pkey PRIMARY KEY (id);


--
-- TOC entry 5157 (class 2606 OID 41682)
-- Name: suggested_prices suggested_prices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.suggested_prices
    ADD CONSTRAINT suggested_prices_pkey PRIMARY KEY (itemcode, price);


--
-- TOC entry 5127 (class 2606 OID 16978)
-- Name: tokens tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT tokens_pkey PRIMARY KEY (tokenid);


--
-- TOC entry 5111 (class 2606 OID 16980)
-- Name: catalogue unique_desc; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT unique_desc UNIQUE (description);


--
-- TOC entry 5129 (class 2606 OID 16982)
-- Name: user_authorization user_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_authorization
    ADD CONSTRAINT user_authorization_pkey PRIMARY KEY (userid);


--
-- TOC entry 5117 (class 2606 OID 16984)
-- Name: credentials username_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT username_unique UNIQUE (username);


--
-- TOC entry 5131 (class 2606 OID 16986)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


--
-- TOC entry 5179 (class 1259 OID 50138)
-- Name: ix_payments_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_payments_date ON public.payments USING btree (payment_date);


--
-- TOC entry 5180 (class 1259 OID 50139)
-- Name: ix_payments_recon; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_payments_recon ON public.payments USING btree (company_id, bank_account_id, is_reconciled, is_excluded);


--
-- TOC entry 5181 (class 1259 OID 50137)
-- Name: ix_payments_scheduled; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_payments_scheduled ON public.payments USING btree (scheduled_payment_id);


--
-- TOC entry 5174 (class 1259 OID 50120)
-- Name: ix_scheduled_payments_next_run; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_payments_next_run ON public.scheduled_payments USING btree (next_run_date, is_pending);


--
-- TOC entry 5175 (class 1259 OID 50122)
-- Name: ix_scheduled_payments_recon; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_payments_recon ON public.scheduled_payments USING btree (company_id, bank_account_id, is_reconciled, is_excluded);


--
-- TOC entry 5176 (class 1259 OID 50121)
-- Name: ix_scheduled_payments_status_flags; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_payments_status_flags ON public.scheduled_payments USING btree (is_processing, is_completed, is_failed, is_cancelled);


--
-- TOC entry 5186 (class 1259 OID 58307)
-- Name: ix_scheduled_receipts_next_run; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_receipts_next_run ON public.scheduled_receipts USING btree (next_run_date, is_pending);


--
-- TOC entry 5187 (class 1259 OID 58308)
-- Name: ix_scheduled_receipts_recon; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_receipts_recon ON public.scheduled_receipts USING btree (company_id, bank_account_id, is_reconciled, is_excluded);


--
-- TOC entry 5188 (class 1259 OID 58309)
-- Name: ix_scheduled_receipts_status_flags; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_scheduled_receipts_status_flags ON public.scheduled_receipts USING btree (is_processing, is_completed, is_failed, is_cancelled);


-- Completed on 2025-09-18 13:51:05

--
-- PostgreSQL database dump complete
--

