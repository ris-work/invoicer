--
-- PostgreSQL database dump
--

-- Dumped from database version 16.3
-- Dumped by pg_dump version 16.3

-- Started on 2024-08-24 13:29:41

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
-- TOC entry 4896 (class 0 OID 0)
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
-- TOC entry 4897 (class 0 OID 0)
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
-- TOC entry 4898 (class 0 OID 0)
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
-- TOC entry 4899 (class 0 OID 0)
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
    user_discounts boolean DEFAULT false NOT NULL,
    maintain_expiry_date boolean DEFAULT false NOT NULL
);


ALTER TABLE public.inventory OWNER TO rishi;

--
-- TOC entry 4900 (class 0 OID 0)
-- Dependencies: 227
-- Name: TABLE inventory; Type: COMMENT; Schema: public; Owner: rishi
--

COMMENT ON TABLE public.inventory IS 'Internal inventory management functions';


--
-- TOC entry 4901 (class 0 OID 0)
-- Dependencies: 227
-- Name: COLUMN inventory.maintain_expiry_date; Type: COMMENT; Schema: public; Owner: rishi
--

COMMENT ON COLUMN public.inventory.maintain_expiry_date IS 'Whether to care about the expiry date';


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
-- TOC entry 4902 (class 0 OID 0)
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
-- TOC entry 4903 (class 0 OID 0)
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
-- TOC entry 4904 (class 0 OID 0)
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
-- TOC entry 4905 (class 0 OID 0)
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
-- TOC entry 4699 (class 2604 OID 34882)
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
-- TOC entry 4709 (class 2604 OID 43091)
-- Name: vat_categories vat_category_id; Type: DEFAULT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.vat_categories ALTER COLUMN vat_category_id SET DEFAULT nextval('public.vat_categories_vat_category_id_seq'::regclass);


--
-- TOC entry 4877 (class 0 OID 26614)
-- Dependencies: 220
-- Data for Name: api_authorization; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.api_authorization (userid, pubkey, "authorization") FROM stdin;
\.


--
-- TOC entry 4876 (class 0 OID 26609)
-- Dependencies: 219
-- Data for Name: authorized_terminals; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.authorized_terminals (userid, terminalid) FROM stdin;
\.


--
-- TOC entry 4886 (class 0 OID 34879)
-- Dependencies: 229
-- Data for Name: catalogue; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.catalogue (itemcode, description, active, created_on, description_pos, description_web, descriptions_other_languages, default_vat_category, vat_depends_on_user, vat_category_adjustable, price_manual, enforce_above_cost, active_web) FROM stdin;
\.


