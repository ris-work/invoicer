--
-- PostgreSQL database dump
--

-- Dumped from database version 16.3
-- Dumped by pg_dump version 16.3

-- Started on 2024-12-23 15:35:17

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 6 (class 2615 OID 51288)
-- Name: imported_dummy; Type: SCHEMA; Schema: -; Owner: rishi
--

CREATE SCHEMA imported_dummy;


ALTER SCHEMA imported_dummy OWNER TO rishi;

--
-- TOC entry 249 (class 1255 OID 59536)
-- Name: accounts_balances_version_force(); Type: FUNCTION; Schema: public; Owner: postgres
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


ALTER FUNCTION public.accounts_balances_version_force() OWNER TO postgres;

--
-- TOC entry 250 (class 1255 OID 59537)
-- Name: no_deletes(); Type: FUNCTION; Schema: public; Owner: postgres
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


ALTER FUNCTION public.no_deletes() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 236 (class 1259 OID 51299)
-- Name: sih; Type: TABLE; Schema: imported_dummy; Owner: rishi
--

CREATE TABLE imported_dummy.sih (
    itemcode bigint NOT NULL,
    "desc" text NOT NULL,
    sih real NOT NULL,
    cost real NOT NULL,
    sell real NOT NULL
);


ALTER TABLE imported_dummy.sih OWNER TO rishi;

--
-- TOC entry 235 (class 1259 OID 51289)
-- Name: sih_current; Type: TABLE; Schema: imported_dummy; Owner: rishi
--

CREATE TABLE imported_dummy.sih_current (
    itemcode integer NOT NULL,
    "desc" text,
    sih real,
    cost real,
    sell real
);


ALTER TABLE imported_dummy.sih_current OWNER TO rishi;

--
-- TOC entry 242 (class 1259 OID 59497)
-- Name: accounts_balances; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.accounts_balances (
    account_type integer NOT NULL,
    account_no bigint NOT NULL,
    amount double precision NOT NULL,
    time_tai timestamp with time zone DEFAULT now() NOT NULL,
    time_as_entered timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.accounts_balances OWNER TO postgres;

--
-- TOC entry 4969 (class 0 OID 0)
-- Dependencies: 242
-- Name: TABLE accounts_balances; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.accounts_balances IS 'Positive is debit';


--
-- TOC entry 237 (class 1259 OID 59471)
-- Name: accounts_information; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.accounts_information (
    account_type integer NOT NULL,
    account_no bigint NOT NULL,
    account_name text NOT NULL,
    account_pii bigint,
    account_i18n_label bigint,
    account_min double precision DEFAULT '-1000000000'::integer NOT NULL,
    account_max double precision DEFAULT 1000000000 NOT NULL
);


ALTER TABLE public.accounts_information OWNER TO postgres;

--
-- TOC entry 239 (class 1259 OID 59479)
-- Name: accounts_journal_entries; Type: TABLE; Schema: public; Owner: postgres
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
    time_as_entered timestamp with time zone NOT NULL
);


ALTER TABLE public.accounts_journal_entries OWNER TO postgres;

--
-- TOC entry 238 (class 1259 OID 59478)
-- Name: accounts_journal_entries_journal_univ_seq_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.accounts_journal_entries_journal_univ_seq_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.accounts_journal_entries_journal_univ_seq_seq OWNER TO postgres;

--
-- TOC entry 4971 (class 0 OID 0)
-- Dependencies: 238
-- Name: accounts_journal_entries_journal_univ_seq_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.accounts_journal_entries_journal_univ_seq_seq OWNED BY public.accounts_journal_entries.journal_univ_seq;


--
-- TOC entry 241 (class 1259 OID 59488)
-- Name: accounts_journal_information; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.accounts_journal_information (
    journal_id bigint NOT NULL,
    journal_name text NOT NULL,
    journal_i18n_label bigint
);


ALTER TABLE public.accounts_journal_information OWNER TO postgres;

