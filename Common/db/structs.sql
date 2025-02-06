--
-- PostgreSQL database dump
--

-- Dumped from database version 17.2
-- Dumped by pg_dump version 17.2

-- Started on 2025-02-06 19:05:17

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
-- TOC entry 5 (class 2615 OID 16782)
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA public;


--
-- TOC entry 272 (class 1255 OID 16784)
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
-- TOC entry 273 (class 1255 OID 16785)
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


SET default_tablespace = '';

SET default_table_access_method = heap;

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
-- TOC entry 5028 (class 0 OID 0)
-- Dependencies: 220
-- Name: TABLE accounts_balances; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.accounts_balances IS 'Positive is debit';


--
-- TOC entry 221 (class 1259 OID 16801)
-- Name: accounts_information; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_information (
    account_type integer NOT NULL,
    account_no bigint NOT NULL,
    account_name text NOT NULL,
    account_pii bigint,
    account_i18n_label bigint,
    account_min double precision DEFAULT '-1000000000'::integer NOT NULL,
    account_max double precision DEFAULT 1000000000 NOT NULL,
    human_friendly_id text
);


--
-- TOC entry 222 (class 1259 OID 16808)
-- Name: accounts_journal_entries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_journal_entries (
    journal_univ_seq bigint NOT NULL,
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
    ref text
);


--
-- TOC entry 223 (class 1259 OID 16814)
-- Name: accounts_journal_entries_journal_univ_seq_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.accounts_journal_entries_journal_univ_seq_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5029 (class 0 OID 0)
-- Dependencies: 223
-- Name: accounts_journal_entries_journal_univ_seq_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.accounts_journal_entries_journal_univ_seq_seq OWNED BY public.accounts_journal_entries.journal_univ_seq;


--
-- TOC entry 224 (class 1259 OID 16815)
-- Name: accounts_journal_information; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_journal_information (
    journal_id bigint NOT NULL,
    journal_name text NOT NULL,
    journal_i18n_label bigint
);


--
-- TOC entry 225 (class 1259 OID 16820)
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.accounts_journal_information_journal_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5030 (class 0 OID 0)
-- Dependencies: 225
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.accounts_journal_information_journal_id_seq OWNED BY public.accounts_journal_information.journal_id;


--
-- TOC entry 226 (class 1259 OID 16821)
-- Name: accounts_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts_types (
    account_type integer NOT NULL,
    account_type_name text NOT NULL,
    account_type_i18n_label bigint
);


--
-- TOC entry 5031 (class 0 OID 0)
-- Dependencies: 226
-- Name: TABLE accounts_types; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.accounts_types IS 'Always these four _real_ accounts';


--
-- TOC entry 227 (class 1259 OID 16826)
-- Name: api_authorization; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.api_authorization (
    userid bigint NOT NULL,
    pubkey text,
    "authorization" text NOT NULL
);


--
-- TOC entry 228 (class 1259 OID 16831)
-- Name: authorized_terminals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.authorized_terminals (
    userid bigint NOT NULL,
    terminalid bigint NOT NULL
);


--
-- TOC entry 229 (class 1259 OID 16834)
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.authorized_terminals_terminalid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5032 (class 0 OID 0)
-- Dependencies: 229
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.authorized_terminals_terminalid_seq OWNED BY public.authorized_terminals.terminalid;


--
-- TOC entry 230 (class 1259 OID 16835)
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.authorized_terminals_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5033 (class 0 OID 0)
-- Dependencies: 230
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.authorized_terminals_userid_seq OWNED BY public.authorized_terminals.userid;


--
-- TOC entry 231 (class 1259 OID 16836)
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
    process_discounts boolean DEFAULT true NOT NULL
);


--
-- TOC entry 232 (class 1259 OID 16853)
-- Name: catalogue_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.catalogue_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5034 (class 0 OID 0)
-- Dependencies: 232
-- Name: catalogue_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.catalogue_itemcode_seq OWNED BY public.catalogue.itemcode;


--
-- TOC entry 233 (class 1259 OID 16854)
-- Name: categories_bitmask; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.categories_bitmask (
    bitmask bigint NOT NULL,
    name text NOT NULL,
    i18n_label bigint
);


--
-- TOC entry 234 (class 1259 OID 16859)
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
-- TOC entry 235 (class 1259 OID 16866)
-- Name: credentials_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.credentials_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5035 (class 0 OID 0)
-- Dependencies: 235
-- Name: credentials_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.credentials_userid_seq OWNED BY public.credentials.userid;