--
-- TOC entry 4873 (class 0 OID 26599)
-- Dependencies: 216
-- Data for Name: credentials; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.credentials (userid, username, valid_until, modified, pubkey, password_pbkdf2, created_time, active) FROM stdin;
5	5555	2024-07-24 07:35:21.733519+05:30	2024-07-24 07:35:21.733519+05:30	\N	aAdj2nz54WkoJa2eBR2YrLaGCKwJBXtWIlJPJpw4JdQ=	2024-07-24 07:26:33.002685+05:30	t
4	rrr	2024-07-24 07:26:21.794263+05:30	2024-07-24 07:26:21.794263+05:30	\N	1rss6LvqOAW5M0vuKJx4nKbGwBHjqp3MZwYRG8AIZ0Q=	2024-07-24 07:26:21.794263+05:30	t
6	666666	2024-07-24 07:35:27.49457+05:30	2024-07-24 07:35:27.49457+05:30	\N	t5riYikGXwyaiKVy6UHB2d+BSpgGUWz39o5Brd7Mwgc=	2024-07-24 07:29:18.673456+05:30	t
1	rishi	2024-07-26 16:54:04.891625+05:30	2024-07-26 16:54:04.891625+05:30	\N	38qHM1al5Yy8mZ2Xg/ZfoOWXmiG9AF1yrAW4vx7R+7U=	2024-07-24 06:06:18.586206+05:30	t
10	eee6+5654654	2024-07-24 07:35:10.718422+05:30	2024-07-24 07:35:10.718422+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:35:10.718422+05:30	f
2	eee	2024-07-24 06:16:04.288612+05:30	2024-07-24 06:16:04.288612+05:30	\N	MqEJaA04kUVl5V3U9kj2JUg89a52sriyP25NjPhLh6s=	2024-07-24 06:16:04.288612+05:30	f
3	aaaaeeee	2024-07-24 07:22:15.887189+05:30	2024-07-24 07:22:15.887189+05:30	\N	guYranbeqAM049/Luwmc7etnuXhZZQbB+FCpY4Go1JE=	2024-07-24 07:08:18.924279+05:30	f
7	r5	2024-07-24 07:30:44.603226+05:30	2024-07-24 07:30:44.603226+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:30:44.603226+05:30	f
9	dddd	2024-07-24 07:35:04.782032+05:30	2024-07-24 07:35:04.782032+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:35:04.782032+05:30	f
11	eeeeeeee	2024-07-24 07:37:30.678292+05:30	2024-07-24 07:37:30.678292+05:30	\N	38qHM1al5Yy8mZ2Xg/ZfoOWXmiG9AF1yrAW4vx7R+7U=	2024-07-24 07:37:30.678292+05:30	f
14	243243423	2024-07-24 07:39:48.356672+05:30	2024-07-24 07:39:48.356672+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:39:48.356672+05:30	f
16	rishikesh	2024-07-24 07:40:04.12393+05:30	2024-07-24 07:40:04.12393+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:40:04.12393+05:30	f
15	eeeeeeeeeee	2024-07-24 07:40:40.606825+05:30	2024-07-24 07:40:40.606825+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:39:53.716808+05:30	f
13	eeeeeeeeeeeeeeeeee	2024-07-24 07:40:49.786896+05:30	2024-07-24 07:40:49.786896+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:39:41.52785+05:30	f
8	eeeeeeeeee	2024-07-24 07:40:57.475087+05:30	2024-07-24 07:40:57.475087+05:30	\N	uziGe8OeArXcfv4tnDtG3XNjZpIAPTbJ8jQKYQjyq/A=	2024-07-24 07:31:27.831314+05:30	f
17	4234	2024-07-24 07:41:12.210839+05:30	2024-07-24 07:41:12.210839+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:41:12.210839+05:30	f
18	qweqweqweqwe	2024-07-24 07:42:24.49324+05:30	2024-07-24 07:42:24.49324+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:42:24.49324+05:30	f
19	qeweqwe	2024-07-24 07:42:33.443966+05:30	2024-07-24 07:42:33.443966+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:42:33.443966+05:30	f
20	rrrr	2024-07-24 07:43:09.354972+05:30	2024-07-24 07:43:09.354972+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:43:09.354972+05:30	f
22	rr	2024-07-24 07:47:02.964061+05:30	2024-07-24 07:47:02.964061+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:47:02.964061+05:30	f
23	tttt	2024-07-24 07:47:08.62423+05:30	2024-07-24 07:47:08.62423+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:47:08.62423+05:30	f
24	444	2024-07-24 07:47:13.768314+05:30	2024-07-24 07:47:13.768314+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:47:13.768314+05:30	f
12	445	2024-07-24 07:47:18.344535+05:30	2024-07-24 07:47:18.344535+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:37:39.49311+05:30	f
26	564564654	2024-07-24 07:58:08.514778+05:30	2024-07-24 07:58:08.514778+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:58:08.514778+05:30	f
28	werwerrwe	2024-07-24 08:00:31.843337+05:30	2024-07-24 08:00:31.843337+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:00:31.843337+05:30	f
29	354354354354	2024-07-24 08:01:44.076213+05:30	2024-07-24 08:01:44.076213+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:01:44.076213+05:30	f
30	354354354	2024-07-24 08:01:47.602542+05:30	2024-07-24 08:01:47.602542+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:01:47.602542+05:30	f
31	333	2024-07-24 08:03:57.937238+05:30	2024-07-24 08:03:57.937238+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:03:57.937238+05:30	f
33	333333333	2024-07-24 08:04:05.104147+05:30	2024-07-24 08:04:05.104147+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:04:05.104147+05:30	f
21	3333	2024-07-24 08:04:11.944493+05:30	2024-07-24 08:04:11.944493+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:43:17.458724+05:30	f
27	donky	2024-07-24 08:06:56.99485+05:30	2024-07-24 08:06:56.99485+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:00:25.816222+05:30	f
34	eeeeee	2024-07-24 08:07:08.830922+05:30	2024-07-24 08:07:08.830922+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:07:08.830922+05:30	f
32	eeeeeee	2024-07-24 08:07:20.5111+05:30	2024-07-24 08:07:20.5111+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:04:00.731512+05:30	f
25	qewqew13133ew	2024-07-24 08:07:27.567398+05:30	2024-07-24 08:07:27.567398+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 07:58:02.277207+05:30	f
35	32323	2024-07-24 08:07:53.038917+05:30	2024-07-24 08:07:53.038917+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:07:53.038917+05:30	f
36	311331	2024-07-24 08:07:55.69482+05:30	2024-07-24 08:07:55.69482+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:07:55.69482+05:30	f
71	Veenu	2024-07-24 19:01:08.286247+05:30	2024-07-24 19:01:08.286247+05:30	\N	POH16UHJ0yOBuJQ0dcBMySZxJLs8V2bV/8m/RpuAEXc=	2024-07-24 19:01:08.286247+05:30	t
37	eeeeee222	2024-07-24 08:12:25.461333+05:30	2024-07-24 08:12:25.461333+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:12:25.461333+05:30	f
39	eee8888	2024-07-24 08:24:51.623122+05:30	2024-07-24 08:24:51.623122+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:24:51.623122+05:30	f
38	444777	2024-07-24 08:24:48.576781+05:30	2024-07-24 08:24:48.576781+05:30	\N	9/BMZnHaZnWLvPLb7PvWCyO1bgo1pkyiWBg7fqLe5Dc=	2024-07-24 08:24:48.576781+05:30	f
\.