--
-- TOC entry 240 (class 1259 OID 59487)
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.accounts_journal_information_journal_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.accounts_journal_information_journal_id_seq OWNER TO postgres;

--
-- TOC entry 4972 (class 0 OID 0)
-- Dependencies: 240
-- Name: accounts_journal_information_journal_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.accounts_journal_information_journal_id_seq OWNED BY public.accounts_journal_information.journal_id;


--
-- TOC entry 243 (class 1259 OID 59501)
-- Name: accounts_types; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.accounts_types (
    account_type integer NOT NULL,
    account_type_name text NOT NULL,
    account_type_i18n_label bigint
);


ALTER TABLE public.accounts_types OWNER TO postgres;

--
-- TOC entry 4973 (class 0 OID 0)
-- Dependencies: 243
-- Name: TABLE accounts_types; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.accounts_types IS 'Always these four _real_ accounts';


--
-- TOC entry 221 (class 1259 OID 26614)
-- Name: api_authorization; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.api_authorization (
    userid bigint NOT NULL,
    pubkey text,
    "authorization" text NOT NULL
);


ALTER TABLE public.api_authorization OWNER TO rishi;

--
-- TOC entry 220 (class 1259 OID 26609)
-- Name: authorized_terminals; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.authorized_terminals (
    userid bigint NOT NULL,
    terminalid bigint NOT NULL
);


ALTER TABLE public.authorized_terminals OWNER TO rishi;

--
-- TOC entry 219 (class 1259 OID 26608)
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.authorized_terminals_terminalid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.authorized_terminals_terminalid_seq OWNER TO rishi;

--
-- TOC entry 4974 (class 0 OID 0)
-- Dependencies: 219
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.authorized_terminals_terminalid_seq OWNED BY public.authorized_terminals.terminalid;


--
-- TOC entry 218 (class 1259 OID 26607)
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.authorized_terminals_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.authorized_terminals_userid_seq OWNER TO rishi;

--
-- TOC entry 4975 (class 0 OID 0)
-- Dependencies: 218
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.authorized_terminals_userid_seq OWNED BY public.authorized_terminals.userid;


--
-- TOC entry 230 (class 1259 OID 34879)
-- Name: catalogue; Type: TABLE; Schema: public; Owner: rishi
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
    white_hole boolean DEFAULT false NOT NULL,
    black_hole boolean DEFAULT false NOT NULL,
    never_discounted boolean DEFAULT false NOT NULL
);


ALTER TABLE public.catalogue OWNER TO rishi;

--
-- TOC entry 229 (class 1259 OID 34878)
-- Name: catalogue_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.catalogue_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.catalogue_itemcode_seq OWNER TO rishi;

--
-- TOC entry 4976 (class 0 OID 0)
-- Dependencies: 229
-- Name: catalogue_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.catalogue_itemcode_seq OWNED BY public.catalogue.itemcode;


--
-- TOC entry 248 (class 1259 OID 59568)
-- Name: categories_bitmask; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.categories_bitmask (
    bitmask bigint NOT NULL,
    name text NOT NULL,
    i18n_label bigint
);


ALTER TABLE public.categories_bitmask OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 26599)
-- Name: credentials; Type: TABLE; Schema: public; Owner: rishi
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


ALTER TABLE public.credentials OWNER TO rishi;

--
-- TOC entry 216 (class 1259 OID 26598)
-- Name: credentials_userid_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.credentials_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.credentials_userid_seq OWNER TO rishi;

--
-- TOC entry 4977 (class 0 OID 0)
-- Dependencies: 216
-- Name: credentials_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.credentials_userid_seq OWNED BY public.credentials.userid;


--
-- TOC entry 231 (class 1259 OID 34890)
-- Name: descriptions_other_languages; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.descriptions_other_languages (
    id bigint NOT NULL,
    language character varying(5) NOT NULL,
    description text NOT NULL,
    description_pos text NOT NULL,
    description_web text NOT NULL
);


