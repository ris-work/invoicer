--
-- PostgreSQL database dump
--

-- Dumped from database version 17.2
-- Dumped by pg_dump version 17.2

-- Started on 2026-02-17 00:14:32

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
-- TOC entry 5038 (class 0 OID 74707)
-- Dependencies: 295
-- Data for Name: accounts_information; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.accounts_information (account_type, account_name, account_pii, account_i18n_label, account_min, account_max, human_friendly_id, allow_credit_on_pos, allow_debit_on_pos, is_bank, is_cash, is_reserve, is_reconcilable, is_inventory_tracked, is_default_cash_register, account_no, loyalty_base_multiplicative_points_percentage, account_surcharges_multiplicative_percentage, account_surcharges_additive_fee) FROM stdin;
0	SAMPLE ASSET ACCOUNT	\N	\N	-1000000000	1000000000	\N	f	f	f	f	f	f	f	f	0	0	0	0
1	SAMPLE LIABILITY ACCOUNT	\N	\N	-1000000000	1000000000	\N	f	f	f	f	f	f	f	f	1	0	0	0
2	SAMPLE INCOME	\N	\N	-1000000000	1000000000	\N	f	f	f	f	f	f	f	f	2	0	0	0
3	SAMPLE EXPENSE	\N	\N	-1000000000	1000000000	\N	f	f	f	f	f	f	f	f	3	0	0	0
\.


--
-- TOC entry 5034 (class 0 OID 16821)
-- Dependencies: 224
-- Data for Name: accounts_types; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.accounts_types (account_type, account_type_name, account_type_i18n_label) FROM stdin;
2	Income	\N
1	Liabilities	\N
0	Assets	\N
3	Expenses	\N
\.


--
-- TOC entry 5035 (class 0 OID 16886)
-- Dependencies: 237
-- Data for Name: permissions_list; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.permissions_list ("Permission") FROM stdin;
ALL
ANALYTICS
POS_BILL
ADD_ACCOUNTS
ADD_JOURNAL_ENTRY
EDIT_INVENTORY_PRICE
EDIT_INVENTORY_ITEM
BILL_BELOW_FLOOR
ADD_INVENTORY_ITEM
ACTIVATION_INVENTORY_ITEM
EDIT_BATCHES
POS_SALES_RETURN
PURCHASE
PURCHASE_RETURN
JOURNAL_REVERSE_ENTRY
REFRESH
VIEW_SERVER_TIME

\.