--
-- TOC entry 4887 (class 0 OID 34890)
-- Dependencies: 230
-- Data for Name: descriptions_other_languages; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.descriptions_other_languages (id, language, description, description_pos, description_web) FROM stdin;
\.


--
-- TOC entry 4884 (class 0 OID 34864)
-- Dependencies: 227
-- Data for Name: inventory; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.inventory (itemcode, batchcode, batch_enabled, mfg_date, exp_date, packed_size, units, measurement_unit, marked_price, selling_price, cost_price, volume_discounts, suppliercode, user_discounts, maintain_expiry_date) FROM stdin;
\.


--
-- TOC entry 4878 (class 0 OID 26621)
-- Dependencies: 221
-- Data for Name: tokens; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.tokens (userid, tokenvalue, tokensecret, not_valid_after, tokenid, active) FROM stdin;
1	eeeee	100000000000000	2024-11-03 00:00:00+05:30	0.004686511169526897	t
1	eeeeee	100000000000000	2024-11-03 00:00:00+05:30	0.34406780147532956	t
2	eeeeerrrr33333	100000000000000	2024-11-03 00:00:00+05:30	0.3329521047816868	f
2	eeeeerrrr33333	100000000000000	2024-11-03 00:00:00+05:30	0.36009396227860035	f
2	eeeeerrrr	100000000000000	2024-11-03 00:00:00+05:30	0.5524140637220545	f
0	kgl73+Qzg3Q=	42JV8plNDCc=	-infinity	4Cp1yqtWwRU=	f
0	Nbh2F17zSBk=	odI1HYkBTlA=	-infinity	8V7VOr2QmSE=	f
0	k5XVknxt2Pc=	LmkFhhOma8M=	-infinity	rCvNgyJ+P+E=	f
0	kptUjxR6wDM=	x6a03igWjpM=	-infinity	0jUoRChQM0Y=	f
0	efs0gM8pd/8=	X8D1yKCbXCA=	-infinity	ra2N22tb2ko=	f
0	Yf9zVss5sXs=	gI8eHwrspF0=	-infinity	2h9BSodXg8M=	f
0	WCB8yGwaSI8=	FUXP2rMSgNQ=	-infinity	RQFIl3N92iQ=	f
0	PBQnfXTGPkE=	N53xidt7UqE=	-infinity	OXiGvLW1Q24=	f
0	OVTrFofqJls=	sYwTf1VmwA4=	-infinity	AQK0X3eynhM=	f
0	lutTLTdDirw=	0uIQPvCv8QY=	-infinity	X062QOQAUJY=	f
0	47JyL1YfGpk=	ZX4vqICr1FU=	-infinity	Q3yjVLuz/6A=	f
0	lWkiEf6Hdt8=	q/FxacEKgwc=	-infinity	gO2Cyf2F+E8=	f
0	dg7sOJYm7u4=	YW5OrSNJJbs=	-infinity	z/tiDwMV6iM=	f
0	KMDDimPVZWM=	jDCXpliYTwc=	-infinity	6PJvwsOA8h0=	f
0	h/6AEX4z8ng=	EWTjK35igGU=	-infinity	C5E34QC6wTU=	f
\.