--
-- TOC entry 236 (class 1259 OID 16867)
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
-- TOC entry 258 (class 1259 OID 25189)
-- Name: i18n_labels; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.i18n_labels (
    id bigint NOT NULL,
    lang text NOT NULL,
    value text
);


--
-- TOC entry 257 (class 1259 OID 25182)
-- Name: idempotency; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.idempotency (
    key text NOT NULL,
    request text,
    time_tai time with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 237 (class 1259 OID 16872)
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
    user_discounts boolean DEFAULT false NOT NULL
);


--
-- TOC entry 5036 (class 0 OID 0)
-- Dependencies: 237
-- Name: TABLE inventory; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.inventory IS 'Internal inventory management functions';


--
-- TOC entry 238 (class 1259 OID 16885)
-- Name: inventory_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.inventory_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5037 (class 0 OID 0)
-- Dependencies: 238
-- Name: inventory_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.inventory_itemcode_seq OWNED BY public.inventory.itemcode;


--
-- TOC entry 256 (class 1259 OID 17013)
-- Name: notification_servicer_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notification_servicer_types (
    notification_servicer_type_id bigint NOT NULL,
    notification_servicer_name text
);


--
-- TOC entry 255 (class 1259 OID 17012)
-- Name: notification_servicer_types_notification_servicer_type_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notification_servicer_types_notification_servicer_type_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5038 (class 0 OID 0)
-- Dependencies: 255
-- Name: notification_servicer_types_notification_servicer_type_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notification_servicer_types_notification_servicer_type_id_seq OWNED BY public.notification_servicer_types.notification_servicer_type_id;


--
-- TOC entry 254 (class 1259 OID 17003)
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
-- TOC entry 253 (class 1259 OID 17002)
-- Name: notification_types_notification_type_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notification_types_notification_type_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5039 (class 0 OID 0)
-- Dependencies: 253
-- Name: notification_types_notification_type_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notification_types_notification_type_id_seq OWNED BY public.notification_types.notification_type_id;


--
-- TOC entry 252 (class 1259 OID 16991)
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
-- TOC entry 251 (class 1259 OID 16990)
-- Name: notifications_notif_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notifications_notif_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5040 (class 0 OID 0)
-- Dependencies: 251
-- Name: notifications_notif_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notifications_notif_id_seq OWNED BY public.notifications.notif_id;


--
-- TOC entry 259 (class 1259 OID 25196)
-- Name: permissions_extended_api_call; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_extended_api_call (
    user_id bigint NOT NULL,
    api_call text NOT NULL,
    allowed_attributes text
);


--
-- TOC entry 239 (class 1259 OID 16886)
-- Name: permissions_list; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list (
    "Permission" text NOT NULL
);


--
-- TOC entry 5041 (class 0 OID 0)
-- Dependencies: 239
-- Name: TABLE permissions_list; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.permissions_list IS 'Comma-separated, no spaces';


--
-- TOC entry 240 (class 1259 OID 16891)
-- Name: permissions_list_categories_names; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list_categories_names (
    category bigint NOT NULL,
    category_name text,
    label_i18n bigint
);


--
-- TOC entry 241 (class 1259 OID 16896)
-- Name: permissions_list_users_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions_list_users_categories (
    userid bigint NOT NULL,
    categories bigint DEFAULT 0 NOT NULL
);


--
-- TOC entry 242 (class 1259 OID 16900)
-- Name: requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.requests (
    time_tai time with time zone DEFAULT now() NOT NULL,
    principal bigint NOT NULL,
    token text NOT NULL,
    request_body text NOT NULL
);


--
-- TOC entry 260 (class 1259 OID 25204)
-- Name: requests_bad; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.requests_bad (
    time_tai timestamp with time zone DEFAULT now() NOT NULL,
    principal bigint,
    token text NOT NULL,
    request_body text
);


--
-- TOC entry 243 (class 1259 OID 16906)
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
-- TOC entry 244 (class 1259 OID 16915)
-- Name: user_authorization; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_authorization (
    userid bigint NOT NULL,
    user_cap text NOT NULL,
    user_default_cap text DEFAULT ''::text,
    check_extended_authorization boolean DEFAULT false NOT NULL
);


--
-- TOC entry 5042 (class 0 OID 0)
-- Dependencies: 244
-- Name: TABLE user_authorization; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.user_authorization IS 'user_cap: Comma-separated
user_default_cap: Comma-separated';


