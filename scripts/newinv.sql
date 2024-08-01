--
-- PostgreSQL database dump
--

-- Dumped from database version 16.3
-- Dumped by pg_dump version 16.3

-- Started on 2024-08-01 06:50:55

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

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 220 (class 1259 OID 26614)
-- Name: api_authorization; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.api_authorization (
    userid bigint NOT NULL,
    pubkey text,
    "authorization" text NOT NULL
);


ALTER TABLE public.api_authorization OWNER TO rishi;

--
-- TOC entry 219 (class 1259 OID 26609)
-- Name: authorized_terminals; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.authorized_terminals (
    userid bigint NOT NULL,
    terminalid bigint NOT NULL
);


ALTER TABLE public.authorized_terminals OWNER TO rishi;

--
-- TOC entry 218 (class 1259 OID 26608)
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
-- TOC entry 4876 (class 0 OID 0)
-- Dependencies: 218
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.authorized_terminals_terminalid_seq OWNED BY public.authorized_terminals.terminalid;


--
-- TOC entry 217 (class 1259 OID 26607)
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
-- TOC entry 4877 (class 0 OID 0)
-- Dependencies: 217
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.authorized_terminals_userid_seq OWNED BY public.authorized_terminals.userid;


--
-- TOC entry 229 (class 1259 OID 34879)
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
    active_web boolean DEFAULT false NOT NULL
);


ALTER TABLE public.catalogue OWNER TO rishi;

--
-- TOC entry 228 (class 1259 OID 34878)
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
-- TOC entry 4878 (class 0 OID 0)
-- Dependencies: 228
-- Name: catalogue_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.catalogue_itemcode_seq OWNED BY public.catalogue.itemcode;


--
-- TOC entry 216 (class 1259 OID 26599)
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
-- TOC entry 215 (class 1259 OID 26598)
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
-- TOC entry 4879 (class 0 OID 0)
-- Dependencies: 215
-- Name: credentials_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.credentials_userid_seq OWNED BY public.credentials.userid;


--
-- TOC entry 230 (class 1259 OID 34890)
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
-- TOC entry 227 (class 1259 OID 34864)
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
-- TOC entry 4880 (class 0 OID 0)
-- Dependencies: 227
-- Name: TABLE inventory; Type: COMMENT; Schema: public; Owner: rishi
--

COMMENT ON TABLE public.inventory IS 'Internal inventory management functions';


--
-- TOC entry 226 (class 1259 OID 34863)
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
-- TOC entry 4881 (class 0 OID 0)
-- Dependencies: 226
-- Name: inventory_itemcode_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.inventory_itemcode_seq OWNED BY public.inventory.itemcode;


--
-- TOC entry 221 (class 1259 OID 26621)
-- Name: tokens; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.tokens (
    userid bigint NOT NULL,
    tokenvalue text NOT NULL,
    tokensecret text NOT NULL,
    not_valid_after timestamp with time zone NOT NULL,
    tokenid text DEFAULT (random())::text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


ALTER TABLE public.tokens OWNER TO rishi;

--
-- TOC entry 225 (class 1259 OID 34833)
-- Name: user_authorization; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.user_authorization (
    userid bigint NOT NULL,
    user_cap text NOT NULL
);


ALTER TABLE public.user_authorization OWNER TO rishi;

--
-- TOC entry 224 (class 1259 OID 34832)
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
-- TOC entry 4882 (class 0 OID 0)
-- Dependencies: 224
-- Name: user_authorization_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.user_authorization_userid_seq OWNED BY public.user_authorization.userid;


--
-- TOC entry 223 (class 1259 OID 26632)
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
-- TOC entry 222 (class 1259 OID 26631)
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
-- TOC entry 4883 (class 0 OID 0)
-- Dependencies: 222
-- Name: users_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.users_userid_seq OWNED BY public.users.userid;


--
-- TOC entry 232 (class 1259 OID 43088)
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
-- TOC entry 231 (class 1259 OID 43087)
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
-- TOC entry 4884 (class 0 OID 0)
-- Dependencies: 231
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: rishi
--

ALTER SEQUENCE public.vat_categories_vat_category_id_seq OWNED BY public.vat_categories.vat_category_id;


--
-- TOC entry 233 (class 1259 OID 43099)
-- Name: volume_discounts; Type: TABLE; Schema: public; Owner: rishi
--

CREATE TABLE public.volume_discounts (
    itemcode bigint NOT NULL,
    start_from bigint DEFAULT 1 NOT NULL,
    discount_per_unit double precision DEFAULT 0 NOT NULL
);


ALTER TABLE public.volume_discounts OWNER TO rishi;

--
-- TOC entry 4684 (class 2604 OID 26612)
-- Name: authorized_terminals userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.authorized_terminals ALTER COLUMN userid SET DEFAULT nextval('public.authorized_terminals_userid_seq'::regclass);


--
-- TOC entry 4698 (class 2604 OID 34882)
-- Name: catalogue itemcode; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue ALTER COLUMN itemcode SET DEFAULT nextval('public.catalogue_itemcode_seq'::regclass);


--
-- TOC entry 4681 (class 2604 OID 26602)
-- Name: credentials userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials ALTER COLUMN userid SET DEFAULT nextval('public.credentials_userid_seq'::regclass);


--
-- TOC entry 4689 (class 2604 OID 34867)
-- Name: inventory itemcode; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.inventory ALTER COLUMN itemcode SET DEFAULT nextval('public.inventory_itemcode_seq'::regclass);


--
-- TOC entry 4688 (class 2604 OID 34836)
-- Name: user_authorization userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.user_authorization ALTER COLUMN userid SET DEFAULT nextval('public.user_authorization_userid_seq'::regclass);


--
-- TOC entry 4687 (class 2604 OID 26635)
-- Name: users userid; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.users ALTER COLUMN userid SET DEFAULT nextval('public.users_userid_seq'::regclass);


--
-- TOC entry 4708 (class 2604 OID 43091)
-- Name: vat_categories vat_category_id; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.vat_categories ALTER COLUMN vat_category_id SET DEFAULT nextval('public.vat_categories_vat_category_id_seq'::regclass);


--
-- TOC entry 4717 (class 2606 OID 26620)
-- Name: api_authorization api_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.api_authorization
    ADD CONSTRAINT api_authorization_pkey PRIMARY KEY (userid, "authorization");


--
-- TOC entry 4725 (class 2606 OID 34889)
-- Name: catalogue catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT catalogue_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 4713 (class 2606 OID 26606)
-- Name: credentials credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT credentials_pkey PRIMARY KEY (userid);


--
-- TOC entry 4723 (class 2606 OID 34877)
-- Name: inventory inventory_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_pkey PRIMARY KEY (itemcode, batchcode);


--
-- TOC entry 4719 (class 2606 OID 34852)
-- Name: tokens tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT tokens_pkey PRIMARY KEY (tokenid);


--
-- TOC entry 4727 (class 2606 OID 34912)
-- Name: catalogue unique_description; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT unique_description UNIQUE (description, description_web, description_pos);


--
-- TOC entry 4715 (class 2606 OID 34910)
-- Name: credentials username_unique; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT username_unique UNIQUE (username);


--
-- TOC entry 4721 (class 2606 OID 26639)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


-- Completed on 2024-08-01 06:50:55

--
-- PostgreSQL database dump complete
--