--
-- TOC entry 5041 (class 0 OID 132186)
-- Dependencies: 318
-- Data for Name: tags_implies; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.tags_implies (id, tag, implies, recorded_at) FROM stdin;
1	amoxicillin	antibiotics	2026-02-16 23:53:04.214516+05:30
2	ciprofloxacin	antibiotics	2026-02-16 23:53:04.214516+05:30
3	antibiotics	prescription_medicine	2026-02-16 23:53:04.214516+05:30
4	metformin	diabetes_medication	2026-02-16 23:53:04.214516+05:30
5	diabetes_medication	prescription_medicine	2026-02-16 23:53:04.214516+05:30
6	lisinopril	blood_pressure_medication	2026-02-16 23:53:04.214516+05:30
7	blood_pressure_medication	prescription_medicine	2026-02-16 23:53:04.214516+05:30
8	atorvastatin	cholesterol_medication	2026-02-16 23:53:04.214516+05:30
9	cholesterol_medication	prescription_medicine	2026-02-16 23:53:04.214516+05:30
10	prescription_medicine	pharmacy	2026-02-16 23:53:04.214516+05:30
11	ibuprofen	nsaid	2026-02-16 23:53:04.214516+05:30
12	naproxen	nsaid	2026-02-16 23:53:04.214516+05:30
13	aspirin	nsaid	2026-02-16 23:53:04.214516+05:30
14	nsaid	painkiller	2026-02-16 23:53:04.214516+05:30
15	acetaminophen	painkiller	2026-02-16 23:53:04.214516+05:30
16	painkiller	otc_medicine	2026-02-16 23:53:04.214516+05:30
17	cough_syrup	cold_remedy	2026-02-16 23:53:04.214516+05:30
18	decongestant	cold_remedy	2026-02-16 23:53:04.214516+05:30
19	cold_remedy	otc_medicine	2026-02-16 23:53:04.214516+05:30
20	loratadine	antihistamine	2026-02-16 23:53:04.214516+05:30
21	cetirizine	antihistamine	2026-02-16 23:53:04.214516+05:30
22	antihistamine	allergy_relief	2026-02-16 23:53:04.214516+05:30
23	allergy_relief	otc_medicine	2026-02-16 23:53:04.214516+05:30
24	otc_medicine	medicine	2026-02-16 23:53:04.214516+05:30
25	medicine	pharmacy	2026-02-16 23:53:04.214516+05:30
26	bandage	wound_care	2026-02-16 23:53:04.214516+05:30
27	gauze	wound_care	2026-02-16 23:53:04.214516+05:30
28	antiseptic	wound_care	2026-02-16 23:53:04.214516+05:30
29	wound_care	first_aid	2026-02-16 23:53:04.214516+05:30
30	thermometer	diagnostic_tool	2026-02-16 23:53:04.214516+05:30
31	blood_pressure_monitor	diagnostic_tool	2026-02-16 23:53:04.214516+05:30
32	diagnostic_tool	first_aid	2026-02-16 23:53:04.214516+05:30
33	first_aid	pharmacy	2026-02-16 23:53:04.214516+05:30
34	vitamin_c	supplement	2026-02-16 23:53:04.214516+05:30
35	vitamin_d	supplement	2026-02-16 23:53:04.214516+05:30
36	multivitamin	supplement	2026-02-16 23:53:04.214516+05:30
37	protein_powder	supplement	2026-02-16 23:53:04.214516+05:30
38	supplement	pharmacy	2026-02-16 23:53:04.214516+05:30
39	toothpaste	oral_hygiene	2026-02-16 23:53:04.214516+05:30
40	toothbrush	oral_hygiene	2026-02-16 23:53:04.214516+05:30
41	dental_floss	oral_hygiene	2026-02-16 23:53:04.214516+05:30
42	oral_hygiene	personal_care	2026-02-16 23:53:04.214516+05:30
43	shampoo	hair_care	2026-02-16 23:53:04.214516+05:30
44	conditioner	hair_care	2026-02-16 23:53:04.214516+05:30
45	hair_care	personal_care	2026-02-16 23:53:04.214516+05:30
46	soap	body_wash	2026-02-16 23:53:04.214516+05:30
47	body_wash	personal_care	2026-02-16 23:53:04.214516+05:30
48	deodorant	personal_care	2026-02-16 23:53:04.214516+05:30
49	sunscreen	skincare	2026-02-16 23:53:04.214516+05:30
50	moisturizer	skincare	2026-02-16 23:53:04.214516+05:30
51	skincare	personal_care	2026-02-16 23:53:04.214516+05:30
52	personal_care	supermarket	2026-02-16 23:53:04.214516+05:30
53	banana	tropical_fruit	2026-02-16 23:53:04.214516+05:30
54	mango	tropical_fruit	2026-02-16 23:53:04.214516+05:30
55	pineapple	tropical_fruit	2026-02-16 23:53:04.214516+05:30
56	tropical_fruit	fruit	2026-02-16 23:53:04.214516+05:30
57	apple	temperate_fruit	2026-02-16 23:53:04.214516+05:30
58	pear	temperate_fruit	2026-02-16 23:53:04.214516+05:30
59	temperate_fruit	fruit	2026-02-16 23:53:04.214516+05:30
60	orange	citrus_fruit	2026-02-16 23:53:04.214516+05:30
61	lemon	citrus_fruit	2026-02-16 23:53:04.214516+05:30
62	citrus_fruit	fruit	2026-02-16 23:53:04.214516+05:30
63	fruit	fresh_produce	2026-02-16 23:53:04.214516+05:30
64	lettuce	leafy_green	2026-02-16 23:53:04.214516+05:30
65	spinach	leafy_green	2026-02-16 23:53:04.214516+05:30
66	kale	leafy_green	2026-02-16 23:53:04.214516+05:30
67	leafy_green	vegetable	2026-02-16 23:53:04.214516+05:30
68	carrot	root_vegetable	2026-02-16 23:53:04.214516+05:30
69	potato	root_vegetable	2026-02-16 23:53:04.214516+05:30
70	onion	root_vegetable	2026-02-16 23:53:04.214516+05:30
71	root_vegetable	vegetable	2026-02-16 23:53:04.214516+05:30
72	tomato	nightshade	2026-02-16 23:53:04.214516+05:30
73	pepper	nightshade	2026-02-16 23:53:04.214516+05:30
74	nightshade	vegetable	2026-02-16 23:53:04.214516+05:30
75	vegetable	fresh_produce	2026-02-16 23:53:04.214516+05:30
76	fresh_produce	grocery	2026-02-16 23:53:04.214516+05:30
77	ground_beef	beef	2026-02-16 23:53:04.214516+05:30
78	steak	beef	2026-02-16 23:53:04.214516+05:30
79	beef	red_meat	2026-02-16 23:53:04.214516+05:30
80	pork_chop	pork	2026-02-16 23:53:04.214516+05:30
81	pork	red_meat	2026-02-16 23:53:04.214516+05:30
82	red_meat	meat	2026-02-16 23:53:04.214516+05:30
83	chicken_breast	poultry	2026-02-16 23:53:04.214516+05:30
84	turkey	poultry	2026-02-16 23:53:04.214516+05:30
85	poultry	meat	2026-02-16 23:53:04.214516+05:30
86	meat	butcher	2026-02-16 23:53:04.214516+05:30
87	salmon	fish	2026-02-16 23:53:04.214516+05:30
88	tuna	fish	2026-02-16 23:53:04.214516+05:30
89	fish	seafood	2026-02-16 23:53:04.214516+05:30
90	shrimp	shellfish	2026-02-16 23:53:04.214516+05:30
91	crab	shellfish	2026-02-16 23:53:04.214516+05:30
92	shellfish	seafood	2026-02-16 23:53:04.214516+05:30
93	seafood	butcher	2026-02-16 23:53:04.214516+05:30
94	butcher	supermarket	2026-02-16 23:53:04.214516+05:30
95	milk	dairy	2026-02-16 23:53:04.214516+05:30
96	cheese	dairy	2026-02-16 23:53:04.214516+05:30
97	yogurt	dairy	2026-02-16 23:53:04.214516+05:30
98	butter	dairy	2026-02-16 23:53:04.214516+05:30
99	cream	dairy	2026-02-16 23:53:04.214516+05:30
100	dairy	refrigerated	2026-02-16 23:53:04.214516+05:30
101	almond_milk	plant_based_milk	2026-02-16 23:53:04.214516+05:30
102	soy_milk	plant_based_milk	2026-02-16 23:53:04.214516+05:30
103	plant_based_milk	dairy_alternative	2026-02-16 23:53:04.214516+05:30
104	dairy_alternative	refrigerated	2026-02-16 23:53:04.214516+05:30
105	egg	refrigerated	2026-02-16 23:53:04.214516+05:30
106	juice	refrigerated	2026-02-16 23:53:04.214516+05:30
107	refrigerated	grocery	2026-02-16 23:53:04.214516+05:30
108	bread	baked_goods	2026-02-16 23:53:04.214516+05:30
109	rolls	baked_goods	2026-02-16 23:53:04.214516+05:30
110	bagel	baked_goods	2026-02-16 23:53:04.214516+05:30
111	croissant	baked_goods	2026-02-16 23:53:04.214516+05:30
112	cake	dessert	2026-02-16 23:53:04.214516+05:30
113	pastry	dessert	2026-02-16 23:53:04.214516+05:30
114	dessert	baked_goods	2026-02-16 23:53:04.214516+05:30
115	baked_goods	bakery	2026-02-16 23:53:04.214516+05:30
116	bakery	supermarket	2026-02-16 23:53:04.214516+05:30
117	pasta	dry_goods	2026-02-16 23:53:04.214516+05:30
118	rice	dry_goods	2026-02-16 23:53:04.214516+05:30
119	cereal	breakfast_food	2026-02-16 23:53:04.214516+05:30
120	oatmeal	breakfast_food	2026-02-16 23:53:04.214516+05:30
121	breakfast_food	dry_goods	2026-02-16 23:53:04.214516+05:30
122	canned_tomato	canned_goods	2026-02-16 23:53:04.214516+05:30
123	canned_beans	canned_goods	2026-02-16 23:53:04.214516+05:30
124	canned_goods	dry_goods	2026-02-16 23:53:04.214516+05:30
125	flour	baking_supply	2026-02-16 23:53:04.214516+05:30
126	sugar	baking_supply	2026-02-16 23:53:04.214516+05:30
127	yeast	baking_supply	2026-02-16 23:53:04.214516+05:30
128	baking_supply	dry_goods	2026-02-16 23:53:04.214516+05:30
129	dry_goods	grocery	2026-02-16 23:53:04.214516+05:30
130	soda	carbonated_drink	2026-02-16 23:53:04.214516+05:30
131	sparkling_water	carbonated_drink	2026-02-16 23:53:04.214516+05:30
132	carbonated_drink	beverage	2026-02-16 23:53:04.214516+05:30
133	coffee	hot_beverage	2026-02-16 23:53:04.214516+05:30
134	tea	hot_beverage	2026-02-16 23:53:04.214516+05:30
135	hot_beverage	beverage	2026-02-16 23:53:04.214516+05:30
136	bottled_water	beverage	2026-02-16 23:53:04.214516+05:30
137	sports_drink	beverage	2026-02-16 23:53:04.214516+05:30
138	beverage	grocery	2026-02-16 23:53:04.214516+05:30
139	chips	salty_snack	2026-02-16 23:53:04.214516+05:30
140	pretzels	salty_snack	2026-02-16 23:53:04.214516+05:30
141	salty_snack	snack_food	2026-02-16 23:53:04.214516+05:30
142	cookies	sweet_snack	2026-02-16 23:53:04.214516+05:30
143	candy	sweet_snack	2026-02-16 23:53:04.214516+05:30
144	chocolate	sweet_snack	2026-02-16 23:53:04.214516+05:30
145	sweet_snack	snack_food	2026-02-16 23:53:04.214516+05:30
146	nuts	snack_food	2026-02-16 23:53:04.214516+05:30
147	dried_fruit	snack_food	2026-02-16 23:53:04.214516+05:30
148	snack_food	grocery	2026-02-16 23:53:04.214516+05:30
149	frozen_pizza	frozen_meal	2026-02-16 23:53:04.214516+05:30
150	frozen_dinner	frozen_meal	2026-02-16 23:53:04.214516+05:30
151	frozen_meal	frozen_food	2026-02-16 23:53:04.214516+05:30
152	ice_cream	frozen_dessert	2026-02-16 23:53:04.214516+05:30
153	frozen_dessert	frozen_food	2026-02-16 23:53:04.214516+05:30
154	frozen_vegetable	frozen_food	2026-02-16 23:53:04.214516+05:30
155	frozen_food	grocery	2026-02-16 23:53:04.214516+05:30
156	laundry_detergent	laundry_supply	2026-02-16 23:53:04.214516+05:30
157	fabric_softener	laundry_supply	2026-02-16 23:53:04.214516+05:30
158	laundry_supply	cleaning_supply	2026-02-16 23:53:04.214516+05:30
159	dish_soap	dishwashing_supply	2026-02-16 23:53:04.214516+05:30
160	dishwashing_supply	cleaning_supply	2026-02-16 23:53:04.214516+05:30
161	all_purpose_cleaner	surface_cleaner	2026-02-16 23:53:04.214516+05:30
162	glass_cleaner	surface_cleaner	2026-02-16 23:53:04.214516+05:30
163	surface_cleaner	cleaning_supply	2026-02-16 23:53:04.214516+05:30
164	trash_bag	waste_management	2026-02-16 23:53:04.214516+05:30
165	waste_management	household_supply	2026-02-16 23:53:04.214516+05:30
166	cleaning_supply	household_supply	2026-02-16 23:53:04.214516+05:30
167	toilet_paper	paper_good	2026-02-16 23:53:04.214516+05:30
168	paper_towel	paper_good	2026-02-16 23:53:04.214516+05:30
169	tissue	paper_good	2026-02-16 23:53:04.214516+05:30
170	paper_good	household_supply	2026-02-16 23:53:04.214516+05:30
171	household_supply	supermarket	2026-02-16 23:53:04.214516+05:30
172	diapers	baby_hygiene	2026-02-16 23:53:04.214516+05:30
173	baby_wipes	baby_hygiene	2026-02-16 23:53:04.214516+05:30
174	baby_hygiene	baby_supply	2026-02-16 23:53:04.214516+05:30
175	baby_formula	baby_food	2026-02-16 23:53:04.214516+05:30
176	baby_food	baby_supply	2026-02-16 23:53:04.214516+05:30
177	baby_supply	supermarket	2026-02-16 23:53:04.214516+05:30
178	dog_food	pet_supply	2026-02-16 23:53:04.214516+05:30
179	cat_food	pet_supply	2026-02-16 23:53:04.214516+05:30
180	cat_litter	pet_supply	2026-02-16 23:53:04.214516+05:30
181	pet_supply	supermarket	2026-02-16 23:53:04.214516+05:30
182	grocery	supermarket	2026-02-16 23:53:04.214516+05:30
183	pharmacy	supermarket	2026-02-16 23:53:04.214516+05:30
184	cyclic_tag	cyclic_tag	2026-02-17 00:05:34.304668+05:30
185	cyclic_tag_referer	cyclic_tag	2026-02-17 00:05:34.304668+05:30
186	cyclic_tag_a	cyclic_tag_b	2026-02-17 00:05:34.304668+05:30
187	cyclic_tag_b	cyclic_tag_c	2026-02-17 00:05:34.304668+05:30
188	cyclic_tag_c	cyclic_tag_a	2026-02-17 00:05:34.304668+05:30
\.


--
-- TOC entry 5036 (class 0 OID 16928)
-- Dependencies: 246
-- Data for Name: vat_categories; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.vat_categories (vat_category_id, vat_percentage, vat_name, active) FROM stdin;
1	18	DEFAULT VAT	t
2	52	TOBACCO MONEY GO BRRR	t
3	40	ALCOHOL MONEY GO BRRR	t
5	12	FOOD 6% TAX	t
4	0	MEDS	t
\.


--
-- TOC entry 5047 (class 0 OID 0)
-- Dependencies: 296
-- Name: accounts_information_2_account_no_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.accounts_information_2_account_no_seq', 1, false);


--
-- TOC entry 5048 (class 0 OID 0)
-- Dependencies: 317
-- Name: tags_implies_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.tags_implies_id_seq', 188, true);


--
-- TOC entry 5049 (class 0 OID 0)
-- Dependencies: 247
-- Name: vat_categories_vat_category_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.vat_categories_vat_category_id_seq', 1, false);


-- Completed on 2026-02-17 00:14:32

--
-- PostgreSQL database dump complete
--