--
-- TOC entry 245 (class 1259 OID 16921)
-- Name: user_authorization_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.user_authorization_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5043 (class 0 OID 0)
-- Dependencies: 245
-- Name: user_authorization_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.user_authorization_userid_seq OWNED BY public.user_authorization.userid;


--
-- TOC entry 246 (class 1259 OID 16922)
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
-- TOC entry 247 (class 1259 OID 16927)
-- Name: users_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.users_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5044 (class 0 OID 0)
-- Dependencies: 247
-- Name: users_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.users_userid_seq OWNED BY public.users.userid;


--
-- TOC entry 248 (class 1259 OID 16928)
-- Name: vat_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.vat_categories (
    vat_category_id bigint NOT NULL,
    vat_percentage double precision NOT NULL,
    vat_name text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


--
-- TOC entry 249 (class 1259 OID 16934)
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.vat_categories_vat_category_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 5045 (class 0 OID 0)
-- Dependencies: 249
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.vat_categories_vat_category_id_seq OWNED BY public.vat_categories.vat_category_id;


--
-- TOC entry 250 (class 1259 OID 16935)
-- Name: volume_discounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.volume_discounts (
    itemcode bigint NOT NULL,
    start_from bigint DEFAULT 1 NOT NULL,
    discount_per_unit double precision DEFAULT 0 NOT NULL
);


--
-- TOC entry 4775 (class 2604 OID 16940)
-- Name: accounts_journal_entries journal_univ_seq; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_entries ALTER COLUMN journal_univ_seq SET DEFAULT nextval('public.accounts_journal_entries_journal_univ_seq_seq'::regclass);


--
-- TOC entry 4777 (class 2604 OID 16941)
-- Name: accounts_journal_information journal_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_information ALTER COLUMN journal_id SET DEFAULT nextval('public.accounts_journal_information_journal_id_seq'::regclass);


--
-- TOC entry 4778 (class 2604 OID 16942)
-- Name: authorized_terminals userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_terminals ALTER COLUMN userid SET DEFAULT nextval('public.authorized_terminals_userid_seq'::regclass);


--
-- TOC entry 4779 (class 2604 OID 16943)
-- Name: catalogue itemcode; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue ALTER COLUMN itemcode SET DEFAULT nextval('public.catalogue_itemcode_seq'::regclass);


--
-- TOC entry 4793 (class 2604 OID 16944)
-- Name: credentials userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials ALTER COLUMN userid SET DEFAULT nextval('public.credentials_userid_seq'::regclass);


--
-- TOC entry 4796 (class 2604 OID 16945)
-- Name: inventory itemcode; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory ALTER COLUMN itemcode SET DEFAULT nextval('public.inventory_itemcode_seq'::regclass);


--
-- TOC entry 4827 (class 2604 OID 17016)
-- Name: notification_servicer_types notification_servicer_type_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_servicer_types ALTER COLUMN notification_servicer_type_id SET DEFAULT nextval('public.notification_servicer_types_notification_servicer_type_id_seq'::regclass);


--
-- TOC entry 4825 (class 2604 OID 17006)
-- Name: notification_types notification_type_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_types ALTER COLUMN notification_type_id SET DEFAULT nextval('public.notification_types_notification_type_id_seq'::regclass);


--
-- TOC entry 4819 (class 2604 OID 16994)
-- Name: notifications notif_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications ALTER COLUMN notif_id SET DEFAULT nextval('public.notifications_notif_id_seq'::regclass);


--
-- TOC entry 4811 (class 2604 OID 16946)
-- Name: user_authorization userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_authorization ALTER COLUMN userid SET DEFAULT nextval('public.user_authorization_userid_seq'::regclass);


--
-- TOC entry 4814 (class 2604 OID 16947)
-- Name: users userid; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users ALTER COLUMN userid SET DEFAULT nextval('public.users_userid_seq'::regclass);


--
-- TOC entry 4815 (class 2604 OID 16948)
-- Name: vat_categories vat_category_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.vat_categories ALTER COLUMN vat_category_id SET DEFAULT nextval('public.vat_categories_vat_category_id_seq'::regclass);


--
-- TOC entry 4831 (class 2606 OID 16954)
-- Name: accounts_information accounts_information_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information
    ADD CONSTRAINT accounts_information_pkey PRIMARY KEY (account_type, account_no);


--
-- TOC entry 4835 (class 2606 OID 16956)
-- Name: accounts_journal_entries accounts_journal_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_entries
    ADD CONSTRAINT accounts_journal_entries_pkey PRIMARY KEY (journal_univ_seq);


--
-- TOC entry 4837 (class 2606 OID 16958)
-- Name: accounts_journal_information accounts_journal_information_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_journal_information
    ADD CONSTRAINT accounts_journal_information_pkey PRIMARY KEY (journal_id);


--
-- TOC entry 4839 (class 2606 OID 16960)
-- Name: accounts_types accounts_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_types
    ADD CONSTRAINT accounts_types_pkey PRIMARY KEY (account_type);


--
-- TOC entry 4841 (class 2606 OID 16962)
-- Name: api_authorization api_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.api_authorization
    ADD CONSTRAINT api_authorization_pkey PRIMARY KEY (userid, "authorization");


--
-- TOC entry 4843 (class 2606 OID 16964)
-- Name: catalogue catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT catalogue_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 4847 (class 2606 OID 16966)
-- Name: categories_bitmask categories_bitmask_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categories_bitmask
    ADD CONSTRAINT categories_bitmask_pkey PRIMARY KEY (bitmask);


--
-- TOC entry 4849 (class 2606 OID 16968)
-- Name: credentials credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT credentials_pkey PRIMARY KEY (userid);


--
-- TOC entry 4833 (class 2606 OID 16989)
-- Name: accounts_information human_friendly_id; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts_information
    ADD CONSTRAINT human_friendly_id UNIQUE (human_friendly_id);


--
-- TOC entry 4875 (class 2606 OID 25195)
-- Name: i18n_labels i18n_labels_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.i18n_labels
    ADD CONSTRAINT i18n_labels_pkey PRIMARY KEY (id, lang);


--
-- TOC entry 4873 (class 2606 OID 25188)
-- Name: idempotency idempotency_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.idempotency
    ADD CONSTRAINT idempotency_pkey PRIMARY KEY (key);


--
-- TOC entry 4853 (class 2606 OID 16970)
-- Name: inventory inventory_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_pkey PRIMARY KEY (itemcode, batchcode);


--
-- TOC entry 4871 (class 2606 OID 17020)
-- Name: notification_servicer_types notification_servicer_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_servicer_types
    ADD CONSTRAINT notification_servicer_types_pkey PRIMARY KEY (notification_servicer_type_id);


--
-- TOC entry 4869 (class 2606 OID 17011)
-- Name: notification_types notification_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notification_types
    ADD CONSTRAINT notification_types_pkey PRIMARY KEY (notification_type_id);


--
-- TOC entry 4867 (class 2606 OID 17000)
-- Name: notifications notifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_pkey PRIMARY KEY (notif_id);


--
-- TOC entry 4877 (class 2606 OID 25202)
-- Name: permissions_extended_api_call permissions_extended_api_call_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_extended_api_call
    ADD CONSTRAINT permissions_extended_api_call_pkey PRIMARY KEY (user_id, api_call);


--
-- TOC entry 4857 (class 2606 OID 16972)
-- Name: permissions_list_categories_names permissions_list_categories_names_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list_categories_names
    ADD CONSTRAINT permissions_list_categories_names_pkey PRIMARY KEY (category);


--
-- TOC entry 4855 (class 2606 OID 16974)
-- Name: permissions_list permissions_list_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list
    ADD CONSTRAINT permissions_list_pkey PRIMARY KEY ("Permission");


--
-- TOC entry 4859 (class 2606 OID 16976)
-- Name: permissions_list_users_categories permissions_list_users_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions_list_users_categories
    ADD CONSTRAINT permissions_list_users_categories_pkey PRIMARY KEY (userid);


--
-- TOC entry 4861 (class 2606 OID 16978)
-- Name: tokens tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT tokens_pkey PRIMARY KEY (tokenid);


--
-- TOC entry 4845 (class 2606 OID 16980)
-- Name: catalogue unique_desc; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT unique_desc UNIQUE (description);


--
-- TOC entry 4863 (class 2606 OID 16982)
-- Name: user_authorization user_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_authorization
    ADD CONSTRAINT user_authorization_pkey PRIMARY KEY (userid);


--
-- TOC entry 4851 (class 2606 OID 16984)
-- Name: credentials username_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT username_unique UNIQUE (username);


--
-- TOC entry 4865 (class 2606 OID 16986)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


-- Completed on 2025-02-06 19:05:17

--
-- PostgreSQL database dump complete
--