ALTER TABLE public.descriptions_other_languages OWNER TO rishi;

--
-- TOC entry 228 (class 1259 OID 34864)
-- Name: inventory; Type: TABLE; Schema: public; Owner: rishi
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


ALTER TABLE public.inventory OWNER TO rishi;

--
-- TOC entry 4978 (class 0 OID 0)
-- Dependencies: 228
-- Name: TABLE inventory; Type: COMMENT; Schema: public; Owner: rishi
--

COMMENT ON TABLE public.inventory IS 'Internal inventory management functions';


--
-- TOC entry 227 (class 1259 OID 34863)
-- Name: inventory_itemcode_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.inventory_itemcode_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.inventory_itemcode_seq OWNER TO rishi;

--
-- TOC entry 4979 (class 0 OID 0)
-- Dependencies: 227
-- Name: inventory_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.inventory_itemcode_seq OWNED BY public.inventory.itemcode;


--
-- TOC entry 244 (class 1259 OID 59539)
-- Name: permissions_list; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.permissions_list (
    "Permission" text NOT NULL
);


ALTER TABLE public.permissions_list OWNER TO postgres;

--
-- TOC entry 4980 (class 0 OID 0)
-- Dependencies: 244
-- Name: TABLE permissions_list; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.permissions_list IS 'Comma-separated, no spaces';


--
-- TOC entry 246 (class 1259 OID 59555)
-- Name: permissions_list_categories_names; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.permissions_list_categories_names (
    category bigint NOT NULL,
    category_name text,
    label_i18n bigint
);


ALTER TABLE public.permissions_list_categories_names OWNER TO postgres;

--
-- TOC entry 247 (class 1259 OID 59562)
-- Name: permissions_list_users_categories; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.permissions_list_users_categories (
    userid bigint NOT NULL,
    categories bigint DEFAULT 0 NOT NULL
);


ALTER TABLE public.permissions_list_users_categories OWNER TO postgres;

--
-- TOC entry 245 (class 1259 OID 59548)
-- Name: requests; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.requests (
    time_tai time with time zone DEFAULT now() NOT NULL,
    principal bigint NOT NULL,
    token text NOT NULL,
    request_body text NOT NULL
);


ALTER TABLE public.requests OWNER TO postgres;

--
-- TOC entry 222 (class 1259 OID 26621)
-- Name: tokens; Type: TABLE; Schema: public; Owner: rishi
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


ALTER TABLE public.tokens OWNER TO rishi;

--
-- TOC entry 226 (class 1259 OID 34833)
-- Name: user_authorization; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.user_authorization (
    userid bigint NOT NULL,
    user_cap text NOT NULL,
    user_default_cap text DEFAULT ''::text
);


ALTER TABLE public.user_authorization OWNER TO rishi;

--
-- TOC entry 4984 (class 0 OID 0)
-- Dependencies: 226
-- Name: TABLE user_authorization; Type: COMMENT; Schema: public; Owner: rishi
--

COMMENT ON TABLE public.user_authorization IS 'user_cap: Comma-separated
user_default_cap: Comma-separated';


--
-- TOC entry 225 (class 1259 OID 34832)
-- Name: user_authorization_userid_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.user_authorization_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.user_authorization_userid_seq OWNER TO rishi;

--
-- TOC entry 4985 (class 0 OID 0)
-- Dependencies: 225
-- Name: user_authorization_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.user_authorization_userid_seq OWNED BY public.user_authorization.userid;


--
-- TOC entry 224 (class 1259 OID 26632)
-- Name: users; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.users (
    userid bigint NOT NULL,
    name text NOT NULL,
    address text,
    email text,
    phone text
);


ALTER TABLE public.users OWNER TO rishi;

--
-- TOC entry 223 (class 1259 OID 26631)
-- Name: users_userid_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.users_userid_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.users_userid_seq OWNER TO rishi;