--
-- TOC entry 4882 (class 0 OID 34833)
-- Dependencies: 225
-- Data for Name: user_authorization; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.user_authorization (userid, user_cap) FROM stdin;
\.


--
-- TOC entry 4880 (class 0 OID 26632)
-- Dependencies: 223
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.users (userid, name, address, email, phone) FROM stdin;
\.


--
-- TOC entry 4889 (class 0 OID 43088)
-- Dependencies: 232
-- Data for Name: vat_categories; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.vat_categories (vat_category_id, vat_percentage, vat_name, active) FROM stdin;
\.


--
-- TOC entry 4890 (class 0 OID 43099)
-- Dependencies: 233
-- Data for Name: volume_discounts; Type: TABLE DATA; Schema: public; Owner: rishi
--

COPY public.volume_discounts (itemcode, start_from, discount_per_unit) FROM stdin;
\.


--
-- TOC entry 4906 (class 0 OID 0)
-- Dependencies: 218
-- Name: authorized_terminals_terminalid_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.authorized_terminals_terminalid_seq', 1, false);


--
-- TOC entry 4907 (class 0 OID 0)
-- Dependencies: 217
-- Name: authorized_terminals_userid_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.authorized_terminals_userid_seq', 1, false);


--
-- TOC entry 4908 (class 0 OID 0)
-- Dependencies: 228
-- Name: catalogue_itemcode_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.catalogue_itemcode_seq', 1, false);


--
-- TOC entry 4909 (class 0 OID 0)
-- Dependencies: 215
-- Name: credentials_userid_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.credentials_userid_seq', 71, true);


--
-- TOC entry 4910 (class 0 OID 0)
-- Dependencies: 226
-- Name: inventory_itemcode_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.inventory_itemcode_seq', 1, false);


--
-- TOC entry 4911 (class 0 OID 0)
-- Dependencies: 224
-- Name: user_authorization_userid_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.user_authorization_userid_seq', 1, false);


--
-- TOC entry 4912 (class 0 OID 0)
-- Dependencies: 222
-- Name: users_userid_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.users_userid_seq', 1, false);


--
-- TOC entry 4913 (class 0 OID 0)
-- Dependencies: 231
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE SET; Schema: public; Owner: rishi
--

SELECT pg_catalog.setval('public.vat_categories_vat_category_id_seq', 1, false);


--
-- TOC entry 4718 (class 2606 OID 26620)
-- Name: api_authorization api_authorization_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.api_authorization
    ADD CONSTRAINT api_authorization_pkey PRIMARY KEY (userid, "authorization");


--
-- TOC entry 4726 (class 2606 OID 34889)
-- Name: catalogue catalogue_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT catalogue_pkey PRIMARY KEY (itemcode);


--
-- TOC entry 4714 (class 2606 OID 26606)
-- Name: credentials credentials_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT credentials_pkey PRIMARY KEY (userid);


--
-- TOC entry 4724 (class 2606 OID 34877)
-- Name: inventory inventory_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_pkey PRIMARY KEY (itemcode, batchcode);


--
-- TOC entry 4720 (class 2606 OID 34852)
-- Name: tokens tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT tokens_pkey PRIMARY KEY (tokenid);


--
-- TOC entry 4728 (class 2606 OID 34912)
-- Name: catalogue unique_description; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.catalogue
    ADD CONSTRAINT unique_description UNIQUE (description, description_web, description_pos);


--
-- TOC entry 4716 (class 2606 OID 34910)
-- Name: credentials username_unique; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.credentials
    ADD CONSTRAINT username_unique UNIQUE (username);


--
-- TOC entry 4722 (class 2606 OID 26639)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: rishi
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


-- Completed on 2024-08-24 13:29:41

--
-- PostgreSQL database dump complete
--