--
-- TOC entry 4986 (class 0 OID 0)
-- Dependencies: 223
-- Name: users_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.users_userid_seq OWNED BY public.users.userid;


--
-- TOC entry 233 (class 1259 OID 43088)
-- Name: vat_categories; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.vat_categories (
    vat_category_id bigint NOT NULL,
    vat_percentage double precision NOT NULL,
    vat_name text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


ALTER TABLE public.vat_categories OWNER TO rishi;

--
-- TOC entry 232 (class 1259 OID 43087)
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE; Schema: public; Owner: rishi
--

CREATE SEQUENCE public.vat_categories_vat_category_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.vat_categories_vat_category_id_seq OWNER TO rishi;

--
-- TOC entry 4987 (class 0 OID 0)
-- Dependencies: 232
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.vat_categories_vat_category_id_seq OWNED BY public.vat_categories.vat_category_id;


--
-- TOC entry 234 (class 1259 OID 43099)
-- Name: volume_discounts; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.volume_discounts (
    itemcode bigint NOT NULL,
    start_from bigint DEFAULT 1 NOT NULL,
    discount_per_unit double precision DEFAULT 0 NOT NULL
);


ALTER TABLE public.volume_discounts OWNER TO rishi;

--
-- TOC entry 4776 (class 2604 OID 59482)
-- Name: accounts_journal_entries journal_univ_seq; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts_journal_entries ALTER COLUMN journal_univ_seq SET DEFAULT nextval('public.accounts_journal_entries_journal_univ_seq_seq'::regclass);


--
-- TOC entry 4778 (class 2604 OID 59491)
-- Name: accounts_journal_information journal_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts_journal_information ALTER COLUMN journal_id SET DEFAULT nextval('public.accounts_journal_information_journal_id_seq'::regclass);


--
-- TOC entry 4737 (class 2604 OID 26612)
-- Name: authorized_terminals userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.authorized_terminals ALTER COLUMN userid SET DEFAULT nextval('public.authorized_terminals_userid_seq'::regclass);


--
-- TOC entry 4754 (class 2604 OID 34882)
-- Name: catalogue itemcode; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue ALTER COLUMN itemcode SET DEFAULT nextval('public.catalogue_itemcode_seq'::regclass);


--
-- TOC entry 4734 (class 2604 OID 26602)
-- Name: credentials userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials ALTER COLUMN userid SET DEFAULT nextval('public.credentials_userid_seq'::regclass);


--
-- TOC entry 4745 (class 2604 OID 34867)
-- Name: inventory itemcode; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.inventory ALTER COLUMN itemcode SET DEFAULT nextval('public.inventory_itemcode_seq'::regclass);


--
-- TOC entry 4743 (class 2604 OID 34836)
-- Name: user_authorization userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.user_authorization ALTER COLUMN userid SET DEFAULT nextval('public.user_authorization_userid_seq'::regclass);


--
-- TOC entry 4742 (class 2604 OID 26635)
-- Name: users userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.users ALTER COLUMN userid SET DEFAULT nextval('public.users_userid_seq'::regclass);


--
-- TOC entry 4770 (class 2604 OID 43091)
-- Name: vat_categories vat_category_id; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.vat_categories ALTER COLUMN vat_category_id SET DEFAULT nextval('public.vat_categories_vat_category_id_seq'::regclass);


--
-- TOC entry 4802 (class 2606 OID 51295)
-- Name: sih_current sih_current_pkey; Type: CONSTRAINT; Schema: imported_dummy; Owner: rishi
--

ALTER TABLE ONLY imported_dummy.sih_current
    ADD CONSTRAINT sih_current_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 4804 (class 2606 OID 51305)
-- Name: sih sih_pkey; Type: CONSTRAINT; Schema: imported_dummy; Owner: rishi
--

ALTER TABLE ONLY imported_dummy.sih
    ADD CONSTRAINT sih_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 4806 (class 2606 OID 59477)
-- Name: accounts_information accounts_information_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts_information
    ADD CONSTRAINT accounts_information_pkey PRIMARY KEY (account_type, account_no);


--
-- TOC entry 4808 (class 2606 OID 59486)
-- Name: accounts_journal_entries accounts_journal_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts_journal_entries
    ADD CONSTRAINT accounts_journal_entries_pkey PRIMARY KEY (journal_univ_seq);


--
-- TOC entry 4810 (class 2606 OID 59495)
-- Name: accounts_journal_information accounts_journal_information_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts_journal_information
    ADD CONSTRAINT accounts_journal_information_pkey PRIMARY KEY (journal_id);


--
-- TOC entry 4812 (class 2606 OID 59507)
-- Name: accounts_types accounts_types_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.accounts_types
    ADD CONSTRAINT accounts_types_pkey PRIMARY KEY (account_type);


--
-- TOC entry 4788 (class 2606 OID 26620)
-- Name: api_authorization api_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.api_authorization
    ADD CONSTRAINT api_authorization_pkey PRIMARY KEY (userid, "authorization");


--
-- TOC entry 4798 (class 2606 OID 34889)
-- Name: catalogue catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT catalogue_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 4820 (class 2606 OID 59574)
-- Name: categories_bitmask categories_bitmask_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.categories_bitmask
    ADD CONSTRAINT categories_bitmask_pkey PRIMARY KEY (bitmask);


--
-- TOC entry 4784 (class 2606 OID 26606)
-- Name: credentials credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT credentials_pkey PRIMARY KEY (userid);


--
-- TOC entry 4796 (class 2606 OID 34877)
-- Name: inventory inventory_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_pkey PRIMARY KEY (itemcode, batchcode);


--
-- TOC entry 4816 (class 2606 OID 59561)
-- Name: permissions_list_categories_names permissions_list_categories_names_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permissions_list_categories_names
    ADD CONSTRAINT permissions_list_categories_names_pkey PRIMARY KEY (category);


--
-- TOC entry 4814 (class 2606 OID 59545)
-- Name: permissions_list permissions_list_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permissions_list
    ADD CONSTRAINT permissions_list_pkey PRIMARY KEY ("Permission");


--
-- TOC entry 4818 (class 2606 OID 59578)
-- Name: permissions_list_users_categories permissions_list_users_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permissions_list_users_categories
    ADD CONSTRAINT permissions_list_users_categories_pkey PRIMARY KEY (userid);


--
-- TOC entry 4790 (class 2606 OID 34852)
-- Name: tokens tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT tokens_pkey PRIMARY KEY (tokenid);


--
-- TOC entry 4800 (class 2606 OID 51298)
-- Name: catalogue unique_desc; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT unique_desc UNIQUE (description);


--
-- TOC entry 4794 (class 2606 OID 59567)
-- Name: user_authorization user_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.user_authorization
    ADD CONSTRAINT user_authorization_pkey PRIMARY KEY (userid);


--
-- TOC entry 4786 (class 2606 OID 34910)
-- Name: credentials username_unique; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT username_unique UNIQUE (username);


--
-- TOC entry 4792 (class 2606 OID 26639)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


--
-- TOC entry 4970 (class 0 OID 0)
-- Dependencies: 242
-- Name: TABLE accounts_balances; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.accounts_balances TO rishi;


--
-- TOC entry 4981 (class 0 OID 0)
-- Dependencies: 244
-- Name: TABLE permissions_list; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.permissions_list TO rishi;


--
-- TOC entry 4982 (class 0 OID 0)
-- Dependencies: 246
-- Name: TABLE permissions_list_categories_names; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.permissions_list_categories_names TO rishi;


--
-- TOC entry 4983 (class 0 OID 0)
-- Dependencies: 247
-- Name: TABLE permissions_list_users_categories; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.permissions_list_users_categories TO rishi;


-- Completed on 2024-12-23 15:35:17

--
-- PostgreSQL database dump complete
--

